using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BocceManager.Services;
using BocceManager.UI.Theme;

namespace BocceManager.Panels;

public class UtilitiesPanel : UserControl
{
    private Button _btnBackup;
    private Button _btnPreview;
    private Button _btnRestore;
    private Button _btnOpenFolder;
    private Label  _lblStatus;

    public UtilitiesPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;
        BuildUI();
    }

    private void BuildUI()
    {
        var toolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = AppTheme.Surface,
            Padding = new Padding(12, 8, 12, 8)
        };

        _btnBackup     = MakeButton("Create Backup",        AppTheme.Accent);
        _btnPreview    = MakeButton("Preview Backup",       Color.FromArgb(59, 130, 246));
        _btnRestore    = MakeButton("Restore from Backup",  AppTheme.ButtonDanger);
        _btnOpenFolder = MakeButton("Open Backups Folder",  Color.FromArgb(100, 116, 139));

        _btnBackup.Click     += OnBackup;
        _btnPreview.Click    += OnPreview;
        _btnRestore.Click    += OnRestore;
        _btnOpenFolder.Click += OnOpenFolder;

        int x = 0;
        foreach (var btn in new[] { _btnBackup, _btnPreview, _btnRestore, _btnOpenFolder })
        {
            btn.Location = new Point(x, 8);
            toolbar.Controls.Add(btn);
            x += btn.Width + 8;
        }

        _lblStatus = new Label
        {
            Dock      = DockStyle.Fill,
            BackColor = AppTheme.ContentBackground,
            ForeColor = AppTheme.TextPrimary,
            Font      = AppTheme.FontDefault,
            Padding   = new Padding(12),
            AutoSize  = false,
            Text      = "Create a backup at any time.\nUse Preview to inspect a backup file before restoring it."
        };

        Controls.Add(_lblStatus);
        Controls.Add(toolbar);
    }

    private static Button MakeButton(string text, Color back) => new()
    {
        Text      = text,
        AutoSize  = true,
        Padding   = new Padding(12, 0, 12, 0),
        Height    = 36,
        FlatStyle = FlatStyle.Flat,
        BackColor = back,
        ForeColor = Color.White,
        Font      = AppTheme.FontButton,
        Cursor    = Cursors.Hand
    };

    // ── BACKUP ──────────────────────────────────────────────────────────────

    private void OnBackup(object sender, EventArgs e)
    {
        _btnBackup.Enabled = false;
        SetStatus("Creating backup...", AppTheme.TextPrimary);
        Application.DoEvents();

        try
        {
            var backupFile = BackupService.CreateBackup();
            var kb = new FileInfo(backupFile).Length / 1024;
            SetStatus(
                $"✓ Backup successful!\n\n" +
                $"File:  {Path.GetFileName(backupFile)}\n" +
                $"Size:  {kb:N0} KB\n" +
                $"Saved: {BackupService.GetBackupFolderPath()}",
                Color.DarkGreen);
        }
        catch (Exception ex)
        {
            SetStatus($"✗ Backup failed:\n{ex.Message}", Color.DarkRed);
        }
        finally
        {
            _btnBackup.Enabled = true;
        }
    }

    // ── PREVIEW ─────────────────────────────────────────────────────────────

    private void OnPreview(object sender, EventArgs e)
    {
        var file = PickBackupFile("Preview Backup");
        if (file == null) return;

        _btnPreview.Enabled = false;
        SetStatus("Reading backup...", AppTheme.TextPrimary);
        Application.DoEvents();

        try
        {
            var summary = BackupService.PreviewBackup(file);

            var sb = new StringBuilder();
            sb.AppendLine($"── Backup File ──────────────────────────────────────");
            sb.AppendLine($"File:     {summary.FileName}");
            sb.AppendLine($"Size:     {summary.FileSizeKb:N0} KB");
            sb.AppendLine($"Created:  {summary.FileDate:yyyy-MM-dd HH:mm:ss}");
            if (summary.DumpedOn.HasValue)
                sb.AppendLine($"Dumped:   {summary.DumpedOn.Value:yyyy-MM-dd HH:mm:ss}");
            if (!string.IsNullOrEmpty(summary.PostgresVersion))
                sb.AppendLine($"Source:   {summary.PostgresVersion}");

            sb.AppendLine();
            sb.AppendLine($"── Table Row Counts ─────────────────────────────────");

            // Show important tables first
            var priority = new[] { "Players", "Leagues", "Seasons", "Divisions", "Teams",
                                   "TeamPlayers", "LookingForTeams", "SpareLists",
                                   "Matches", "Games", "TeamStandings" };

            foreach (var t in priority.Where(summary.TableCounts.ContainsKey))
                sb.AppendLine($"  {t,-28} {summary.TableCounts[t],6:N0} rows");

            // Then remaining tables that have data
            foreach (var kv in summary.TableCounts
                         .Where(kv => !priority.Contains(kv.Key) && kv.Value > 0)
                         .OrderBy(kv => kv.Key))
                sb.AppendLine($"  {kv.Key,-28} {kv.Value,6:N0} rows");

            sb.AppendLine();
            sb.AppendLine($"Total tables with data: {summary.TableCounts.Count(kv => kv.Value > 0)}");

            SetStatus(sb.ToString(), AppTheme.TextPrimary);
        }
        catch (Exception ex)
        {
            SetStatus($"✗ Could not read backup:\n{ex.Message}", Color.DarkRed);
        }
        finally
        {
            _btnPreview.Enabled = true;
        }
    }

    // ── RESTORE ─────────────────────────────────────────────────────────────

    private void OnRestore(object sender, EventArgs e)
    {
        var file = PickBackupFile("Select Backup to Restore");
        if (file == null) return;

        // Show preview first, then confirm
        BackupSummary summary = null;
        try   { summary = BackupService.PreviewBackup(file); }
        catch { /* preview is best-effort */ }

        var confirmMsg = new StringBuilder();
        confirmMsg.AppendLine($"You are about to RESTORE the database from:");
        confirmMsg.AppendLine();
        confirmMsg.AppendLine($"  {Path.GetFileName(file)}");
        if (summary != null)
        {
            confirmMsg.AppendLine($"  Created:  {summary.FileDate:yyyy-MM-dd HH:mm}");
            confirmMsg.AppendLine($"  Size:     {summary.FileSizeKb:N0} KB");
            if (summary.TableCounts.TryGetValue("Players", out var playerCount))
                confirmMsg.AppendLine($"  Players:  {playerCount}");
        }
        confirmMsg.AppendLine();
        confirmMsg.AppendLine("⚠  This will REPLACE all current data. This cannot be undone.");
        confirmMsg.AppendLine();
        confirmMsg.AppendLine("Are you sure you want to continue?");

        if (MessageBox.Show(
                confirmMsg.ToString(),
                "Confirm Restore",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            return;

        _btnRestore.Enabled = false;
        SetStatus("Restoring database — please wait...", AppTheme.TextPrimary);
        Application.DoEvents();

        try
        {
            BackupService.RestoreBackup(file);
            SetStatus(
                $"✓ Restore complete!\n\n" +
                $"Database restored from: {Path.GetFileName(file)}\n\n" +
                "Please restart the application for all changes to take effect.",
                Color.DarkGreen);
        }
        catch (Exception ex)
        {
            SetStatus($"✗ Restore failed:\n{ex.Message}", Color.DarkRed);
        }
        finally
        {
            _btnRestore.Enabled = true;
        }
    }

    // ── OPEN FOLDER ─────────────────────────────────────────────────────────

    private void OnOpenFolder(object sender, EventArgs e)
    {
        try
        {
            var folder = BackupService.GetBackupFolderPath();
            Directory.CreateDirectory(folder);
            Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open folder:\n{ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ── HELPERS ─────────────────────────────────────────────────────────────

    private string PickBackupFile(string title)
    {
        var backupFolder = BackupService.GetBackupFolderPath();
        if (!Directory.Exists(backupFolder) || !Directory.GetFiles(backupFolder, "*.sql").Any())
        {
            MessageBox.Show("No backup files found. Create a backup first.",
                "No Backups", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return null;
        }

        var ofd = new OpenFileDialog
        {
            Title          = title,
            InitialDirectory = backupFolder,
            Filter         = "SQL Backup Files (*.sql)|*.sql|All Files (*.*)|*.*",
            DefaultExt     = "sql"
        };

        return ofd.ShowDialog() == DialogResult.OK ? ofd.FileName : null;
    }

    private void SetStatus(string text, Color color)
    {
        _lblStatus.ForeColor = color;
        _lblStatus.Text      = text;
    }
}

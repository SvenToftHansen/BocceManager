using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BocceManager.Data;
using BocceManager.Services;
using BocceManager.UI.Theme;

namespace BocceManager.Panels;

public class UtilitiesPanel : UserControl
{
    private Button      _btnBackup;
    private Button      _btnPreview;
    private Button      _btnRestore;
    private Button      _btnOpenFolder;
    private RichTextBox _log;
    private ProgressBar _progressBar;
    private Stopwatch   _stopwatch = new();

    // Ideas tab
    private TextBox     _txtNewIdea = null!;
    private ListBox     _lstIdeas = null!;
    private Button      _btnAddIdea = null!;
    private Button      _btnDeleteIdea = null!;
    private Button      _btnMarkCollected = null!;

    public UtilitiesPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;
        BuildUI();
    }

    private void BuildUI()
    {
        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.FontDefault,
            Padding = new Point(12, 6)
        };

        // Backup/Restore tab
        var backupPage = new TabPage("  Backup & Restore  ");
        BuildBackupTab(backupPage);
        tabs.TabPages.Add(backupPage);

        // Ideas tab
        var ideasPage = new TabPage("  Ideas  ");
        BuildIdeasTab(ideasPage);
        tabs.TabPages.Add(ideasPage);

        Controls.Add(tabs);
    }

    private void BuildBackupTab(TabPage page)
    {
        var toolbar = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 60,
            BackColor = AppTheme.Surface,
            Padding   = new Padding(12, 8, 12, 8)
        };

        _btnBackup     = MakeButton("Create Backup",       AppTheme.Accent);
        _btnPreview    = MakeButton("Preview Backup",      Color.FromArgb(59, 130, 246));
        _btnRestore    = MakeButton("Restore from Backup", AppTheme.ButtonDanger);
        _btnOpenFolder = MakeButton("Open Backups Folder", Color.FromArgb(100, 116, 139));

        _btnBackup.Click     += OnBackup;
        _btnPreview.Click    += OnPreview;
        _btnRestore.Click    += async (s, e) => await OnRestoreAsync();
        _btnOpenFolder.Click += OnOpenFolder;

        int x = 0;
        foreach (var btn in new[] { _btnBackup, _btnPreview, _btnRestore, _btnOpenFolder })
        {
            btn.Location = new Point(x, 8);
            toolbar.Controls.Add(btn);
            x += btn.Width + 8;
        }

        _progressBar = new ProgressBar
        {
            Dock                  = DockStyle.Top,
            Height                = 6,
            Style                 = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 30,
            Visible               = false
        };

        _log = new RichTextBox
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.FromArgb(15, 23, 42),
            ForeColor = Color.FromArgb(203, 213, 225),
            Font      = new System.Drawing.Font("Consolas", 10f),
            ReadOnly  = true,
            BorderStyle = BorderStyle.None,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            Padding   = new Padding(8)
        };

        LogLine("Ready.", Color.FromArgb(100, 116, 139));
        LogLine("Tip: close pgAdmin and any other database tools before restoring.",
                Color.FromArgb(234, 179, 8));

        page.Controls.Add(_log);
        page.Controls.Add(_progressBar);
        page.Controls.Add(toolbar);
    }

    private void BuildIdeasTab(TabPage page)
    {
        var toolbar = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 80,
            BackColor = AppTheme.Surface,
            Padding   = new Padding(8)
        };

        var lblTitle = new Label
        {
            Text      = "Capture ideas to develop later",
            Font      = AppTheme.FontSmallBold,
            ForeColor = AppTheme.TextPrimary,
            Dock      = DockStyle.Top,
            Height    = 24,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _txtNewIdea = new TextBox
        {
            Dock        = DockStyle.Top,
            Font        = AppTheme.FontDefault,
            BackColor   = AppTheme.ContentBackground,
            ForeColor   = AppTheme.TextPrimary,
            Height      = 28,
            Multiline   = false,
            BorderStyle = BorderStyle.FixedSingle,
            Margin      = new Padding(0, 4, 0, 4)
        };

        var btnPanel = new Panel { Dock = DockStyle.Top, Height = 36, Padding = new Padding(0, 4, 0, 0) };
        _btnAddIdea = MakeButton("Add Idea", AppTheme.ButtonSuccess);
        _btnAddIdea.Click += OnAddIdea;
        btnPanel.Controls.Add(_btnAddIdea);

        toolbar.Controls.Add(btnPanel);
        toolbar.Controls.Add(_txtNewIdea);
        toolbar.Controls.Add(lblTitle);

        var splitContainer = new SplitContainer
        {
            Dock        = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            Panel1MinSize = 300,
            Panel2MinSize = 200
        };

        // Left panel: list of ideas
        _lstIdeas = new ListBox
        {
            Dock            = DockStyle.Fill,
            Font            = AppTheme.FontDefault,
            BackColor       = AppTheme.ContentBackground,
            ForeColor       = AppTheme.TextPrimary,
            BorderStyle     = BorderStyle.None,
            IntegralHeight  = false
        };
        _lstIdeas.SelectedIndexChanged += OnIdeasSelected;
        splitContainer.Panel1.Controls.Add(_lstIdeas);

        // Right panel: buttons
        var btnPanel2 = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = AppTheme.Surface,
            Padding   = new Padding(8)
        };

        _btnMarkCollected = MakeButton("Mark Collected", Color.FromArgb(34, 197, 94));
        _btnDeleteIdea    = MakeButton("Delete Idea",   AppTheme.ButtonDanger);

        _btnMarkCollected.Click += OnMarkCollected;
        _btnDeleteIdea.Click    += OnDeleteIdea;

        _btnMarkCollected.Dock = DockStyle.Top;
        _btnMarkCollected.Height = 40;
        _btnMarkCollected.Margin = new Padding(0, 0, 0, 8);

        _btnDeleteIdea.Dock = DockStyle.Top;
        _btnDeleteIdea.Height = 40;

        btnPanel2.Controls.Add(_btnDeleteIdea);
        btnPanel2.Controls.Add(_btnMarkCollected);
        splitContainer.Panel2.Controls.Add(btnPanel2);

        page.Controls.Add(splitContainer);
        page.Controls.Add(toolbar);

        LoadIdeas();
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

    // ── LOG HELPERS ─────────────────────────────────────────────────────────

    private void LogLine(string text, Color? color = null)
    {
        var ts    = DateTime.Now.ToString("HH:mm:ss");
        var line  = $"[{ts}]  {text}\n";
        var col   = color ?? Color.FromArgb(203, 213, 225);

        _log.SelectionStart  = _log.TextLength;
        _log.SelectionLength = 0;
        _log.SelectionColor  = col;
        _log.AppendText(line);
        _log.ScrollToCaret();
    }

    private void LogElapsed(string label, Color? color = null)
    {
        LogLine($"{label}  (+{_stopwatch.Elapsed.TotalSeconds:0.0}s)", color);
    }

    private void LogSeparator() => LogLine("─────────────────────────────────────────────",
                                            Color.FromArgb(51, 65, 85));

    // ── BACKUP ──────────────────────────────────────────────────────────────

    private void OnBackup(object sender, EventArgs e)
    {
        SetAllButtons(false);
        LogSeparator();
        LogLine("Starting backup...");
        _stopwatch.Restart();
        Application.DoEvents();

        try
        {
            var backupFile = BackupService.CreateBackup();
            var kb = new FileInfo(backupFile).Length / 1024;
            LogElapsed($"✓ Backup complete — {Path.GetFileName(backupFile)}  ({kb:N0} KB)",
                        Color.FromArgb(34, 197, 94));
            LogLine($"  Saved to: {BackupService.GetBackupFolderPath()}",
                    Color.FromArgb(100, 116, 139));
        }
        catch (Exception ex)
        {
            LogElapsed($"✗ Backup failed:", Color.FromArgb(239, 68, 68));
            LogLine($"  {ex.Message}", Color.FromArgb(239, 68, 68));
        }
        finally
        {
            _stopwatch.Stop();
            SetAllButtons(true);
        }
    }

    // ── PREVIEW ─────────────────────────────────────────────────────────────

    private void OnPreview(object sender, EventArgs e)
    {
        var file = PickBackupFile("Preview Backup");
        if (file == null) return;

        SetAllButtons(false);
        LogSeparator();
        LogLine($"Previewing: {Path.GetFileName(file)}");
        Application.DoEvents();

        try
        {
            var summary = BackupService.PreviewBackup(file);

            LogLine($"  File:     {summary.FileName}",    Color.FromArgb(148, 163, 184));
            LogLine($"  Size:     {summary.FileSizeKb:N0} KB",  Color.FromArgb(148, 163, 184));
            LogLine($"  Created:  {summary.FileDate:yyyy-MM-dd HH:mm:ss}", Color.FromArgb(148, 163, 184));
            if (summary.DumpedOn.HasValue)
                LogLine($"  Dumped:   {summary.DumpedOn.Value:yyyy-MM-dd HH:mm:ss}", Color.FromArgb(148, 163, 184));

            LogLine("  Table row counts:", Color.FromArgb(148, 163, 184));

            var priority = new[] { "Players", "Leagues", "Seasons", "Divisions", "Teams",
                                   "TeamPlayers", "LookingForTeams", "SpareLists",
                                   "Matches", "Games", "TeamStandings" };
            foreach (var t in priority.Where(summary.TableCounts.ContainsKey))
                LogLine($"    {t,-28} {summary.TableCounts[t],5:N0} rows", Color.FromArgb(99, 202, 183));

            foreach (var kv in summary.TableCounts
                         .Where(kv => !priority.Contains(kv.Key) && kv.Value > 0)
                         .OrderBy(kv => kv.Key))
                LogLine($"    {kv.Key,-28} {kv.Value,5:N0} rows", Color.FromArgb(99, 202, 183));

            LogLine($"  Total tables with data: {summary.TableCounts.Count(kv => kv.Value > 0)}",
                    Color.FromArgb(148, 163, 184));
        }
        catch (Exception ex)
        {
            LogLine($"✗ Could not read backup: {ex.Message}", Color.FromArgb(239, 68, 68));
        }
        finally
        {
            SetAllButtons(true);
        }
    }

    // ── RESTORE ─────────────────────────────────────────────────────────────

    private async Task OnRestoreAsync()
    {
        var file = PickBackupFile("Select Backup to Restore");
        if (file == null) return;

        BackupSummary summary = null;
        try { summary = BackupService.PreviewBackup(file); } catch { }

        var confirmMsg = new StringBuilder();
        confirmMsg.AppendLine("You are about to RESTORE the database from:");
        confirmMsg.AppendLine();
        confirmMsg.AppendLine($"  {Path.GetFileName(file)}");
        if (summary != null)
        {
            confirmMsg.AppendLine($"  Created:  {summary.FileDate:yyyy-MM-dd HH:mm}");
            confirmMsg.AppendLine($"  Size:     {summary.FileSizeKb:N0} KB");
            if (summary.TableCounts.TryGetValue("Players", out var pc))
                confirmMsg.AppendLine($"  Players:  {pc}");
        }
        confirmMsg.AppendLine();
        confirmMsg.AppendLine("⚠  IMPORTANT: Close pgAdmin and any other database");
        confirmMsg.AppendLine("   tools BEFORE clicking Yes. Open connections will");
        confirmMsg.AppendLine("   block the restore.");
        confirmMsg.AppendLine();
        confirmMsg.AppendLine("This will REPLACE all current data. Cannot be undone.");

        if (MessageBox.Show(confirmMsg.ToString(), "Confirm Restore",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            return;

        SetAllButtons(false);
        _progressBar.Visible = true;
        _stopwatch.Restart();
        LogSeparator();
        LogLine($"Starting restore from: {Path.GetFileName(file)}",
                Color.FromArgb(251, 191, 36));
        LogLine("Navigation is locked until restore completes.",
                Color.FromArgb(251, 191, 36));

        // Lock only the nav sidebar — leaves the log panel live and readable.
        var mainForm = this.FindForm() as MainForm;
        mainForm?.LockNavigation();

        try
        {
            await Task.Run(() =>
                BackupService.RestoreBackup(file, step =>
                    this.Invoke(() => LogElapsed(step))
                )
            );

            _progressBar.Visible = false;
            LogElapsed("✓ Restore complete!", Color.FromArgb(34, 197, 94));
            LogLine("  Restarting application...", Color.FromArgb(100, 116, 139));
            await Task.Delay(1500); // let the user see the message
            Application.Restart();
        }
        catch (Exception ex)
        {
            _progressBar.Visible = false;
            LogElapsed("✗ Restore failed:", Color.FromArgb(239, 68, 68));
            LogLine($"  {ex.Message}", Color.FromArgb(239, 68, 68));
        }
        finally
        {
            _stopwatch.Stop();
            mainForm?.UnlockNavigation();
            SetAllButtons(true);
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
            Title            = title,
            InitialDirectory = backupFolder,
            Filter           = "SQL Backup Files (*.sql)|*.sql|All Files (*.*)|*.*",
            DefaultExt       = "sql"
        };
        return ofd.ShowDialog() == DialogResult.OK ? ofd.FileName : null;
    }

    private void SetAllButtons(bool enabled)
    {
        _btnBackup.Enabled     = enabled;
        _btnPreview.Enabled    = enabled;
        _btnRestore.Enabled    = enabled;
        _btnOpenFolder.Enabled = enabled;
    }

    // ── Ideas ─────────────────────────────────────────────────────────────────
    private void LoadIdeas()
    {
        _lstIdeas.Items.Clear();
        try
        {
            using var db = new BocceDbContext();
            var ideas = db.NewIdeas.OrderByDescending(i => i.DateCreated).ToList();
            foreach (var idea in ideas)
            {
                string marker = idea.DateCollected.HasValue ? "✓" : "○";
                _lstIdeas.Items.Add(new IdeaItem(idea.Id, $"{marker} {idea.Idea}"));
            }
        }
        catch { }
    }

    private void OnAddIdea(object? sender, EventArgs e)
    {
        string idea = _txtNewIdea.Text.Trim();
        if (string.IsNullOrEmpty(idea)) return;

        try
        {
            using var db = new BocceDbContext();
            db.NewIdeas.Add(new BocceManager.Data.Entities.NewIdea
            {
                Idea = idea,
                DateCreated = DateTime.UtcNow
            });
            db.SaveChanges();

            _txtNewIdea.Clear();
            LoadIdeas();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving idea:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnIdeasSelected(object? sender, EventArgs e)
    {
        _btnMarkCollected.Enabled = _lstIdeas.SelectedIndex >= 0 && !(_lstIdeas.SelectedItem is IdeaItem item && GetIdea(item.Id)?.DateCollected.HasValue == true);
        _btnDeleteIdea.Enabled = _lstIdeas.SelectedIndex >= 0;
    }

    private void OnMarkCollected(object? sender, EventArgs e)
    {
        if (_lstIdeas.SelectedItem is not IdeaItem item) return;

        try
        {
            using var db = new BocceDbContext();
            var idea = db.NewIdeas.Find(item.Id);
            if (idea != null)
            {
                idea.DateCollected = DateTime.UtcNow;
                db.SaveChanges();
                LoadIdeas();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error updating idea:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnDeleteIdea(object? sender, EventArgs e)
    {
        if (_lstIdeas.SelectedItem is not IdeaItem item) return;

        if (MessageBox.Show("Delete this idea?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        try
        {
            using var db = new BocceDbContext();
            var idea = db.NewIdeas.Find(item.Id);
            if (idea != null)
            {
                db.NewIdeas.Remove(idea);
                db.SaveChanges();
                LoadIdeas();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error deleting idea:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private BocceManager.Data.Entities.NewIdea? GetIdea(int id)
    {
        try
        {
            using var db = new BocceDbContext();
            return db.NewIdeas.Find(id);
        }
        catch
        {
            return null;
        }
    }

    private sealed record IdeaItem(int Id, string Text) { public override string ToString() => Text; }
}

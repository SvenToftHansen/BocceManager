using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using BocceManager.Services;
using BocceManager.UI.Theme;

namespace BocceManager.Panels;

public class UtilitiesPanel : UserControl
{
    private Button _btnBackup;
    private Button _btnRestore;
    private Button _btnOpenFolder;
    private Label _lblStatus;

    public UtilitiesPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;
        BuildUI();
    }

    private void BuildUI()
    {
        // Toolbar
        var toolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = AppTheme.Surface,
            Padding = new Padding(12, 8, 12, 8)
        };

        _btnBackup = MakeButton("Create Backup", AppTheme.Accent);
        _btnRestore = MakeButton("Restore from Backup", AppTheme.Accent);
        _btnOpenFolder = MakeButton("Open Backups Folder", Color.FromArgb(100, 116, 139));

        _btnBackup.Click += OnBackup;
        _btnRestore.Click += OnRestore;
        _btnOpenFolder.Click += OnOpenFolder;

        int x = 0;
        foreach (var btn in new[] { _btnBackup, _btnRestore, _btnOpenFolder })
        {
            btn.Location = new Point(x, 8);
            toolbar.Controls.Add(btn);
            x += btn.Width + 8;
        }

        // Status panel
        _lblStatus = new Label
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.ContentBackground,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.FontDefault,
            Padding = new Padding(12),
            AutoSize = false,
            Text = "Ready to backup or restore your database"
        };

        Controls.Add(_lblStatus);
        Controls.Add(toolbar);
    }

    private static Button MakeButton(string text, Color back) => new()
    {
        Text = text,
        AutoSize = true,
        Padding = new Padding(12, 0, 12, 0),
        Height = 36,
        FlatStyle = FlatStyle.Flat,
        BackColor = back,
        ForeColor = Color.White,
        Font = AppTheme.FontButton,
        Cursor = Cursors.Hand
    };

    private void OnBackup(object sender, EventArgs e)
    {
        _btnBackup.Enabled = false;
        _lblStatus.Text = "Creating backup...";
        Application.DoEvents();

        try
        {
            var backupFile = BackupService.CreateBackup();
            var fileInfo = new FileInfo(backupFile);
            _lblStatus.ForeColor = Color.DarkGreen;
            _lblStatus.Text = $"✓ Backup successful!\n\nFile: {Path.GetFileName(backupFile)}\nSize: {fileInfo.Length / 1024} KB\nLocation: Backups folder";
        }
        catch (Exception ex)
        {
            _lblStatus.ForeColor = Color.DarkRed;
            _lblStatus.Text = $"✗ Backup failed:\n{ex.Message}";
        }
        finally
        {
            _btnBackup.Enabled = true;
        }
    }

    private void OnRestore(object sender, EventArgs e)
    {
        var backupFolder = BackupService.GetBackupFolderPath();
        if (!Directory.Exists(backupFolder))
        {
            MessageBox.Show("No backups folder found. Create a backup first.", "No Backups", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var files = Directory.GetFiles(backupFolder, "*.sql");
        if (files.Length == 0)
        {
            MessageBox.Show("No backup files found in the Backups folder.", "No Backups", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var ofd = new OpenFileDialog
        {
            Title = "Select Backup to Restore",
            InitialDirectory = backupFolder,
            Filter = "SQL Backup Files (*.sql)|*.sql|All Files (*.*)|*.*",
            DefaultExt = "sql"
        };

        if (ofd.ShowDialog() != DialogResult.OK)
            return;

        if (MessageBox.Show(
            $"This will restore the database from:\n\n{Path.GetFileName(ofd.FileName)}\n\nThis action cannot be undone. Continue?",
            "Restore Database",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        _btnRestore.Enabled = false;
        _lblStatus.Text = "Restoring database...";
        Application.DoEvents();

        try
        {
            BackupService.RestoreBackup(ofd.FileName);
            _lblStatus.ForeColor = Color.DarkGreen;
            _lblStatus.Text = $"✓ Restore successful!\n\nDatabase restored from: {Path.GetFileName(ofd.FileName)}\n\nPlease restart the application for changes to take effect.";
        }
        catch (Exception ex)
        {
            _lblStatus.ForeColor = Color.DarkRed;
            _lblStatus.Text = $"✗ Restore failed:\n{ex.Message}";
        }
        finally
        {
            _btnRestore.Enabled = true;
        }
    }

    private void OnOpenFolder(object sender, EventArgs e)
    {
        try
        {
            var backupFolder = BackupService.GetBackupFolderPath();
            Directory.CreateDirectory(backupFolder);
            Process.Start(new ProcessStartInfo
            {
                FileName = backupFolder,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open backup folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

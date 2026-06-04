using System;
using System.Diagnostics;
using System.Windows.Forms;
using BocceManager.Services;

namespace BocceManager.Panels;

public partial class UtilitiesPanel : UserControl
{
    private Button _btnBackupDatabase;
    private Button _btnOpenBackupFolder;
    private Label _lblStatus;

    public UtilitiesPanel()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        // Panel setup
        this.Dock = DockStyle.Fill;
        this.Padding = new Padding(12);
        this.AutoScroll = true;

        // Header
        var lblHeader = new Label
        {
            Text = "Database Utilities",
            Font = new System.Drawing.Font("Segoe UI", 16, System.Drawing.FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 12)
        };

        // Backup section
        var gbBackup = new GroupBox
        {
            Text = "Database Backup",
            Padding = new Padding(12),
            Size = new System.Drawing.Size(400, 200),
            Margin = new Padding(0, 0, 0, 12)
        };

        var lblBackupDesc = new Label
        {
            Text = "Create a complete backup of the PostgreSQL database.\nBackups are stored in the Backups folder and can be used to restore data if needed.",
            Dock = DockStyle.Top,
            AutoSize = true,
            Height = 50,
            Margin = new Padding(0, 0, 0, 12)
        };

        _btnBackupDatabase = new Button
        {
            Text = "Create Backup Now",
            Size = new System.Drawing.Size(150, 36),
            Margin = new Padding(0, 0, 6, 0)
        };
        _btnBackupDatabase.Click += OnBackupDatabase;

        _btnOpenBackupFolder = new Button
        {
            Text = "Open Backup Folder",
            Size = new System.Drawing.Size(150, 36)
        };
        _btnOpenBackupFolder.Click += OnOpenBackupFolder;

        var pnlButtons = new Panel
        {
            Height = 40,
            Dock = DockStyle.Top,
            Margin = new Padding(0, 0, 0, 12)
        };
        pnlButtons.Controls.Add(_btnOpenBackupFolder);
        pnlButtons.Controls.Add(_btnBackupDatabase);

        _lblStatus = new Label
        {
            Text = "",
            Dock = DockStyle.Top,
            Height = 80,
            AutoSize = false,
            Margin = new Padding(0, 12, 0, 0)
        };

        gbBackup.Controls.Add(_lblStatus);
        gbBackup.Controls.Add(pnlButtons);
        gbBackup.Controls.Add(lblBackupDesc);

        this.Controls.Add(gbBackup);
        this.Controls.Add(lblHeader);
    }

    private void OnBackupDatabase(object sender, EventArgs e)
    {
        _btnBackupDatabase.Enabled = false;
        _lblStatus.Text = "Creating backup...";
        Application.DoEvents();

        try
        {
            var backupFile = BackupService.CreateBackup();
            var fileInfo = new System.IO.FileInfo(backupFile);
            _lblStatus.Text = $"✓ Backup successful!\n\nFile: {System.IO.Path.GetFileName(backupFile)}\nSize: {fileInfo.Length / 1024} KB\nLocation: Backups folder";
            _lblStatus.ForeColor = System.Drawing.Color.DarkGreen;
        }
        catch (Exception ex)
        {
            _lblStatus.Text = $"✗ Backup failed:\n\n{ex.Message}";
            _lblStatus.ForeColor = System.Drawing.Color.DarkRed;
        }
        finally
        {
            _btnBackupDatabase.Enabled = true;
        }
    }

    private void OnOpenBackupFolder(object sender, EventArgs e)
    {
        try
        {
            var backupFolder = BackupService.GetBackupFolderPath();
            System.IO.Directory.CreateDirectory(backupFolder);
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

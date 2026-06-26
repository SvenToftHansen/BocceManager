using System.Diagnostics;
using BocceManager.Data;
using BocceManager.Services;
using BocceManager.UI.Theme;

namespace BocceManager.Panels;

public class ParametersPanel : UserControl
{
    // Folder params (auto-save on browse)
    private TextBox _txtBackupFolder    = null!;
    private TextBox _txtDocsFolder      = null!;
    private TextBox _txtReportPdfFolder = null!;

    // File param (auto-save on browse)
    private TextBox _txtClubRulesDoc = null!;

    // Text params (saved via Save button)
    private TextBox _txtClubName      = null!;
    private TextBox _txtCaptainName   = null!;
    private TextBox _txtCaptainEmail  = null!;
    private TextBox _txtInitiationFeeAmount = null!;

    public ParametersPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock      = DockStyle.Fill;
        BuildUI();
    }

    private void BuildUI()
    {
        var scroll = new Panel
        {
            Dock        = DockStyle.Fill,
            AutoScroll  = true,
            BackColor   = AppTheme.ContentBackground,
            Padding     = new Padding(0)
        };

        var toolbar = MakeToolbar();

        var outer = new TableLayoutPanel
        {
            Dock            = DockStyle.Fill,
            ColumnCount     = 1,
            RowCount        = 2,
            Padding         = Padding.Empty,
            Margin          = Padding.Empty,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        };
        outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        scroll.Dock  = DockStyle.Fill;
        toolbar.Dock = DockStyle.Fill;
        outer.Controls.Add(scroll,   0, 0);
        outer.Controls.Add(toolbar,  0, 1);
        Controls.Add(outer);

        // Populate scroll area rows
        int y = 16;

        y = AddSectionHeader(scroll, "FOLDER LOCATIONS", y);
        y = AddFolderRow(scroll, "Backup Folder",      "BackupFolder",      ref _txtBackupFolder,    y);
        y = AddFolderRow(scroll, "Documents Folder",   "DocumentsFolder",   ref _txtDocsFolder,      y);
        y = AddFolderRow(scroll, "Report PDF Folder",  "ReportPdfLocation", ref _txtReportPdfFolder, y);

        y += 12;

        y = AddSectionHeader(scroll, "CLUB DOCUMENT", y);
        y = AddFileRow(scroll, "Club Rules Document", "ClubRulesDocument", ref _txtClubRulesDoc, y);

        y += 12;

        y = AddSectionHeader(scroll, "CLUB SETTINGS", y);
        y = AddTextRow(scroll, "Club Name",            "ClubName",           ref _txtClubName,     y);
        y = AddTextRow(scroll, "League Captain Name",  "LeagueCaptainName",  ref _txtCaptainName,  y);
        y = AddTextRow(scroll, "League Captain Email", "LeagueCaptainEmail", ref _txtCaptainEmail, y);

        y += 12;

        y = AddSectionHeader(scroll, "FEES", y);
        y = AddTextRow(scroll, "Initiation Fee Amount", "InitiationFeeAmount", ref _txtInitiationFeeAmount, y);

        // Load text param values
        LoadTextParams();
    }

    // -- Section header --

    private static int AddSectionHeader(Panel parent, string title, int y)
    {
        var lbl = new Label
        {
            Text      = title,
            Font      = AppTheme.FontSmall,
            ForeColor = AppTheme.TextMuted,
            AutoSize  = true,
            Location  = new Point(16, y)
        };
        var sep = new Panel
        {
            BackColor = AppTheme.Separator,
            Location  = new Point(16, y + 18),
            Height    = 1,
            Anchor    = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
        };
        sep.Width = parent.ClientSize.Width - 32;
        parent.SizeChanged += (_, _) => sep.Width = parent.ClientSize.Width - 32;
        parent.Controls.Add(lbl);
        parent.Controls.Add(sep);
        return y + 26;
    }

    // -- Folder row (FolderBrowserDialog, auto-saves) --

    private int AddFolderRow(Panel parent, string label, string paramKey, ref TextBox field, int y)
    {
        AddLabel(parent, label, y);
        var txt = MakePathBox(parent, y);
        field = txt;

        try
        {
            using var db = new BocceDbContext();
            var v = AppParameterService.GetAppParameter(db, paramKey);
            txt.Text = v ?? string.Empty;
        }
        catch { }

        var btn = MakeBrowseButton(parent, y);
        btn.Click += (_, _) =>
        {
            using var dlg = new FolderBrowserDialog
            {
                Description            = "Select " + label,
                UseDescriptionForTitle = true,
                ShowNewFolderButton    = true
            };
            var cur = txt.Text.Trim();
            if (Directory.Exists(cur)) dlg.SelectedPath = cur;
            if (dlg.ShowDialog() != DialogResult.OK) return;
            txt.Text = dlg.SelectedPath;
            try
            {
                using var db = new BocceDbContext();
                AppParameterService.SetAppParameter(db, paramKey, dlg.SelectedPath);
            }
            catch { }
        };

        return y + 40;
    }

    // -- File row (OpenFileDialog, auto-saves, has Open button) --

    private int AddFileRow(Panel parent, string label, string paramKey, ref TextBox field, int y)
    {
        AddLabel(parent, label, y);
        var txt = MakePathBox(parent, y, extraButtons: 2);
        field = txt;

        try
        {
            using var db = new BocceDbContext();
            // Migrate from legacy bocce-rules-doc.txt on first run
            var stored = AppParameterService.GetAppParameter(db, paramKey);
            if (string.IsNullOrEmpty(stored))
            {
                var legacyFile = Path.Combine(AppContext.BaseDirectory, "bocce-rules-doc.txt");
                if (File.Exists(legacyFile))
                {
                    stored = File.ReadAllText(legacyFile).Trim();
                    if (!string.IsNullOrEmpty(stored))
                        AppParameterService.SetAppParameter(db, paramKey, stored);
                }
            }
            txt.Text = stored ?? string.Empty;
        }
        catch { }

        var btnBrowse = MakeBrowseButton(parent, y, rightOffset: 216);
        var btnOpen   = new Button
        {
            Text      = "Open",
            Size      = new Size(96, 28),
            Location  = new Point(9999, y + 4),   // x will be set below
            Anchor    = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            Font      = AppTheme.FontButton,
            Cursor    = Cursors.Hand,
            FlatAppearance = { BorderSize = 1, BorderColor = AppTheme.Separator }
        };
        btnOpen.Location = new Point(parent.ClientSize.Width - 108, y + 4);
        parent.SizeChanged += (_, _) => btnOpen.Location = new Point(parent.ClientSize.Width - 108, y + 4);
        parent.Controls.Add(btnOpen);

        btnBrowse.Click += (_, _) =>
        {
            using var dlg = new OpenFileDialog
            {
                Title           = "Select Club Rules Document",
                Filter          = "Documents|*.pdf;*.doc;*.docx|All Files|*.*",
                CheckFileExists = true
            };
            var cur = txt.Text.Trim();
            if (cur.Length > 0)
                try { dlg.InitialDirectory = Path.GetDirectoryName(cur)!; } catch { }
            if (dlg.ShowDialog() != DialogResult.OK) return;
            txt.Text = dlg.FileName;
            try
            {
                using var db = new BocceDbContext();
                AppParameterService.SetAppParameter(db, paramKey, dlg.FileName);
            }
            catch { }
        };

        btnOpen.Click += (_, _) =>
        {
            var path = txt.Text.Trim();
            if (string.IsNullOrEmpty(path))
            {
                MessageBox.Show("No rules document has been selected yet.",
                    "Parameters", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!File.Exists(path))
            {
                MessageBox.Show("The document file could not be found.",
                    "Parameters", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open document: " + ex.Message,
                    "Parameters", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        return y + 40;
    }

    // -- Text row (editable, saved by Save button) --

    private int AddTextRow(Panel parent, string label, string paramKey, ref TextBox field, int y)
    {
        AddLabel(parent, label, y);
        var txt = new TextBox
        {
            Font      = AppTheme.FontDefault,
            BackColor = AppTheme.ContentBackground,
            ForeColor = AppTheme.TextPrimary,
            Location  = new Point(200, y + 4),
            Height    = 26,
            Anchor    = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            Tag       = paramKey
        };
        txt.Width = parent.ClientSize.Width - 216;
        parent.SizeChanged += (_, _) => txt.Width = parent.ClientSize.Width - 216;
        parent.Controls.Add(txt);
        field = txt;
        return y + 40;
    }

    // -- Shared helpers --

    private static void AddLabel(Panel parent, string text, int y)
    {
        parent.Controls.Add(new Label
        {
            Text      = text,
            Font      = AppTheme.FontDefault,
            ForeColor = AppTheme.TextSecondary,
            AutoSize  = false,
            Size      = new Size(180, 28),
            Location  = new Point(16, y + 6),
            TextAlign = ContentAlignment.MiddleLeft
        });
    }

    private static TextBox MakePathBox(Panel parent, int y, int extraButtons = 1)
    {
        var txt = new TextBox
        {
            ReadOnly  = true,
            Font      = AppTheme.FontDefault,
            BackColor = AppTheme.ContentBackground,
            ForeColor = AppTheme.TextPrimary,
            Location  = new Point(200, y + 4),
            Height    = 26,
            Anchor    = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
        };
        // Right edge is reserved for Browse buttons (each ~108px)
        txt.Width = parent.ClientSize.Width - 200 - (extraButtons * 108) - 8;
        parent.SizeChanged += (_, _) =>
            txt.Width = parent.ClientSize.Width - 200 - (extraButtons * 108) - 8;
        parent.Controls.Add(txt);
        return txt;
    }

    private static Button MakeBrowseButton(Panel parent, int y, int rightOffset = 108)
    {
        var btn = new Button
        {
            Text      = "Browse...",
            Size      = new Size(100, 28),
            Location  = new Point(parent.ClientSize.Width - rightOffset, y + 4),
            Anchor    = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.Accent,
            ForeColor = Color.White,
            Font      = AppTheme.FontButton,
            Cursor    = Cursors.Hand,
            FlatAppearance = { BorderSize = 0 }
        };
        parent.SizeChanged += (_, _) =>
            btn.Location = new Point(parent.ClientSize.Width - rightOffset, y + 4);
        parent.Controls.Add(btn);
        return btn;
    }

    // -- Load / Save text params --

    private void LoadTextParams()
    {
        try
        {
            using var db = new BocceDbContext();
            SetText(_txtClubName,             AppParameterService.GetAppParameter(db, "ClubName"));
            SetText(_txtCaptainName,          AppParameterService.GetAppParameter(db, "LeagueCaptainName"));
            SetText(_txtCaptainEmail,         AppParameterService.GetAppParameter(db, "LeagueCaptainEmail"));
            SetText(_txtInitiationFeeAmount,  AppParameterService.GetAppParameter(db, "InitiationFeeAmount"));
        }
        catch { }

        static void SetText(TextBox box, string? value) =>
            box.Text = value ?? string.Empty;
    }

    private void SaveTextParams()
    {
        try
        {
            using var db = new BocceDbContext();
            AppParameterService.SetAppParameter(db, "ClubName",             _txtClubName.Text.Trim());
            AppParameterService.SetAppParameter(db, "LeagueCaptainName",  _txtCaptainName.Text.Trim());
            AppParameterService.SetAppParameter(db, "LeagueCaptainEmail", _txtCaptainEmail.Text.Trim());
            AppParameterService.SetAppParameter(db, "InitiationFeeAmount", _txtInitiationFeeAmount.Text.Trim());
        }
        catch (Exception ex)
        {
            MessageBox.Show("Save failed: " + ex.Message,
                "Parameters", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        MessageBox.Show("Settings saved.",
            "Parameters", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // -- Toolbar --

    private Panel MakeToolbar()
    {
        var panel = new Panel
        {
            Height    = 46,
            BackColor = AppTheme.Surface,
            Padding   = new Padding(12, 8, 12, 8)
        };

        var btnSave = new Button
        {
            Text      = "Save Settings",
            Location  = new Point(12, 8),
            Size      = new Size(130, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.Accent,
            ForeColor = Color.White,
            Font      = AppTheme.FontButton,
            Cursor    = Cursors.Hand,
            FlatAppearance = { BorderSize = 0 }
        };
        btnSave.Click += (_, _) => SaveTextParams();
        panel.Controls.Add(btnSave);
        return panel;
    }
}

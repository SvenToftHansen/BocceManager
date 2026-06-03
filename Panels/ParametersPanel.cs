using System.Diagnostics;
using BocceManager.Data;
using BocceManager.Services;
using BocceManager.UI.Theme;

namespace BocceManager.Panels;

public class ParametersPanel : UserControl
{
    private TextBox _txtDocPath = null!;

    public ParametersPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;
        BuildUI();
    }

    private void BuildUI()
    {
        var docSection = MakeDocumentSection();
        var grid = MakeGrid();
        LoadAppParams(grid);

        var toolbar = MakeToolbar(() => SaveAppParams(grid));

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3,
            Padding = Padding.Empty, Margin = Padding.Empty,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        docSection.Dock = DockStyle.Fill;
        grid.Dock       = DockStyle.Fill;
        toolbar.Dock    = DockStyle.Fill;
        layout.Controls.Add(docSection, 0, 0);
        layout.Controls.Add(grid,       0, 1);
        layout.Controls.Add(toolbar,    0, 2);

        Controls.Add(layout);
    }

    // ── Club Rules Document ───────────────────────────────────────────────────

    private static readonly string DocPathFile =
        Path.Combine(AppContext.BaseDirectory, "bocce-rules-doc.txt");

    private static string LoadDocPath()
    {
        try { return File.Exists(DocPathFile) ? File.ReadAllText(DocPathFile).Trim() : ""; }
        catch { return ""; }
    }

    private static void SaveDocPath(string path)
    {
        try { File.WriteAllText(DocPathFile, path); }
        catch { }
    }

    private Panel MakeDocumentSection()
    {
        var section = new Panel { BackColor = AppTheme.Surface };

        section.Controls.Add(new Label
        {
            Text = "Club Rules Document",
            Font = AppTheme.FontDefaultBold, ForeColor = AppTheme.TextPrimary,
            AutoSize = true, Location = new Point(14, 10)
        });

        _txtDocPath = new TextBox
        {
            ReadOnly = true,
            Font = AppTheme.FontDefault,
            BackColor = AppTheme.ContentBackground, ForeColor = AppTheme.TextPrimary,
            Location = new Point(14, 34), Size = new Size(420, 26),
            Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right,
            Text = LoadDocPath()
        };

        Button Btn(string text, Color back, int rightOffset, int w, Color? fore = null) => new Button
        {
            Text = text, Size = new Size(w, 28),
            Location = new Point(rightOffset, 33),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat, BackColor = back,
            ForeColor = fore ?? Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand,
            FlatAppearance = { BorderSize = 0 }
        };

        var btnBrowse = Btn("Browse...", AppTheme.Accent,  544, 96);
        var btnOpen   = Btn("Open",      AppTheme.Surface,  648, 66, AppTheme.TextPrimary);
        btnOpen.FlatAppearance.BorderSize  = 1;
        btnOpen.FlatAppearance.BorderColor = AppTheme.Separator;

        section.Controls.Add(new Label
        {
            Text = "PDF or Word document (.pdf, .doc, .docx) — the club’s official rulebook, referenced by all leagues.",
            Font = AppTheme.FontSmall, ForeColor = AppTheme.TextMuted,
            AutoSize = true, Location = new Point(14, 68)
        });

        btnBrowse.Click += (_, _) =>
        {
            using var dlg = new OpenFileDialog
            {
                Title = "Select Club Rules Document",
                Filter = "Documents|*.pdf;*.doc;*.docx|PDF Files|*.pdf|Word Documents|*.doc;*.docx|All Files|*.*",
                CheckFileExists = true
            };
            var current = _txtDocPath.Text.Trim();
            if (current.Length > 0)
                try { dlg.InitialDirectory = Path.GetDirectoryName(current)!; } catch { }

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _txtDocPath.Text = dlg.FileName;
                SaveDocPath(dlg.FileName);
            }
        };

        btnOpen.Click += (_, _) =>
        {
            var path = _txtDocPath.Text.Trim();
            if (string.IsNullOrEmpty(path))
            {
                MessageBox.Show("No rules document has been selected yet.",
                    "BocceManager", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!File.Exists(path))
            {
                MessageBox.Show("The document file could not be found. It may have been moved or deleted.",
                    "BocceManager", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open the document:\n\n{ex.Message}",
                    "BocceManager", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        section.Controls.AddRange([_txtDocPath, btnBrowse, btnOpen]);
        return section;
    }

    // ── Load / Save ───────────────────────────────────────────────────────────

    private static void LoadAppParams(DataGridView grid)
    {
        grid.Rows.Clear();
        try
        {
            using var db = new BocceDbContext();
            foreach (var p in AppParameterService.Load(db))
                grid.Rows.Add(p.Id, p.Key, p.Value, p.Description ?? "");
        }
        catch { }
    }

    private static void SaveAppParams(DataGridView grid)
    {
        try
        {
            using var db = new BocceDbContext();
            foreach (DataGridViewRow row in grid.Rows)
            {
                var idCell = row.Cells["Id"].Value;
                if (idCell == null) continue;
                int id = Convert.ToInt32(idCell);
                var param = db.AppParameters.Find(id);
                if (param == null) continue;
                param.Value = row.Cells["Value"].Value?.ToString() ?? "";
            }
            db.SaveChanges();
        }
        catch (Exception ex) { ShowError(ex); return; }
        LoadAppParams(grid);
        ShowSaved();
    }

    // ── Grid ──────────────────────────────────────────────────────────────────

    private static DataGridView MakeGrid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            SelectionMode = DataGridViewSelectionMode.CellSelect,
            MultiSelect = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            BorderStyle = BorderStyle.None,
            BackgroundColor = AppTheme.ContentBackground,
            GridColor = AppTheme.GridLines,
            Font = AppTheme.FontDefault,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor           = AppTheme.GridHeaderBackground,
                ForeColor           = AppTheme.GridHeaderText,
                SelectionBackColor  = AppTheme.GridHeaderBackground,
                SelectionForeColor  = AppTheme.GridHeaderText,
                Font                = AppTheme.FontGridHeader,
                Padding             = new Padding(4, 0, 0, 0)
            },
            EnableHeadersVisualStyles = false,
            RowTemplate = { Height = 30 },
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        };
        var lockedStyle = new DataGridViewCellStyle
        {
            BackColor          = AppTheme.Surface,
            ForeColor          = AppTheme.TextSecondary,
            SelectionBackColor = AppTheme.Surface,
            SelectionForeColor = AppTheme.TextSecondary
        };

        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id",          Visible = false,                                              FillWeight = 1,  MinimumWidth = 2   });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Key",         HeaderText = "Parameter", ReadOnly = true, DefaultCellStyle = lockedStyle, FillWeight = 25, MinimumWidth = 140 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Value",       HeaderText = "Value",                                         FillWeight = 25, MinimumWidth = 120 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "Description", ReadOnly = true, DefaultCellStyle = lockedStyle, FillWeight = 50, MinimumWidth = 100 });
        grid.AlternatingRowsDefaultCellStyle =
            new DataGridViewCellStyle { BackColor = AppTheme.GridAlternateRow };

        grid.CellEnter += (_, e) =>
        {
            if (e.RowIndex < 0) return;
            var valueCol = grid.Columns["Value"];
            if (valueCol == null || e.ColumnIndex == valueCol.Index) return;
            grid.BeginInvoke(() => grid.CurrentCell = grid.Rows[e.RowIndex].Cells["Value"]);
        };

        return grid;
    }

    private static Panel MakeToolbar(Action onSave)
    {
        var panel = new Panel
        {
            Height = 46, BackColor = AppTheme.Surface, Padding = new Padding(12, 8, 12, 8)
        };

        var btnSave = new Button
        {
            Text = "Save Changes", Location = new Point(0, 8), Size = new Size(140, 30),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.Accent, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 }
        };
        btnSave.Click += (_, _) => onSave();
        panel.Controls.Add(btnSave);
        return panel;
    }

private static void ShowSaved()
        => MessageBox.Show("Parameters saved.", "BocceManager", MessageBoxButtons.OK, MessageBoxIcon.Information);

    private static void ShowError(Exception ex)
        => MessageBox.Show($"Save failed:\n\n{ex.Message}", "BocceManager", MessageBoxButtons.OK, MessageBoxIcon.Error);
}

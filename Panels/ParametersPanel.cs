using BocceManager.Data;
using BocceManager.Services;
using BocceManager.UI.Theme;

namespace BocceManager.Panels;

public class ParametersPanel : UserControl
{
    public ParametersPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;
        BuildUI();
    }

    private void BuildUI()
    {
        var grid = MakeGrid();
        LoadAppParams(grid);

        var toolbar = MakeToolbar(
            () => AddRow(grid),
            () => DeleteRow(grid),
            () => SaveAppParams(grid));

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2,
            Padding = Padding.Empty, Margin = Padding.Empty,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        grid.Dock    = DockStyle.Fill;
        toolbar.Dock = DockStyle.Fill;
        layout.Controls.Add(grid,    0, 0);
        layout.Controls.Add(toolbar, 0, 1);

        Controls.Add(layout);
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
        var rows = GridRows(grid);
        try
        {
            using var db = new BocceDbContext();
            AppParameterService.Save(db, rows);
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
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
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
                BackColor = AppTheme.GridHeaderBackground,
                ForeColor = AppTheme.GridHeaderText,
                Font = AppTheme.FontGridHeader,
                Padding = new Padding(4, 0, 0, 0)
            },
            EnableHeadersVisualStyles = false,
            RowTemplate = { Height = 30 },
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        };
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id",          Visible = false,                FillWeight = 1,  MinimumWidth = 2   });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Key",         HeaderText = "Key",         FillWeight = 25, MinimumWidth = 120 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Value",       HeaderText = "Value",       FillWeight = 30, MinimumWidth = 120 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "Description", FillWeight = 45, MinimumWidth = 100 });
        grid.AlternatingRowsDefaultCellStyle =
            new DataGridViewCellStyle { BackColor = AppTheme.GridAlternateRow };
        return grid;
    }

    private static Panel MakeToolbar(Action onAdd, Action onDelete, Action onSave)
    {
        var panel = new Panel
        {
            Height = 46, BackColor = AppTheme.Surface, Padding = new Padding(12, 8, 12, 8)
        };

        Button Btn(string text, Color back, int x) => new Button
        {
            Text = text, Location = new Point(x, 8), Size = new Size(120, 30),
            FlatStyle = FlatStyle.Flat, BackColor = back, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 }
        };

        var btnAdd    = Btn("+ Add Row",       AppTheme.ButtonSuccess, 0);
        var btnDelete = Btn("Delete Selected", AppTheme.ButtonDanger,  130);
        var btnSave   = Btn("Save Changes",    AppTheme.Accent,        360);

        btnAdd.Click    += (_, _) => onAdd();
        btnDelete.Click += (_, _) => onDelete();
        btnSave.Click   += (_, _) => onSave();

        panel.Controls.AddRange([btnAdd, btnDelete, btnSave]);
        return panel;
    }

    // ── Row helpers ───────────────────────────────────────────────────────────

    private static void AddRow(DataGridView grid)
    {
        int idx = grid.Rows.Add("0", "", "", "");
        grid.CurrentCell = grid.Rows[idx].Cells["Key"];
        grid.BeginEdit(true);
    }

    private static void DeleteRow(DataGridView grid)
    {
        if (grid.SelectedRows.Count == 0) return;
        grid.Rows.Remove(grid.SelectedRows[0]);
    }

    private static List<(string Key, string Value, string Desc)> GridRows(DataGridView grid)
    {
        var result = new List<(string, string, string)>();
        foreach (DataGridViewRow row in grid.Rows)
        {
            var key = row.Cells["Key"].Value?.ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(key)) continue;
            result.Add((key,
                row.Cells["Value"].Value?.ToString() ?? "",
                row.Cells["Description"].Value?.ToString() ?? ""));
        }
        return result;
    }

    private static void ShowSaved()
        => MessageBox.Show("Parameters saved.", "BocceManager", MessageBoxButtons.OK, MessageBoxIcon.Information);

    private static void ShowError(Exception ex)
        => MessageBox.Show($"Save failed:\n\n{ex.Message}", "BocceManager", MessageBoxButtons.OK, MessageBoxIcon.Error);
}

using BocceManager.Services;
using BocceManager.UI.Theme;

namespace BocceManager.Panels;

/// <summary>
/// Represents one season or division that could receive a cascaded value change.
/// </summary>
public sealed record PropagateTarget(
    string  EntityType,   // "Season" or "Division"
    int     Id,
    string  Name,
    string  ParentName,   // season name (for divisions) or "" (for seasons)
    int?    CurrentMin,
    int?    CurrentMax,
    int?    NewMin,        // null = this field not changing
    int?    NewMax);

/// <summary>
/// Modal dialog that lets the user choose which child seasons/divisions
/// should receive a cascaded Players Per Team Min/Max change.
/// </summary>
public class PropagateDialog : Form
{
    private DataGridView _grid = null!;

    /// <summary>Targets the user left checked when they clicked Apply.</summary>
    public IReadOnlyList<PropagateTarget> ApprovedTargets { get; private set; } = [];

    public PropagateDialog(
        int?   oldMin, int?   oldMax,
        int?   newMin, int?   newMax,
        IReadOnlyList<PropagateTarget> targets)
    {
        Text            = "Propagate Changes";
        Size            = new Size(760, 520);
        MinimumSize     = new Size(640, 420);
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        BackColor       = AppTheme.ContentBackground;
        Font            = AppTheme.FontDefault;

        BuildUI(oldMin, oldMax, newMin, newMax, targets);
    }

    private void BuildUI(
        int? oldMin, int? oldMax,
        int? newMin, int? newMax,
        IReadOnlyList<PropagateTarget> targets)
    {
        // ── Summary header ────────────────────────────────────────────────────
        var summary = new Panel
        {
            Dock = DockStyle.Top,
            Height = 80,
            BackColor = AppTheme.Surface,
            Padding = new Padding(16, 12, 16, 12)
        };

        var lines = new List<string>();
        if (newMin.HasValue)
            lines.Add($"Players Per Team Minimum:  {AppParameterService.Fmt(oldMin)} → {newMin}");
        if (newMax.HasValue)
            lines.Add($"Players Per Team Maximum:  {AppParameterService.Fmt(oldMax)} → {newMax}");

        summary.Controls.Add(new Label
        {
            Text      = "You changed at League level:\n" + string.Join("\n", lines),
            Font      = AppTheme.FontDefault,
            ForeColor = AppTheme.TextPrimary,
            AutoSize  = true,
            Location  = new Point(16, 10)
        });

        // ── Instruction ───────────────────────────────────────────────────────
        var instrPanel = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 36,
            BackColor = AppTheme.ContentBackground
        };
        instrPanel.Controls.Add(new Label
        {
            Text      = "Select the seasons and divisions to update. Uncheck any you want to leave unchanged.",
            Font      = AppTheme.FontSmall,
            ForeColor = AppTheme.TextSecondary,
            AutoSize  = true,
            Location  = new Point(16, 10)
        });

        // ── Grid ──────────────────────────────────────────────────────────────
        _grid = new DataGridView
        {
            Dock        = DockStyle.Fill,
            SelectionMode          = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect            = false,
            AllowUserToAddRows     = false,
            AllowUserToDeleteRows  = false,
            AllowUserToResizeRows  = false,
            RowHeadersVisible      = false,
            BorderStyle            = BorderStyle.None,
            BackgroundColor        = AppTheme.ContentBackground,
            GridColor              = AppTheme.GridLines,
            Font                   = AppTheme.FontDefault,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor          = AppTheme.GridHeaderBackground,
                ForeColor          = AppTheme.GridHeaderText,
                SelectionBackColor = AppTheme.GridHeaderBackground,
                SelectionForeColor = AppTheme.GridHeaderText,
                Font               = AppTheme.FontGridHeader,
                Padding            = new Padding(4, 0, 0, 0)
            },
            EnableHeadersVisualStyles = false,
            RowTemplate = { Height = 30 },
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        _grid.AlternatingRowsDefaultCellStyle =
            new DataGridViewCellStyle { BackColor = AppTheme.GridAlternateRow };

        _grid.Columns.Add(new DataGridViewCheckBoxColumn
            { Name = "Check", HeaderText = "", FillWeight = 5, MinimumWidth = 36 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "Type", HeaderText = "Type", FillWeight = 10, MinimumWidth = 70, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "Name", HeaderText = "Name", FillWeight = 28, MinimumWidth = 140, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "CurMin", HeaderText = "Min now", FillWeight = 10, MinimumWidth = 70, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "CurMax", HeaderText = "Max now", FillWeight = 10, MinimumWidth = 70, ReadOnly = true });

        if (newMin.HasValue)
            _grid.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "NewMin", HeaderText = "Min → new", FillWeight = 10, MinimumWidth = 80, ReadOnly = true });
        if (newMax.HasValue)
            _grid.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "NewMax", HeaderText = "Max → new", FillWeight = 10, MinimumWidth = 80, ReadOnly = true });

        // Populate rows
        foreach (var t in targets)
        {
            string indent = t.EntityType == "Division" ? "    " : "";
            var row = new DataGridViewRow();
            row.CreateCells(_grid);
            row.Cells["Check"].Value  = true;
            row.Cells["Type"].Value   = t.EntityType;
            row.Cells["Name"].Value   = indent + t.Name + (t.ParentName.Length > 0 ? $"  ({t.ParentName})" : "");
            row.Cells["CurMin"].Value = AppParameterService.Fmt(t.CurrentMin);
            row.Cells["CurMax"].Value = AppParameterService.Fmt(t.CurrentMax);
            if (newMin.HasValue) row.Cells["NewMin"].Value = newMin.ToString();
            if (newMax.HasValue) row.Cells["NewMax"].Value = newMax.ToString();
            row.Tag = t;
            _grid.Rows.Add(row);
        }
        _grid.ClearSelection();

        // ── Toolbar ───────────────────────────────────────────────────────────
        var toolbar = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 54,
            BackColor = AppTheme.Surface,
            Padding   = new Padding(12, 10, 12, 10)
        };

        Button Btn(string text, Color back, int x, int w = 130) => new Button
        {
            Text      = text,
            Location  = new Point(x, 10),
            Size      = new Size(w, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = back,
            ForeColor = Color.White,
            Font      = AppTheme.FontButton,
            Cursor    = Cursors.Hand,
            FlatAppearance = { BorderSize = 0 }
        };

        var btnAll   = Btn("Select All",    AppTheme.Surface,       0,   110);
        btnAll.ForeColor   = AppTheme.TextPrimary;
        btnAll.FlatAppearance.BorderColor = AppTheme.Separator;
        btnAll.FlatAppearance.BorderSize  = 1;

        var btnNone  = Btn("Clear All",     AppTheme.Surface,       118, 110);
        btnNone.ForeColor  = AppTheme.TextPrimary;
        btnNone.FlatAppearance.BorderColor = AppTheme.Separator;
        btnNone.FlatAppearance.BorderSize  = 1;

        var btnApply = Btn("Apply Selected", AppTheme.Accent,       toolbar.Width - 290, 140);
        var btnSkip  = Btn("Skip",           AppTheme.ButtonDanger, toolbar.Width - 142, 130);
        btnApply.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnSkip.Anchor  = AnchorStyles.Top | AnchorStyles.Right;

        btnAll.Click   += (_, _) => SetAll(true);
        btnNone.Click  += (_, _) => SetAll(false);
        btnApply.Click += (_, _) => { CollectApproved(); DialogResult = DialogResult.OK;  Close(); };
        btnSkip.Click  += (_, _) => { ApprovedTargets = [];              DialogResult = DialogResult.Cancel; Close(); };

        toolbar.Controls.AddRange([btnAll, btnNone, btnApply, btnSkip]);

        // Assemble using TableLayoutPanel to avoid overlap
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4,
            Padding = Padding.Empty, Margin = Padding.Empty,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        summary.Dock     = DockStyle.Fill;
        instrPanel.Dock  = DockStyle.Fill;
        _grid.Dock       = DockStyle.Fill;
        toolbar.Dock     = DockStyle.Fill;
        layout.Controls.Add(summary,    0, 0);
        layout.Controls.Add(instrPanel, 0, 1);
        layout.Controls.Add(_grid,      0, 2);
        layout.Controls.Add(toolbar,    0, 3);
        Controls.Add(layout);
    }

    private void SetAll(bool value)
    {
        foreach (DataGridViewRow row in _grid.Rows)
            row.Cells["Check"].Value = value;
    }

    private void CollectApproved()
    {
        var result = new List<PropagateTarget>();
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.Cells["Check"].Value is true && row.Tag is PropagateTarget t)
                result.Add(t);
        }
        ApprovedTargets = result;
    }

}

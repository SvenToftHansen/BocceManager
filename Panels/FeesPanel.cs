using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.Services;
using BocceManager.UI.Controls;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Panels;

public class FeesPanel : UserControl
{
    // ── Data item types ───────────────────────────────────────────────────────
    private sealed class PlayerItem  { public int Id; public string Name = ""; public override string ToString() => Name; }
    private sealed class DivItem     { public int? Id; public string Name = ""; public override string ToString() => Name; }
    private sealed class TeamItm     { public int? Id; public string Name = ""; public override string ToString() => Name; }

    private record InitRow(int Id, string Name, decimal Owing, decimal Paid, bool IsOnTeam, bool IsOnSpareList);
    private record SeasonRow(int Id, string Name, decimal Owing, decimal Paid,
        HashSet<int> DivisionIds, HashSet<int> TeamIds);

    private const int MissingWidth = 270;
    private const int FilterBarHeight = 82;

    // ── Initiation Fees tab ───────────────────────────────────────────────────
    private DataGridView    _initGrid              = null!;
    private ListBox         _lstInitMissing        = null!;
    private Label           _lblInitMissingCount   = null!;
    private SplitContainer  _initSplit             = null!;
    private Button          _btnInitChevron        = null!;
    private SearchBoxControl _txtInitSearch        = null!;
    private ComboBox        _cmbInitFilter         = null!;
    private ComboBox        _cmbOnTeamFilter       = null!;
    private ComboBox        _cmbOnSpareListFilter  = null!;
    private List<InitRow>   _allInitFees           = [];

    // ── Seasonal Fees tab ─────────────────────────────────────────────────────
    private DataGridView    _seasonGrid            = null!;
    private ListBox         _lstSeasonMissing      = null!;
    private Label           _lblSeasonMissingCount = null!;
    private SplitContainer  _seasonSplit           = null!;
    private Button          _btnSeasonChevron      = null!;
    private Button          _btnSeasonPaid         = null!;
    private Button          _btnSeasonUnpaid       = null!;
    private Button          _btnAddSeasonalFees    = null!;
    private SearchBoxControl _txtSeasonSearch      = null!;
    private ComboBox        _cmbSeasonFilter       = null!;
    private ComboBox        _cmbDivision           = null!;
    private ComboBox        _cmbTeam               = null!;
    private List<SeasonRow> _allSeasonFees         = [];
    private int?            _currentSeasonId;
    private bool            _seasonIsLocked        = false;

    private bool _loading = false;

    // ── Constructor ───────────────────────────────────────────────────────────
    public FeesPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock      = DockStyle.Fill;
        BuildUI();
        AppParameterService.DefaultsChanged += OnDefaultsChanged;
        Reload();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            AppParameterService.DefaultsChanged -= OnDefaultsChanged;
        base.Dispose(disposing);
    }

    private void OnDefaultsChanged(object? sender, DefaultsChangedEventArgs e) => Reload();

    // ── UI Construction ───────────────────────────────────────────────────────

    private void BuildUI()
    {
        var tabs = new TabControl { Dock = DockStyle.Fill, Font = AppTheme.FontDefault };
        tabs.TabPages.Add(BuildInitiationTab());
        tabs.TabPages.Add(BuildSeasonalTab());
        Controls.Add(tabs);
    }

    // ── Initiation Fees Tab ───────────────────────────────────────────────────

    private TabPage BuildInitiationTab()
    {
        var page = new TabPage("  Initiation Fees  ") { BackColor = AppTheme.ContentBackground };

        var filterBar = BuildInitFilterBar();
        var separator = MakeSeparator();

        _initSplit = MakeSplit();
        _initSplit.Panel1.BackColor = AppTheme.ContentBackground;
        _initSplit.Panel2.BackColor = AppTheme.ContentBackground;
        _initSplit.Panel1.Padding = new Padding(4, 4, 2, 4);
        _initSplit.Panel2.Padding = new Padding(2, 4, 4, 4);

        // Left bordered section: header + grid
        var leftBorder = MakeBorderBox();
        var leftHeader = BuildSectionHeader("INITIATION FEES — OWING / PAID", out _btnInitChevron,
            out var btnInitPaid, out var btnInitUnpaid);
        _btnInitChevron.Click  += (_, _) => ToggleMissingPanel(_initSplit, _btnInitChevron);
        btnInitPaid.Click      += (_, _) => MarkSelectedInitiationFees(asPaid: true);
        btnInitUnpaid.Click    += (_, _) => MarkSelectedInitiationFees(asPaid: false);

        _initGrid = MakeGrid();
        _initGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "InitId",    Visible = false });
        _initGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "InitName",  HeaderText = "Player",       AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        _initGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "InitOwing", HeaderText = "Owing", Width = 110, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "C2" } });
        _initGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "InitPaid",  HeaderText = "Paid",  Width = 110, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "C2" } });
        _initGrid.CellDoubleClick += OnInitiationFeeDoubleClick;

        leftBorder.Controls.Add(_initGrid);       // Fill — add first
        leftBorder.Controls.Add(leftHeader);      // Top  — add last
        _initSplit.Panel1.Controls.Add(leftBorder);

        // Right bordered section: missing panel
        var rightBorder = MakeBorderBox();
        BuildMissingSection(rightBorder, "Add Selected to Initiation Fees",
            AddSelectedInitiationFees, out _lstInitMissing, out _lblInitMissingCount, out _);
        _initSplit.Panel2.Controls.Add(rightBorder);

        _initSplit.Panel2Collapsed = true;

        // Add controls: Fill first, then Bottom controls last
        page.Controls.Add(_initSplit);   // [0] Fill
        page.Controls.Add(separator);   // [1] Bottom — 1px separator
        page.Controls.Add(filterBar);   // [2] Bottom — filter bar (outermost = lowest dock)
        return page;
    }

    // ── Seasonal Fees Tab ─────────────────────────────────────────────────────

    private TabPage BuildSeasonalTab()
    {
        var page = new TabPage("  Seasonal Fees  ") { BackColor = AppTheme.ContentBackground };

        var filterBar = BuildSeasonFilterBar();
        var separator = MakeSeparator();

        _seasonSplit = MakeSplit();
        _seasonSplit.Panel1.BackColor = AppTheme.ContentBackground;
        _seasonSplit.Panel2.BackColor = AppTheme.ContentBackground;
        _seasonSplit.Panel1.Padding = new Padding(4, 4, 2, 4);
        _seasonSplit.Panel2.Padding = new Padding(2, 4, 4, 4);

        // Left bordered section
        var leftBorder = MakeBorderBox();
        var leftHeader = BuildSectionHeader("SEASONAL FEES — OWING / PAID", out _btnSeasonChevron,
            out _btnSeasonPaid, out _btnSeasonUnpaid);
        _btnSeasonChevron.Click  += (_, _) => ToggleMissingPanel(_seasonSplit, _btnSeasonChevron);
        _btnSeasonPaid.Click     += (_, _) => MarkSelectedSeasonalFees(asPaid: true);
        _btnSeasonUnpaid.Click   += (_, _) => MarkSelectedSeasonalFees(asPaid: false);

        _seasonGrid = MakeGrid();
        _seasonGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SeasonFeeId",   Visible = false });
        _seasonGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SeasonFeeName", HeaderText = "Player",  AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        _seasonGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SeasonFeeOwing", HeaderText = "Owing", Width = 110, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "C2" } });
        _seasonGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SeasonFeePaid",  HeaderText = "Paid",  Width = 110, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "C2" } });
        _seasonGrid.CellDoubleClick += OnSeasonalFeeDoubleClick;

        leftBorder.Controls.Add(_seasonGrid);     // Fill — add first
        leftBorder.Controls.Add(leftHeader);      // Top  — add last
        _seasonSplit.Panel1.Controls.Add(leftBorder);

        // Right bordered section: missing panel
        var rightBorder = MakeBorderBox();
        BuildMissingSection(rightBorder, "Add Selected to Seasonal Fees",
            AddSelectedSeasonalFees, out _lstSeasonMissing, out _lblSeasonMissingCount, out _btnAddSeasonalFees);
        _seasonSplit.Panel2.Controls.Add(rightBorder);

        _seasonSplit.Panel2Collapsed = true;

        page.Controls.Add(_seasonSplit);  // [0] Fill
        page.Controls.Add(separator);    // [1] Bottom — 1px separator
        page.Controls.Add(filterBar);    // [2] Bottom
        return page;
    }

    // ── Filter Bar Builders ───────────────────────────────────────────────────

    private Panel BuildInitFilterBar()
    {
        var bar = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = FilterBarHeight,
            BackColor = Color.FromArgb(235, 235, 235)
        };

        // Row 1 (y=8): Player search spanning full width
        var searchLbl = MakeLabel("Search:", 8, 14);
        bar.Controls.Add(searchLbl);

        _txtInitSearch = new SearchBoxControl("Search players…")
        {
            Location = new Point(8 + searchLbl.PreferredWidth + 4, 8),
            Height   = 28,
            Anchor   = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
        };
        _txtInitSearch.Width = bar.ClientSize.Width - _txtInitSearch.Left - 8;
        bar.SizeChanged += (_, _) => _txtInitSearch.Width = bar.ClientSize.Width - _txtInitSearch.Left - 8;
        _txtInitSearch.SearchTextChanged += (_, _) => ApplyInitFilter();
        bar.Controls.Add(_txtInitSearch);

        // Row 2 (y=46): Paid filter | On Team | On Spare List
        int x = 8, y2 = 46;
        var filterLbl = MakeLabel("Filter:", x, y2 + 6);
        bar.Controls.Add(filterLbl);
        x += filterLbl.PreferredWidth + 4;

        _cmbInitFilter = MakeCombo(x, y2, 110, ["All", "Paid", "Unpaid"]);
        _cmbInitFilter.SelectedIndexChanged += (_, _) => ApplyInitFilter();
        bar.Controls.Add(_cmbInitFilter);
        x += 118;

        var onTeamLbl = MakeLabel("On Team:", x, y2 + 6);
        bar.Controls.Add(onTeamLbl);
        x += onTeamLbl.PreferredWidth + 4;

        _cmbOnTeamFilter = MakeCombo(x, y2, 80, ["All", "Yes", "No"]);
        _cmbOnTeamFilter.SelectedIndexChanged += (_, _) => ApplyInitFilter();
        bar.Controls.Add(_cmbOnTeamFilter);
        x += 88;

        var onSpareLbl = MakeLabel("On Spare List:", x, y2 + 6);
        bar.Controls.Add(onSpareLbl);
        x += onSpareLbl.PreferredWidth + 4;

        _cmbOnSpareListFilter = MakeCombo(x, y2, 80, ["All", "Yes", "No"]);
        _cmbOnSpareListFilter.SelectedIndexChanged += (_, _) => ApplyInitFilter();
        bar.Controls.Add(_cmbOnSpareListFilter);

        return bar;
    }

    private Panel BuildSeasonFilterBar()
    {
        var bar = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = FilterBarHeight,
            BackColor = Color.FromArgb(235, 235, 235)
        };

        // Row 1 (y=8): Player search
        int x = 8, y1 = 8;
        var searchLbl = MakeLabel("Search:", x, y1 + 6);
        bar.Controls.Add(searchLbl);
        x += searchLbl.PreferredWidth + 4;

        _txtSeasonSearch = new SearchBoxControl("Search players…")
        {
            Location = new Point(x, y1),
            Height   = 28,
            Anchor   = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
        };
        _txtSeasonSearch.Width = bar.ClientSize.Width - x - 8;
        bar.SizeChanged += (_, _) => _txtSeasonSearch.Width = bar.ClientSize.Width - _txtSeasonSearch.Left - 8;
        _txtSeasonSearch.SearchTextChanged += (_, _) => ApplySeasonFilter();
        bar.Controls.Add(_txtSeasonSearch);

        // Row 2 (y=46): Division | Team | Filter
        x = 8;
        int y2 = 46;
        var divLbl = MakeLabel("Division:", x, y2 + 6);
        bar.Controls.Add(divLbl);
        x += divLbl.PreferredWidth + 4;

        _cmbDivision = MakeCombo(x, y2, 160, []);
        _cmbDivision.SelectedIndexChanged += (_, _) =>
        {
            if (_loading) return;
            LoadTeams((_cmbDivision.SelectedItem as DivItem)?.Id);
            ApplySeasonFilter();
        };
        bar.Controls.Add(_cmbDivision);
        x += 168;

        var teamLbl = MakeLabel("Team:", x, y2 + 6);
        bar.Controls.Add(teamLbl);
        x += teamLbl.PreferredWidth + 4;

        _cmbTeam = MakeCombo(x, y2, 160, []);
        _cmbTeam.SelectedIndexChanged += (_, _) => { if (!_loading) ApplySeasonFilter(); };
        bar.Controls.Add(_cmbTeam);
        x += 168;

        var filterLbl = MakeLabel("Filter:", x, y2 + 6);
        bar.Controls.Add(filterLbl);
        x += filterLbl.PreferredWidth + 4;

        _cmbSeasonFilter = MakeCombo(x, y2, 130, ["All", "Paid", "Unpaid"]);
        _cmbSeasonFilter.SelectedIndexChanged += (_, _) => { if (!_loading) ApplySeasonFilter(); };
        bar.Controls.Add(_cmbSeasonFilter);

        return bar;
    }

    // ── Section Header ────────────────────────────────────────────────────────

    private static Panel BuildSectionHeader(string title, out Button chevron,
        out Button btnMarkPaid, out Button btnMarkUnpaid)
    {
        var header = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 36,
            BackColor = AppTheme.Surface
        };

        var lbl = new Label
        {
            Text      = title,
            Font      = AppTheme.FontDefaultBold,
            ForeColor = AppTheme.TextPrimary,
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(10, 0, 0, 0)
        };

        var btnChevron = new Button
        {
            Text      = "▶",
            Width     = 30,
            Dock      = DockStyle.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextMuted,
            Font      = new Font("Segoe UI", 9f),
            Cursor    = Cursors.Hand,
            FlatAppearance = { BorderSize = 0 }
        };
        chevron = btnChevron;

        var spacer = new Panel { Width = 8, Dock = DockStyle.Right, BackColor = AppTheme.Surface };

        var btnUnpaid = new Button
        {
            Text      = "✗ Unpaid",
            Width     = 82,
            Dock      = DockStyle.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextSecondary,
            Font      = AppTheme.FontDefault,
            Cursor    = Cursors.Hand,
            FlatAppearance = { BorderSize = 1, BorderColor = AppTheme.Separator }
        };
        btnMarkUnpaid = btnUnpaid;

        var btnPaid = new Button
        {
            Text      = "✓ Paid",
            Width     = 72,
            Dock      = DockStyle.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.Surface,
            ForeColor = Color.FromArgb(34, 139, 34),
            Font      = AppTheme.FontDefault,
            Cursor    = Cursors.Hand,
            FlatAppearance = { BorderSize = 1, BorderColor = AppTheme.Separator }
        };
        btnMarkPaid = btnPaid;

        // Add order: Fill first; Rights added later = processed first = rightmost
        header.Controls.Add(lbl);       // [0] Fill
        header.Controls.Add(btnPaid);   // [1] Right — leftmost of the action buttons
        header.Controls.Add(btnUnpaid); // [2] Right
        header.Controls.Add(spacer);    // [3] Right — gap before chevron
        header.Controls.Add(btnChevron);// [4] Right — rightmost
        return header;
    }

    // ── Missing Section ───────────────────────────────────────────────────────

    private static void BuildMissingSection(
        Panel parent,
        string addBtnText,
        EventHandler addHandler,
        out ListBox list,
        out Label countLabel,
        out Button addButton)
    {
        var header = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 36,
            BackColor = AppTheme.Surface
        };
        var lbl = new Label
        {
            Text      = "MISSING — 0",
            Font      = AppTheme.FontDefaultBold,
            ForeColor = AppTheme.TextMuted,
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(10, 0, 0, 0)
        };
        countLabel = lbl;
        header.Controls.Add(lbl);

        var toolbar = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 86,
            BackColor = AppTheme.Surface,
            Padding   = new Padding(8)
        };

        var btnSelectAll = new Button
        {
            Text      = "Select All",
            Location  = new Point(8, 8),
            Size      = new Size(104, 28),
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            Font      = AppTheme.FontButton,
            Cursor    = Cursors.Hand,
            FlatAppearance = { BorderSize = 1, BorderColor = AppTheme.Separator }
        };

        var btnAdd = new Button
        {
            Text      = addBtnText,
            Location  = new Point(8, 46),
            Size      = new Size(toolbar.ClientSize.Width - 16, 30),
            Anchor    = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.Accent,
            ForeColor = Color.White,
            Font      = AppTheme.FontButton,
            Cursor    = Cursors.Hand,
            FlatAppearance = { BorderSize = 0 }
        };
        btnAdd.Click += addHandler;
        addButton = btnAdd;
        toolbar.Controls.Add(btnSelectAll);
        toolbar.Controls.Add(btnAdd);

        var lb = new ListBox
        {
            Dock           = DockStyle.Fill,
            SelectionMode  = SelectionMode.MultiExtended,
            Font           = AppTheme.FontDefault,
            BackColor      = AppTheme.ContentBackground,
            ForeColor      = AppTheme.TextPrimary,
            BorderStyle    = BorderStyle.None,
            IntegralHeight = false
        };
        list = lb;

        btnSelectAll.Click += (_, _) =>
        {
            lb.BeginUpdate();
            for (int i = 0; i < lb.Items.Count; i++) lb.SetSelected(i, true);
            lb.EndUpdate();
        };

        parent.Controls.Add(lb);       // Fill — add first
        parent.Controls.Add(toolbar);  // Bottom
        parent.Controls.Add(header);   // Top — add last
    }

    // ── Shared Control Factories ──────────────────────────────────────────────

    private static Panel MakeBorderBox()
        => new Panel { Dock = DockStyle.Fill, BackColor = Color.Black, Padding = new Padding(1) };

    private static Panel MakeSeparator()
        => new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.Black };

    private static SplitContainer MakeSplit()
    {
        var sc = new SplitContainer
        {
            Dock          = DockStyle.Fill,
            Orientation   = Orientation.Vertical,
            BackColor     = AppTheme.ContentBackground,
            SplitterWidth = 4
        };
        sc.Panel1.BackColor = AppTheme.ContentBackground;
        sc.Panel2.BackColor = AppTheme.ContentBackground;
        return sc;
    }

    private static DataGridView MakeGrid()
    {
        var grid = new DataGridView
        {
            Dock                  = DockStyle.Fill,
            BackgroundColor       = AppTheme.ContentBackground,
            GridColor             = AppTheme.Separator,
            BorderStyle           = BorderStyle.None,
            RowHeadersVisible     = false,
            AllowUserToAddRows    = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect           = true,
            ReadOnly              = true,
            AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.None,
            ColumnHeadersHeight   = 32,
            RowTemplate           = { Height = 30 }
        };
        grid.ColumnHeadersDefaultCellStyle.Font      = AppTheme.FontDefaultBold;
        grid.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.Surface;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = AppTheme.TextPrimary;
        grid.DefaultCellStyle.Font             = AppTheme.FontDefault;
        grid.DefaultCellStyle.BackColor        = AppTheme.ContentBackground;
        grid.DefaultCellStyle.ForeColor        = AppTheme.TextPrimary;
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(210, 225, 242);
        grid.DefaultCellStyle.SelectionForeColor = AppTheme.TextPrimary;
        grid.AlternatingRowsDefaultCellStyle.BackColor        = AppTheme.Surface;
        grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(200, 215, 232);
        grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = AppTheme.TextPrimary;
        grid.EnableHeadersVisualStyles = false;
        return grid;
    }

    private static Label MakeLabel(string text, int x, int y)
        => new Label
        {
            Text      = text,
            Font      = AppTheme.FontDefault,
            ForeColor = AppTheme.TextSecondary,
            AutoSize  = true,
            Location  = new Point(x, y)
        };

    private static ComboBox MakeCombo(int x, int y, int width, string[] items)
    {
        var c = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font          = AppTheme.FontDefault,
            Location      = new Point(x, y),
            Width         = width
        };
        foreach (var item in items) c.Items.Add(item);
        if (c.Items.Count > 0) c.SelectedIndex = 0;
        return c;
    }

    // ── Bulk Mark Paid / Unpaid ───────────────────────────────────────────────

    private void MarkSelectedInitiationFees(bool asPaid)
    {
        var ids = _initGrid.SelectedRows.Cast<DataGridViewRow>()
            .Where(r => r.Cells["InitId"].Value is int)
            .Select(r => (int)r.Cells["InitId"].Value)
            .ToList();
        if (ids.Count == 0) return;

        try
        {
            using var db = new BocceDbContext();
            var fees  = db.InitiationFees.Where(f => ids.Contains(f.Id)).ToList();
            var today = DateOnly.FromDateTime(DateTime.Today);
            foreach (var fee in fees)
            {
                fee.AmountPaid = asPaid ? fee.AmountOwing : 0m;
                fee.PaidDate   = asPaid ? today : null;
            }
            db.SaveChanges();
        }
        catch (Exception ex) { MessageBox.Show("Could not update fees: " + ex.Message, "Fees", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }

        foreach (var id in ids)
        {
            var idx = _allInitFees.FindIndex(r => r.Id == id);
            if (idx >= 0)
            {
                decimal newPaid = asPaid ? _allInitFees[idx].Owing : 0m;
                _allInitFees[idx] = _allInitFees[idx] with { Paid = newPaid };
            }
        }
        ApplyInitFilter();
    }

    private void MarkSelectedSeasonalFees(bool asPaid)
    {
        if (_seasonIsLocked) return;
        var ids = _seasonGrid.SelectedRows.Cast<DataGridViewRow>()
            .Where(r => r.Cells["SeasonFeeId"].Value is int)
            .Select(r => (int)r.Cells["SeasonFeeId"].Value)
            .ToList();
        if (ids.Count == 0) return;

        try
        {
            using var db = new BocceDbContext();
            var fees  = db.SeasonFees.Where(f => ids.Contains(f.Id)).ToList();
            var today = DateOnly.FromDateTime(DateTime.Today);
            foreach (var fee in fees)
            {
                fee.AmountPaid = asPaid ? fee.AmountOwing : 0m;
                fee.PaidDate   = asPaid ? today : null;
            }
            db.SaveChanges();
        }
        catch (Exception ex) { MessageBox.Show("Could not update fees: " + ex.Message, "Fees", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }

        foreach (var id in ids)
        {
            var idx = _allSeasonFees.FindIndex(r => r.Id == id);
            if (idx >= 0)
            {
                decimal newPaid = asPaid ? _allSeasonFees[idx].Owing : 0m;
                _allSeasonFees[idx] = _allSeasonFees[idx] with { Paid = newPaid };
            }
        }
        ApplySeasonFilter();
    }

    // ── Missing Panel Toggle ──────────────────────────────────────────────────

    private static void ToggleMissingPanel(SplitContainer split, Button chevron)
    {
        if (split.Panel2Collapsed) ExpandMissingPanel(split, chevron);
        else CollapseMissingPanel(split, chevron);
    }

    private static void ExpandMissingPanel(SplitContainer split, Button chevron)
    {
        split.Panel2Collapsed = false;
        int w = split.ClientSize.Width;
        if (w > 4)
        {
            int pos = Math.Max(200, w - MissingWidth - split.SplitterWidth);
            if (split.SplitterDistance != pos) split.SplitterDistance = pos;
        }
        chevron.Text = "◀";
    }

    private static void CollapseMissingPanel(SplitContainer split, Button chevron)
    {
        split.Panel2Collapsed = true;
        chevron.Text = "▶";
    }

    private static void AutoSetMissingVisibility(SplitContainer split, Button chevron, int count)
    {
        if (count > 0 && split.Panel2Collapsed)  ExpandMissingPanel(split, chevron);
        else if (count == 0 && !split.Panel2Collapsed) CollapseMissingPanel(split, chevron);
    }

    // ── Data Loading ──────────────────────────────────────────────────────────

    private void Reload()
    {
        using (var db = new BocceDbContext())
        {
            _currentSeasonId = AppParameterService.GetDefaultSeasonId(db);
            _seasonIsLocked  = _currentSeasonId.HasValue
                && (db.Seasons.Find(_currentSeasonId.Value)?.IsLocked ?? false);
        }

        _btnSeasonPaid.Enabled       = !_seasonIsLocked;
        _btnSeasonUnpaid.Enabled     = !_seasonIsLocked;
        _btnAddSeasonalFees.Enabled  = !_seasonIsLocked;

        LoadInitiationFees();
        LoadMissingInitiationFees();
        LoadSeasonalFees();
        LoadDivisions();
        LoadMissingSeasonalFees();
    }

    private void LoadDivisions()
    {
        _loading = true;
        try
        {
            _cmbDivision.Items.Clear();
            _cmbDivision.Items.Add(new DivItem { Id = null, Name = "All" });

            if (_currentSeasonId.HasValue)
            {
                using var db = new BocceDbContext();
                var divs = db.Divisions
                    .Where(d => d.SeasonId == _currentSeasonId.Value)
                    .OrderBy(d => d.SortName)
                    .Select(d => new DivItem { Id = d.Id, Name = d.Name })
                    .ToList();
                foreach (var d in divs) _cmbDivision.Items.Add(d);
            }
            _cmbDivision.SelectedIndex = 0;
        }
        finally { _loading = false; }

        LoadTeams(null);
    }

    private void LoadTeams(int? divisionId)
    {
        _loading = true;
        try
        {
            _cmbTeam.Items.Clear();
            _cmbTeam.Items.Add(new TeamItm { Id = null, Name = "All" });

            if (divisionId.HasValue)
            {
                using var db = new BocceDbContext();
                var teams = db.Teams
                    .Where(t => t.DivisionId == divisionId.Value)
                    .OrderBy(t => t.SortOrder)
                    .Select(t => new TeamItm { Id = t.Id, Name = t.DisplayName ?? t.SystemName })
                    .ToList();
                foreach (var t in teams) _cmbTeam.Items.Add(t);
            }
            _cmbTeam.SelectedIndex = 0;
            _cmbTeam.Enabled = divisionId.HasValue;
        }
        finally { _loading = false; }
    }

    private void LoadInitiationFees()
    {
        _allInitFees = [];
        try
        {
            using var db = new BocceDbContext();

            HashSet<int> onTeamIds  = [];
            HashSet<int> onSpareIds = [];

            if (_currentSeasonId.HasValue)
            {
                onTeamIds = db.TeamPlayers
                    .Where(tp => tp.Team.Division.SeasonId == _currentSeasonId.Value)
                    .Select(tp => tp.PlayerId)
                    .ToHashSet();

                var leagueId = AppParameterService.GetDefaultLeagueId(db);
                if (leagueId.HasValue)
                    onSpareIds = db.SpareLists
                        .Where(s => s.LeagueId == leagueId.Value && s.IsActive)
                        .Select(s => s.PlayerId)
                        .ToHashSet();
            }

            var fees = db.InitiationFees
                .Include(f => f.Player)
                .OrderBy(f => f.Player.LastName).ThenBy(f => f.Player.FirstName)
                .Select(f => new { f.Id, f.PlayerId, Name = f.Player.LastName + ", " + f.Player.FirstName, f.AmountOwing, f.AmountPaid })
                .ToList();

            _allInitFees = fees
                .Select(f => new InitRow(f.Id, f.Name, f.AmountOwing, f.AmountPaid,
                    onTeamIds.Contains(f.PlayerId),
                    onSpareIds.Contains(f.PlayerId)))
                .ToList();
        }
        catch { }
        ApplyInitFilter();
    }

    private void ApplyInitFilter()
    {
        var filterStr = _cmbInitFilter.SelectedItem?.ToString() ?? "All";
        var search    = _txtInitSearch?.SearchText ?? "";

        _initGrid.Rows.Clear();

        var onTeamStr  = _cmbOnTeamFilter?.SelectedItem?.ToString()      ?? "All";
        var onSpareStr = _cmbOnSpareListFilter?.SelectedItem?.ToString() ?? "All";

        var rows = _allInitFees
            .Where(r => filterStr switch
            {
                "Paid"   => r.Paid > 0 && r.Paid >= r.Owing,
                "Unpaid" => r.Paid == 0 || r.Paid < r.Owing,
                _        => true
            })
            .Where(r => SearchQueryService.MatchesAnyTerm(r.Name, search))
            .Where(r => onTeamStr switch { "Yes" => r.IsOnTeam,     "No" => !r.IsOnTeam,     _ => true })
            .Where(r => onSpareStr switch { "Yes" => r.IsOnSpareList, "No" => !r.IsOnSpareList, _ => true });

        foreach (var r in rows)
        {
            int i   = _initGrid.Rows.Add();
            var row = _initGrid.Rows[i];
            row.Cells["InitId"].Value    = r.Id;
            row.Cells["InitName"].Value  = r.Name;
            row.Cells["InitOwing"].Value = r.Owing;
            row.Cells["InitPaid"].Value  = r.Paid;
            ApplyPaidStyle(row, r.Paid, r.Owing);
        }
    }

    private void LoadMissingInitiationFees()
    {
        _lstInitMissing.Items.Clear();
        try
        {
            using var db = new BocceDbContext();
            var existingIds = db.InitiationFees.Select(f => f.PlayerId).ToHashSet();
            foreach (var p in db.Players
                .Where(p => p.IsActive && !existingIds.Contains(p.Id))
                .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
                .Select(p => new PlayerItem { Id = p.Id, Name = p.LastName + ", " + p.FirstName })
                .ToList())
                _lstInitMissing.Items.Add(p);
        }
        catch { }

        int count = _lstInitMissing.Items.Count;
        _lblInitMissingCount.Text = $"MISSING — {count}";
        AutoSetMissingVisibility(_initSplit, _btnInitChevron, count);
    }

    private void LoadSeasonalFees()
    {
        _allSeasonFees = [];

        if (!_currentSeasonId.HasValue) { ApplySeasonFilter(); return; }

        try
        {
            using var db = new BocceDbContext();

            var fees = db.SeasonFees
                .Include(f => f.Player)
                .Where(f => f.SeasonId == _currentSeasonId.Value)
                .OrderBy(f => f.Player.LastName).ThenBy(f => f.Player.FirstName)
                .Select(f => new { f.Id, Name = f.Player.LastName + ", " + f.Player.FirstName, f.AmountOwing, f.AmountPaid, f.PlayerId })
                .ToList();

            var memberships = db.TeamPlayers
                .Where(tp => tp.Team.Division.SeasonId == _currentSeasonId.Value)
                .Select(tp => new { tp.PlayerId, DivId = tp.Team.DivisionId, tp.TeamId })
                .ToList();

            _allSeasonFees = fees.Select(f =>
            {
                var m = memberships.Where(x => x.PlayerId == f.PlayerId).ToList();
                return new SeasonRow(f.Id, f.Name, f.AmountOwing, f.AmountPaid,
                    m.Select(x => x.DivId).ToHashSet(),
                    m.Select(x => x.TeamId).ToHashSet());
            }).ToList();
        }
        catch { }

        ApplySeasonFilter();
    }

    private void ApplySeasonFilter()
    {
        var filterStr  = _cmbSeasonFilter?.SelectedItem?.ToString() ?? "All";
        var search     = _txtSeasonSearch?.SearchText ?? "";
        var divId      = (_cmbDivision?.SelectedItem as DivItem)?.Id;
        var teamId     = (_cmbTeam?.SelectedItem as TeamItm)?.Id;

        _seasonGrid.Rows.Clear();

        var rows = _allSeasonFees
            .Where(r => filterStr switch
            {
                "Paid"   => r.Paid > 0 && r.Paid >= r.Owing,
                "Unpaid" => r.Paid == 0 || r.Paid < r.Owing,
                _        => true
            })
            .Where(r => SearchQueryService.MatchesAnyTerm(r.Name, search))
            .Where(r => divId  == null || r.DivisionIds.Contains(divId.Value))
            .Where(r => teamId == null || r.TeamIds.Contains(teamId.Value));

        foreach (var r in rows)
        {
            int i   = _seasonGrid.Rows.Add();
            var row = _seasonGrid.Rows[i];
            row.Cells["SeasonFeeId"].Value    = r.Id;
            row.Cells["SeasonFeeName"].Value  = r.Name;
            row.Cells["SeasonFeeOwing"].Value = r.Owing;
            row.Cells["SeasonFeePaid"].Value  = r.Paid;
            ApplyPaidStyle(row, r.Paid, r.Owing);
        }
    }

    private void LoadMissingSeasonalFees()
    {
        _lstSeasonMissing.Items.Clear();

        if (!_currentSeasonId.HasValue)
        {
            _lblSeasonMissingCount.Text = "MISSING — 0";
            AutoSetMissingVisibility(_seasonSplit, _btnSeasonChevron, 0);
            return;
        }

        try
        {
            using var db = new BocceDbContext();
            var leagueId = AppParameterService.GetDefaultLeagueId(db);
            if (!leagueId.HasValue) return;

            var existingIds = db.SeasonFees
                .Where(f => f.SeasonId == _currentSeasonId.Value).Select(f => f.PlayerId).ToHashSet();

            var onTeamIds = db.TeamPlayers
                .Where(tp => tp.Team.Division.SeasonId == _currentSeasonId.Value).Select(tp => tp.PlayerId).ToHashSet();

            var onSpareIds = db.SpareLists
                .Where(s => s.LeagueId == leagueId.Value && s.IsActive).Select(s => s.PlayerId).ToHashSet();

            var eligibleIds = onTeamIds.Union(onSpareIds).ToHashSet();

            foreach (var p in db.Players
                .Where(p => p.IsActive && eligibleIds.Contains(p.Id) && !existingIds.Contains(p.Id))
                .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
                .Select(p => new PlayerItem { Id = p.Id, Name = p.LastName + ", " + p.FirstName })
                .ToList())
                _lstSeasonMissing.Items.Add(p);
        }
        catch { }

        int count = _lstSeasonMissing.Items.Count;
        _lblSeasonMissingCount.Text = $"MISSING — {count}";
        AutoSetMissingVisibility(_seasonSplit, _btnSeasonChevron, count);
    }

    // ── Add from Missing ──────────────────────────────────────────────────────

    private void AddSelectedInitiationFees(object? sender, EventArgs e)
    {
        var sel = _lstInitMissing.SelectedItems.Cast<PlayerItem>().ToList();
        if (sel.Count == 0) { MessageBox.Show("Select at least one player.", "Fees", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        try
        {
            using var db = new BocceDbContext();
            foreach (var p in sel) FeeService.EnsureInitiationFee(db, p.Id);
        }
        catch (Exception ex) { MessageBox.Show("Error: " + ex.Message, "Fees", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        LoadInitiationFees();
        LoadMissingInitiationFees();
    }

    private void AddSelectedSeasonalFees(object? sender, EventArgs e)
    {
        if (_seasonIsLocked) return;
        var sel = _lstSeasonMissing.SelectedItems.Cast<PlayerItem>().ToList();
        if (sel.Count == 0) { MessageBox.Show("Select at least one player.", "Fees", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        if (!_currentSeasonId.HasValue) return;
        try
        {
            using var db = new BocceDbContext();
            foreach (var p in sel) FeeService.EnsureSeasonFee(db, p.Id, _currentSeasonId.Value);
        }
        catch (Exception ex) { MessageBox.Show("Error: " + ex.Message, "Fees", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        LoadSeasonalFees();
        LoadMissingSeasonalFees();
    }

    // ── Toggle Paid / Unpaid (double-click) ──────────────────────────────────

    private void OnInitiationFeeDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        var row = _initGrid.Rows[e.RowIndex];
        if (row.Cells["InitId"].Value is not int feeId) return;
        if (row.Cells["InitOwing"].Value is not decimal owing) return;
        if (row.Cells["InitPaid"].Value is not decimal paid) return;

        bool isNowPaid    = paid > 0 && paid >= owing;
        decimal newPaid   = isNowPaid ? 0m : owing;
        DateOnly? newDate = isNowPaid ? null : DateOnly.FromDateTime(DateTime.Today);

        try
        {
            using var db = new BocceDbContext();
            var fee = db.InitiationFees.Find(feeId);
            if (fee == null) return;
            fee.AmountPaid = newPaid; fee.PaidDate = newDate;
            db.SaveChanges();
        }
        catch (Exception ex) { MessageBox.Show("Could not update fee: " + ex.Message, "Fees", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }

        var idx = _allInitFees.FindIndex(r => r.Id == feeId);
        if (idx >= 0) _allInitFees[idx] = _allInitFees[idx] with { Paid = newPaid };
        ApplyInitFilter();
    }

    private void OnSeasonalFeeDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (_seasonIsLocked) return;
        if (e.RowIndex < 0) return;
        var row = _seasonGrid.Rows[e.RowIndex];
        if (row.Cells["SeasonFeeId"].Value is not int feeId) return;
        if (row.Cells["SeasonFeeOwing"].Value is not decimal owing) return;
        if (row.Cells["SeasonFeePaid"].Value is not decimal paid) return;

        bool isNowPaid    = paid > 0 && paid >= owing;
        decimal newPaid   = isNowPaid ? 0m : owing;
        DateOnly? newDate = isNowPaid ? null : DateOnly.FromDateTime(DateTime.Today);

        try
        {
            using var db = new BocceDbContext();
            var fee = db.SeasonFees.Find(feeId);
            if (fee == null) return;
            fee.AmountPaid = newPaid; fee.PaidDate = newDate;
            db.SaveChanges();
        }
        catch (Exception ex) { MessageBox.Show("Could not update fee: " + ex.Message, "Fees", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }

        var idx = _allSeasonFees.FindIndex(r => r.Id == feeId);
        if (idx >= 0) _allSeasonFees[idx] = _allSeasonFees[idx] with { Paid = newPaid };
        ApplySeasonFilter();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void ApplyPaidStyle(DataGridViewRow row, decimal paid, decimal owing)
    {
        var color = paid > 0 && paid >= owing ? Color.FromArgb(34, 139, 34) : AppTheme.TextPrimary;
        row.DefaultCellStyle.ForeColor          = color;
        row.DefaultCellStyle.SelectionForeColor = color;
    }

}

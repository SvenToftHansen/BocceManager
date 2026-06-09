using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.Services;
using BocceManager.UI.Controls;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Panels;

public class DivisionPanel : UserControl
{
    private bool _isLoadingData = false;
    private bool _isDirty = false;
    private bool _isCreatingNew = false;

    // â”€â”€ State â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    private int? _selectedLeagueId;
    private int? _selectedSeasonId;
    private int? _selectedDivisionId;
    private int? _currentTeamId;

    // â”€â”€ Header â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    // ── Left panel ────────────────────────────────────────────────────────────
    private TextBox  _txtSearch    = null!;
    private ListBox  _lstDivisions = null!;
    private List<(int Id, string Display)> _allDivisions = [];

    private TabControl _tabs = null!;

    // â”€â”€ Editor tab â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    private Label         _lblName       = null!;
    private Label         _lblSystemName = null!;
    private Label         _lblSortKey    = null!;
    private ComboBox      _cmbDay        = null!;
    private ComboBox      _cmbTime       = null!;
    private CheckBox      _chkActive     = null!;
    private Label         _lblCreated    = null!;

    // â”€â”€ Parameters tab â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    private ThemedNumericUpDown _numTeamsInDiv  = null!;
    private ThemedNumericUpDown _numPlayersMin  = null!;
    private ThemedNumericUpDown _numPlayersMax  = null!;

    // â”€â”€ Teams tab â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    private DataGridView _teamsGrid      = null!;
    private DataGridView _playersGrid    = null!;
    private Label        _lblTeamTitle   = null!;
    private ComboBox     _cmbCaptain     = null!;
    private Button       _btnAddTeam     = null!;
    private Button       _btnBuildTeams  = null!;
    private Button       _btnDeleteTeam  = null!;
    private Button       _btnDeleteAllTeams = null!;
    private Button       _btnAddPlayer   = null!;
    private Button       _btnRemovePlayer = null!;
    private Button       _btnDeleteAllPlayers = null!;

    // â”€â”€ Shared toolbar â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    private Button _btnAdd    = null!;
    private Button _btnSave   = null!;
    private Button _btnCancel = null!;
    private Button _btnDelete = null!;

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public DivisionPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;
        BuildUI();
        AppParameterService.DefaultsChanged += OnDefaultsChanged;
        LoadContext();
    }

    private void OnDefaultsChanged(object? sender, DefaultsChangedEventArgs e)
    {
        LoadContext();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            AppParameterService.DefaultsChanged -= OnDefaultsChanged;
        base.Dispose(disposing);
    }

    public void SelectDivision(int divisionId)
    {
        try
        {
            using var db = new BocceDbContext();
            var d = db.Divisions.Find(divisionId);
            if (d == null) return;
            var s = db.Seasons.Find(d.SeasonId);
            if (s == null) return;

            var currentLeague = AppParameterService.GetDefaultLeagueId(db);
            var currentSeason = AppParameterService.GetDefaultSeasonId(db);
            if (currentLeague != s.LeagueId)
                AppParameterService.SetDefaultLeagueId(db, s.LeagueId);
            if (currentSeason != d.SeasonId)
                AppParameterService.SetDefaultSeasonId(db, d.SeasonId);

            _selectedLeagueId = s.LeagueId;
            _selectedSeasonId = d.SeasonId;
            LoadDivisionList();
            SelectInList(divisionId);
        }
        catch { }
    }

    // â”€â”€ Build UI â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void BuildUI()
    {
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            Panel1MinSize = 0,
            Panel2MinSize = 0,
            BackColor = AppTheme.ContentBackground
        };

        void Apply()
        {
            if (split.Width <= 1) return;
            const int preferred = 220, desiredLeft = 180, desiredRight = 400;
            int maxTotal = Math.Max(0, split.Width - 1);
            int leftMin  = desiredLeft;
            int rightMin = desiredRight;
            if (leftMin + rightMin > maxTotal)
            {
                if (maxTotal == 0) { leftMin = 0; rightMin = 0; }
                else { double r = desiredLeft / (double)(desiredLeft + desiredRight); leftMin = (int)Math.Floor(maxTotal * r); rightMin = maxTotal - leftMin; }
            }
            split.Panel1MinSize   = leftMin;
            split.Panel2MinSize   = rightMin;
            int maxLeft = split.Width - rightMin;
            if (maxLeft < leftMin) maxLeft = leftMin;
            int dist = Math.Max(leftMin, Math.Min(preferred, maxLeft));
            split.FixedPanel      = FixedPanel.Panel1;
            split.IsSplitterFixed = true;
            if (dist > 0) split.SplitterDistance = dist;
        }

        split.SizeChanged   += (_, _) => Apply();
        split.HandleCreated += (_, _) => BeginInvoke(new Action(Apply));

        BuildLeftPanel(split.Panel1);
        BuildRightPanel(split.Panel2);
        Controls.Add(split);
    }

    private void BuildLeftPanel(SplitterPanel panel)
    {
        panel.BackColor = AppTheme.Surface;
        panel.Padding = new Padding(8, 8, 8, 8);

        var lblTitle = new Label
        {
            Dock = DockStyle.Top,
            Text = "Divisions",
            Font = AppTheme.FontSmallBold,
            ForeColor = AppTheme.TextPrimary,
            Height = 22,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _txtSearch = new TextBox
        {
            Dock = DockStyle.Top,
            Font = AppTheme.FontDefault,
            ForeColor = AppTheme.TextSecondary,
            BackColor = AppTheme.ContentBackground,
            Text = "Search...",
            Height = 28
        };
        _txtSearch.Enter += (_, _) => { if (_txtSearch.Text == "Search...") { _txtSearch.Text = ""; _txtSearch.ForeColor = AppTheme.TextPrimary; } };
        _txtSearch.Leave += (_, _) => { if (string.IsNullOrEmpty(_txtSearch.Text)) { _txtSearch.Text = "Search..."; _txtSearch.ForeColor = AppTheme.TextSecondary; } };
        _txtSearch.TextChanged += (_, _) => FilterDivisionList();

        _lstDivisions = new ListBox
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.FontDefault,
            BackColor = AppTheme.ContentBackground,
            ForeColor = AppTheme.TextPrimary,
            BorderStyle = BorderStyle.None,
            IntegralHeight = false
        };
        _lstDivisions.SelectedIndexChanged += OnListDivisionSelected;

        panel.Controls.Add(_lstDivisions);
        panel.Controls.Add(_txtSearch);
        panel.Controls.Add(lblTitle);
    }

    private void BuildRightPanel(SplitterPanel panel)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54f));

        var tabs    = BuildTabs();
        var toolbar = BuildSaveToolbar();
        toolbar.Dock = DockStyle.Fill;

        layout.Controls.Add(tabs,    0, 0);
        layout.Controls.Add(toolbar, 0, 1);
        panel.Controls.Add(layout);
    }

    private TabControl BuildTabs()
    {
        _tabs = new TabControl { Dock = DockStyle.Fill, Font = AppTheme.FontDefault, Padding = new Point(16, 6) };
        _tabs.TabPages.Add(BuildEditorTab());
        _tabs.TabPages.Add(BuildParametersTab());
        _tabs.TabPages.Add(BuildTeamsTab());
        return _tabs;
    }

    // â”€â”€ Editor Tab â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private TabPage BuildEditorTab()
    {
        var page   = new TabPage("  Editor  ") { BackColor = AppTheme.ContentBackground };
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = AppTheme.ContentBackground };

        const int lx = 20, ix = 210, iw = 420;
        int y = 20;
        var cc = new List<Control>();
        void Add(params Control[] items) => cc.AddRange(items);

        Add(Lbl("Name", lx, y));
        _lblName = new Label { Location = new Point(ix, y + 3), AutoSize = true, Font = AppTheme.FontDefaultBold, ForeColor = AppTheme.TextPrimary };
        Add(_lblName, Hint("Computed from Day Slot + Time Slot", ix + 220, y + 4)); y += 38;

        Add(Lbl("Day Slot", lx, y));
        _cmbDay = new ComboBox { Location = new Point(ix, y), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList, Font = AppTheme.FontDefault };
        _cmbDay.SelectedIndexChanged += OnSlotChanged;
        _cmbDay.SelectedIndexChanged += (_, _) => MarkDirty();
        Add(_cmbDay); y += 44;

        Add(Lbl("Time Slot", lx, y));
        _cmbTime = new ComboBox { Location = new Point(ix, y), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList, Font = AppTheme.FontDefault };
        _cmbTime.SelectedIndexChanged += OnSlotChanged;
        _cmbTime.SelectedIndexChanged += (_, _) => MarkDirty();
        Add(_cmbTime); y += 44;

        Add(Sep(lx, y, iw + ix - lx)); y += 10;
        Add(SecHdr("Computed Names  (update automatically when Day/Time changes)", lx, y)); y += 34;

        Add(Lbl("System Name", lx, y));
        _lblSystemName = new Label { Location = new Point(ix, y + 3), AutoSize = true, Font = AppTheme.FontDefaultBold, ForeColor = AppTheme.TextPrimary };
        Add(_lblSystemName, Hint("Mo-0900 format - used in team names", ix + 120, y + 4)); y += 38;

        Add(Lbl("Sort Key", lx, y));
        _lblSortKey = new Label { Location = new Point(ix, y + 3), AutoSize = true, Font = AppTheme.FontDefaultBold, ForeColor = AppTheme.TextPrimary };
        Add(_lblSortKey, Hint("1-0900 format - used for ordering", ix + 120, y + 4)); y += 38;

        Add(Sep(lx, y, iw + ix - lx)); y += 10;
        Add(SecHdr("Status", lx, y)); y += 34;

        Add(Lbl("Active", lx, y));
        _chkActive = new CheckBox { Location = new Point(ix, y), AutoSize = true, Font = AppTheme.FontDefault, ForeColor = AppTheme.TextPrimary, Checked = true };
        _chkActive.CheckedChanged += (_, _) => MarkDirty();
        Add(_chkActive); y += 38;

        Add(Lbl("Created", lx, y));
        _lblCreated = new Label { Location = new Point(ix, y + 3), AutoSize = true, Font = AppTheme.FontDefault, ForeColor = AppTheme.TextSecondary };
        Add(_lblCreated); y += 44;

        scroll.Controls.AddRange([.. cc]);
        page.Controls.Add(scroll);
        LoadSlotCombos();
        return page;
    }

    // â”€â”€ Parameters Tab â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private TabPage BuildParametersTab()
    {
        var page   = new TabPage("  Parameters  ") { BackColor = AppTheme.ContentBackground };
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = AppTheme.ContentBackground };

        const int lx = 20, ix = 220, iw = 400;
        int y = 20;
        var cc = new List<Control>();
        void Add(params Control[] items) => cc.AddRange(items);

        Add(SecHdr("Division Parameters", lx, y)); y += 34;

        Add(Lbl("Teams in Division", lx, y));
        _numTeamsInDiv = Num(ix, y, 0, 99);
        _numTeamsInDiv.ValueChanged += (_, _) => MarkDirty();
        Add(_numTeamsInDiv, Hint("Max/target teams for this division. 0 = inherit from season or league. Even preferred; odd = one bye per round.", ix + 100, y + 4)); y += 44;

        Add(Sep(lx, y, iw + ix - lx)); y += 10;
        Add(SecHdr("Player Limits  (override season values when set)", lx, y)); y += 34;

        Add(Lbl("Players / Team Min", lx, y));
        _numPlayersMin = Num(ix, y, 0, 99);
        _numPlayersMin.ValueChanged += (_, _) => MarkDirty();
        Add(_numPlayersMin, Hint("0 = use season default", ix + 100, y + 4)); y += 38;

        Add(Lbl("Players / Team Max", lx, y));
        _numPlayersMax = Num(ix, y, 0, 99);
        _numPlayersMax.ValueChanged += (_, _) => MarkDirty();
        Add(_numPlayersMax, Hint("0 = use season default", ix + 100, y + 4)); y += 38;

        scroll.Controls.AddRange([.. cc]);
        page.Controls.Add(scroll);
        return page;
    }

    // â”€â”€ Teams Tab â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private TabPage BuildTeamsTab()
    {
        var page = new TabPage("  Teams  ") { BackColor = AppTheme.ContentBackground };

        // â”€â”€ Teams grid â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        _teamsGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false,
            RowHeadersVisible = false, BorderStyle = BorderStyle.None,
            BackgroundColor = AppTheme.ContentBackground, GridColor = AppTheme.GridLines,
            Font = AppTheme.FontDefault,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor          = AppTheme.GridHeaderBackground, ForeColor          = AppTheme.GridHeaderText,
                SelectionBackColor = AppTheme.GridHeaderBackground, SelectionForeColor = AppTheme.GridHeaderText,
                Font = AppTheme.FontGridHeader, Padding = new Padding(4, 0, 0, 0)
            },
            EnableHeadersVisualStyles = false,
            RowTemplate = { Height = 30 },
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        _teamsGrid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = AppTheme.GridAlternateRow };

        _teamsGrid.Columns.Add(new DataGridViewTextBoxColumn  { Name = "TmId",      Visible = false,                                ReadOnly = true });
        _teamsGrid.Columns.Add(new DataGridViewTextBoxColumn  { Name = "TmLetter",  HeaderText = "Letter",       FillWeight = 7,  ReadOnly = true });
        _teamsGrid.Columns.Add(new DataGridViewTextBoxColumn  { Name = "TmSysName", HeaderText = "System Name",  FillWeight = 14, ReadOnly = true });
        _teamsGrid.Columns.Add(new DataGridViewTextBoxColumn  { Name = "TmDisplay", HeaderText = "Display Name", FillWeight = 20, ReadOnly = true });
        _teamsGrid.Columns.Add(new DataGridViewTextBoxColumn  { Name = "TmCaptain", HeaderText = "Captain",      FillWeight = 24, ReadOnly = true });
        _teamsGrid.Columns.Add(new DataGridViewTextBoxColumn  { Name = "TmPlayers", HeaderText = "Players",      FillWeight = 8,  ReadOnly = true });
        _teamsGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "TmActive",  HeaderText = "Active",       FillWeight = 7  });

        _teamsGrid.SelectionChanged   += OnTeamSelected;
        _teamsGrid.CellContentClick   += OnTeamCellClick;
        _teamsGrid.CellValueChanged   += OnTeamActiveChanged;

        // â”€â”€ Players sub-panel â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var playerPanel = new Panel { Dock = DockStyle.Bottom, Height = 220, BackColor = AppTheme.ContentBackground };

        _playersGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false,
            RowHeadersVisible = false, BorderStyle = BorderStyle.None,
            BackgroundColor = AppTheme.ContentBackground, GridColor = AppTheme.GridLines,
            Font = AppTheme.FontDefault, ReadOnly = true,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor          = AppTheme.GridHeaderBackground, ForeColor          = AppTheme.GridHeaderText,
                SelectionBackColor = AppTheme.GridHeaderBackground, SelectionForeColor = AppTheme.GridHeaderText,
                Font = AppTheme.FontGridHeader, Padding = new Padding(4, 0, 0, 0)
            },
            EnableHeadersVisualStyles = false,
            RowTemplate = { Height = 28 },
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        _playersGrid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = AppTheme.GridAlternateRow };
        _playersGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "PlId",   Visible = false });
        _playersGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "PlName", HeaderText = "Player",  FillWeight = 70 });
        _playersGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "PlRole", HeaderText = "Role",    FillWeight = 30 });

        var captainStrip = new Panel { Dock = DockStyle.Bottom, Height = 42, BackColor = AppTheme.Surface, Padding = new Padding(10, 8, 8, 6) };
        var captainLbl   = new Label  { Text = "Captain:", Left = 10, Top = 12, AutoSize = true, Font = AppTheme.FontDefaultBold, ForeColor = AppTheme.TextPrimary };
        _cmbCaptain = new ComboBox { Left = 85, Top = 8, Width = 260, DropDownStyle = ComboBoxStyle.DropDownList, Font = AppTheme.FontDefault, Enabled = false };
        _cmbCaptain.SelectedIndexChanged += OnCaptainChanged;
        captainStrip.Controls.AddRange([captainLbl, _cmbCaptain]);

        var playerBtns = new Panel { Dock = DockStyle.Right, Width = 148, BackColor = AppTheme.ContentBackground, Padding = new Padding(8) };
        _btnAddPlayer = new Button
        {
            Text = "Add Player", Location = new Point(8, 0), Size = new Size(132, 30),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.ButtonSuccess, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 }, Enabled = false
        };
        _btnAddPlayer.Click += (_, _) => AddPlayerToTeam();

        _btnRemovePlayer = new Button
        {
            Text = "Remove Player", Location = new Point(8, 38), Size = new Size(132, 30),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.ButtonDanger, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 }, Enabled = false
        };
        _btnRemovePlayer.Click += (_, _) => RemovePlayerFromTeam();

        _btnDeleteAllPlayers = new Button
        {
            Text = "Delete All Players", Location = new Point(8, 76), Size = new Size(132, 30),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.ButtonDanger, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 }, Enabled = false
        };
        _btnDeleteAllPlayers.Click += (_, _) => DeleteAllPlayersFromTeam();
        playerBtns.Controls.AddRange([_btnAddPlayer, _btnRemovePlayer, _btnDeleteAllPlayers]);

        _lblTeamTitle = new Label { Dock = DockStyle.Top, Height = 28, Font = AppTheme.FontSectionHeading, ForeColor = AppTheme.Accent, Text = "Players - select a team above", Padding = new Padding(2, 4, 0, 0) };

        // Dock order: Fill added first (lowest priority), then Right, Bottom, Top (highest)
        playerPanel.Controls.Add(_playersGrid);
        playerPanel.Controls.Add(playerBtns);
        playerPanel.Controls.Add(captainStrip);
        playerPanel.Controls.Add(_lblTeamTitle);

        var splitter = new Panel { Dock = DockStyle.Bottom, Height = 5, BackColor = AppTheme.Separator };

        // Teams tab toolbar
        var teamToolbar = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = AppTheme.Surface, Padding = new Padding(12, 8, 12, 8) };
        _btnAddTeam = new Button
        {
            Text = "+ Add Team", Location = new Point(12, 8), Size = new Size(120, 30),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.ButtonSuccess, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 }, Enabled = false
        };
        _btnAddTeam.Click += (_, _) => AddTeam();

        _btnBuildTeams = new Button
        {
            Text = "Create All Teams", Location = new Point(144, 8), Size = new Size(130, 30),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.Accent, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 }, Enabled = false
        };
        _btnBuildTeams.Click += (_, _) => CreateAllTeams();

        _btnDeleteTeam = new Button
        {
            Text = "Delete Team", Location = new Point(286, 8), Size = new Size(120, 30),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.ButtonDanger, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 }, Enabled = false
        };
        _btnDeleteTeam.Click += (_, _) => DeleteTeam();

        _btnDeleteAllTeams = new Button
        {
            Text = "Delete All Teams", Location = new Point(418, 8), Size = new Size(130, 30),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.ButtonDanger, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 }, Enabled = false
        };
        _btnDeleteAllTeams.Click += (_, _) => DeleteAllTeams();

        var teamHint = new Label
        {
            Text = "Teams are lettered A, B, C... automatically. Deleting re-sequences the remaining teams.",
            Location = new Point(562, 16), AutoSize = true,
            Font = AppTheme.FontSmall, ForeColor = AppTheme.TextMuted
        };
        teamToolbar.Controls.AddRange([_btnAddTeam, _btnBuildTeams, _btnDeleteTeam, _btnDeleteAllTeams, teamHint]);

        // Build the tab content: grid + player panel + splitter + toolbar (all bottom-docked except grid)
        // Dock order: Fill first, then bottom items in reverse visual order
        var content = new Panel { Dock = DockStyle.Fill };
        content.Controls.Add(_teamsGrid);     // Fill
        content.Controls.Add(playerPanel);    // Bottom
        content.Controls.Add(splitter);       // Bottom (above playerPanel)
        content.Controls.Add(teamToolbar);    // Bottom (above splitter)

        page.Controls.Add(content);
        return page;
    }

    // â”€â”€ Save Toolbar â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private Panel BuildSaveToolbar()
    {
        var toolbar = new Panel
        {
            Dock = DockStyle.Bottom, Height = 54,
            BackColor = AppTheme.Surface, Padding = new Padding(12, 10, 12, 10)
        };

        _btnAdd = new Button
        {
            Text = "+ Add Division", Location = new Point(12, 10), Size = new Size(140, 32),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.ButtonSuccess, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 }
        };
        _btnAdd.Click += (_, _) => AddDivision();

        _btnSave = new Button
        {
            Text = "Save Changes", Location = new Point(160, 10), Size = new Size(140, 32),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.Accent, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 },
            Enabled = false
        };
        _btnSave.Click += (_, _) => SaveDivision();

        _btnCancel = new Button
        {
            Text = "Cancel", Location = new Point(308, 10), Size = new Size(140, 32),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.Surface, ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand,
            FlatAppearance = { BorderSize = 1, BorderColor = AppTheme.Separator },
            Visible = false
        };
        _btnCancel.Click += (_, _) => CancelAddDivision();

        _btnDelete = new Button
        {
            Text = "Delete Division", Location = new Point(308, 10), Size = new Size(140, 32),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.ButtonDanger, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand,
            FlatAppearance = { BorderSize = 0 }, Enabled = false
        };
        _btnDelete.Click += (_, _) => DeleteDivision();

        toolbar.Controls.AddRange([_btnAdd, _btnSave, _btnCancel, _btnDelete]);
        return toolbar;
    }

    // â”€â”€ Data Loading â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void LoadContext()
    {
        try
        {
            using var db = new BocceDbContext();
            _selectedLeagueId = AppParameterService.GetDefaultLeagueId(db);
            _selectedSeasonId = AppParameterService.GetDefaultSeasonId(db);
        }
        catch { }
        LoadSlotCombos();
        LoadDivisionList();
    }

    private void MarkDirty()
    {
        if (_isLoadingData || _btnSave == null) return;
        _isDirty = true;
        _btnSave.Enabled = true;
    }

    private void ClearDirty()
    {
        if (_btnSave == null) return;
        _isDirty = false;
        _btnSave.Enabled = false;
    }

    private void LoadDivisionList()
    {
        _allDivisions.Clear();
        if (_selectedLeagueId.HasValue && _selectedSeasonId.HasValue)
        {
            try
            {
                using var db = new BocceDbContext();
                var season = db.Seasons.Find(_selectedSeasonId.Value);
                if (season != null && season.LeagueId == _selectedLeagueId.Value)
                {
                    _allDivisions = db.Divisions
                        .Where(d => d.SeasonId == _selectedSeasonId.Value)
                        .OrderBy(d => d.SortName).ThenBy(d => d.Name)
                        .Select(d => new { d.Id, Display = d.Name + (d.IsActive ? "" : " (inactive)") })
                        .AsEnumerable()
                        .Select(d => (d.Id, d.Display))
                        .ToList();
                }
            }
            catch { }
        }
        FilterDivisionList();
        if (_lstDivisions.Items.Count > 0 && _lstDivisions.SelectedIndex < 0)
            _lstDivisions.SelectedIndex = 0;
        else if (_lstDivisions.Items.Count == 0)
            ClearEditor();
    }

    private void FilterDivisionList()
    {
        var query = _txtSearch.Text == "Search..." ? "" : _txtSearch.Text;
        var prev  = _lstDivisions.SelectedItem is ListItem sel ? sel.Id : (int?)null;

        _isLoadingData = true;
        try
        {
            _lstDivisions.BeginUpdate();
            _lstDivisions.Items.Clear();
            foreach (var (id, display) in _allDivisions)
                if (SearchQueryService.MatchesAnyTerm(display, query))
                    _lstDivisions.Items.Add(new ListItem(id, display));
            _lstDivisions.EndUpdate();
        }
        finally { _isLoadingData = false; }

        if (prev.HasValue) SelectInList(prev.Value);
    }

    private void SelectInList(int divisionId)
    {
        for (int i = 0; i < _lstDivisions.Items.Count; i++)
            if (_lstDivisions.Items[i] is ListItem li && li.Id == divisionId)
            { _lstDivisions.SelectedIndex = i; return; }
    }

    private void OnListDivisionSelected(object? sender, EventArgs e)
    {
        if (_isLoadingData) return;
        if (_lstDivisions.SelectedItem is ListItem li)
            LoadDivision(li.Id);
        else
            ClearEditor();
    }

    private void LoadSlotCombos()
    {
        _cmbDay.SelectedIndexChanged -= OnSlotChanged;
        _cmbTime.SelectedIndexChanged -= OnSlotChanged;
        _cmbDay.Items.Clear();
        _cmbTime.Items.Clear();
        _cmbDay.Items.Add(new SlotItem(0, "(none)"));
        _cmbTime.Items.Add(new SlotItem(0, "(none)"));
        try
        {
            using var db = new BocceDbContext();
            IQueryable<DaySlot>  daysQuery  = db.DaySlots.Where(d => d.IsActive);
            IQueryable<TimeSlot> timesQuery = db.TimeSlots.Where(t => t.IsActive);

            if (_selectedSeasonId.HasValue)
            {
                var dayIds  = db.SeasonDaySlots.Where(s => s.SeasonId == _selectedSeasonId.Value)
                                               .Select(s => s.DaySlotId).ToList();
                var timeIds = db.SeasonTimeSlots.Where(s => s.SeasonId == _selectedSeasonId.Value)
                                                .Select(s => s.TimeSlotId).ToList();
                if (dayIds.Count  > 0) daysQuery  = daysQuery.Where(d => dayIds.Contains(d.Id));
                if (timeIds.Count > 0) timesQuery = timesQuery.Where(t => timeIds.Contains(t.Id));
            }

            foreach (var d in daysQuery.OrderBy(d => d.DayNbr).ToList())
                _cmbDay.Items.Add(new SlotItem(d.Id, d.DayName));
            foreach (var t in timesQuery.OrderBy(t => t.SortOrder).ToList())
                _cmbTime.Items.Add(new SlotItem(t.Id, t.Timeslot12h));
        }
        catch { }
        _cmbDay.SelectedIndex  = 0;
        _cmbTime.SelectedIndex = 0;
        _cmbDay.SelectedIndexChanged  += OnSlotChanged;
        _cmbTime.SelectedIndexChanged += OnSlotChanged;
    }

    private void LoadDivision(int divisionId)
    {
        _selectedDivisionId = divisionId;

        try
        {
            using var db = new BocceDbContext();
            var d = db.Divisions
                .Include(x => x.DaySlot)
                .Include(x => x.TimeSlot)
                .FirstOrDefault(x => x.Id == divisionId);
            if (d == null) return;

            var season = db.Seasons
                .Include(x => x.League)
                .FirstOrDefault(x => x.Id == d.SeasonId);

            _lblName.Text    = d.Name;
            _chkActive.Checked = d.IsActive;
            _lblCreated.Text   = ""; // Division has no CreatedAt â€” show blank

            // Day slot
            _cmbDay.SelectedIndexChanged -= OnSlotChanged;
            _cmbDay.SelectedIndex = 0;
            if (d.DaySlotId.HasValue)
                for (int i = 0; i < _cmbDay.Items.Count; i++)
                    if (_cmbDay.Items[i] is SlotItem si && si.Id == d.DaySlotId.Value)
                    { _cmbDay.SelectedIndex = i; break; }
            _cmbDay.SelectedIndexChanged += OnSlotChanged;

            // Time slot
            _cmbTime.SelectedIndexChanged -= OnSlotChanged;
            _cmbTime.SelectedIndex = 0;
            if (d.TimeSlotId.HasValue)
                for (int i = 0; i < _cmbTime.Items.Count; i++)
                    if (_cmbTime.Items[i] is SlotItem si && si.Id == d.TimeSlotId.Value)
                    { _cmbTime.SelectedIndex = i; break; }
            _cmbTime.SelectedIndexChanged += OnSlotChanged;

            _lblSystemName.Text = d.ShortName;
            _lblSortKey.Text    = d.SortName;

            // Handle parameter inheritance: Division inherits from Season
            int teamsInDiv = d.TeamsInDivision > 0 ? d.TeamsInDivision : (season?.MaxTeamsInDivision ?? 0);
            int playersMin = (d.PlayersPerTeamMinimum ?? 0) > 0 ? (d.PlayersPerTeamMinimum ?? 0) : (season?.PlayersPerTeamMinimum ?? 0);
            int playersMax = (d.PlayersPerTeamMaximum ?? 0) > 0 ? (d.PlayersPerTeamMaximum ?? 0) : (season?.PlayersPerTeamMaximum ?? 0);

            _numTeamsInDiv.Value = teamsInDiv;
            _numPlayersMin.Value = playersMin;
            _numPlayersMax.Value = playersMax;
        }
        catch { }

        _currentTeamId = null;
        LoadTeams(divisionId);
        ClearPlayersPanel();
        _isCreatingNew = false;
        _btnCancel.Visible = false;
        _btnDelete.Visible = true;
        ClearDirty();
    }

    private void ClearEditor()
    {
        _selectedDivisionId = null;
        _currentTeamId      = null;
        _isCreatingNew = false;
        _lblName.Text = "";
        _cmbDay.SelectedIndex  = 0;
        _cmbTime.SelectedIndex = 0;
        _lblSystemName.Text = "";
        _lblSortKey.Text    = "";
        _chkActive.Checked = true;
        _lblCreated.Text   = "";
        _numTeamsInDiv.Value = 0;
        _numPlayersMin.Value = 0;
        _numPlayersMax.Value = 0;
        _btnDelete.Enabled  = false;
        _btnCancel.Visible  = false;
        _btnAddTeam.Enabled = false;
        _btnDeleteTeam.Enabled   = false;
        _btnAddPlayer.Enabled    = false;
        _btnRemovePlayer.Enabled = false;
        _teamsGrid.Rows.Clear();
        ClearPlayersPanel();
        ClearDirty();
    }

    private void OnSlotChanged(object? sender, EventArgs e)
    {
        // Recompute system name, sort key, and division name from the selected slots
        int dayId  = _cmbDay.SelectedItem  is SlotItem ds ? ds.Id : 0;
        int timeId = _cmbTime.SelectedItem is SlotItem ts ? ts.Id : 0;
        if (dayId == 0 || timeId == 0)
        {
            _lblSystemName.Text = "";
            _lblSortKey.Text    = "";
            _lblName.Text = "";
            return;
        }
        try
        {
            using var db = new BocceDbContext();
            var day  = db.DaySlots.Find(dayId);
            var time = db.TimeSlots.Find(timeId);
            if (day == null || time == null) return;
            _lblSystemName.Text = BuildShortName(day.DayAbbr, time.Timeslot24h);
            _lblSortKey.Text    = $"{day.DayNbr}-{time.Timeslot24h}";

            // Auto-generate the division name from day and time (for both new and existing divisions)
            _lblName.Text = $"{day.DayName} {time.Timeslot12h}";
        }
        catch { }
    }

    // â”€â”€ New Division â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void AddDivision()
    {
        if (!_selectedSeasonId.HasValue)
        {
            MessageBox.Show("Select a season first.", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        _isCreatingNew = true;
        _isLoadingData = true;
        _lstDivisions.SelectedIndex = -1;
        _isLoadingData = false;
        ClearEditor();
        _btnCancel.Visible = true;
        _btnDelete.Visible = false;
        _cmbDay.Focus();
    }

    private void CancelAddDivision()
    {
        _isCreatingNew = false;
        _btnCancel.Visible = false;
        _btnDelete.Visible = _selectedDivisionId.HasValue;
        ClearEditor();
        LoadDivisionList();
    }

    // â”€â”€ Edit Mode â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€


    // â”€â”€ Save â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void SaveDivision()
    {
        if (!_selectedSeasonId.HasValue)
        {
            MessageBox.Show("Select a season first.", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Get selected day and time
        string dayName = _cmbDay.SelectedItem is SlotItem ds && ds.Id > 0 ? ds.Display : null;
        string timeName = _cmbTime.SelectedItem is SlotItem ts && ts.Id > 0 ? ts.Display : null;

        // For new divisions, day and time are required
        if (!_selectedDivisionId.HasValue && (string.IsNullOrEmpty(dayName) || string.IsNullOrEmpty(timeName)))
        {
            MessageBox.Show("Select a Day and Time for the division.", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Auto-generate name from day and time for new divisions
        string name;
        if (!_selectedDivisionId.HasValue)
        {
            // New division: generate name
            name = $"{dayName} {timeName}";
        }
        else
        {
            // Existing division: use entered name or auto-generate
            name = _lblName.Text.Trim();
            if (string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(dayName) && !string.IsNullOrEmpty(timeName))
                name = $"{dayName} {timeName}";
        }

        if (string.IsNullOrEmpty(name))
        {
            MessageBox.Show("Division name is required.", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Check for duplicate division names in the same season
        try
        {
            using var db = new BocceDbContext();
            bool isDuplicate = db.Divisions
                .Where(d => d.SeasonId == _selectedSeasonId.Value && d.Name == name)
                .Any(d => !_selectedDivisionId.HasValue || d.Id != _selectedDivisionId.Value);

            if (isDuplicate)
            {
                MessageBox.Show($"A division named \"{name}\" already exists in this season.", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                if (_selectedDivisionId.HasValue)
                {
                    // For editing: restore original values
                    LoadDivision(_selectedDivisionId.Value);
                }
                // For new: do nothing, stays in edit mode
                return;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Validation failed:\n\n{ex.Message}", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        int savedId;
        try
        {
            using var db = new BocceDbContext();
            Division div;
            if (_selectedDivisionId.HasValue)
                div = db.Divisions.Find(_selectedDivisionId.Value) ?? throw new Exception("Division not found.");
            else
            {
                div = new Division { SeasonId = _selectedSeasonId.Value };
                db.Divisions.Add(div);
            }

            div.Name     = name;
            div.IsActive = _chkActive.Checked;
            // Save displayed values (which might be inherited from parent)
            div.TeamsInDivision       = (int)_numTeamsInDiv.Value;
            div.PlayersPerTeamMinimum = (int)_numPlayersMin.Value > 0 ? (int)_numPlayersMin.Value : null;
            div.PlayersPerTeamMaximum = (int)_numPlayersMax.Value > 0 ? (int)_numPlayersMax.Value : null;

            int dayId  = _cmbDay.SelectedItem  is SlotItem dslot ? dslot.Id : 0;
            int timeId = _cmbTime.SelectedItem is SlotItem tslot ? tslot.Id : 0;
            div.DaySlotId  = dayId  > 0 ? dayId  : (int?)null;
            div.TimeSlotId = timeId > 0 ? timeId : (int?)null;
            div.ShortName  = _lblSystemName.Text;
            div.SortName   = _lblSortKey.Text;

            db.SaveChanges();
            savedId = div.Id;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Save failed:\n\n{ex.Message}", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _selectedDivisionId = savedId;
        _btnDelete.Enabled  = true;
        _btnAddTeam.Enabled = true;
        MessageBox.Show("Division saved.", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Information);
        _isCreatingNew = false;
        _btnCancel.Visible = false;
        _btnDelete.Visible = true;
        ClearDirty();
        LoadDivisionList();
        SelectInList(savedId);
        LoadTeams(savedId);
    }

    // â”€â”€ Delete Division â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void DeleteDivision()
    {
        if (!_selectedDivisionId.HasValue) return;
        int divId = _selectedDivisionId.Value;
        string divName = _lblName.Text.Trim();

        int teamCount = 0, playerCount = 0;
        try
        {
            using var db = new BocceDbContext();
            teamCount   = db.Teams.Count(t => t.DivisionId == divId);
            playerCount = db.TeamPlayers.Count(tp => tp.Team.DivisionId == divId);
        }
        catch { }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Permanently delete \"{divName}\"?");
        if (teamCount > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"  Teams to be disbanded ......... {teamCount}");
            sb.AppendLine($"  Player assignments removed .... {playerCount}");
            sb.AppendLine();
            sb.AppendLine("Players are NOT deleted - only their team assignments.");
        }
        sb.AppendLine(); sb.AppendLine("This cannot be undone. Continue?");

        if (MessageBox.Show(sb.ToString(), "Confirm Delete",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;

        try
        {
            using var db = new BocceDbContext();
            var div = db.Divisions
                .Include(d => d.Teams).ThenInclude(t => t.TeamPlayers)
                .FirstOrDefault(d => d.Id == divId);
            if (div != null)
            {
                // Remove child records in dependency order
                db.DivisionParameters.RemoveRange(db.DivisionParameters.Where(x => x.DivisionId == divId));
                db.TeamStandings     .RemoveRange(db.TeamStandings     .Where(x => x.DivisionId == divId));

                // Schedule: Games â†’ MatchTeamResults â†’ Matches â†’ Weeks
                var weekIds  = db.ScheduleWeeks.Where(w => w.DivisionId == divId).Select(w => w.Id).ToList();
                var matchIds = db.Matches.Where(m => weekIds.Contains(m.ScheduleWeekId)).Select(m => m.Id).ToList();
                db.Games            .RemoveRange(db.Games            .Where(g => matchIds.Contains(g.MatchId)));
                db.MatchTeamResults .RemoveRange(db.MatchTeamResults .Where(r => matchIds.Contains(r.MatchId)));
                db.Matches          .RemoveRange(db.Matches          .Where(m => weekIds .Contains(m.ScheduleWeekId)));
                db.ScheduleWeeks    .RemoveRange(db.ScheduleWeeks    .Where(w => w.DivisionId == divId));

                foreach (var team in div.Teams)
                    db.TeamPlayers.RemoveRange(team.TeamPlayers);
                db.Teams.RemoveRange(div.Teams);
                db.Divisions.Remove(div);
                db.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Delete failed:\n{ex.Message}", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        MessageBox.Show("Division deleted.", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Information);
        _selectedDivisionId = null;
        LoadDivisionList();
        if (_lstDivisions.Items.Count == 0) ClearEditor();
    }

    // â”€â”€ Teams â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void LoadTeams(int divisionId)
    {
        _teamsGrid.SelectionChanged -= OnTeamSelected;
        _teamsGrid.Rows.Clear();
        try
        {
            using var db = new BocceDbContext();
            var (minPlayers, maxPlayers) = ResolvePlayerLimits();

            var teams = db.Teams
                .Where(t => t.DivisionId == divisionId)
                .OrderBy(t => t.TeamLetter)
                .Select(t => new
                {
                    t.Id, t.TeamLetter, t.SystemName,
                    Display  = t.DisplayName ?? t.SystemName,
                    Captain  = t.Captain != null ? t.Captain.LastName + ", " + t.Captain.FirstName : "",
                    PlayerCount = db.TeamPlayers.Count(tp => tp.TeamId == t.Id),
                    t.IsActive
                }).ToList();

            foreach (var t in teams)
            {
                // Show players as "current/max" or just count if max not set
                string playerDisplay = maxPlayers > 0
                    ? $"{t.PlayerCount}/{maxPlayers}"
                    : t.PlayerCount.ToString();

                // If below minimum, add indicator
                if (minPlayers > 0 && t.PlayerCount < minPlayers)
                    playerDisplay += " [!]";

                _teamsGrid.Rows.Add(t.Id, t.TeamLetter, t.SystemName, t.Display, t.Captain, playerDisplay, t.IsActive);
            }
            _teamsGrid.ClearSelection();
        }
        catch { }
        _teamsGrid.SelectionChanged += OnTeamSelected;
        ClearPlayersPanel();
    }

    private void OnTeamSelected(object? sender, EventArgs e)
    {
        if (_teamsGrid.SelectedRows.Count == 0)
        {
            _currentTeamId = null;
            ClearPlayersPanel();
            return;
        }
        var row = _teamsGrid.SelectedRows[0];
        if (row.Cells["TmId"].Value == null) return;
        _currentTeamId = Convert.ToInt32(row.Cells["TmId"].Value);
        _btnRemovePlayer.Enabled = false;
        string displayName = row.Cells["TmDisplay"].Value?.ToString() ?? "";
        _lblTeamTitle.Text = $"Players - Team {displayName}";
        LoadPlayersForTeam(_currentTeamId.Value);
    }

    private void OnTeamCellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.ColumnIndex == _teamsGrid.Columns["TmActive"].Index && e.RowIndex >= 0)
            _teamsGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
    }

    private void OnTeamActiveChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.ColumnIndex != _teamsGrid.Columns["TmActive"].Index || e.RowIndex < 0) return;
        var row = _teamsGrid.Rows[e.RowIndex];
        if (row.Cells["TmId"].Value is null or DBNull) return;
        int teamId   = Convert.ToInt32(row.Cells["TmId"].Value);
        bool isActive = Convert.ToBoolean(row.Cells["TmActive"].Value);
        try
        {
            using var db = new BocceDbContext();
            var team = db.Teams.Find(teamId);
            if (team != null) { team.IsActive = isActive; db.SaveChanges(); }
        }
        catch { }
    }

    private int ResolveMaxTeams()
    {
        if (!_selectedDivisionId.HasValue) return 0;
        try
        {
            using var db = new BocceDbContext();
            var div = db.Divisions.Find(_selectedDivisionId.Value);
            if (div == null) return 0;
            var season = db.Seasons.Find(div.SeasonId);
            var league = db.Leagues.Find(season?.LeagueId ?? 0);
            return ResolveMaxTeamsForDivision(div, season, league);
        }
        catch { return 0; }
    }

    private static int ResolveMaxTeamsForDivision(Division div, Season? season, League? league)
    {
        if (div.TeamsInDivision > 0) return div.TeamsInDivision;
        if (season?.MaxTeamsInDivision > 0) return season.MaxTeamsInDivision;
        return league?.MaxTeamsInDivision ?? 0;
    }

    private bool? ChooseDivisionOrLeagueScope(string title, string heading, string text)
    {
        var divisionButton = new TaskDialogButton("This Division only");
        var leagueButton = new TaskDialogButton("Entire League (all divisions)");

        var page = new TaskDialogPage
        {
            Caption = title,
            Heading = heading,
            Text = text,
            AllowCancel = true,
            DefaultButton = divisionButton
        };

        page.Buttons.Add(divisionButton);
        page.Buttons.Add(leagueButton);
        page.Buttons.Add(TaskDialogButton.Cancel);

        var result = TaskDialog.ShowDialog(page);
        if (ReferenceEquals(result, divisionButton)) return false;
        if (ReferenceEquals(result, leagueButton)) return true;
        return null;
    }

    private (int min, int max) ResolvePlayerLimits()
    {
        if (!_selectedDivisionId.HasValue) return (0, 0);
        try
        {
            using var db = new BocceDbContext();
            var div = db.Divisions.Include(d => d.Season).FirstOrDefault(d => d.Id == _selectedDivisionId.Value);
            if (div == null) return (0, 0);

            int min = (div.PlayersPerTeamMinimum ?? 0) > 0
                ? div.PlayersPerTeamMinimum.Value
                : (div.Season?.PlayersPerTeamMinimum ?? 0);

            int max = (div.PlayersPerTeamMaximum ?? 0) > 0
                ? div.PlayersPerTeamMaximum.Value
                : (div.Season?.PlayersPerTeamMaximum ?? 0);

            return (min, max);
        }
        catch { return (0, 0); }
    }

    private void AddTeam()
    {
        if (!_selectedDivisionId.HasValue) return;
        int divId = _selectedDivisionId.Value;

        try
        {
            using var db = new BocceDbContext();
            var div = db.Divisions.Find(divId);
            if (div == null) return;

            int currentCount = db.Teams.Count(t => t.DivisionId == divId && !t.IsByeTeam);
            int maxTeams     = ResolveMaxTeams();
            if (maxTeams > 0 && currentCount >= maxTeams)
            {
                string source = div.TeamsInDivision > 0 ? "this division's Parameters tab"
                    : "the season or league default";
                MessageBox.Show(
                    $"Maximum of {maxTeams} team(s) already reached ({source}).\n\n" +
                    "Increase \"Teams in Division\" on the Parameters tab to allow more.",
                    "Maximum Teams Reached", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var existing = db.Teams.Where(t => t.DivisionId == divId && !t.IsByeTeam)
                .OrderBy(t => t.TeamLetter).ToList();
            char nextLetter = existing.Count > 0
                ? (char)(existing.Max(t => string.IsNullOrEmpty(t.TeamLetter) ? 'A' - 1 : t.TeamLetter[0]) + 1)
                : 'A';

            db.Teams.Add(new Team
            {
                DivisionId  = divId,
                TeamLetter  = nextLetter.ToString(),
                SystemName  = $"{nextLetter}-{div.ShortName}",
                DisplayName = null,
                IsActive    = true
            });
            db.SaveChanges();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not add team:\n{ex.Message}", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        LoadTeams(divId);
    }

    private void CreateAllTeams()
    {
        if (!_selectedDivisionId.HasValue) return;

        var scopeChoice = ChooseDivisionOrLeagueScope(
            "Create/Sync Teams",
            "Choose team sync scope",
            "Select whether to synchronize teams for only the selected division or every division in the selected league.");

        if (!scopeChoice.HasValue)
            return;

        bool leagueScope = scopeChoice.Value;

        try
        {
            using var db = new BocceDbContext();

            var targetDivisions = new List<Division>();
            if (leagueScope)
            {
                if (!_selectedLeagueId.HasValue)
                {
                    MessageBox.Show("Select a league first.", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (!_selectedSeasonId.HasValue)
                {
                    MessageBox.Show("Select a season first.", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                targetDivisions = db.Divisions
                    .Where(d => d.Season.LeagueId == _selectedLeagueId.Value
                             && d.SeasonId == _selectedSeasonId.Value)
                    .OrderBy(d => d.SortName)
                    .ThenBy(d => d.Name)
                    .ToList();
            }
            else
            {
                var div = db.Divisions.Find(_selectedDivisionId.Value);
                if (div != null) targetDivisions.Add(div);
            }

            if (targetDivisions.Count == 0)
            {
                MessageBox.Show("No divisions found for selected scope.", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int addedTotal = 0;
            int removedTotal = 0;
            int skippedTotal = 0;

            foreach (var div in targetDivisions)
            {
                var season = db.Seasons.Find(div.SeasonId);
                var league = db.Leagues.Find(season?.LeagueId ?? 0);
                int maxTeams = ResolveMaxTeamsForDivision(div, season, league);

                if (maxTeams <= 0)
                {
                    skippedTotal++;
                    continue;
                }

                var teams = db.Teams
                    .Include(t => t.TeamPlayers)
                    .Where(t => t.DivisionId == div.Id && !t.IsByeTeam)
                    .OrderBy(t => t.TeamLetter)
                    .ToList();

                int currentCount = teams.Count;

                if (currentCount < maxTeams)
                {
                    char nextLetter = teams.Count > 0
                        ? (char)(teams.Max(t => string.IsNullOrEmpty(t.TeamLetter) ? 'A' - 1 : t.TeamLetter[0]) + 1)
                        : 'A';

                    int toCreate = maxTeams - currentCount;
                    for (int i = 0; i < toCreate; i++)
                    {
                        db.Teams.Add(new Team
                        {
                            DivisionId = div.Id,
                            TeamLetter = nextLetter.ToString(),
                            SystemName = $"{nextLetter}-{div.ShortName}",
                            DisplayName = null,
                            IsActive = true
                        });
                        nextLetter++;
                    }
                    addedTotal += toCreate;
                }
                else if (currentCount > maxTeams)
                {
                    int toRemove = currentCount - maxTeams;

                    // First remove highest-letter empty teams.
                    var emptyCandidates = teams
                        .Where(t => (t.TeamPlayers == null || t.TeamPlayers.Count == 0) && !t.CaptainPlayerId.HasValue)
                        .OrderByDescending(t => t.TeamLetter)
                        .ToList();

                    var removeList = new List<Team>();
                    foreach (var t in emptyCandidates)
                    {
                        if (removeList.Count >= toRemove) break;
                        removeList.Add(t);
                    }

                    // If still short, remove by highest letter regardless of content.
                    if (removeList.Count < toRemove)
                    {
                        var highLetterCandidates = teams
                            .Where(t => !removeList.Any(r => r.Id == t.Id))
                            .OrderByDescending(t => t.TeamLetter)
                            .ToList();

                        foreach (var t in highLetterCandidates)
                        {
                            if (removeList.Count >= toRemove) break;
                            removeList.Add(t);
                        }
                    }

                    foreach (var team in removeList)
                    {
                        if (team.TeamPlayers != null && team.TeamPlayers.Count > 0)
                            db.TeamPlayers.RemoveRange(team.TeamPlayers);
                        db.Teams.Remove(team);
                    }

                    removedTotal += removeList.Count;
                }

                // Commit add/remove changes first so deleted rows do not participate in re-lettering.
                db.SaveChanges();

                // Always resequence after sync so final letters are contiguous (A..N).
                ResequenceTeams(div.Id, db);
            }

            db.SaveChanges();

            var summary =
                $"Team sync complete ({(leagueScope ? "League" : "Division")} scope).\n\n" +
                $"Added teams: {addedTotal}\n" +
                $"Removed teams: {removedTotal}\n" +
                $"Skipped divisions (no configured max): {skippedTotal}";

            MessageBox.Show(summary, "Teams Synchronized", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not create teams:\n{ex.Message}", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (_selectedDivisionId.HasValue)
            LoadTeams(_selectedDivisionId.Value);
    }

    private void DeleteTeam()
    {
        if (_currentTeamId == null || !_selectedDivisionId.HasValue) return;
        int teamId = _currentTeamId.Value;
        int divId  = _selectedDivisionId.Value;
        string teamName = _teamsGrid.SelectedRows.Count > 0
            ? (_teamsGrid.SelectedRows[0].Cells["TmDisplay"].Value?.ToString() ?? "")
            : "";

        int playerCount = 0;
        bool hasCaptain = false;
        try
        {
            using var db = new BocceDbContext();
            playerCount = db.TeamPlayers.Count(tp => tp.TeamId == teamId);
            var team = db.Teams.Find(teamId);
            hasCaptain = team?.CaptainPlayerId.HasValue ?? false;
        }
        catch { }

        // First confirmation: simple yes/no
        if (MessageBox.Show(
            $"Are you sure you want to delete team \"{teamName}\"?\n\n" +
            "This action CANNOT be undone.",
            "Confirm Delete Team",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;

        // Second confirmation: show cascade impact details
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("You are about to permanently delete the following:");
        sb.AppendLine();
        if (playerCount > 0)
            sb.AppendLine($"  Player assignments removed .......... {playerCount}");
        if (hasCaptain)
            sb.AppendLine($"  Captains cleared .................... 1");
        sb.AppendLine();
        sb.AppendLine("Warning: Players will NOT be deleted - only their team assignment.");
        sb.AppendLine();
        sb.AppendLine("Remaining teams will be re-lettered (A, B, C...).");
        sb.AppendLine();
        sb.AppendLine("This cannot be undone. Continue?");

        if (MessageBox.Show(sb.ToString(), "Confirm Cascade Impact",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;

        try
        {
            using var db = new BocceDbContext();
            var team = db.Teams.Include(t => t.TeamPlayers).FirstOrDefault(t => t.Id == teamId);
            if (team != null)
            {
                db.TeamPlayers.RemoveRange(team.TeamPlayers);
                db.Teams.Remove(team);
                db.SaveChanges();
                ResequenceTeams(divId, db);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Delete failed:\n{ex.Message}", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _currentTeamId = null;
        LoadTeams(divId);
    }

    private void DeleteAllTeams()
    {
        if (!_selectedDivisionId.HasValue && !_selectedLeagueId.HasValue) return;

        var scopeChoice = ChooseDivisionOrLeagueScope(
            "Delete All Teams",
            "Choose delete scope",
            "Select whether to delete all teams in only the selected division or in every division in the selected league.");

        if (!scopeChoice.HasValue)
            return;

        bool leagueScope = scopeChoice.Value;
        List<Division> targetDivisions;
        int teamCount;
        int playerCount;
        int captainCount;

        try
        {
            using var db = new BocceDbContext();

            if (leagueScope)
            {
                if (!_selectedLeagueId.HasValue)
                {
                    MessageBox.Show("Select a league first.", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (!_selectedSeasonId.HasValue)
                {
                    MessageBox.Show("Select a season first.", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                targetDivisions = db.Divisions
                    .Where(d => d.Season.LeagueId == _selectedLeagueId.Value
                             && d.SeasonId == _selectedSeasonId.Value)
                    .OrderBy(d => d.SortName)
                    .ThenBy(d => d.Name)
                    .ToList();
            }
            else
            {
                if (!_selectedDivisionId.HasValue)
                {
                    MessageBox.Show("Select a division first.", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                targetDivisions = db.Divisions
                    .Where(d => d.Id == _selectedDivisionId.Value)
                    .ToList();
            }

            if (targetDivisions.Count == 0)
            {
                MessageBox.Show("No divisions found for selected scope.", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var divisionIds = targetDivisions.Select(d => d.Id).ToList();
            teamCount = db.Teams.Count(t => divisionIds.Contains(t.DivisionId) && !t.IsByeTeam);
            playerCount = db.TeamPlayers.Count(tp => divisionIds.Contains(tp.Team.DivisionId));
            captainCount = db.Teams.Count(t => divisionIds.Contains(t.DivisionId) && t.CaptainPlayerId.HasValue && !t.IsByeTeam);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not read delete scope:\n{ex.Message}", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (teamCount == 0)
        {
            MessageBox.Show("No teams to delete in the selected scope.", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        string scopeLabel = leagueScope
            ? $"the entire league ({targetDivisions.Count} division(s))"
            : "this division";

        if (MessageBox.Show(
            $"Are you sure you want to permanently delete ALL {teamCount} team(s) in {scopeLabel}?\n\n" +
            "This action CANNOT be undone.",
            "Confirm Delete All Teams",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("You are about to permanently delete the following:");
        sb.AppendLine();
        sb.AppendLine($"  Teams to be deleted ................. {teamCount}");
        sb.AppendLine($"  Player assignments removed .......... {playerCount}");
        sb.AppendLine($"  Captains cleared .................... {captainCount}");
        sb.AppendLine();
        sb.AppendLine("Warning: Players will NOT be deleted - only their team assignments.");
        sb.AppendLine();
        sb.AppendLine("This cannot be undone. Continue?");

        if (MessageBox.Show(sb.ToString(), "Confirm Cascade Impact",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;

        try
        {
            using var db = new BocceDbContext();
            var divisionIds = targetDivisions.Select(d => d.Id).ToList();
            var teams = db.Teams
                .Include(t => t.TeamPlayers)
                .Where(t => divisionIds.Contains(t.DivisionId) && !t.IsByeTeam)
                .ToList();

            foreach (var team in teams)
                db.TeamPlayers.RemoveRange(team.TeamPlayers);

            db.Teams.RemoveRange(teams);
            db.SaveChanges();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Delete failed:\n{ex.Message}", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        MessageBox.Show($"Deleted {teamCount} team(s).", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Information);
        _currentTeamId = null;
        if (_selectedDivisionId.HasValue)
            LoadTeams(_selectedDivisionId.Value);
        else
            _teamsGrid.Rows.Clear();
    }

    private void DeleteAllPlayersFromTeam()
    {
        if (_currentTeamId == null) return;
        int teamId = _currentTeamId.Value;

        int playerCount = 0;
        bool hasCaptain = false;
        try
        {
            using var db = new BocceDbContext();
            playerCount = db.TeamPlayers.Count(tp => tp.TeamId == teamId);
            var team = db.Teams.Find(teamId);
            hasCaptain = team?.CaptainPlayerId.HasValue ?? false;
        }
        catch { }

        if (playerCount == 0)
        {
            MessageBox.Show("No players on this team.", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var teamName = _teamsGrid.SelectedRows.Count > 0
            ? (_teamsGrid.SelectedRows[0].Cells["TmDisplay"].Value?.ToString() ?? "")
            : "";

        // First confirmation: simple yes/no
        if (MessageBox.Show(
            $"Are you sure you want to remove all {playerCount} player(s) from {teamName}?\n\n" +
            "This action CANNOT be undone.",
            "Confirm Remove All Players",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;

        // Second confirmation: show cascade impact details
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("You are about to permanently remove the following:");
        sb.AppendLine();
        sb.AppendLine($"  Player assignments removed .......... {playerCount}");
        if (hasCaptain)
            sb.AppendLine($"  Captains cleared .................... 1");
        sb.AppendLine();
        sb.AppendLine("Warning: Players will NOT be deleted - only their team assignment.");
        sb.AppendLine();
        sb.AppendLine("This cannot be undone. Continue?");

        if (MessageBox.Show(sb.ToString(), "Confirm Cascade Impact",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;

        try
        {
            using var db = new BocceDbContext();
            var players = db.TeamPlayers.Where(tp => tp.TeamId == teamId).ToList();
            var playerIds = players.Select(p => p.PlayerId).ToList();
            db.TeamPlayers.RemoveRange(players);

            // Clear captain assignment
            var team = db.Teams.Include(t => t.Division).ThenInclude(d => d.Season).FirstOrDefault(t => t.Id == teamId);
            if (team != null)
            {
                team.CaptainPlayerId = null;
                team.DisplayName = null;

                // Removed players become active LFT entries again for this league.
                var lftEntries = db.LookingForTeams
                    .Where(l => l.LeagueId == team.Division.Season.LeagueId
                             && l.TeamId == teamId
                             && playerIds.Contains(l.PlayerId))
                    .ToList();
                foreach (var lft in lftEntries)
                    lft.TeamId = null;
            }

            db.SaveChanges();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Remove failed:\n{ex.Message}", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        MessageBox.Show($"Removed all players from {teamName}.", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Information);
        LoadPlayersForTeam(teamId);
        if (_selectedDivisionId.HasValue) LoadTeams(_selectedDivisionId.Value);
    }

    private void ResequenceTeams(int divisionId, BocceDbContext db)
    {
        string divShortName = db.Divisions.Find(divisionId)?.ShortName ?? "";
        var teams = db.Teams
            .Where(t => t.DivisionId == divisionId && !t.IsByeTeam)
            .OrderBy(t => t.TeamLetter)
            .ToList();

        char letter = 'A';
        foreach (var team in teams)
        {
            team.TeamLetter = letter.ToString();
            team.SystemName = $"{letter}-{divShortName}";
            if (team.CaptainPlayerId.HasValue)
            {
                var captain = db.Players.Find(team.CaptainPlayerId.Value);
                if (captain != null)
                    team.DisplayName = $"{letter}-{captain.LastName}";
            }
            letter++;
        }
        db.SaveChanges();
    }

    // â”€â”€ Players â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void LoadPlayersForTeam(int teamId)
    {
        _playersGrid.Rows.Clear();
        int? captainId = null;
        var playerItems = new List<IntItem>();

        try
        {
            using var db = new BocceDbContext();
            var team = db.Teams.Find(teamId);
            captainId = team?.CaptainPlayerId;

            var tps = db.TeamPlayers
                .Include(tp => tp.Player)
                .Where(tp => tp.TeamId == teamId)
                .OrderBy(tp => tp.Player.LastName).ThenBy(tp => tp.Player.FirstName)
                .ToList();

            playerItems.Add(new IntItem(0, "(none)"));
            foreach (var tp in tps)
            {
                string fullName = $"{tp.Player.LastName}, {tp.Player.FirstName}";
                string role = tp.Role == "captain" ? "Captain" : "Player";
                _playersGrid.Rows.Add(tp.PlayerId, fullName, role);
                playerItems.Add(new IntItem(tp.PlayerId, fullName));
            }
            _playersGrid.ClearSelection();
        }
        catch { }

        _cmbCaptain.SelectedIndexChanged -= OnCaptainChanged;
        _cmbCaptain.Items.Clear();
        foreach (var item in playerItems) _cmbCaptain.Items.Add(item);
        _cmbCaptain.SelectedIndex = 0;
        if (captainId.HasValue)
            for (int i = 0; i < _cmbCaptain.Items.Count; i++)
                if (_cmbCaptain.Items[i] is IntItem ci && ci.Id == captainId.Value)
                { _cmbCaptain.SelectedIndex = i; break; }
        _cmbCaptain.SelectedIndexChanged += OnCaptainChanged;

        _playersGrid.SelectionChanged += (_, _) =>
            _btnRemovePlayer.Enabled = _playersGrid.SelectedRows.Count > 0;
    }

    private void ClearPlayersPanel()
    {
        _playersGrid.Rows.Clear();
        _lblTeamTitle.Text = "Players - select a team above";
        _cmbCaptain.SelectedIndexChanged -= OnCaptainChanged;
        _cmbCaptain.Items.Clear();
        _cmbCaptain.Enabled = false;
        _cmbCaptain.SelectedIndexChanged += OnCaptainChanged;
        _btnRemovePlayer.Enabled = false;
    }

    private void AddPlayerToTeam()
    {
        if (_currentTeamId == null || !_selectedDivisionId.HasValue) return;
        int teamId = _currentTeamId.Value;
        int divisionId = _selectedDivisionId.Value;

        // Get max players limit upfront
        int currentPlayerCount = 0;
        int maxPlayersPerTeam = 0;

        try
        {
            using var db = new BocceDbContext();
            var team = db.Teams.Find(teamId);
            var division = db.Divisions.Include(d => d.Season).FirstOrDefault(d => d.Id == divisionId);
            if (team == null || division == null) return;

            currentPlayerCount = db.TeamPlayers.Count(tp => tp.TeamId == teamId);
            maxPlayersPerTeam = division.PlayersPerTeamMaximum > 0
                ? division.PlayersPerTeamMaximum ?? 0
                : (division.Season?.PlayersPerTeamMaximum ?? 0);

            if (maxPlayersPerTeam == 0)
            {
                MessageBox.Show(
                    "Cannot add players: Max players per team is not configured.\n\n" +
                    "Set a value in Division Parameters or Season Parameters.",
                    "Configuration Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int availableSlots = maxPlayersPerTeam - currentPlayerCount;
            if (availableSlots <= 0)
            {
                MessageBox.Show(
                    $"This team is at maximum capacity ({maxPlayersPerTeam} players).\n\n" +
                    "Remove players before adding new ones.",
                    "Team at Capacity", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }
        catch { return; }

        // Loop picker until valid selection is made
        while (true)
        {
            var excludeIds = new HashSet<int>();
            try
            {
                using var db = new BocceDbContext();
                // Exclude players already on ANY team in this division
                excludeIds = db.TeamPlayers
                    .Where(tp => tp.Team.DivisionId == divisionId)
                    .Select(tp => tp.PlayerId)
                    .ToHashSet();
            }
            catch { }

            var playerIds = PickPlayersMultiple(excludeIds);
            if (playerIds.Count == 0) return;

            int availableSlots = maxPlayersPerTeam - currentPlayerCount;

            // Validate selection
            if (playerIds.Count > availableSlots)
            {
                MessageBox.Show(
                    $"You selected {playerIds.Count} player(s) but only {availableSlots} slot(s) are available.\n\n" +
                    $"Team max: {maxPlayersPerTeam}  |  Current: {currentPlayerCount}  |  Available: {availableSlots}\n\n" +
                    $"Please deselect {playerIds.Count - availableSlots} player(s) and try again.",
                    "Selection Exceeds Capacity", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                continue; // Loop back to picker
            }

            // Valid selection - proceed with adding players
            try
            {
                using var db = new BocceDbContext();
                int count = 0;
                var skipped = new List<string>();

                foreach (var playerId in playerIds)
                {
                    // Check if player already on this team
                    var alreadyOnThisTeam = db.TeamPlayers.Any(tp => tp.TeamId == teamId && tp.PlayerId == playerId);
                    if (alreadyOnThisTeam) continue;

                    // Check if player already on another team in this division (should not happen due to picker, but validate)
                    var alreadyInDivision = db.TeamPlayers
                        .Any(tp => tp.Team.DivisionId == divisionId && tp.PlayerId == playerId);
                    if (alreadyInDivision)
                    {
                        var player = db.Players.Find(playerId);
                        skipped.Add(player?.FullName ?? $"Player {playerId}");
                        continue;
                    }

                    // Check if player would exceed team limit (3 teams max in league)
                    var teamCountInLeague = db.TeamPlayers
                        .Where(tp => tp.PlayerId == playerId && tp.Team.Division.Season.LeagueId == _selectedLeagueId)
                        .Select(tp => tp.TeamId)
                        .Distinct()
                        .Count();
                    if (teamCountInLeague >= 3)
                    {
                        var player = db.Players.Find(playerId);
                        skipped.Add($"{player?.FullName ?? $"Player {playerId}"} (already on 3 teams)");
                        continue;
                    }

                    db.TeamPlayers.Add(new TeamPlayer
                    {
                        TeamId    = teamId,
                        PlayerId  = playerId,
                        Role      = "player",
                        JoinedDate = DateOnly.FromDateTime(DateTime.Today)
                    });

                    // Keep LookingForTeam history: assigned entries get TeamId set.
                    if (_selectedSeasonId.HasValue)
                    {
                        var lft = db.LookingForTeams.FirstOrDefault(l =>
                            l.PlayerId == playerId &&
                            (!_selectedLeagueId.HasValue || l.LeagueId == _selectedLeagueId.Value) &&
                            l.SeasonId == _selectedSeasonId.Value);
                        if (lft != null)
                            lft.TeamId = teamId;
                    }

                    count++;
                }

                if (count > 0) db.SaveChanges();

                var msg2 = $"Added {count} player(s) to team.\n\nTeam now has {currentPlayerCount + count}/{maxPlayersPerTeam} players.";
                if (skipped.Count > 0)
                    msg2 += $"\n\nSkipped {skipped.Count}:\n  - " + string.Join("\n  - ", skipped);

                MessageBox.Show(msg2, count > 0 ? "Success" : "Info", MessageBoxButtons.OK,
                    count > 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                break; // Exit loop after successful add
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not add player(s):\n{ex.Message}", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        } // Close while loop

        LoadPlayersForTeam(teamId);
        if (_selectedDivisionId.HasValue) LoadTeams(_selectedDivisionId.Value);
        // Re-select the same team row
        foreach (DataGridViewRow r in _teamsGrid.Rows)
            if (r.Cells["TmId"].Value != null && Convert.ToInt32(r.Cells["TmId"].Value) == teamId)
            { _teamsGrid.ClearSelection(); r.Selected = true; break; }
    }

    private void RemovePlayerFromTeam()
    {
        if (_currentTeamId == null || _playersGrid.SelectedRows.Count == 0) return;
        int teamId   = _currentTeamId.Value;
        int playerId = Convert.ToInt32(_playersGrid.SelectedRows[0].Cells["PlId"].Value);
        string name  = _playersGrid.SelectedRows[0].Cells["PlName"].Value?.ToString() ?? "";

        if (MessageBox.Show($"Remove \"{name}\" from this team?\n\nThe player is NOT deleted - only the team assignment.",
            "Confirm Remove", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;

        try
        {
            using var db = new BocceDbContext();
            var tp = db.TeamPlayers.FirstOrDefault(x => x.TeamId == teamId && x.PlayerId == playerId);
            if (tp != null) { db.TeamPlayers.Remove(tp); db.SaveChanges(); }

            // Clear captain if it was this player
            var team = db.Teams.Include(t => t.Division).ThenInclude(d => d.Season).FirstOrDefault(t => t.Id == teamId);
            if (team?.CaptainPlayerId == playerId)
            {
                team.CaptainPlayerId = null;
                team.DisplayName     = null;
                db.SaveChanges();
            }

            // Player is no longer assigned to this team: mark LFT entry active again.
            if (team != null)
            {
                var lft = db.LookingForTeams.FirstOrDefault(l =>
                    l.LeagueId == team.Division.Season.LeagueId &&
                    l.PlayerId == playerId &&
                    l.SeasonId == team.Division.SeasonId &&
                    l.TeamId == teamId);
                if (lft != null)
                {
                    lft.TeamId = null;
                    db.SaveChanges();
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Remove failed:\n{ex.Message}", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        LoadPlayersForTeam(teamId);
        if (_selectedDivisionId.HasValue) LoadTeams(_selectedDivisionId.Value);
        foreach (DataGridViewRow r in _teamsGrid.Rows)
            if (r.Cells["TmId"].Value != null && Convert.ToInt32(r.Cells["TmId"].Value) == teamId)
            { _teamsGrid.ClearSelection(); r.Selected = true; break; }
    }

    private void OnCaptainChanged(object? sender, EventArgs e)
    {
        if (_currentTeamId == null) return;
        int teamId   = _currentTeamId.Value;
        int playerId = _cmbCaptain.SelectedItem is IntItem ci ? ci.Id : 0;

        if (playerId > 0)
        {
            // Check if player is already captain of another team in this league
            try
            {
                using var db = new BocceDbContext();
                var team = db.Teams.Find(teamId);
                if (team == null) return;
                var otherCaptaincy = db.Teams
                    .Where(t => t.Division.Season.LeagueId == team.Division.Season.LeagueId
                             && t.Id != teamId
                             && t.CaptainPlayerId == playerId)
                    .Select(t => t.DisplayName ?? t.SystemName)
                    .FirstOrDefault();

                if (!string.IsNullOrEmpty(otherCaptaincy))
                {
                    MessageBox.Show(
                        $"This player is already captain of: {otherCaptaincy}\n\nA player can only be captain of one team.",
                        "Captain Already Assigned", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _cmbCaptain.SelectedIndex = 0; // Reset to "(none)"
                    return;
                }
            }
            catch { }
        }

        try
        {
            using var db = new BocceDbContext();
            var team = db.Teams.Find(teamId);
            if (team == null) return;

            // Update role flags on all TeamPlayers
            var tps = db.TeamPlayers.Where(tp => tp.TeamId == teamId).ToList();
            foreach (var tp in tps)
                tp.Role = tp.PlayerId == playerId ? "captain" : "player";

            if (playerId > 0)
            {
                team.CaptainPlayerId = playerId;
                var captain = db.Players.Find(playerId);
                if (captain != null)
                    team.DisplayName = $"{team.TeamLetter}-{captain.LastName}";
            }
            else
            {
                team.CaptainPlayerId = null;
                team.DisplayName     = null;
            }
            db.SaveChanges();
        }
        catch { return; }

        // Refresh grid row and players panel
        LoadPlayersForTeam(teamId);
        if (_selectedDivisionId.HasValue) LoadTeams(_selectedDivisionId.Value);
        foreach (DataGridViewRow r in _teamsGrid.Rows)
            if (r.Cells["TmId"].Value != null && Convert.ToInt32(r.Cells["TmId"].Value) == teamId)
            { _teamsGrid.ClearSelection(); r.Selected = true; break; }
    }

    // â”€â”€ Player picker dialog â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private int? PickPlayer(HashSet<int> excludeIds)
    {
        using var form = new Form
        {
            Text = "Select Player to Add", Width = 460, Height = 460,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false,
            BackColor = AppTheme.ContentBackground
        };

        var search = new TextBox
        {
            Dock = DockStyle.Top, Font = AppTheme.FontDefault, Height = 30,
            PlaceholderText = "Search by name... (OR: | \\ / : ;)", BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary, BorderStyle = BorderStyle.FixedSingle
        };

        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false, ReadOnly = true,
            AllowUserToAddRows = false, RowHeadersVisible = false,
            BorderStyle = BorderStyle.None,
            BackgroundColor = AppTheme.ContentBackground,
            Font = AppTheme.FontDefault, RowTemplate = { Height = 28 },
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            EnableHeadersVisualStyles = false,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor          = AppTheme.GridHeaderBackground, ForeColor          = AppTheme.GridHeaderText,
                SelectionBackColor = AppTheme.GridHeaderBackground, SelectionForeColor = AppTheme.GridHeaderText,
                Font = AppTheme.FontGridHeader
            }
        };
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "PId",   Visible = false });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "PName", HeaderText = "Name" });
        grid.DoubleClick += (_, _) => { if (grid.SelectedRows.Count > 0) form.DialogResult = DialogResult.OK; };

        var btnBar = new Panel { Dock = DockStyle.Bottom, Height = 46, BackColor = AppTheme.Surface };
        var btnOk     = new Button { Text = "Add Player", DialogResult = DialogResult.OK,     Left = 12,  Top = 8, Width = 120, Height = 30, FlatStyle = FlatStyle.Flat, BackColor = AppTheme.Accent,       ForeColor = Color.White, Font = AppTheme.FontButton };
        var btnCancel = new Button { Text = "Cancel",     DialogResult = DialogResult.Cancel, Left = 144, Top = 8, Width = 90,  Height = 30, FlatStyle = FlatStyle.Flat, Font = AppTheme.FontButton };
        btnBar.Controls.AddRange([btnOk, btnCancel]);

        List<(int Id, string Name)> all = [];
        try
        {
            using var db = new BocceDbContext();
            all = db.Players
                .Where(p => p.IsActive && !excludeIds.Contains(p.Id))
                .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
                .ToList()
                .Select(p => (p.Id, $"{p.LastName}, {p.FirstName}"))
                .ToList();
        }
        catch { }

        void Filter(string q)
        {
            grid.Rows.Clear();
            foreach (var (id, name) in all)
            {
                bool matches = SearchQueryService.MatchesAnyTerm(name, q);

                if (matches) grid.Rows.Add(id, name);
            }
            grid.ClearSelection();
        }
        search.TextChanged += (_, _) => Filter(search.Text);
        Filter("");

        // Dock order: Fill (grid), then Bottom (btnBar), Top (search)
        form.Controls.Add(grid);
        form.Controls.Add(btnBar);
        form.Controls.Add(search);
        form.AcceptButton = btnOk;
        form.CancelButton = btnCancel;

        if (form.ShowDialog(this) == DialogResult.OK && grid.SelectedRows.Count > 0)
        {
            var cell = grid.SelectedRows[0].Cells["PId"].Value;
            if (cell != null && cell != DBNull.Value) return Convert.ToInt32(cell);
        }
        return null;
    }

    // â”€â”€ Navigation helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€


    // â”€â”€ Control factories â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static string BuildShortName(string dayAbbr, string time24h)
    {
        var prefix = dayAbbr.Length >= 2
            ? $"{char.ToUpper(dayAbbr[0])}{char.ToLower(dayAbbr[1])}"
            : dayAbbr;
        return $"{prefix}-{time24h}";
    }

    private static Label Lbl(string text, int x, int y) => new()
    {
        Text = text, Font = AppTheme.FontDefaultBold, ForeColor = AppTheme.TextPrimary,
        AutoSize = true, Location = new Point(x, y + 3)
    };

    private static ThemedNumericUpDown Num(int x, int y, decimal min, decimal max, decimal def = 0) => new()
    {
        Location = new Point(x, y), Size = new Size(90, 26),
        Font = AppTheme.FontDefault, Minimum = min, Maximum = max, Value = def, DecimalPlaces = 0
    };

    private static Label Hint(string text, int x, int y) => new()
    {
        Text = text, AutoSize = true, Font = AppTheme.FontSmall,
        ForeColor = AppTheme.TextMuted, Location = new Point(x, y)
    };

    private static Panel Sep(int x, int y, int w) => new()
    {
        Location = new Point(x, y), Size = new Size(w, 1), BackColor = AppTheme.Separator
    };

    private static Label SecHdr(string text, int x, int y) => new()
    {
        Text = text, Location = new Point(x, y), AutoSize = true,
        Font = AppTheme.FontSectionHeading, ForeColor = AppTheme.Accent
    };

    // â”€â”€ Multi-select player picker â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private List<int> PickPlayersMultiple(HashSet<int> excludeIds)
    {
        var result = new List<int>();
        using var form = new Form
        {
            Text = "Add Players to Team", Width = 750, Height = 600,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false,
            BackColor = AppTheme.ContentBackground
        };

        // Load all available players
        List<(int Id, string Name)> allPlayers = [];
        HashSet<int> lookingForTeam = [];
        try
        {
            using var db = new BocceDbContext();
            allPlayers = db.Players
                .Where(p => p.IsActive && !excludeIds.Contains(p.Id))
                .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
                .ToList()
                .Select(p => (p.Id, $"{p.LastName}, {p.FirstName}"))
                .ToList();

            if (_selectedSeasonId.HasValue)
            {
                lookingForTeam = db.LookingForTeams
                    .Where(l => l.SeasonId == _selectedSeasonId.Value && !l.TeamId.HasValue)
                    .Select(l => l.PlayerId)
                    .ToHashSet();
            }
        }
        catch { }

        // Search box
        var searchBox = new TextBox
        {
            Location = new Point(10, 8), Width = 300, Height = 28,
            Font = AppTheme.FontDefault, PlaceholderText = "Search... (OR delimiters: | \\ / : ;)",
            BackColor = AppTheme.Surface, ForeColor = AppTheme.TextPrimary, BorderStyle = BorderStyle.FixedSingle
        };

        // Left side: Available players
        var lblAvailable = new Label { Text = "Available Players", Font = AppTheme.FontDefaultBold, AutoSize = true, Location = new Point(10, 42) };
        var cmbAvailable = new CheckedListBox
        {
            Location = new Point(10, 62), Width = 300, Height = 440,
            Font = AppTheme.FontDefault, BackColor = AppTheme.Surface, ForeColor = AppTheme.TextPrimary,
            CheckOnClick = true
        };

        // Right side: Selected players
        var lblSelected = new Label { Text = "Players to Add", Font = AppTheme.FontDefaultBold, AutoSize = true, Location = new Point(430, 42) };
        var cmbSelected = new CheckedListBox
        {
            Location = new Point(430, 62), Width = 300, Height = 440,
            Font = AppTheme.FontDefault, BackColor = AppTheme.Surface, ForeColor = AppTheme.TextPrimary,
            CheckOnClick = true
        };

        // Middle buttons
        var btnAdd = new Button
        {
            Text = "Add  >>", Width = 80, Height = 30, Top = 200, Left = 320,
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.Accent, ForeColor = Color.White, Font = AppTheme.FontButton
        };
        var btnRemove = new Button
        {
            Text = "<< Remove", Width = 80, Height = 30, Top = 240, Left = 320,
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.Accent, ForeColor = Color.White, Font = AppTheme.FontButton
        };

        // Bottom buttons
        var btnOk = new Button
        {
            Text = "Add Players", DialogResult = DialogResult.OK, Left = 430, Top = 520, Width = 140, Height = 30,
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.Accent, ForeColor = Color.White, Font = AppTheme.FontButton
        };
        var btnCancel = new Button
        {
            Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 580, Top = 520, Width = 140, Height = 30,
            FlatStyle = FlatStyle.Flat, Font = AppTheme.FontButton
        };

        // Filter function with multi-delimiter OR search
        void RefreshAvailable(string query)
        {
            cmbAvailable.Items.Clear();
            foreach (var (id, name) in allPlayers)
            {
                // Skip if already in selected
                if (cmbSelected.Items.Cast<IntItem>().Any(x => x.Id == id)) continue;

                bool matches = SearchQueryService.MatchesAnyTerm(name, query);

                if (matches)
                {
                    string displayName = lookingForTeam.Contains(id) ? $"◆ {name}" : name;
                    cmbAvailable.Items.Add(new IntItem(id, displayName));
                }
            }
        }
        RefreshAvailable("");

        searchBox.TextChanged += (_, _) => RefreshAvailable(searchBox.Text);

        btnAdd.Click += (_, _) =>
        {
            var toMove = new List<(int Index, IntItem Item)>();
            for (int i = cmbAvailable.Items.Count - 1; i >= 0; i--)
            {
                if (cmbAvailable.GetItemChecked(i))
                {
                    var item = (IntItem)cmbAvailable.Items[i];
                    toMove.Add((i, item));
                }
            }

            foreach (var (_, item) in toMove)
                cmbSelected.Items.Add(item);

            // Clear checkboxes in available
            for (int i = 0; i < cmbAvailable.Items.Count; i++)
                cmbAvailable.SetItemChecked(i, false);

            RefreshAvailable(searchBox.Text);
        };

        btnRemove.Click += (_, _) =>
        {
            var toRemove = new List<(int Index, IntItem Item)>();
            for (int i = cmbSelected.Items.Count - 1; i >= 0; i--)
            {
                if (cmbSelected.GetItemChecked(i))
                {
                    var item = (IntItem)cmbSelected.Items[i];
                    toRemove.Add((i, item));
                }
            }

            foreach (var (_, item) in toRemove)
                cmbSelected.Items.Remove(item);

            // Clear checkboxes in selected
            for (int i = 0; i < cmbSelected.Items.Count; i++)
                cmbSelected.SetItemChecked(i, false);

            RefreshAvailable(searchBox.Text);
        };

        form.Controls.AddRange([searchBox, lblAvailable, cmbAvailable, btnAdd, btnRemove, lblSelected, cmbSelected, btnOk, btnCancel]);
        form.AcceptButton = btnOk;
        form.CancelButton = btnCancel;

        if (form.ShowDialog(this) == DialogResult.OK)
            result = cmbSelected.Items.Cast<IntItem>().Select(x => x.Id).ToList();

        return result;
    }

    private sealed record ListItem(int Id, string Display) { public override string ToString() => Display; }
    private sealed record IntItem(int Id, string Name)    { public override string ToString() => Name; }
    private sealed record SlotItem(int Id, string Display) { public override string ToString() => Display; }
}


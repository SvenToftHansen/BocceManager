using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.Services;
using BocceManager.UI.Controls;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Panels;

public class SeasonPanel : UserControl
{
    private bool _isLoadingData = false;
    private bool _isDirty = false;
    private bool _isCreatingNew = false;
    private bool _isNewSeasonDraft = false;
    private readonly System.Windows.Forms.Timer _autoSaveTimer = new() { Interval = 1500 };
    private bool _seasonNameCustomized = false;
    private bool _settingSeasonNameProgrammatically = false;

    // ── State ─────────────────────────────────────────────────────────────────
    private int? _selectedLeagueId;
    private int? _selectedSeasonId;
    private int? _previousSeasonId;
    private bool _isCopied;
    private int? _copySourceId;
    private bool _copyDivisions;
    private bool _copyTeams;

    // ── Left panel ────────────────────────────────────────────────────────────
    private SearchBoxControl _txtSearch   = null!;
    private ListBox _lstSeasons  = null!;

    // ── Editor – basic ────────────────────────────────────────────────────────
    private TextBox        _txtName           = null!;
    private DateTimePicker _dtpStartDate      = null!;
    private ThemedNumericUpDown _numWeeks          = null!;
    private DateTimePicker _dtpPlayoffStart   = null!;
    private CheckBox       _chkIsCurrent      = null!;
    private CheckBox       _chkIsLocked       = null!;
    private ComboBox       _cmbStatus         = null!;
    private Label          _lblCreatedAt      = null!;

    // ── Editor – division defaults ────────────────────────────────────────────
    private ThemedNumericUpDown _numMaxTeamsDiv = null!;

    // ── Editor – scoring ──────────────────────────────────────────────────────
    private ThemedNumericUpDown _numPlayersMin     = null!;
    private ThemedNumericUpDown _numPlayersMax     = null!;
    private ThemedNumericUpDown _numPtsWin         = null!;
    private ThemedNumericUpDown _numPtsTie         = null!;
    private ThemedNumericUpDown _numPtsLoss        = null!;
    private ThemedNumericUpDown _numPtsNoShow      = null!;
    private ThemedNumericUpDown _numPtsToWin       = null!;
    private ComboBox      _cmbScoringMode    = null!;
    private ThemedNumericUpDown _numForfeitPM         = null!;
    private ThemedNumericUpDown _numForfeitOpponentPM = null!;

    // ── Editor – fees ─────────────────────────────────────────────────────────
    private TextBox _txtSeasonFeeAmount = null!;

    // ── Editor – playoff settings ─────────────────────────────────────────────
    private ComboBox      _cboTeamsPlayoffs     = null!;
    private CheckBox      _chkFirstPlace        = null!;
    private ComboBox      _cmbPlayoffTiebreaker = null!;

    private Button _btnAdd    = null!;
    private Button _btnSave   = null!;
    private Button _btnCancel = null!;
    private Button _btnDelete = null!;

    // ── Divisions tab ─────────────────────────────────────────────────────────
    private DataGridView _divisionsGrid = null!;

    // ── Slots tab ─────────────────────────────────────────────────────────────
    private CheckedListBox _daysList  = null!;
    private CheckedListBox _timesList = null!;
    private Button         _btnBuild  = null!;

    // ── Courts tab ────────────────────────────────────────────────────────────
    private CheckedListBox _courtsList      = null!;
    private ComboBox       _cmbCourtDisplay = null!;
    private Button         _btnCourtUp      = null!;
    private Button         _btnCourtDown    = null!;

    // Tab references for lock-state show/hide
    private TabControl _tabs          = null!;
    private TabPage    _tabParameters = null!;
    private TabPage    _tabDivisions  = null!;
    private TabPage    _tabSlots      = null!;
    private TabPage    _tabCourts     = null!;

    // All seasons for search filtering
    private List<(int Id, string Display)> _allSeasons = [];

    // ─────────────────────────────────────────────────────────────────────────

    public SeasonPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;
        BuildUI();
        _autoSaveTimer.Tick += (_, _) => { _autoSaveTimer.Stop(); if (_isDirty && !_isCreatingNew) SaveSeason(silent: true); };
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

    public void SelectSeason(int seasonId)
    {
        try
        {
            using var db = new BocceDbContext();
            var s = db.Seasons.Find(seasonId);
            if (s == null) return;

            // If season belongs to a different league, update default
            var currentDefaultLeague = AppParameterService.GetDefaultLeagueId(db);
            if (currentDefaultLeague != s.LeagueId)
                AppParameterService.SetDefaultLeagueId(db, s.LeagueId);

            // Reload for this league, then select the season
            _selectedLeagueId = s.LeagueId;
            LoadSeasonList();
            SelectInList(seasonId);
        }
        catch { }
    }

    // ── Build UI ──────────────────────────────────────────────────────────────

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
            Text = "Seasons",
            Font = AppTheme.FontSmallBold,
            ForeColor = AppTheme.TextPrimary,
            Height = 22,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _txtSearch = new SearchBoxControl("Search seasons...")
        {
            Dock = DockStyle.Top,
            Height = 28,
            Margin = new Padding(0, 0, 0, 6)
        };
        _txtSearch.SearchTextChanged += (_, _) => FilterSeasonList();

        _lstSeasons = new ListBox
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.FontDefault,
            BackColor = AppTheme.ContentBackground,
            ForeColor = AppTheme.TextPrimary,
            BorderStyle = BorderStyle.None,
            IntegralHeight = false
        };
        _lstSeasons.SelectedIndexChanged += OnListSeasonSelected;

        panel.Controls.Add(_lstSeasons);
        panel.Controls.Add(_txtSearch);
        panel.Controls.Add(lblTitle);
    }

    private void BuildRightPanel(SplitterPanel panel)
    {
        var toolbar = BuildSaveToolbar();
        _tabs = BuildTabs();

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2,
            Padding = Padding.Empty, Margin = Padding.Empty,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, toolbar.Height));
        _tabs.Dock = DockStyle.Fill;
        toolbar.Dock = DockStyle.Fill;
        layout.Controls.Add(_tabs, 0, 0);
        layout.Controls.Add(toolbar, 0, 1);

        panel.Controls.Add(layout);
    }

    private TabControl BuildTabs()
    {
        var tabs = new TabControl
        {
            Dock = DockStyle.Fill, Font = AppTheme.FontDefault, Padding = new Point(16, 6)
        };
        tabs.TabPages.Add(BuildEditorTab());
        _tabParameters = BuildParametersTab();
        _tabDivisions  = BuildDivisionsTab();
        _tabSlots      = BuildSlotsTab();
        tabs.TabPages.Add(_tabParameters);
        tabs.TabPages.Add(_tabDivisions);
        tabs.TabPages.Add(_tabSlots);
        _tabCourts     = BuildCourtsTab();
        tabs.TabPages.Add(_tabCourts);
        return tabs;
    }

    // ── Editor Tab ────────────────────────────────────────────────────────────

    private TabPage BuildEditorTab()
    {
        var page   = new TabPage("  Editor  ") { BackColor = AppTheme.ContentBackground };
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = AppTheme.ContentBackground };

        const int lx = 20, ix = 220, iw = 440;
        int y = 20;
        var cc = new List<Control>();
        void Add(params Control[] items) => cc.AddRange(items);

        // ── Basic ──────────────────────────────────────────────────────────
        Add(Lbl("Name *", lx, y));
        _txtName = new TextBox { Location = new Point(ix, y), Size = new Size(iw, 26), Font = AppTheme.FontDefault, BackColor = AppTheme.ContentBackground, ForeColor = AppTheme.TextPrimary };
        _txtName.TextChanged += OnSeasonNameTextChanged;
        Add(_txtName); y += 44;

        // ── Lock State ────────────────────────────────────────────────────
        Add(Sep(lx, y, iw + ix - lx)); y += 10;
        Add(SecHdr("Lock State", lx, y)); y += 34;

        Add(Lbl("Is Current Season", lx, y));
        _chkIsCurrent = new CheckBox { Location = new Point(ix, y), AutoSize = true, Font = AppTheme.FontDefault, ForeColor = AppTheme.TextPrimary };
        _chkIsCurrent.CheckedChanged += (_, _) =>
        {
            if (!_isLoadingData && _chkIsCurrent.Checked) _chkIsLocked.Checked = false;
            MarkDirty();
        };
        Add(_chkIsCurrent, Hint("Only one season per league can be current (★)", ix + 26, y + 4)); y += 38;

        Add(Lbl("Is Locked", lx, y));
        _chkIsLocked = new CheckBox { Location = new Point(ix, y), AutoSize = true, Font = AppTheme.FontDefault, ForeColor = AppTheme.TextPrimary };
        _chkIsLocked.CheckedChanged += (_, _) =>
        {
            if (!_isLoadingData && _chkIsLocked.Checked) _chkIsCurrent.Checked = false;
            MarkDirty();
            ApplyEditorLockState(_chkIsLocked.Checked);
        };
        Add(_chkIsLocked, Hint("When locked: Parameters / Divisions / Slots tabs are hidden", ix + 26, y + 4)); y += 44;

        // ── Created ───────────────────────────────────────────────────────
        Add(Sep(lx, y, iw + ix - lx)); y += 10;
        Add(Lbl("Created", lx, y));
        _lblCreatedAt = new Label { Location = new Point(ix, y + 3), AutoSize = true, Font = AppTheme.FontDefault, ForeColor = AppTheme.TextSecondary };
        Add(_lblCreatedAt); y += 44;

        scroll.Controls.AddRange([.. cc]);
        page.Controls.Add(scroll);
        return page;
    }

    // ── Parameters Tab ────────────────────────────────────────────────────────

    private TabPage BuildParametersTab()
    {
        var page   = new TabPage("  Parameters  ") { BackColor = AppTheme.ContentBackground };
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = AppTheme.ContentBackground };

        const int lx = 20, ix = 220, iw = 440;
        int y = 20;
        var cc = new List<Control>();
        void Add(params Control[] items) => cc.AddRange(items);

        // ── Season Dates & Status ─────────────────────────────────────────
        Add(SecHdr("Season Dates & Status", lx, y)); y += 34;

        Add(Lbl("Status", lx, y));
        _cmbStatus = new ComboBox { Location = new Point(ix, y), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList, Font = AppTheme.FontDefault };
        _cmbStatus.Items.AddRange(["Setup", "League Play", "Playoff Play", "Completed"]);
        _cmbStatus.SelectedIndexChanged += (_, _) => MarkDirty();
        Add(_cmbStatus, Hint("Controls which operations are allowed", ix + 210, y + 4)); y += 44;

        Add(Lbl("League Start Date", lx, y));
        _dtpStartDate = new DateTimePicker { Location = new Point(ix, y), Width = 180, Format = DateTimePickerFormat.Short, Font = AppTheme.FontDefault };
        _dtpStartDate.ValueChanged += OnSeasonStartDateChanged;
        Add(_dtpStartDate); y += 44;

        Add(Lbl("Playoff Start Date", lx, y));
        _dtpPlayoffStart = new DateTimePicker { Location = new Point(ix, y), Width = 200, Format = DateTimePickerFormat.Short, Font = AppTheme.FontDefault, ShowCheckBox = true, Checked = false };
        _dtpPlayoffStart.ValueChanged += (_, _) => MarkDirty();
        Add(_dtpPlayoffStart, Hint("Optional — uncheck if not yet known", ix + 212, y + 4)); y += 44;

        Add(Lbl("Weeks in Season", lx, y));
        _numWeeks = Num(ix, y, 0, 99);
        _numWeeks.ValueChanged += (_, _) => MarkDirty();
        Add(_numWeeks, Hint("Required before divisions can be auto-built", ix + 100, y + 4)); y += 44;

        Add(Sep(lx, y, iw + ix - lx)); y += 10;

        // ── Fees ──────────────────────────────────────────────────────────
        Add(SecHdr("Fees", lx, y)); y += 34;

        Add(Lbl("Seasonal Fee Amount", lx, y));
        _txtSeasonFeeAmount = new TextBox
        {
            Location  = new Point(ix, y),
            Width     = 120,
            Font      = AppTheme.FontDefault,
            BackColor = AppTheme.ContentBackground,
            ForeColor = AppTheme.TextPrimary
        };
        _txtSeasonFeeAmount.TextChanged += (_, _) => MarkDirty();
        Add(_txtSeasonFeeAmount, Hint("Seasonal play fee charged to team players and spare list players", ix + 130, y + 4)); y += 44;

        Add(Sep(lx, y, iw + ix - lx)); y += 10;

        // ── Division Defaults ─────────────────────────────────────────────
        Add(SecHdr("Division Defaults", lx, y)); y += 34;

        Add(Lbl("Max Teams / Division", lx, y));
        _numMaxTeamsDiv = Num(ix, y, 0, 99);
        _numMaxTeamsDiv.ValueChanged += (_, _) => MarkDirty();
        Add(_numMaxTeamsDiv, Hint("0 = use league default  |  Divisions inherit this unless they set their own value", ix + 100, y + 4)); y += 44;

        Add(Sep(lx, y, iw + ix - lx)); y += 10;

        // ── Scoring Parameters ────────────────────────────────────────────
        Add(SecHdr("Scoring Parameters", lx, y)); y += 34;

        Add(Lbl("Players / Team Min", lx, y));
        _numPlayersMin = Num(ix, y, 0, 99);
        _numPlayersMin.ValueChanged += (_, _) => MarkDirty();
        Add(_numPlayersMin); y += 38;

        Add(Lbl("Players / Team Max", lx, y));
        _numPlayersMax = Num(ix, y, 0, 99);
        _numPlayersMax.ValueChanged += (_, _) => MarkDirty();
        Add(_numPlayersMax); y += 38;

        Add(Lbl("Points for Win", lx, y));
        _numPtsWin = Num(ix, y, -99, 99, 2);
        _numPtsWin.ValueChanged += (_, _) => MarkDirty();
        Add(_numPtsWin); y += 38;

        Add(Lbl("Points for Tie", lx, y));
        _numPtsTie = Num(ix, y, -99, 99, 1);
        _numPtsTie.ValueChanged += (_, _) => MarkDirty();
        Add(_numPtsTie); y += 38;

        Add(Lbl("Points for Loss", lx, y));
        _numPtsLoss = Num(ix, y, -99, 99, 0);
        _numPtsLoss.ValueChanged += (_, _) => MarkDirty();
        Add(_numPtsLoss); y += 38;

        Add(Lbl("Points for No Show", lx, y));
        _numPtsNoShow = Num(ix, y, -99, 99, -1);
        _numPtsNoShow.ValueChanged += (_, _) => MarkDirty();
        Add(_numPtsNoShow); y += 38;

        Add(Lbl("Points to Win Game", lx, y));
        _numPtsToWin = Num(ix, y, 1, 99, 12);
        _numPtsToWin.ValueChanged += (_, _) => MarkDirty();
        Add(_numPtsToWin); y += 38;

        Add(Lbl("Scoring Mode", lx, y));
        _cmbScoringMode = StrCombo(ix, y, 260,
            ("games_mode",       "Individual Games"),
            ("match_score_mode", "Match Games"));
        _cmbScoringMode.SelectedIndexChanged += (_, _) => MarkDirty();
        Add(_cmbScoringMode); y += 28;
        Add(new Label
        {
            Text = "Individual Games: can have 2 wins, 1 win + 1 loss, or 2 losses  |  Match Games: cumulative score from both games (Win/Loss/Tie)",
            Location = new Point(lx, y), Size = new Size(iw + ix - lx - 10, 28),
            Font = AppTheme.FontSmall, ForeColor = AppTheme.TextMuted
        }); y += 32;

        Add(Lbl("Forfeit Plus/Minus", lx, y));
        _numForfeitPM = Num(ix, y, -99, 99, -6);
        _numForfeitPM.ValueChanged += (_, _) => MarkDirty();
        Add(_numForfeitPM, Hint("Applied to a team that forfeits", ix + 100, y + 4)); y += 38;

        Add(Lbl("Forfeit Opponent Plus/Minus", lx, y));
        _numForfeitOpponentPM = Num(ix, y, -99, 99, 1);
        _numForfeitOpponentPM.ValueChanged += (_, _) => MarkDirty();
        Add(_numForfeitOpponentPM, Hint("Applied to the opponent of a one-sided forfeit; both teams get Forfeit Plus/Minus on a double forfeit", ix + 100, y + 4)); y += 38;

        // ── Playoff Settings ──────────────────────────────────────────────
        Add(Sep(lx, y, iw + ix - lx)); y += 10;
        Add(SecHdr("Playoff Settings", lx, y)); y += 34;

        Add(Lbl("Teams in Playoffs", lx, y));
        _cboTeamsPlayoffs = new ComboBox
        {
            Location = new Point(ix, y), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList,
            Font = AppTheme.FontDefault
        };
        _cboTeamsPlayoffs.SelectedIndexChanged += (_, _) => MarkDirty();
        Add(_cboTeamsPlayoffs, Hint("Ladder-only format: 4, 8, 12, 16, 24, 32, 48, 64, 96", ix + 210, y + 4)); y += 38;

        Add(Lbl("First Place Guaranteed", lx, y));
        _chkFirstPlace = new CheckBox { Location = new Point(ix, y), AutoSize = true, Font = AppTheme.FontDefault, ForeColor = AppTheme.TextPrimary, Checked = true };
        _chkFirstPlace.CheckedChanged += (_, _) => MarkDirty();
        Add(_chkFirstPlace); y += 44;

        Add(Lbl("Playoff Tiebreaker", lx, y));
        _cmbPlayoffTiebreaker = StrCombo(ix, y, 260,
            ("none",  "None"),
            ("1b1p",  "1 Ball by One Player"),
            ("1b4p",  "1 Ball by Each Player (4)"),
            ("2b1p",  "2 Balls for One Player"),
            ("2b4p",  "2 Balls for Each Player (4)"));
        _cmbPlayoffTiebreaker.SelectedIndexChanged += (_, _) => MarkDirty();
        Add(_cmbPlayoffTiebreaker); y += 50;

        scroll.Controls.AddRange([.. cc]);
        page.Controls.Add(scroll);
        return page;
    }

    // ── Save Toolbar ──────────────────────────────────────────────────────────

    private Panel BuildSaveToolbar()
    {
        var toolbar = new Panel
        {
            Height = 54,
            BackColor = AppTheme.Surface, Padding = new Padding(12, 10, 12, 10)
        };

        _btnAdd = new Button
        {
            Text = "+ Add Season", Location = new Point(12, 10), Size = new Size(140, 32),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.ButtonSuccess, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 }
        };
        _btnAdd.Click += (_, _) => AddSeason();

        _btnSave = new Button
        {
            Text = "Create Season", Location = new Point(160, 10), Size = new Size(140, 32),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.Accent, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 },
            Visible = false
        };
        _btnSave.Click += (_, _) => SaveSeason();

        _btnCancel = new Button
        {
            Text = "Cancel", Location = new Point(308, 10), Size = new Size(140, 32),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.Surface, ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand,
            FlatAppearance = { BorderSize = 1, BorderColor = AppTheme.Separator },
            Visible = false
        };
        _btnCancel.Click += (_, _) => { if (_isCreatingNew) CancelAddSeason(); else CancelEditSeason(); };

        _btnDelete = new Button
        {
            Text = "Delete Season", Location = new Point(308, 10), Size = new Size(140, 32),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.ButtonDanger, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand,
            FlatAppearance = { BorderSize = 0 }, Enabled = false
        };
        _btnDelete.Click += (_, _) => DeleteSeason();

        toolbar.Controls.AddRange([_btnAdd, _btnSave, _btnCancel, _btnDelete]);
        return toolbar;
    }

    // ── Divisions Tab ─────────────────────────────────────────────────────────

    private TabPage BuildDivisionsTab()
    {
        var page = new TabPage("  Divisions  ");

        _divisionsGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            BorderStyle = BorderStyle.None,
            BackgroundColor = AppTheme.ContentBackground,
            GridColor = AppTheme.GridLines,
            Font = AppTheme.FontDefault,
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
        _divisionsGrid.AlternatingRowsDefaultCellStyle =
            new DataGridViewCellStyle { BackColor = AppTheme.GridAlternateRow };

        _divisionsGrid.Columns.Add(new DataGridViewTextBoxColumn  { Name = "DivId",    Visible = false,                                                ReadOnly = true });
        _divisionsGrid.Columns.Add(new DataGridViewTextBoxColumn  { Name = "DivName",  HeaderText = "Name",     FillWeight = 28,                        ReadOnly = true });
        _divisionsGrid.Columns.Add(new DataGridViewTextBoxColumn  { Name = "Short",    HeaderText = "Sys Name", FillWeight = 10,                        ReadOnly = true });
        _divisionsGrid.Columns.Add(new DataGridViewTextBoxColumn  { Name = "Sort",     HeaderText = "Sort Key", FillWeight = 10,                        ReadOnly = true });
        _divisionsGrid.Columns.Add(new DataGridViewTextBoxColumn  { Name = "Day",      HeaderText = "Day",      FillWeight = 13,                        ReadOnly = true });
        _divisionsGrid.Columns.Add(new DataGridViewTextBoxColumn  { Name = "Time",     HeaderText = "Time",     FillWeight = 11,                        ReadOnly = true });
        _divisionsGrid.Columns.Add(new DataGridViewTextBoxColumn  { Name = "Teams",    HeaderText = "Teams",    FillWeight = 7,                         ReadOnly = true });
        _divisionsGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "DivAct",   HeaderText = "Active",   FillWeight = 7  });

        _divisionsGrid.CellContentClick += OnDivisionCellClick;
        _divisionsGrid.CellValueChanged  += OnDivisionActiveChanged;

        var toolbar = new Panel { Height = 46, BackColor = AppTheme.Surface, Padding = new Padding(12, 8, 12, 8) };

        var btnDel = new Button
        {
            Text = "Delete Division", Location = new Point(12, 8), Size = new Size(140, 30),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.ButtonDanger, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 }
        };
        btnDel.Click += OnDeleteDivision;

        var btnOpen = new Button
        {
            Text = "Open Division", Location = new Point(164, 8), Size = new Size(130, 30),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.Accent, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 }
        };
        btnOpen.Click += (_, _) =>
        {
            if (_divisionsGrid.SelectedRows.Count == 0) return;
            int divId = Convert.ToInt32(_divisionsGrid.SelectedRows[0].Cells["DivId"].Value);
            (FindForm() as MainForm)?.NavigateToDivision(divId);
        };

        var hint = new Label
        {
            Text = "Use the Day / Time Slots tab to auto-create divisions.",
            Location = new Point(308, 14), AutoSize = true,
            Font = AppTheme.FontSmall, ForeColor = AppTheme.TextMuted
        };
        toolbar.Controls.AddRange([btnDel, btnOpen, hint]);
        page.Controls.Add(MakeLayout(_divisionsGrid, toolbar));
        return page;
    }

    // ── Slots Tab ─────────────────────────────────────────────────────────────

    private TabPage BuildSlotsTab()
    {
        var page = new TabPage("  Day / Time Slots  ");

        var outer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
            BackColor = AppTheme.ContentBackground, Padding = new Padding(16)
        };
        outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
        outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68));
        outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var daysPanel = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.ContentBackground };
        _daysList = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true, Font = AppTheme.FontDefault, BackColor = AppTheme.Surface, ForeColor = AppTheme.TextPrimary, BorderStyle = BorderStyle.FixedSingle };
        _daysList.ItemCheck += (_, _) => MarkDirty();
        daysPanel.Controls.Add(_daysList);
        daysPanel.Controls.Add(new Label { Text = "Play Days", Dock = DockStyle.Top, Height = 28, Font = AppTheme.FontSectionHeading, ForeColor = AppTheme.Accent });

        var timesPanel = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.ContentBackground, Padding = new Padding(20, 0, 0, 0) };
        _timesList = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true, Font = AppTheme.FontDefault, BackColor = AppTheme.Surface, ForeColor = AppTheme.TextPrimary, BorderStyle = BorderStyle.FixedSingle };
        _timesList.ItemCheck += (_, _) => MarkDirty();
        timesPanel.Controls.Add(_timesList);
        timesPanel.Controls.Add(new Label { Text = "Play Times", Dock = DockStyle.Top, Height = 28, Font = AppTheme.FontSectionHeading, ForeColor = AppTheme.Accent });

        outer.Controls.Add(daysPanel, 0, 0);
        outer.Controls.Add(timesPanel, 1, 0);

        var toolbar = new Panel { Height = 54, BackColor = AppTheme.Surface, Padding = new Padding(12, 10, 12, 10) };
        _btnBuild = new Button
        {
            Text = "Build Divisions from Selected Slots", Location = new Point(12, 10),
            Size = new Size(260, 32), FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.ButtonSuccess, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 }
        };
        _btnBuild.Click += OnBuildDivisions;
        var bHint = new Label
        {
            Text = "Creates one division per Day × Time combination. Existing combinations are skipped.",
            Location = new Point(285, 18), AutoSize = true,
            Font = AppTheme.FontSmall, ForeColor = AppTheme.TextMuted
        };
        toolbar.Controls.AddRange([_btnBuild, bHint]);

        page.Controls.Add(MakeLayout(outer, toolbar));
        LoadSlotsList();
        return page;
    }

    // ── Courts Tab ────────────────────────────────────────────────────────────

    private TabPage BuildCourtsTab()
    {
        var page = new TabPage("  Courts  ") { BackColor = AppTheme.ContentBackground };
        var outer = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.ContentBackground, Padding = new Padding(16) };

        var displayPanel = new Panel { Dock = DockStyle.Top, Height = 36 };
        var lblDisplay = new Label
        {
            Text = "Court Display:", Location = new Point(0, 6), AutoSize = true,
            Font = AppTheme.FontDefault, ForeColor = AppTheme.TextPrimary
        };
        _cmbCourtDisplay = new ComboBox
        {
            Location = new Point(110, 2), Size = new Size(120, 26),
            Font = AppTheme.FontDefault, BackColor = AppTheme.ContentBackground,
            ForeColor = AppTheme.TextPrimary, DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbCourtDisplay.Items.AddRange(["Number", "Letter"]);
        _cmbCourtDisplay.SelectedIndex = 0;
        _cmbCourtDisplay.SelectedIndexChanged += (_, _) => MarkDirty();
        displayPanel.Controls.AddRange([lblDisplay, _cmbCourtDisplay]);

        var hint = new Label
        {
            Text = "Checked courts are used for this season's league and playoff scheduling, " +
                   "in the order listed (top = highest priority). Select a court and use Move Up/Down to reorder.",
            Dock = DockStyle.Bottom, Height = 40,
            Font = AppTheme.FontSmall, ForeColor = AppTheme.TextMuted
        };

        var listPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
            BackColor = AppTheme.ContentBackground
        };
        listPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280));
        listPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        listPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _courtsList = new CheckedListBox
        {
            Dock = DockStyle.Fill, CheckOnClick = true,
            Font = AppTheme.FontDefault, BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary, BorderStyle = BorderStyle.FixedSingle
        };
        _courtsList.ItemCheck += (_, _) => MarkDirty();

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(10, 0, 0, 0), BackColor = AppTheme.ContentBackground
        };
        _btnCourtUp = new Button
        {
            Text = "Move Up", Width = 84, Height = 30, Font = AppTheme.FontDefault,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(0, 0, 0, 8)
        };
        _btnCourtDown = new Button
        {
            Text = "Move Down", Width = 84, Height = 30, Font = AppTheme.FontDefault,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
        };
        _btnCourtUp.Click   += (_, _) => MoveCourtItem(-1);
        _btnCourtDown.Click += (_, _) => MoveCourtItem(1);
        btnPanel.Controls.AddRange([_btnCourtUp, _btnCourtDown]);

        listPanel.Controls.Add(_courtsList, 0, 0);
        listPanel.Controls.Add(btnPanel, 1, 0);

        outer.Controls.Add(listPanel);
        outer.Controls.Add(hint);
        outer.Controls.Add(displayPanel);
        page.Controls.Add(outer);

        return page;
    }

    private void MoveCourtItem(int direction)
    {
        int idx = _courtsList.SelectedIndex;
        if (idx < 0) return;
        int newIdx = idx + direction;
        if (newIdx < 0 || newIdx >= _courtsList.Items.Count) return;

        var item       = _courtsList.Items[idx];
        var otherItem  = _courtsList.Items[newIdx];
        bool itemChk   = _courtsList.GetItemChecked(idx);
        bool otherChk  = _courtsList.GetItemChecked(newIdx);

        _courtsList.Items[idx]    = otherItem;
        _courtsList.Items[newIdx] = item;
        _courtsList.SetItemChecked(idx, otherChk);
        _courtsList.SetItemChecked(newIdx, itemChk);
        _courtsList.SelectedIndex = newIdx;
        MarkDirty();
    }

    private void LoadSeasonCourts(int? seasonId)
    {
        _courtsList.Items.Clear();
        try
        {
            using var db = new BocceDbContext();

            var allCourts = db.Courts.Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToList();

            var selectedIds = seasonId.HasValue
                ? db.SeasonCourts.Where(sc => sc.SeasonId == seasonId.Value)
                    .OrderBy(sc => sc.SortOrder).ThenBy(sc => sc.Court.CourtNumber)
                    .Select(sc => sc.CourtId)
                    .ToList()
                : [];

            // Selected courts first, in this season's priority order; then remaining unselected courts.
            var ordered = selectedIds
                .Select(id => allCourts.FirstOrDefault(c => c.Id == id))
                .Where(c => c != null)
                .Cast<Court>()
                .Concat(allCourts.Where(c => !selectedIds.Contains(c.Id)))
                .ToList();

            var selectedSet = selectedIds.ToHashSet();
            foreach (var c in ordered)
            {
                int idx = _courtsList.Items.Add(new CourtItem(c.Id, $"Court {c.CourtNumber}"));
                _courtsList.SetItemChecked(idx, selectedSet.Contains(c.Id));
            }

            string displayStyle = "number";
            if (seasonId.HasValue)
                displayStyle = db.Seasons.Find(seasonId.Value)?.CourtDisplayStyle ?? "number";
            _cmbCourtDisplay.SelectedIndex = displayStyle == "letter" ? 1 : 0;
        }
        catch { }
    }

    // ── Data Loading ──────────────────────────────────────────────────────────

    private void LoadContext()
    {
        _isLoadingData = true;
        try
        {
            using var db = new BocceDbContext();
            _selectedLeagueId = AppParameterService.GetDefaultLeagueId(db);

            LoadSeasonList();
        }
        catch { _selectedLeagueId = null; }
        finally
        {
            _isLoadingData = false;
        }
    }

    private void LoadSeasonList()
    {
        _isLoadingData = true;
        try
        {
            _allSeasons.Clear();

            if (_selectedLeagueId.HasValue)
            {
                try
                {
                    using var db = new BocceDbContext();
                    _allSeasons = db.Seasons
                        .Where(s => s.LeagueId == _selectedLeagueId.Value)
                        .OrderByDescending(s => s.StartDate)
                        .ThenBy(s => s.Name)
                        .Select(s => new { s.Id, s.Name, s.IsCurrent, s.Status })
                        .AsEnumerable()
                        .Select(s => (s.Id, s.Name + (s.IsCurrent ? "  ★" : "") + (s.Status == "Completed" ? " (completed)" : "")))
                        .ToList();
                }
                catch { }
            }

            FilterSeasonList();

            // Auto-select default season or first item
            int? toSelect = null;
            try
            {
                using var db = new BocceDbContext();
                var defaultSeasonId = AppParameterService.GetDefaultSeasonId(db);
                if (defaultSeasonId.HasValue && _allSeasons.Any(s => s.Id == defaultSeasonId.Value))
                    toSelect = defaultSeasonId;
                else if (_selectedSeasonId.HasValue && _allSeasons.Any(s => s.Id == _selectedSeasonId.Value))
                    toSelect = _selectedSeasonId;
                else if (_allSeasons.Count > 0)
                    toSelect = _allSeasons[0].Id;
            }
            catch { }

            if (toSelect.HasValue)
                SelectInList(toSelect.Value);
            else
                ClearEditor();
        }
        finally
        {
            _isLoadingData = false;
        }
    }

    private void FilterSeasonList()
    {
        var query = _txtSearch.SearchText;

        _isLoadingData = true;
        _lstSeasons.SelectedIndexChanged -= OnListSeasonSelected;
        _lstSeasons.BeginUpdate();

        var prevId = _selectedSeasonId;
        _lstSeasons.Items.Clear();

        foreach (var (id, display) in _allSeasons)
        {
            if (SearchQueryService.MatchesAnyTerm(display, query))
                _lstSeasons.Items.Add(new ListItem(id, display));
        }

        _lstSeasons.EndUpdate();
        _lstSeasons.SelectedIndexChanged += OnListSeasonSelected;
        _isLoadingData = false;

        if (prevId.HasValue)
            SelectInList(prevId.Value);
    }

    private void SelectInList(int seasonId)
    {
        for (int i = 0; i < _lstSeasons.Items.Count; i++)
        {
            if (_lstSeasons.Items[i] is ListItem li && li.Id == seasonId)
            {
                _lstSeasons.SelectedIndex = i;
                return;
            }
        }
    }

    private void OnListSeasonSelected(object? sender, EventArgs e)
    {
        if (_isLoadingData) return;

        if (_isDirty && !_isCreatingNew)
        {
            _autoSaveTimer.Stop();
            SaveSeason(silent: true);
        }

        if (_lstSeasons.SelectedItem is ListItem li)
        {
            _previousSeasonId = _selectedSeasonId;
            LoadSeason(li.Id);
        }
        else
            ClearEditor();
    }

    private void LoadSeason(int seasonId)
    {
        _isLoadingData = true;
        try
        {
            _selectedSeasonId = seasonId;
            _isCopied = false; _copySourceId = null; _copyDivisions = false; _copyTeams = false;
            _isNewSeasonDraft = false;
            _seasonNameCustomized = false;

            using var db = new BocceDbContext();
            var s = db.Seasons.Find(seasonId);
            if (s == null) return;

            _txtName.Text            = s.Name;
            _dtpStartDate.Value      = s.StartDate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.Today;
            _numWeeks.Value          = s.WeeksInSeason;

            _dtpPlayoffStart.Checked = s.PlayoffStartDate.HasValue;
            if (s.PlayoffStartDate.HasValue)
                _dtpPlayoffStart.Value = s.PlayoffStartDate.Value.ToDateTime(TimeOnly.MinValue);

            _chkIsCurrent.Checked    = s.IsCurrent;
            _chkIsLocked.Checked     = s.IsLocked;
            SelStr(_cmbStatus, s.Status ?? "Setup");
            _lblCreatedAt.Text       = s.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

            var feeParam = db.SeasonParameters
                .FirstOrDefault(p => p.SeasonId == seasonId && p.Key == "SeasonFeeAmount");
            _txtSeasonFeeAmount.Text = feeParam?.Value ?? "0.00";

            _numMaxTeamsDiv.Value = s.MaxTeamsInDivision;
            _numPlayersMin.Value     = s.PlayersPerTeamMinimum ?? 0;
            _numPlayersMax.Value     = s.PlayersPerTeamMaximum ?? 0;
            _numPtsWin.Value         = s.PointsForWin;
            _numPtsTie.Value         = s.PointsForTie;
            _numPtsLoss.Value        = s.PointsForLoss;
            _numPtsNoShow.Value      = s.PointsForNoShow;
            _numPtsToWin.Value       = s.PointsToWinGame;
            SelStr(_cmbScoringMode, s.ScoringMode);
            _numForfeitPM.Value         = s.ForfeitPlusMinus;
            _numForfeitOpponentPM.Value = s.ForfeitOpponentPlusMinus;

            int totalTeams = db.Teams.Count(t => t.Division.SeasonId == seasonId && t.IsActive);
            PopulateTeamsPlayoffsDropdown(totalTeams);
            if (s.TeamsInPlayoffs > 0)
                _cboTeamsPlayoffs.SelectedItem = s.TeamsInPlayoffs.ToString();
            else
                _cboTeamsPlayoffs.SelectedIndex = 0;

            _chkFirstPlace.Checked       = s.FirstPlaceGuaranteed;
            SelStr(_cmbPlayoffTiebreaker, s.PlayoffTiebreakerFormat ?? "none");
        }
        catch { }
        finally
        {
            _isLoadingData = false;
        }

        LoadDivisions(seasonId);
        LoadSeasonSlots(seasonId);
        LoadSeasonCourts(seasonId);
        UpdateDeleteButtonState();
        _isCreatingNew = false;
        ClearDirty();
        ApplyEditorLockState(_chkIsLocked.Checked);
    }

    private void ApplyEditorLockState(bool isLocked)
    {
        // Name, IsCurrent, IsLocked are always enabled regardless of lock state
        _dtpStartDate.Enabled        = !isLocked;
        _numWeeks.Enabled            = !isLocked;
        _dtpPlayoffStart.Enabled     = !isLocked;
        _cmbStatus.Enabled           = !isLocked;
        _txtSeasonFeeAmount.Enabled  = !isLocked;
        _numMaxTeamsDiv.Enabled      = !isLocked;
        _numPlayersMin.Enabled       = !isLocked;
        _numPlayersMax.Enabled       = !isLocked;
        _numPtsWin.Enabled           = !isLocked;
        _numPtsTie.Enabled           = !isLocked;
        _numPtsLoss.Enabled          = !isLocked;
        _numPtsNoShow.Enabled        = !isLocked;
        _numPtsToWin.Enabled         = !isLocked;
        _cmbScoringMode.Enabled      = !isLocked;
        _numForfeitPM.Enabled         = !isLocked;
        _numForfeitOpponentPM.Enabled = !isLocked;
        _cboTeamsPlayoffs.Enabled    = !isLocked;
        _chkFirstPlace.Enabled       = !isLocked;
        _cmbPlayoffTiebreaker.Enabled = !isLocked;
        _daysList.Enabled            = !isLocked;
        _timesList.Enabled           = !isLocked;
        _btnBuild.Enabled            = !isLocked;
        _courtsList.Enabled          = !isLocked;
        _cmbCourtDisplay.Enabled     = !isLocked;
        _btnCourtUp.Enabled          = !isLocked;
        _btnCourtDown.Enabled        = !isLocked;

        // Hide Parameters/Divisions/Slots/Courts tabs when locked; restore when unlocked
        if (_tabs != null && _tabParameters != null)
        {
            if (isLocked)
            {
                if (_tabs.TabPages.Contains(_tabParameters)) _tabs.TabPages.Remove(_tabParameters);
                if (_tabs.TabPages.Contains(_tabDivisions))  _tabs.TabPages.Remove(_tabDivisions);
                if (_tabs.TabPages.Contains(_tabSlots))      _tabs.TabPages.Remove(_tabSlots);
                if (_tabs.TabPages.Contains(_tabCourts))     _tabs.TabPages.Remove(_tabCourts);
            }
            else
            {
                if (!_tabs.TabPages.Contains(_tabParameters)) _tabs.TabPages.Add(_tabParameters);
                if (!_tabs.TabPages.Contains(_tabDivisions))  _tabs.TabPages.Add(_tabDivisions);
                if (!_tabs.TabPages.Contains(_tabSlots))      _tabs.TabPages.Add(_tabSlots);
                if (!_tabs.TabPages.Contains(_tabCourts))     _tabs.TabPages.Add(_tabCourts);
            }
        }

        Refresh();
    }

    private void ClearEditor()
    {
        _selectedSeasonId = null;
        _isCopied = false; _copySourceId = null; _copyDivisions = false; _copyTeams = false;
        _isNewSeasonDraft = false;
        _seasonNameCustomized = false;
        _isCreatingNew = false;

        _txtName.Text = "";
        _dtpStartDate.Value = DateTime.Today;
        _numWeeks.Value = 0;
        _dtpPlayoffStart.Checked = false;
        _chkIsCurrent.Checked = false; _chkIsLocked.Checked = false; _cmbStatus.SelectedIndex = 0; _lblCreatedAt.Text = "";

        _txtSeasonFeeAmount.Text = "0.00";

        _numMaxTeamsDiv.Value = 0;
        _numPlayersMin.Value = 0; _numPlayersMax.Value = 0;
        _numPtsWin.Value = 2; _numPtsTie.Value = 1; _numPtsLoss.Value = 0;
        _numPtsNoShow.Value = -1; _numPtsToWin.Value = 12;
        if (_cmbScoringMode.Items.Count > 0) _cmbScoringMode.SelectedIndex = 0;
        _numForfeitPM.Value = -6; _numForfeitOpponentPM.Value = 1;

        _cboTeamsPlayoffs.Items.Clear();
        _cboTeamsPlayoffs.Items.Add("");
        if (_cboTeamsPlayoffs.Items.Count > 0) _cboTeamsPlayoffs.SelectedIndex = 0;
        _chkFirstPlace.Checked = true;
        if (_cmbPlayoffTiebreaker.Items.Count > 0) _cmbPlayoffTiebreaker.SelectedIndex = 0;

        _btnDelete.Enabled = false;
        _btnCancel.Visible = false;
        _divisionsGrid.Rows.Clear();
        LoadSeasonSlots(null);
        LoadSeasonCourts(null);
        ClearDirty();
        ApplyEditorLockState(false);
    }

    private void PopulateTeamsPlayoffsDropdown(int maxTeams)
    {
        // Valid playoff team counts: powers of 2 and 3x powers of 2
        var validCounts = new[] { 4, 8, 12, 16, 24, 32, 48, 64, 96 };
        _cboTeamsPlayoffs.Items.Clear();
        _cboTeamsPlayoffs.Items.Add("");
        foreach (var count in validCounts)
        {
            if (count <= maxTeams)
                _cboTeamsPlayoffs.Items.Add(count.ToString());
        }
    }

    private void LoadDivisions(int seasonId)
    {
        _divisionsGrid.Rows.Clear();
        try
        {
            using var db = new BocceDbContext();
            var rows = db.Divisions
                .Where(d => d.SeasonId == seasonId)
                .OrderBy(d => d.SortName).ThenBy(d => d.Name)
                .Select(d => new
                {
                    d.Id, d.Name, d.ShortName, d.SortName,
                    Day  = d.DaySlot  != null ? d.DaySlot.DayName      : "-",
                    Time = d.TimeSlot != null ? d.TimeSlot.Timeslot12h : "-",
                    Teams = db.Teams.Count(t => t.DivisionId == d.Id),
                    d.IsActive
                }).ToList();

            foreach (var r in rows)
                _divisionsGrid.Rows.Add(r.Id, $"{r.Name} ({r.Teams})", r.ShortName, r.SortName, r.Day, r.Time, r.Teams, r.IsActive);
            _divisionsGrid.ClearSelection();
        }
        catch { }
    }

    private void LoadSlotsList()
    {
        _daysList.Items.Clear();
        _timesList.Items.Clear();
        try
        {
            using var db = new BocceDbContext();
            foreach (var d in db.DaySlots.Where(d => d.IsActive).OrderBy(d => d.DayNbr).ToList())
                _daysList.Items.Add(new SlotItem(d.Id, d.DayName));
            foreach (var t in db.TimeSlots.Where(t => t.IsActive).OrderBy(t => t.SortOrder).ToList())
                _timesList.Items.Add(new SlotItem(t.Id, t.Timeslot12h));
        }
        catch { }
    }

    private void LoadSeasonSlots(int? seasonId)
    {
        var configuredDays  = new HashSet<int>();
        var configuredTimes = new HashSet<int>();
        if (seasonId.HasValue)
        {
            try
            {
                using var db = new BocceDbContext();
                configuredDays  = db.SeasonDaySlots.Where(s => s.SeasonId == seasonId.Value).Select(s => s.DaySlotId).ToHashSet();
                configuredTimes = db.SeasonTimeSlots.Where(s => s.SeasonId == seasonId.Value).Select(s => s.TimeSlotId).ToHashSet();
            }
            catch { }
        }
        for (int i = 0; i < _daysList.Items.Count; i++)
            if (_daysList.Items[i] is SlotItem di) _daysList.SetItemChecked(i, configuredDays.Contains(di.Id));
        for (int i = 0; i < _timesList.Items.Count; i++)
            if (_timesList.Items[i] is SlotItem ti) _timesList.SetItemChecked(i, configuredTimes.Contains(ti.Id));
    }


    // ── Add Season ────────────────────────────────────────────────────────────

    private void AddSeason()
    {
        if (!_selectedLeagueId.HasValue)
        {
            MessageBox.Show("Select a league in the top bar first.", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _previousSeasonId = _selectedSeasonId;
        _lstSeasons.SelectedIndexChanged -= OnListSeasonSelected;
        _lstSeasons.ClearSelected();
        _lstSeasons.SelectedIndexChanged += OnListSeasonSelected;
        ClearEditor();
        _isCreatingNew = true;  // Set after ClearEditor (which resets it)
        _isNewSeasonDraft = true;
        _seasonNameCustomized = false;
        ApplyDefaultSeasonNameForNewDraft();
        UpdateButtonVisibility();

        try
        {
            using var db = new BocceDbContext();
            var all     = db.Seasons.Where(s => s.LeagueId == _selectedLeagueId.Value).ToList();
            var current = all.FirstOrDefault(s => s.IsCurrent) ?? (all.Count == 1 ? all[0] : null);

            if (current != null)
            {
                var resCopy = MessageBox.Show(
                    $"Copy settings from \"{current.Name}\"?",
                    "New Season", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (resCopy == DialogResult.Yes)
                {
                    var resDivs = MessageBox.Show(
                        "Copy Divisions to the new season?",
                        "Copy Divisions?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    _copyDivisions = resDivs == DialogResult.Yes;

                    if (_copyDivisions)
                    {
                        var resTeams = MessageBox.Show(
                            "Copy Teams to the new divisions?",
                            "Copy Teams?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        _copyTeams = resTeams == DialogResult.Yes;
                    }
                    else
                    {
                        _copyTeams = false;
                    }

                    OfferCopyDate(current);
                }
            }
        }
        catch { }

        _txtName.Focus();
    }

    private void CancelAddSeason()
    {
        _autoSaveTimer.Stop();
        _isCreatingNew = false;
        ClearEditor();
        if (_previousSeasonId.HasValue)
        {
            _selectedSeasonId = _previousSeasonId;
            SelectInList(_previousSeasonId.Value);
            LoadSeason(_previousSeasonId.Value);
        }
        else
        {
            LoadSeasonList();
        }
    }

    private void CancelEditSeason()
    {
        _autoSaveTimer.Stop();
        _isDirty = false;
        if (_selectedSeasonId.HasValue)
            LoadSeason(_selectedSeasonId.Value);
        else
            ClearEditor();
        UpdateButtonVisibility();
    }

    private void OnSeasonStartDateChanged(object? sender, EventArgs e)
    {
        ApplyDefaultSeasonNameForNewDraft();
        MarkDirty();
    }

    private void OnSeasonNameTextChanged(object? sender, EventArgs e)
    {
        if (!_isNewSeasonDraft || _settingSeasonNameProgrammatically)
            return;
        _seasonNameCustomized = true;
        MarkDirty();
    }

    private void ApplyDefaultSeasonNameForNewDraft()
    {
        if (!_isNewSeasonDraft || _seasonNameCustomized || !_selectedLeagueId.HasValue)
            return;

        var name = BuildDefaultSeasonName(_selectedLeagueId.Value, _dtpStartDate.Value);
        _settingSeasonNameProgrammatically = true;
        _txtName.Text = name;
        _settingSeasonNameProgrammatically = false;
    }

    private static string BuildDefaultSeasonName(int leagueId, DateTime startDate)
    {
        var yearName = "Season " + startDate.Year;
        try
        {
            using var db = new BocceDbContext();
            var existingNames = db.Seasons
                .Where(s => s.LeagueId == leagueId)
                .Select(s => s.Name)
                .ToList();

            var yearExists = existingNames.Any(n =>
                string.Equals((n ?? string.Empty).Trim(), yearName, StringComparison.OrdinalIgnoreCase));

            return yearExists ? "Season " + startDate.ToString("MMMM yyyy") : yearName;
        }
        catch
        {
            return yearName;
        }
    }

    private void OfferCopyDate(Season source)
    {
        var defaultDate = source.StartDate?.AddYears(1)
            ?? DateOnly.FromDateTime(DateTime.Today).AddYears(1);

        var picked = PromptDate("New Season Start Date",
            "Enter the start date for the new season:", defaultDate);
        if (picked == null) return;

        _dtpStartDate.Value      = picked.Value.ToDateTime(TimeOnly.MinValue);
        _numWeeks.Value          = source.WeeksInSeason;

        _dtpPlayoffStart.Checked = source.PlayoffStartDate.HasValue;
        if (source.PlayoffStartDate.HasValue)
            _dtpPlayoffStart.Value = source.PlayoffStartDate.Value.AddYears(1).ToDateTime(TimeOnly.MinValue);

        _chkIsCurrent.Checked = false;
        _cmbStatus.SelectedIndex = 0; // Default to "Setup"

        _numMaxTeamsDiv.Value = source.MaxTeamsInDivision;
        _numPlayersMin.Value   = source.PlayersPerTeamMinimum ?? 0;
        _numPlayersMax.Value   = source.PlayersPerTeamMaximum ?? 0;
        _numPtsWin.Value       = source.PointsForWin;
        _numPtsTie.Value       = source.PointsForTie;
        _numPtsLoss.Value      = source.PointsForLoss;
        _numPtsNoShow.Value    = source.PointsForNoShow;
        _numPtsToWin.Value     = source.PointsToWinGame;
        SelStr(_cmbScoringMode, source.ScoringMode);
        _numForfeitPM.Value         = source.ForfeitPlusMinus;
        _numForfeitOpponentPM.Value = source.ForfeitOpponentPlusMinus;

        _chkFirstPlace.Checked       = source.FirstPlaceGuaranteed;
        SelStr(_cmbPlayoffTiebreaker, source.PlayoffTiebreakerFormat ?? "none");

        using (var db = new BocceDbContext())
        {
            int sourceTotalTeams = db.Teams.Count(t => t.Division.SeasonId == source.Id && t.IsActive);
            PopulateTeamsPlayoffsDropdown(sourceTotalTeams);
            if (source.TeamsInPlayoffs > 0)
                _cboTeamsPlayoffs.SelectedItem = source.TeamsInPlayoffs.ToString();
            else
                _cboTeamsPlayoffs.SelectedIndex = 0;

            var sourceFeeParm = db.SeasonParameters
                .FirstOrDefault(p => p.SeasonId == source.Id && p.Key == "SeasonFeeAmount");
            _txtSeasonFeeAmount.Text = sourceFeeParm?.Value ?? "0.00";
        }

        using (var db = new BocceDbContext())
        {
            var sourceDays  = db.SeasonDaySlots .Where(s => s.SeasonId == source.Id).Select(s => s.DaySlotId) .ToHashSet();
            var sourceTimes = db.SeasonTimeSlots.Where(s => s.SeasonId == source.Id).Select(s => s.TimeSlotId).ToHashSet();

            for (int i = 0; i < _daysList.Items.Count; i++)
                if (_daysList.Items[i] is SlotItem di)
                    _daysList.SetItemChecked(i, sourceDays.Contains(di.Id));

            for (int i = 0; i < _timesList.Items.Count; i++)
                if (_timesList.Items[i] is SlotItem ti)
                    _timesList.SetItemChecked(i, sourceTimes.Contains(ti.Id));

            LoadSeasonCourts(source.Id);
        }

        _isCopied     = true;
        _copySourceId = source.Id;
    }


    private void UpdateDeleteButtonState()
    {
        _btnDelete.Enabled = _selectedSeasonId.HasValue;
    }

    private void MarkDirty()
    {
        if (_isLoadingData || _btnSave == null) return;
        _isDirty = true;
        UpdateButtonVisibility();
        if (!_isCreatingNew) { _autoSaveTimer.Stop(); _autoSaveTimer.Start(); }
    }

    private void ClearDirty()
    {
        if (_btnSave == null) return;
        _isDirty = false;
        _autoSaveTimer.Stop();
        UpdateButtonVisibility();
    }

    private void UpdateButtonVisibility()
    {
        _btnSave.Visible = _isCreatingNew;
        if (_isCreatingNew)
        {
            _btnAdd.Visible    = false;
            _btnCancel.Visible = true;
            _btnDelete.Visible = false;
        }
        else
        {
            _btnAdd.Visible    = true;
            _btnCancel.Visible = false;
            _btnDelete.Visible = _selectedSeasonId.HasValue;
        }
    }

    // ── Save ──────────────────────────────────────────────────────────────────

    private void SaveSeason(bool silent = false)
    {
        var name = _txtName.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            if (!silent) { MessageBox.Show("Season name is required.", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Warning); _txtName.Focus(); }
            return;
        }
        if (!_selectedLeagueId.HasValue) return;

        bool isNew = !_selectedSeasonId.HasValue;

        // Check for duplicate season names in the same league
        try
        {
            using var db = new BocceDbContext();
            bool isDuplicate = db.Seasons
                .Where(s => s.LeagueId == _selectedLeagueId.Value && s.Name == name)
                .Any(s => !_selectedSeasonId.HasValue || s.Id != _selectedSeasonId.Value);

            if (isDuplicate)
            {
                if (!silent) MessageBox.Show($"A season named \"{name}\" already exists in this league.", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (_selectedSeasonId.HasValue) LoadSeason(_selectedSeasonId.Value);
                return;
            }
        }
        catch (Exception ex)
        {
            if (!silent) MessageBox.Show($"Validation failed:\n\n{ex.Message}", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        int savedId;

        try
        {
            using var db = new BocceDbContext();

            Season season;
            if (_selectedSeasonId.HasValue)
                season = db.Seasons.Find(_selectedSeasonId.Value) ?? throw new Exception("Season not found.");
            else
            {
                season = new Season { LeagueId = _selectedLeagueId.Value };
                db.Seasons.Add(season);
            }

            ApplyEditorToSeason(season);
            db.SaveChanges();
            savedId = season.Id;

            var selDayIds  = _daysList.CheckedItems.Cast<SlotItem>().Select(s => s.Id).ToHashSet();
            var selTimeIds = _timesList.CheckedItems.Cast<SlotItem>().Select(s => s.Id).ToHashSet();
            db.SeasonDaySlots.RemoveRange(db.SeasonDaySlots.Where(s => s.SeasonId == savedId));
            db.SeasonTimeSlots.RemoveRange(db.SeasonTimeSlots.Where(s => s.SeasonId == savedId));
            foreach (var id in selDayIds)
                db.SeasonDaySlots.Add(new SeasonDaySlot { SeasonId = savedId, DaySlotId = id });
            foreach (var id in selTimeIds)
                db.SeasonTimeSlots.Add(new SeasonTimeSlot { SeasonId = savedId, TimeSlotId = id });
            db.SaveChanges();

            db.SeasonCourts.RemoveRange(db.SeasonCourts.Where(sc => sc.SeasonId == savedId));
            int courtSort = 0;
            for (int i = 0; i < _courtsList.Items.Count; i++)
            {
                if (_courtsList.GetItemChecked(i) && _courtsList.Items[i] is CourtItem ci)
                    db.SeasonCourts.Add(new SeasonCourt { SeasonId = savedId, CourtId = ci.Id, SortOrder = courtSort++ });
            }
            db.SaveChanges();

            var feeParam = db.SeasonParameters
                .FirstOrDefault(p => p.SeasonId == savedId && p.Key == "SeasonFeeAmount");
            if (feeParam == null)
                db.SeasonParameters.Add(new SeasonParameter
                {
                    SeasonId    = savedId,
                    Key         = "SeasonFeeAmount",
                    Value       = _txtSeasonFeeAmount.Text.Trim(),
                    Description = "Seasonal play fee for this season",
                    IsActive    = true
                });
            else
                feeParam.Value = _txtSeasonFeeAmount.Text.Trim();
            db.SaveChanges();

            if (season.IsCurrent)
            {
                var others = db.Seasons
                    .Where(s => s.LeagueId == _selectedLeagueId.Value && s.Id != savedId && s.IsCurrent)
                    .ToList();
                if (others.Count > 0)
                { foreach (var o in others) o.IsCurrent = false; db.SaveChanges(); }
            }

            _lblCreatedAt.Text = season.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        }
        catch (Exception ex)
        {
            if (!silent) MessageBox.Show($"Save failed:\n\n{ex.Message}", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else AppLogger.Error(ex, "Autosave failed for season {Id}", _selectedSeasonId);
            return;
        }

        _selectedSeasonId = savedId;

        if (isNew)
        {
            string divMsg = "";
            if (_isCopied && _copySourceId.HasValue)
            {
                var (divs, teams, players, lft) = CopySeasonData(_copySourceId.Value, savedId, _copyDivisions, _copyTeams);
                var parts = new System.Collections.Generic.List<string>();
                if (divs > 0)    parts.Add($"{divs} division(s)");
                if (teams > 0)   parts.Add($"{teams} team(s)");
                if (players > 0) parts.Add($"{players} player assignment(s)");
                divMsg = parts.Count > 0
                    ? $"\n\n{string.Join(", ", parts)} copied from previous season."
                      + (lft > 0 ? $"\n{lft} Looking For Team entry(s) updated." : "")
                    : "\n\nSettings copied from previous season.";
                _isCopied = false; _copySourceId = null; _copyDivisions = false; _copyTeams = false;
            }
            else
            {
                int n = BuildDivisionsFromSlots(savedId);
                divMsg = n > 0 ? $"\n\n{n} division(s) created from selected day/time slots." : "";
            }
            if (!silent) MessageBox.Show("Season created." + divMsg, "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        _isNewSeasonDraft = false;
        _previousSeasonId = null;
        _isCreatingNew = false;
        ClearDirty();

        LoadSeasonList();
        SelectInList(savedId);
        LoadDivisions(savedId);
        LoadSeasonSlots(savedId);
        LoadSeasonCourts(savedId);
        UpdateDeleteButtonState();
    }

    private void ApplyEditorToSeason(Season s)
    {
        s.Name             = _txtName.Text.Trim();
        s.StartDate        = DateOnly.FromDateTime(_dtpStartDate.Value);
        s.WeeksInSeason    = (int)_numWeeks.Value;
        s.PlayoffStartDate     = _dtpPlayoffStart.Checked ? DateOnly.FromDateTime(_dtpPlayoffStart.Value) : null;
        s.MaxTeamsInDivision   = (int)_numMaxTeamsDiv.Value;
        s.IsCurrent        = _chkIsCurrent.Checked;
        s.IsLocked         = _chkIsLocked.Checked;
        s.Status           = StrVal(_cmbStatus) ?? "Setup";
        s.PlayersPerTeamMinimum = _numPlayersMin.Value > 0 ? (int)_numPlayersMin.Value : (int?)null;
        s.PlayersPerTeamMaximum = _numPlayersMax.Value > 0 ? (int)_numPlayersMax.Value : (int?)null;
        s.PointsForWin     = (int)_numPtsWin.Value;
        s.PointsForTie     = (int)_numPtsTie.Value;
        s.PointsForLoss    = (int)_numPtsLoss.Value;
        s.PointsForNoShow  = (int)_numPtsNoShow.Value;
        s.PointsToWinGame  = (int)_numPtsToWin.Value;
        s.GamesPerMatch    = 2;
        s.ScoringMode      = StrVal(_cmbScoringMode)   ?? "games_mode";
        s.ForfeitPlusMinus         = (int)_numForfeitPM.Value;
        s.ForfeitOpponentPlusMinus = (int)_numForfeitOpponentPM.Value;
        s.TeamsInPlayoffs  = int.TryParse(_cboTeamsPlayoffs.SelectedItem?.ToString(), out var count) ? count : 0;
        s.FirstPlaceGuaranteed  = _chkFirstPlace.Checked;
        s.PlayoffType      = "ladder";
        s.PlayoffGamesPerMatch  = 2;
        s.PlayoffScoringMode    = "match_play";
        s.PlayoffTiebreakerFormat  = StrVal(_cmbPlayoffTiebreaker) ?? "none";
        s.CourtDisplayStyle    = _cmbCourtDisplay.SelectedIndex == 1 ? "letter" : "number";
    }

    // ── Division helpers ──────────────────────────────────────────────────────

    private (int divs, int teams, int players, int lft) CopySeasonData(
        int sourceSeasonId, int newSeasonId, bool copyDivisions, bool copyTeams)
    {
        try
        {
            using var db = new BocceDbContext();

            if (!copyDivisions) return (0, 0, 0, 0);

            var sourceDivs = db.Divisions
                .Where(d => d.SeasonId == sourceSeasonId)
                .ToList();

            var sourceTeams = copyTeams
                ? db.Teams
                    .Include(t => t.TeamPlayers)
                    .Where(t => sourceDivs.Select(d => d.Id).Contains(t.DivisionId))
                    .ToList()
                : [];

            var divMap  = new Dictionary<int, int>();
            var teamMap = new Dictionary<int, int>();

            foreach (var src in sourceDivs)
            {
                var newDiv = new Division
                {
                    SeasonId              = newSeasonId,
                    Name                  = src.Name,
                    ShortName             = src.ShortName,
                    SortName              = src.SortName,
                    DaySlotId             = src.DaySlotId,
                    TimeSlotId            = src.TimeSlotId,
                    PlayersPerTeamMinimum = src.PlayersPerTeamMinimum,
                    PlayersPerTeamMaximum = src.PlayersPerTeamMaximum,
                    TeamCount             = 0,
                    IsActive              = true
                };
                db.Divisions.Add(newDiv);
                db.SaveChanges();
                divMap[src.Id] = newDiv.Id;
            }

            int playerCount = 0;
            foreach (var srcTeam in sourceTeams)
            {
                if (!divMap.TryGetValue(srcTeam.DivisionId, out int newDivId)) continue;

                var newTeam = new Team
                {
                    DivisionId  = newDivId,
                    TeamLetter  = srcTeam.TeamLetter,
                    SystemName  = srcTeam.SystemName,
                    DisplayName = srcTeam.DisplayName,
                    IsActive    = srcTeam.IsActive
                };
                db.Teams.Add(newTeam);
                db.SaveChanges();
                teamMap[srcTeam.Id] = newTeam.Id;

                foreach (var tp in srcTeam.TeamPlayers)
                {
                    db.TeamPlayers.Add(new TeamPlayer
                    {
                        TeamId      = newTeam.Id,
                        PlayerId    = tp.PlayerId,
                        Role        = tp.Role,
                        IsActive    = tp.IsActive,
                        JoinedDate  = DateOnly.FromDateTime(DateTime.Today)
                    });
                    playerCount++;
                }

                if (srcTeam.CaptainPlayerId.HasValue)
                    newTeam.CaptainPlayerId = srcTeam.CaptainPlayerId;
            }
            if (sourceTeams.Count > 0) db.SaveChanges();

            int lftUpdated = 0;
            if (teamMap.Count > 0)
            {
                var lftEntries = db.LookingForTeams
                    .Where(l => l.TeamId.HasValue && teamMap.Keys.Contains(l.TeamId.Value))
                    .ToList();
                foreach (var lft in lftEntries)
                {
                    if (teamMap.TryGetValue(lft.TeamId!.Value, out int newTeamId))
                    {
                        lft.TeamId = newTeamId;
                        lftUpdated++;
                    }
                }
                if (lftUpdated > 0) db.SaveChanges();
            }

            // Copy SeasonFeeAmount parameter and assign fees to spare list and team players
            var league = db.Seasons.Where(s => s.Id == newSeasonId).Select(s => s.LeagueId).FirstOrDefault();
            FeeService.CopySeasonFeeParameter(db, sourceSeasonId, newSeasonId);
            FeeService.AssignSeasonFeesForSpareList(db, newSeasonId, league);
            FeeService.AssignSeasonFeesForTeamPlayers(db, newSeasonId);

            return (sourceDivs.Count, sourceTeams.Count, playerCount, lftUpdated);
        }
        catch { return (0, 0, 0, 0); }
    }

    private int BuildDivisionsFromSlots(int seasonId)
    {
        var selDays  = _daysList.CheckedItems.Cast<SlotItem>().ToList();
        var selTimes = _timesList.CheckedItems.Cast<SlotItem>().ToList();
        if (selDays.Count == 0 || selTimes.Count == 0) return 0;

        try
        {
            using var db = new BocceDbContext();
            var dayIds  = selDays.Select(d => d.Id).ToHashSet();
            var timeIds = selTimes.Select(t => t.Id).ToHashSet();
            var days    = db.DaySlots.Where(d => dayIds.Contains(d.Id)).OrderBy(d => d.DayNbr).ToList();
            var times   = db.TimeSlots.Where(t => timeIds.Contains(t.Id)).OrderBy(t => t.SortOrder).ToList();

            var existingCombos = db.Divisions
                .Where(d => d.SeasonId == seasonId && d.DaySlotId != null && d.TimeSlotId != null)
                .Select(d => new { d.DaySlotId, d.TimeSlotId })
                .ToList();

            int count = 0;
            foreach (var day in days)
            {
                foreach (var time in times)
                {
                    if (existingCombos.Any(e => e.DaySlotId == day.Id && e.TimeSlotId == time.Id)) continue;
                    db.Divisions.Add(new Division
                    {
                        SeasonId      = seasonId,
                        Name          = $"{day.DayName} {time.Timeslot12h}",
                        ShortName     = DivShortName(day.DayAbbr, time.Timeslot24h),
                        SortName      = $"{day.DayNbr}-{time.Timeslot24h}",
                        DaySlotId     = day.Id,
                        TimeSlotId    = time.Id,
                        TeamCount     = 0,
                        IsActive      = true
                    });
                    count++;
                }
            }
            db.SaveChanges();
            return count;
        }
        catch { return 0; }
    }

    private void OnDivisionCellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.ColumnIndex == _divisionsGrid.Columns["DivAct"].Index && e.RowIndex >= 0)
            _divisionsGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
    }

    private void OnDivisionActiveChanged(object? sender, DataGridViewCellEventArgs e)
    {
        var col = _divisionsGrid.Columns["DivAct"];
        if (col == null || e.ColumnIndex != col.Index || e.RowIndex < 0) return;
        var row = _divisionsGrid.Rows[e.RowIndex];
        if (row.Cells["DivId"].Value == null || row.Cells["DivId"].Value == DBNull.Value) return;
        int divId    = Convert.ToInt32(row.Cells["DivId"].Value);
        bool isActive = Convert.ToBoolean(row.Cells["DivAct"].Value);
        try
        {
            using var db = new BocceDbContext();
            var div = db.Divisions.Find(divId);
            if (div != null) { div.IsActive = isActive; db.SaveChanges(); }
        }
        catch { }
    }

    private void OnBuildDivisions(object? sender, EventArgs e)
    {
        if (!_selectedSeasonId.HasValue)
        {
            MessageBox.Show("Save the season first, then build divisions.", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (_numWeeks.Value == 0)
        {
            MessageBox.Show("Enter the number of weeks in the season before building divisions.", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        int seasonId   = _selectedSeasonId.Value;
        var selDayIds  = _daysList.CheckedItems.Cast<SlotItem>().Select(s => s.Id).ToHashSet();
        var selTimeIds = _timesList.CheckedItems.Cast<SlotItem>().Select(s => s.Id).ToHashSet();

        var orphans = new List<(int Id, string Name, int Teams, int Players)>();
        try
        {
            using var db = new BocceDbContext();
            var existing = db.Divisions
                .Include(d => d.Teams).ThenInclude(t => t.TeamPlayers)
                .Where(d => d.SeasonId == seasonId).ToList();

            foreach (var div in existing)
            {
                bool covered = div.DaySlotId.HasValue  && selDayIds.Contains(div.DaySlotId.Value)
                            && div.TimeSlotId.HasValue && selTimeIds.Contains(div.TimeSlotId.Value);
                if (!covered)
                    orphans.Add((div.Id, div.Name, div.Teams.Count,
                                 div.Teams.Sum(t => t.TeamPlayers.Count)));
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error checking existing divisions:\n{ex.Message}", "Golden Vista Bocce League Master",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (orphans.Count > 0)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("The following divisions no longer match the selected day/time slots and will be DELETED:\n");
            foreach (var (_, name, tc, pc) in orphans)
            {
                sb.Append($"  - {name}");
                if (tc > 0) sb.Append($"  ({tc} team(s), {pc} player assignment(s) removed)");
                sb.AppendLine();
            }
            sb.AppendLine();
            sb.AppendLine("Teams will be disbanded.  Players are NOT deleted.");
            sb.AppendLine();
            sb.AppendLine("Continue?");

            if (MessageBox.Show(sb.ToString(), "Confirm Delete Orphaned Divisions",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;

            try
            {
                using var db = new BocceDbContext();
                var ids = orphans.Select(o => o.Id).ToHashSet();
                var toDelete = db.Divisions
                    .Include(d => d.Teams).ThenInclude(t => t.TeamPlayers)
                    .Where(d => ids.Contains(d.Id)).ToList();
                foreach (var div in toDelete)
                {
                    foreach (var team in div.Teams)
                        db.TeamPlayers.RemoveRange(team.TeamPlayers);
                    db.Teams.RemoveRange(div.Teams);
                }
                db.Divisions.RemoveRange(toDelete);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Delete failed:\n{ex.Message}", "Golden Vista Bocce League Master",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        int n = BuildDivisionsFromSlots(seasonId);

        var msg = orphans.Count > 0 ? $"{orphans.Count} division(s) removed." : "";
        if (n > 0) msg += (msg.Length > 0 ? "\n" : "") + $"{n} new division(s) created.";
        if (msg.Length == 0) msg = "No changes - all selected slot combinations already have divisions.";

        MessageBox.Show(msg, "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Information);
        LoadDivisions(seasonId);
        LoadSeasonSlots(seasonId);
    }

    private void OnDeleteDivision(object? sender, EventArgs e)
    {
        if (_divisionsGrid.SelectedRows.Count == 0) return;
        int divId      = Convert.ToInt32(_divisionsGrid.SelectedRows[0].Cells["DivId"].Value);
        string divName = _divisionsGrid.SelectedRows[0].Cells["DivName"].Value?.ToString() ?? "";

        int teamCount = 0, playerCount = 0;
        try
        {
            using var db = new BocceDbContext();
            teamCount   = db.Teams.Count(t => t.DivisionId == divId);
            playerCount = db.TeamPlayers.Count(tp => tp.Team.DivisionId == divId);
        }
        catch { }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Delete division \"{divName}\"?");
        if (teamCount > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"  Teams to be disbanded ......... {teamCount}");
            sb.AppendLine($"  Player assignments removed .... {playerCount}");
            sb.AppendLine();
            sb.AppendLine("Players are NOT deleted - only their team assignments.");
        }
        sb.AppendLine();
        sb.AppendLine("This cannot be undone. Continue?");

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

        if (_selectedSeasonId.HasValue) LoadDivisions(_selectedSeasonId.Value);
    }

    // ── Delete Season ─────────────────────────────────────────────────────────

    private void DeleteSeason()
    {
        if (!_selectedSeasonId.HasValue) return;
        int seasonId = _selectedSeasonId.Value;
        string seasonName = _txtName.Text.Trim();

        int divCount = 0, teamCount = 0;
        try
        {
            using var db = new BocceDbContext();
            divCount  = db.Divisions.Count(d => d.SeasonId == seasonId);
            teamCount = db.Teams.Count(t => t.Division.SeasonId == seasonId);
        }
        catch { }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Permanently delete \"{seasonName}\"?");
        sb.AppendLine();
        sb.AppendLine($"  Divisions ............. {divCount}");
        sb.AppendLine($"  Teams ................. {teamCount}");
        sb.AppendLine();
        sb.AppendLine("This cannot be undone. Continue?");

        if (MessageBox.Show(sb.ToString(), "Confirm Delete",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;

        try
        {
            using var db = new BocceDbContext();
            var season = db.Seasons
                .Include(s => s.Divisions).ThenInclude(d => d.Teams).ThenInclude(t => t.TeamPlayers)
                .FirstOrDefault(s => s.Id == seasonId);
            if (season == null) return;

            var divisionIds = season.Divisions.Select(d => d.Id).ToList();

            foreach (var div in season.Divisions)
                foreach (var team in div.Teams)
                    team.CaptainPlayerId = null;
            db.SaveChanges();

            var weekIds = db.ScheduleWeeks.Where(w => divisionIds.Contains(w.DivisionId))
                                          .Select(w => w.Id).ToList();
            if (weekIds.Count > 0)
            {
                var matchIds = db.Matches.Where(m => weekIds.Contains(m.ScheduleWeekId))
                                         .Select(m => m.Id).ToList();
                db.Games.RemoveRange(db.Games.Where(g => matchIds.Contains(g.MatchId)));
                db.MatchTeamResults.RemoveRange(db.MatchTeamResults.Where(r => matchIds.Contains(r.MatchId)));
                db.Matches.RemoveRange(db.Matches.Where(m => weekIds.Contains(m.ScheduleWeekId)));
            }
            db.ScheduleWeeks.RemoveRange(db.ScheduleWeeks.Where(w => divisionIds.Contains(w.DivisionId)));

            var playoffMatchIds = db.PlayoffMatches.Where(pm => pm.SeasonId == seasonId)
                                                   .Select(pm => pm.Id).ToList();
            foreach (var pmId in playoffMatchIds)
                db.PlayoffGames.RemoveRange(db.PlayoffGames.Where(pg => pg.PlayoffMatchId == pmId));
            db.PlayoffMatches.RemoveRange(db.PlayoffMatches.Where(pm => pm.SeasonId == seasonId));
            db.PlayoffRounds.RemoveRange(db.PlayoffRounds.Where(pr => pr.SeasonId == seasonId));

            foreach (var div in season.Divisions)
            {
                db.TeamStandings.RemoveRange(db.TeamStandings.Where(x => x.DivisionId == div.Id));
                foreach (var team in div.Teams)
                {
                    db.TeamPlayers.RemoveRange(team.TeamPlayers);
                }
                db.Teams.RemoveRange(div.Teams);
            }
            db.Divisions.RemoveRange(season.Divisions);

            db.SeasonParameters.RemoveRange(db.SeasonParameters.Where(x => x.SeasonId == seasonId));
            db.SeasonDaySlots.RemoveRange(db.SeasonDaySlots.Where(x => x.SeasonId == seasonId));
            db.SeasonTimeSlots.RemoveRange(db.SeasonTimeSlots.Where(x => x.SeasonId == seasonId));
            db.SeasonCourts.RemoveRange(db.SeasonCourts.Where(x => x.SeasonId == seasonId));
            db.SeasonFees.RemoveRange(db.SeasonFees.Where(x => x.SeasonId == seasonId));

            db.Seasons.Remove(season);
            db.SaveChanges();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Delete failed:\n{ex.Message}", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        MessageBox.Show("Season deleted.", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Information);
        _selectedSeasonId = null;
        LoadSeasonList();
        if (_lstSeasons.Items.Count == 0) ClearEditor();
    }

    // ── Date prompt ───────────────────────────────────────────────────────────

    private DateOnly? PromptDate(string caption, string message, DateOnly defaultDate)
    {
        using var form = new Form
        {
            Text = caption, Width = 360, Height = 148,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false,
            BackColor = AppTheme.ContentBackground
        };
        var lbl = new Label { Text = message, Left = 12, Top = 14, Width = 330, Height = 20, ForeColor = AppTheme.TextPrimary, Font = AppTheme.FontDefault };
        var dtp = new DateTimePicker { Left = 12, Top = 38, Width = 200, Format = DateTimePickerFormat.Short, Font = AppTheme.FontDefault, Value = defaultDate.ToDateTime(TimeOnly.MinValue) };
        var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Left = 168, Top = 72, Width = 80, Height = 30, FlatStyle = FlatStyle.Flat, BackColor = AppTheme.Accent, ForeColor = Color.White, Font = AppTheme.FontButton };
        var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 257, Top = 72, Width = 80, Height = 30, FlatStyle = FlatStyle.Flat, Font = AppTheme.FontButton };
        form.Controls.AddRange([lbl, dtp, btnOk, btnCancel]);
        form.AcceptButton = btnOk; form.CancelButton = btnCancel;
        return form.ShowDialog(this) == DialogResult.OK ? DateOnly.FromDateTime(dtp.Value) : null;
    }

    // ── Control factories ─────────────────────────────────────────────────────

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

    private static ComboBox StrCombo(int x, int y, int w, params (string key, string label)[] items)
    {
        var cb = new ComboBox { Location = new Point(x, y), Width = w, DropDownStyle = ComboBoxStyle.DropDownList, Font = AppTheme.FontDefault };
        foreach (var (key, label) in items) cb.Items.Add(new StrItem(key, label));
        if (cb.Items.Count > 0) cb.SelectedIndex = 0;
        return cb;
    }

    private static string? StrVal(ComboBox cb) => cb.SelectedItem is StrItem si ? si.Key : null;

    private static void SelStr(ComboBox cb, string value)
    {
        for (int i = 0; i < cb.Items.Count; i++)
            if (cb.Items[i] is StrItem si && si.Key == value) { cb.SelectedIndex = i; return; }
        if (cb.Items.Count > 0) cb.SelectedIndex = 0;
    }

    private static string DivShortName(string dayAbbr, string time24h)
    {
        var prefix = dayAbbr.Length >= 2
            ? $"{char.ToUpper(dayAbbr[0])}{char.ToLower(dayAbbr[1])}"
            : dayAbbr;
        return $"{prefix}-{time24h}";
    }

    private static TableLayoutPanel MakeLayout(Control fill, Panel toolbar)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2,
            Padding = Padding.Empty, Margin = Padding.Empty,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, toolbar.Height));
        fill.Dock = DockStyle.Fill; toolbar.Dock = DockStyle.Fill;
        layout.Controls.Add(fill, 0, 0);
        layout.Controls.Add(toolbar, 0, 1);
        return layout;
    }

    private sealed record ListItem(int Id, string Name)    { public override string ToString() => Name; }
    private sealed record StrItem(string Key, string Label) { public override string ToString() => Label; }
    private sealed record SlotItem(int Id, string Display)  { public override string ToString() => Display; }
    private sealed record CourtItem(int Id, string Display) { public override string ToString() => Display; }
}

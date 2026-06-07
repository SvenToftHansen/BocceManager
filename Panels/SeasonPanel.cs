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
    private bool _isNewSeasonDraft = false;
    private bool _seasonNameCustomized = false;
    private bool _settingSeasonNameProgrammatically = false;

    // ── State ─────────────────────────────────────────────────────────────────
    private int? _selectedLeagueId;
    private int? _selectedSeasonId;
    private int? _previousSeasonId;
    private bool _isCopied;
    private int? _copySourceId;

    // ── Left panel ────────────────────────────────────────────────────────────
    private TextBox _txtSearch   = null!;
    private ListBox _lstSeasons  = null!;

    // ── Editor – basic ────────────────────────────────────────────────────────
    private TextBox        _txtName           = null!;
    private DateTimePicker _dtpStartDate      = null!;
    private ThemedNumericUpDown _numWeeks          = null!;
    private ThemedNumericUpDown _numGamesPerSeason = null!;
    private DateTimePicker _dtpPlayoffStart   = null!;
    private CheckBox       _chkIsCurrent      = null!;
    private CheckBox       _chkActive         = null!;
    private Label          _lblCreatedAt      = null!;

    // ── Editor – division defaults ────────────────────────────────────────────
    private ThemedNumericUpDown _numMaxTeamsDiv = null!;

    // ── Editor – scoring ──────────────────────────────────────────────────────
    private ComboBox      _cmbGameInterval   = null!;
    private CheckBox      _chkTimeslotDriven = null!;
    private ThemedNumericUpDown _numPlayersMin     = null!;
    private ThemedNumericUpDown _numPlayersMax     = null!;
    private ThemedNumericUpDown _numPtsWin         = null!;
    private ThemedNumericUpDown _numPtsTie         = null!;
    private ThemedNumericUpDown _numPtsLoss        = null!;
    private ThemedNumericUpDown _numPtsNoShow      = null!;
    private ThemedNumericUpDown _numPtsToWin       = null!;
    private ThemedNumericUpDown _numGamesPerMatch  = null!;
    private ComboBox      _cmbScoringMode    = null!;

    // ── Editor – playoff settings ─────────────────────────────────────────────
    private ThemedNumericUpDown _numTeamsPlayoffs     = null!;
    private CheckBox      _chkFirstPlace        = null!;
    private ComboBox      _cmbPlayoffType       = null!;
    private ThemedNumericUpDown _numPlayoffGames      = null!;
    private ComboBox      _cmbPlayoffScoring    = null!;
    private CheckBox      _chkPlayoffTiebreaker = null!;

    private Button _btnEdit   = null!;
    private Button _btnSave   = null!;
    private Button _btnDelete = null!;
    private Button _btnCancel = null!;

    // ── Divisions tab ─────────────────────────────────────────────────────────
    private DataGridView _divisionsGrid = null!;

    // ── Slots tab ─────────────────────────────────────────────────────────────
    private CheckedListBox _daysList  = null!;
    private CheckedListBox _timesList = null!;
    private Button         _btnBuild  = null!;

    // All seasons for search filtering
    private List<(int Id, string Display)> _allSeasons = [];

    // ─────────────────────────────────────────────────────────────────────────

    public SeasonPanel()
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

        _txtSearch = new TextBox
        {
            Dock = DockStyle.Top,
            Font = AppTheme.FontDefault,
            ForeColor = AppTheme.TextSecondary,
            BackColor = AppTheme.ContentBackground,
            Text = "Search...",
            Height = 28,
            Margin = new Padding(0, 0, 0, 6)
        };
        _txtSearch.Enter += (_, _) => { if (_txtSearch.Text == "Search...") { _txtSearch.Text = ""; _txtSearch.ForeColor = AppTheme.TextPrimary; } };
        _txtSearch.Leave += (_, _) => { if (string.IsNullOrEmpty(_txtSearch.Text)) { _txtSearch.Text = "Search..."; _txtSearch.ForeColor = AppTheme.TextSecondary; } };
        _txtSearch.TextChanged += (_, _) => FilterSeasonList();

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

        var btnNew = new Button
        {
            Dock = DockStyle.Bottom,
            Text = "+ New Season",
            Height = 32,
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.ButtonSuccess,
            ForeColor = Color.White,
            Font = AppTheme.FontButton,
            Cursor = Cursors.Hand,
            FlatAppearance = { BorderSize = 0 }
        };
        btnNew.Click += (_, _) => StartNewSeason();

        panel.Controls.Add(_lstSeasons);
        panel.Controls.Add(_txtSearch);
        panel.Controls.Add(lblTitle);
        panel.Controls.Add(btnNew);
    }

    private void BuildRightPanel(SplitterPanel panel)
    {
        var toolbar = BuildSaveToolbar();
        var tabs = BuildTabs();

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2,
            Padding = Padding.Empty, Margin = Padding.Empty,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, toolbar.Height));
        tabs.Dock = DockStyle.Fill;
        toolbar.Dock = DockStyle.Fill;
        layout.Controls.Add(tabs, 0, 0);
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
        tabs.TabPages.Add(BuildParametersTab());
        tabs.TabPages.Add(BuildDivisionsTab());
        tabs.TabPages.Add(BuildSlotsTab());
        return tabs;
    }

    // ── Editor Tab ────────────────────────────────────────────────────────────

    private TabPage BuildEditorTab()
    {
        var page   = new TabPage("  Editor  ");
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

        Add(Lbl("Start Date", lx, y));
        _dtpStartDate = new DateTimePicker { Location = new Point(ix, y), Width = 180, Format = DateTimePickerFormat.Short, Font = AppTheme.FontDefault };
        _dtpStartDate.ValueChanged += OnSeasonStartDateChanged;
        Add(_dtpStartDate); y += 44;

        Add(Lbl("Weeks in Season", lx, y));
        _numWeeks = Num(ix, y, 0, 99);
        Add(_numWeeks, Hint("Required before divisions can be auto-built", ix + 100, y + 4)); y += 44;

        Add(Lbl("Games per Season", lx, y));
        _numGamesPerSeason = Num(ix, y, 0, 999);
        Add(_numGamesPerSeason, Hint("Total games each team plays", ix + 100, y + 4)); y += 44;

        // ── Status ────────────────────────────────────────────────────────
        Add(Sep(lx, y, iw + ix - lx)); y += 10;
        Add(SecHdr("Status", lx, y)); y += 34;

        Add(Lbl("Is Current Season", lx, y));
        _chkIsCurrent = new CheckBox { Location = new Point(ix, y), AutoSize = true, Font = AppTheme.FontDefault, ForeColor = AppTheme.TextPrimary };
        Add(_chkIsCurrent, Hint("Only one season per league can be current (★)", ix + 26, y + 4)); y += 38;

        Add(Lbl("Active", lx, y));
        _chkActive = new CheckBox { Location = new Point(ix, y), AutoSize = true, Font = AppTheme.FontDefault, ForeColor = AppTheme.TextPrimary, Checked = true };
        Add(_chkActive); y += 38;

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
        var page   = new TabPage("  Parameters  ");
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = AppTheme.ContentBackground };

        const int lx = 20, ix = 220, iw = 440;
        int y = 20;
        var cc = new List<Control>();
        void Add(params Control[] items) => cc.AddRange(items);

        // ── Division Defaults ─────────────────────────────────────────────
        Add(SecHdr("Division Defaults", lx, y)); y += 34;

        Add(Lbl("Max Teams / Division", lx, y));
        _numMaxTeamsDiv = Num(ix, y, 0, 99);
        Add(_numMaxTeamsDiv, Hint("0 = use league default  |  Divisions inherit this unless they set their own value", ix + 100, y + 4)); y += 44;

        Add(Sep(lx, y, iw + ix - lx)); y += 10;

        // ── Scoring Parameters ────────────────────────────────────────────
        Add(SecHdr("Scoring Parameters", lx, y)); y += 34;

        Add(Lbl("Game Interval", lx, y));
        _cmbGameInterval = StrCombo(ix, y, 300,
            ("weekly",              "Weekly - same day each week"),
            ("schedule_determined", "Schedule Determined"));
        Add(_cmbGameInterval); y += 44;

        Add(Lbl("Timeslot Driven", lx, y));
        _chkTimeslotDriven = new CheckBox { Location = new Point(ix, y), AutoSize = true, Font = AppTheme.FontDefault, ForeColor = AppTheme.TextPrimary, Checked = true };
        Add(_chkTimeslotDriven, Hint("Divisions play at fixed day/time slots", ix + 26, y + 4)); y += 38;

        Add(Lbl("Players / Team Min", lx, y));
        _numPlayersMin = Num(ix, y, 0, 99);
        Add(_numPlayersMin); y += 38;

        Add(Lbl("Players / Team Max", lx, y));
        _numPlayersMax = Num(ix, y, 0, 99);
        Add(_numPlayersMax); y += 38;

        Add(Lbl("Points for Win", lx, y));
        _numPtsWin = Num(ix, y, -99, 99, 2);
        Add(_numPtsWin); y += 38;

        Add(Lbl("Points for Tie", lx, y));
        _numPtsTie = Num(ix, y, -99, 99, 1);
        Add(_numPtsTie); y += 38;

        Add(Lbl("Points for Loss", lx, y));
        _numPtsLoss = Num(ix, y, -99, 99, 0);
        Add(_numPtsLoss); y += 38;

        Add(Lbl("Points for No Show", lx, y));
        _numPtsNoShow = Num(ix, y, -99, 99, -1);
        Add(_numPtsNoShow); y += 38;

        Add(Lbl("Points to Win Game", lx, y));
        _numPtsToWin = Num(ix, y, 1, 99, 12);
        Add(_numPtsToWin); y += 38;

        Add(Lbl("Games per Match", lx, y));
        _numGamesPerMatch = Num(ix, y, 1, 99, 2);
        Add(_numGamesPerMatch); y += 38;

        Add(Lbl("Scoring Mode", lx, y));
        _cmbScoringMode = StrCombo(ix, y, 260,
            ("games_mode",       "Games Mode"),
            ("match_score_mode", "Match Score Mode"),
            ("match_play",       "Match Play"));
        Add(_cmbScoringMode); y += 28;
        Add(new Label
        {
            Text = "Games Mode: most games won wins match  |  Match Score: cumulative points across games  |  Match Play: win/loss per match overall",
            Location = new Point(lx, y), Size = new Size(iw + ix - lx - 10, 28),
            Font = AppTheme.FontSmall, ForeColor = AppTheme.TextMuted
        }); y += 32;

        // ── Playoff Settings ──────────────────────────────────────────────
        Add(Sep(lx, y, iw + ix - lx)); y += 10;
        Add(SecHdr("Playoff Settings", lx, y)); y += 34;

        Add(Lbl("Teams in Playoffs", lx, y));
        _numTeamsPlayoffs = Num(ix, y, 0, 99, 0);
        Add(_numTeamsPlayoffs, Hint("0 = no playoffs", ix + 100, y + 4)); y += 38;

        Add(Lbl("Playoff Start Date", lx, y));
        _dtpPlayoffStart = new DateTimePicker { Location = new Point(ix, y), Width = 200, Format = DateTimePickerFormat.Short, Font = AppTheme.FontDefault, ShowCheckBox = true, Checked = false };
        Add(_dtpPlayoffStart, Hint("Optional - uncheck if not yet known", ix + 212, y + 4)); y += 44;

        Add(Lbl("First Place Guaranteed", lx, y));
        _chkFirstPlace = new CheckBox { Location = new Point(ix, y), AutoSize = true, Font = AppTheme.FontDefault, ForeColor = AppTheme.TextPrimary, Checked = true };
        Add(_chkFirstPlace); y += 38;

        Add(Lbl("Playoff Type", lx, y));
        _cmbPlayoffType = StrCombo(ix, y, 200,
            ("ladder",      "Ladder"),
            ("round_robin", "Round Robin"));
        Add(_cmbPlayoffType); y += 44;

        Add(Lbl("Playoff Games / Match", lx, y));
        _numPlayoffGames = Num(ix, y, 1, 99, 2);
        Add(_numPlayoffGames); y += 38;

        Add(Lbl("Playoff Scoring Mode", lx, y));
        _cmbPlayoffScoring = StrCombo(ix, y, 260,
            ("match_play",       "Match Play"),
            ("games_mode",       "Games Mode"),
            ("match_score_mode", "Match Score Mode"));
        Add(_cmbPlayoffScoring); y += 44;

        Add(Lbl("Playoff Tiebreaker End", lx, y));
        _chkPlayoffTiebreaker = new CheckBox { Location = new Point(ix, y), AutoSize = true, Font = AppTheme.FontDefault, ForeColor = AppTheme.TextPrimary, Checked = true };
        Add(_chkPlayoffTiebreaker); y += 50;

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

        _btnEdit = new Button
        {
            Text = "Edit Season", Location = new Point(12, 10), Size = new Size(130, 32),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.Accent, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 },
            Visible = false
        };
        _btnEdit.Click += (_, _) => EnterEditMode();

        _btnDelete = new Button
        {
            Text = "Delete Season", Location = new Point(150, 10), Size = new Size(140, 32),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.ButtonDanger, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand,
            FlatAppearance = { BorderSize = 0 }, Enabled = false, Visible = false
        };
        _btnDelete.Click += (_, _) => DeleteSeason();

        _btnSave = new Button
        {
            Text = "Save Season", Location = new Point(12, 10), Size = new Size(130, 32),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.Accent, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 },
            Visible = false
        };
        _btnSave.Click += (_, _) => SaveSeason();

        _btnCancel = new Button
        {
            Text = "Cancel", Location = new Point(150, 10), Size = new Size(140, 32),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.Surface, ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand,
            FlatAppearance = { BorderSize = 1, BorderColor = AppTheme.Separator },
            Visible = false
        };
        _btnCancel.Click += (_, _) => ExitEditMode();

        toolbar.Controls.AddRange([_btnEdit, _btnDelete, _btnSave, _btnCancel]);
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
        daysPanel.Controls.Add(_daysList);
        daysPanel.Controls.Add(new Label { Text = "Play Days", Dock = DockStyle.Top, Height = 28, Font = AppTheme.FontSectionHeading, ForeColor = AppTheme.Accent });

        var timesPanel = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.ContentBackground, Padding = new Padding(20, 0, 0, 0) };
        _timesList = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true, Font = AppTheme.FontDefault, BackColor = AppTheme.Surface, ForeColor = AppTheme.TextPrimary, BorderStyle = BorderStyle.FixedSingle };
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

    // ── Data Loading ──────────────────────────────────────────────────────────

    private void LoadContext()
    {
        try
        {
            using var db = new BocceDbContext();
            _selectedLeagueId = AppParameterService.GetDefaultLeagueId(db);
        }
        catch { _selectedLeagueId = null; }

        LoadSeasonList();
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
                        .Select(s => new { s.Id, s.Name, s.IsCurrent, s.IsActive })
                        .AsEnumerable()
                        .Select(s => (s.Id, s.Name + (s.IsCurrent ? "  ★" : "") + (s.IsActive ? "" : " (inactive)")))
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
        var query = _txtSearch.Text == "Search..." ? "" : _txtSearch.Text;

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
        if (_lstSeasons.SelectedItem is ListItem li)
            LoadSeason(li.Id);
        else
            ClearEditor();
    }

    private void LoadSeason(int seasonId)
    {
        _selectedSeasonId = seasonId;
        _isCopied = false; _copySourceId = null;
        _isNewSeasonDraft = false;
        _seasonNameCustomized = false;

        try
        {
            using var db = new BocceDbContext();
            var s = db.Seasons.Find(seasonId);
            if (s == null) return;

            _txtName.Text            = s.Name;
            _dtpStartDate.Value      = s.StartDate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.Today;
            _numWeeks.Value          = s.WeeksInSeason;
            _numGamesPerSeason.Value = s.GamesPerSeason;

            _dtpPlayoffStart.Checked = s.PlayoffStartDate.HasValue;
            if (s.PlayoffStartDate.HasValue)
                _dtpPlayoffStart.Value = s.PlayoffStartDate.Value.ToDateTime(TimeOnly.MinValue);

            bool onlyOne = db.Seasons.Count(x => x.LeagueId == s.LeagueId) == 1;
            _chkIsCurrent.Checked    = s.IsCurrent || onlyOne;
            _chkActive.Checked       = s.IsActive;
            _lblCreatedAt.Text       = s.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

            _numMaxTeamsDiv.Value = s.MaxTeamsInDivision;
            SelStr(_cmbGameInterval, s.GameInterval);
            _chkTimeslotDriven.Checked = s.TimeslotDriven;
            _numPlayersMin.Value     = s.PlayersPerTeamMinimum ?? 0;
            _numPlayersMax.Value     = s.PlayersPerTeamMaximum ?? 0;
            _numPtsWin.Value         = s.PointsForWin;
            _numPtsTie.Value         = s.PointsForTie;
            _numPtsLoss.Value        = s.PointsForLoss;
            _numPtsNoShow.Value      = s.PointsForNoShow;
            _numPtsToWin.Value       = s.PointsToWinGame;
            _numGamesPerMatch.Value  = s.GamesPerMatch;
            SelStr(_cmbScoringMode, s.ScoringMode);

            _numTeamsPlayoffs.Value      = s.TeamsInPlayoffs;
            _chkFirstPlace.Checked       = s.FirstPlaceGuaranteed;
            SelStr(_cmbPlayoffType, s.PlayoffType);
            _numPlayoffGames.Value       = s.PlayoffGamesPerMatch;
            SelStr(_cmbPlayoffScoring, s.PlayoffScoringMode);
            _chkPlayoffTiebreaker.Checked = s.PlayoffTiebreakerEnd;
        }
        catch { }

        LoadDivisions(seasonId);
        LoadSeasonSlots(seasonId);
        SetEditModeUI(false);
    }

    private void ClearEditor()
    {
        _selectedSeasonId = null;
        _isCopied = false; _copySourceId = null;
        _isNewSeasonDraft = false;
        _seasonNameCustomized = false;

        _txtName.Text = "";
        _dtpStartDate.Value = DateTime.Today;
        _numWeeks.Value = 0; _numGamesPerSeason.Value = 0;
        _dtpPlayoffStart.Checked = false;
        _chkIsCurrent.Checked = false; _chkActive.Checked = true; _lblCreatedAt.Text = "";

        _numMaxTeamsDiv.Value = 0;
        if (_cmbGameInterval.Items.Count > 0) _cmbGameInterval.SelectedIndex = 0;
        _chkTimeslotDriven.Checked = true;
        _numPlayersMin.Value = 0; _numPlayersMax.Value = 0;
        _numPtsWin.Value = 2; _numPtsTie.Value = 1; _numPtsLoss.Value = 0;
        _numPtsNoShow.Value = -1; _numPtsToWin.Value = 12; _numGamesPerMatch.Value = 2;
        if (_cmbScoringMode.Items.Count > 0) _cmbScoringMode.SelectedIndex = 0;

        _numTeamsPlayoffs.Value = 0; _chkFirstPlace.Checked = true;
        if (_cmbPlayoffType.Items.Count > 0) _cmbPlayoffType.SelectedIndex = 0;
        _numPlayoffGames.Value = 2;
        if (_cmbPlayoffScoring.Items.Count > 0) _cmbPlayoffScoring.SelectedIndex = 0;
        _chkPlayoffTiebreaker.Checked = true;

        _btnDelete.Enabled = false;
        _divisionsGrid.Rows.Clear();
        LoadSeasonSlots(null);
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
                _divisionsGrid.Rows.Add(r.Id, r.Name, r.ShortName, r.SortName, r.Day, r.Time, r.Teams, r.IsActive);
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

    // ── New Season ────────────────────────────────────────────────────────────

    private void StartNewSeason()
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
        _isNewSeasonDraft = true;
        _seasonNameCustomized = false;
        SetEditModeUI(true);
        ApplyDefaultSeasonNameForNewDraft();

        try
        {
            using var db = new BocceDbContext();
            var all     = db.Seasons.Where(s => s.LeagueId == _selectedLeagueId.Value).ToList();
            var current = all.FirstOrDefault(s => s.IsCurrent) ?? (all.Count == 1 ? all[0] : null);

            if (current != null)
            {
                var res = MessageBox.Show(
                    $"Copy settings and divisions from \"{current.Name}\"?\n\n" +
                    "All season parameters and divisions will be copied.\nTeams and scores will not be carried over.",
                    "Copy Previous Season?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (res == DialogResult.Yes)
                    OfferCopyDate(current);
            }
        }
        catch { }

        _txtName.Focus();
    }

    private void OnSeasonStartDateChanged(object? sender, EventArgs e)
    {
        ApplyDefaultSeasonNameForNewDraft();
    }

    private void OnSeasonNameTextChanged(object? sender, EventArgs e)
    {
        if (!_isNewSeasonDraft || _settingSeasonNameProgrammatically)
            return;
        _seasonNameCustomized = true;
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
        var yearName = startDate.Year.ToString();
        try
        {
            using var db = new BocceDbContext();
            var existingNames = db.Seasons
                .Where(s => s.LeagueId == leagueId)
                .Select(s => s.Name)
                .ToList();

            var yearExists = existingNames.Any(n =>
                string.Equals((n ?? string.Empty).Trim(), yearName, StringComparison.OrdinalIgnoreCase));

            return yearExists ? startDate.ToString("MMMM yyyy") : yearName;
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
        _numGamesPerSeason.Value = source.GamesPerSeason;

        _dtpPlayoffStart.Checked = source.PlayoffStartDate.HasValue;
        if (source.PlayoffStartDate.HasValue)
            _dtpPlayoffStart.Value = source.PlayoffStartDate.Value.AddYears(1).ToDateTime(TimeOnly.MinValue);

        _chkIsCurrent.Checked = false;
        _chkActive.Checked    = true;

        _numMaxTeamsDiv.Value = source.MaxTeamsInDivision;
        SelStr(_cmbGameInterval, source.GameInterval);
        _chkTimeslotDriven.Checked = source.TimeslotDriven;
        _numPlayersMin.Value   = source.PlayersPerTeamMinimum ?? 0;
        _numPlayersMax.Value   = source.PlayersPerTeamMaximum ?? 0;
        _numPtsWin.Value       = source.PointsForWin;
        _numPtsTie.Value       = source.PointsForTie;
        _numPtsLoss.Value      = source.PointsForLoss;
        _numPtsNoShow.Value    = source.PointsForNoShow;
        _numPtsToWin.Value     = source.PointsToWinGame;
        _numGamesPerMatch.Value = source.GamesPerMatch;
        SelStr(_cmbScoringMode, source.ScoringMode);

        _numTeamsPlayoffs.Value      = source.TeamsInPlayoffs;
        _chkFirstPlace.Checked       = source.FirstPlaceGuaranteed;
        SelStr(_cmbPlayoffType, source.PlayoffType);
        _numPlayoffGames.Value       = source.PlayoffGamesPerMatch;
        SelStr(_cmbPlayoffScoring, source.PlayoffScoringMode);
        _chkPlayoffTiebreaker.Checked = source.PlayoffTiebreakerEnd;

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
        }

        _isCopied     = true;
        _copySourceId = source.Id;
    }

    // ── Edit Mode ─────────────────────────────────────────────────────────────

    private void EnterEditMode()
    {
        if (_selectedSeasonId == null) return;
        SetEditModeUI(true);
    }

    private void ExitEditMode()
    {
        if (!_selectedSeasonId.HasValue && _previousSeasonId.HasValue)
        {
            _selectedSeasonId = _previousSeasonId;
            _previousSeasonId = null;
            SelectInList(_selectedSeasonId.Value);
        }

        SetEditModeUI(false);
        if (_selectedSeasonId.HasValue)
            LoadSeason(_selectedSeasonId.Value);
    }

    private void SetEditModeUI(bool editMode)
    {
        _txtName.ReadOnly           = !editMode;
        _dtpStartDate.Enabled       = editMode;
        _numWeeks.Enabled           = editMode;
        _numGamesPerSeason.Enabled  = editMode;
        _dtpPlayoffStart.Enabled    = editMode;
        _chkIsCurrent.Enabled       = editMode;
        _chkActive.Enabled          = editMode;
        _numMaxTeamsDiv.Enabled     = editMode;
        _cmbGameInterval.Enabled    = editMode;
        _chkTimeslotDriven.Enabled  = editMode;
        _numPlayersMin.Enabled      = editMode;
        _numPlayersMax.Enabled      = editMode;
        _numPtsWin.Enabled          = editMode;
        _numPtsTie.Enabled          = editMode;
        _numPtsLoss.Enabled         = editMode;
        _numPtsNoShow.Enabled       = editMode;
        _numPtsToWin.Enabled        = editMode;
        _numGamesPerMatch.Enabled   = editMode;
        _cmbScoringMode.Enabled     = editMode;
        _numTeamsPlayoffs.Enabled   = editMode;
        _chkFirstPlace.Enabled      = editMode;
        _cmbPlayoffType.Enabled     = editMode;
        _numPlayoffGames.Enabled    = editMode;
        _cmbPlayoffScoring.Enabled  = editMode;
        _chkPlayoffTiebreaker.Enabled = editMode;

        _daysList.Enabled  = editMode;
        _timesList.Enabled = editMode;
        _btnBuild.Enabled  = !editMode && _selectedSeasonId.HasValue;

        _divisionsGrid.Columns["DivAct"].ReadOnly = editMode;

        _btnEdit.Visible   = !editMode && _selectedSeasonId.HasValue;
        _btnDelete.Visible = !editMode && _selectedSeasonId.HasValue;
        _btnDelete.Enabled = !editMode && _selectedSeasonId.HasValue;
        _btnSave.Visible   = editMode;
        _btnCancel.Visible = editMode;
    }

    // ── Save ──────────────────────────────────────────────────────────────────

    private void SaveSeason()
    {
        var name = _txtName.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            MessageBox.Show("Season name is required.", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtName.Focus(); return;
        }
        if (!_selectedLeagueId.HasValue)
        {
            MessageBox.Show("Select a league in the top bar first.", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        bool isNew = !_selectedSeasonId.HasValue;
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

            if (season.IsCurrent)
            {
                var others = db.Seasons
                    .Where(s => s.LeagueId == _selectedLeagueId.Value && s.Id != savedId && s.IsCurrent)
                    .ToList();
                if (others.Count > 0)
                { foreach (var o in others) o.IsCurrent = false; db.SaveChanges(); }
            }

            if (db.Seasons.Count(s => s.LeagueId == _selectedLeagueId.Value) == 1 && !season.IsCurrent)
            {
                season.IsCurrent = true;
                _chkIsCurrent.Checked = true;
                db.SaveChanges();
            }

            _lblCreatedAt.Text = season.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Save failed:\n\n{ex.Message}", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _selectedSeasonId = savedId;

        string divMsg = "";
        if (isNew)
        {
            if (_isCopied && _copySourceId.HasValue)
            {
                var (divs, teams, players, lft) = CopySeasonData(_copySourceId.Value, savedId);
                if (divs > 0)
                    divMsg = $"\n\n{divs} division(s), {teams} team(s), {players} player assignment(s) copied from previous season."
                           + (lft > 0 ? $"\n{lft} Looking For Team entry(s) updated to new teams." : "");
                _isCopied = false; _copySourceId = null;
            }
            else
            {
                int n = BuildDivisionsFromSlots(savedId);
                divMsg = n > 0 ? $"\n\n{n} division(s) created from selected day/time slots." : "";
            }
        }

        MessageBox.Show("Season saved." + divMsg, "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Information);

        ExitEditMode();

        LoadSeasonList();
        SelectInList(savedId);
        LoadDivisions(savedId);
        LoadSeasonSlots(savedId);
    }

    private void ApplyEditorToSeason(Season s)
    {
        s.Name             = _txtName.Text.Trim();
        s.StartDate        = DateOnly.FromDateTime(_dtpStartDate.Value);
        s.WeeksInSeason    = (int)_numWeeks.Value;
        s.GamesPerSeason   = (int)_numGamesPerSeason.Value;
        s.PlayoffStartDate     = _dtpPlayoffStart.Checked ? DateOnly.FromDateTime(_dtpPlayoffStart.Value) : null;
        s.MaxTeamsInDivision   = (int)_numMaxTeamsDiv.Value;
        s.IsCurrent        = _chkIsCurrent.Checked;
        s.IsActive         = _chkActive.Checked;
        s.GameInterval     = StrVal(_cmbGameInterval)  ?? "weekly";
        s.TimeslotDriven   = _chkTimeslotDriven.Checked;
        s.PlayersPerTeamMinimum = _numPlayersMin.Value > 0 ? (int)_numPlayersMin.Value : (int?)null;
        s.PlayersPerTeamMaximum = _numPlayersMax.Value > 0 ? (int)_numPlayersMax.Value : (int?)null;
        s.PointsForWin     = (int)_numPtsWin.Value;
        s.PointsForTie     = (int)_numPtsTie.Value;
        s.PointsForLoss    = (int)_numPtsLoss.Value;
        s.PointsForNoShow  = (int)_numPtsNoShow.Value;
        s.PointsToWinGame  = (int)_numPtsToWin.Value;
        s.GamesPerMatch    = (int)_numGamesPerMatch.Value;
        s.ScoringMode      = StrVal(_cmbScoringMode)   ?? "games_mode";
        s.TeamsInPlayoffs  = (int)_numTeamsPlayoffs.Value;
        s.FirstPlaceGuaranteed  = _chkFirstPlace.Checked;
        s.PlayoffType      = StrVal(_cmbPlayoffType)   ?? "ladder";
        s.PlayoffGamesPerMatch  = (int)_numPlayoffGames.Value;
        s.PlayoffScoringMode    = StrVal(_cmbPlayoffScoring) ?? "match_play";
        s.PlayoffTiebreakerEnd  = _chkPlayoffTiebreaker.Checked;
    }

    // ── Division helpers ──────────────────────────────────────────────────────

    private (int divs, int teams, int players, int lft) CopySeasonData(int sourceSeasonId, int newSeasonId)
    {
        try
        {
            using var db = new BocceDbContext();

            var sourceDivs = db.Divisions
                .Where(d => d.SeasonId == sourceSeasonId)
                .ToList();

            var sourceTeams = db.Teams
                .Include(t => t.TeamPlayers)
                .Where(t => sourceDivs.Select(d => d.Id).Contains(t.DivisionId))
                .ToList();

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
                    TeamsInDivision       = 0,
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
            db.SaveChanges();

            int lftUpdated = 0;
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
                        TeamsInDivision = 0,
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
                db.DivisionParameters.RemoveRange(db.DivisionParameters.Where(x => x.DivisionId == div.Id));
                db.TeamStandings.RemoveRange(db.TeamStandings.Where(x => x.DivisionId == div.Id));
                foreach (var team in div.Teams)
                {
                    db.TeamPlayers.RemoveRange(team.TeamPlayers);
                    db.TeamParameters.RemoveRange(db.TeamParameters.Where(x => x.TeamId == team.Id));
                }
                db.Teams.RemoveRange(div.Teams);
            }
            db.Divisions.RemoveRange(season.Divisions);

            db.SeasonParameters.RemoveRange(db.SeasonParameters.Where(x => x.SeasonId == seasonId));
            db.SeasonDaySlots.RemoveRange(db.SeasonDaySlots.Where(x => x.SeasonId == seasonId));
            db.SeasonTimeSlots.RemoveRange(db.SeasonTimeSlots.Where(x => x.SeasonId == seasonId));
            db.SeasonCourts.RemoveRange(db.SeasonCourts.Where(x => x.SeasonId == seasonId));
            db.SeasonFees.RemoveRange(db.SeasonFees.Where(x => x.SeasonId == seasonId));

            var applicantIds = db.TeamApplicants.Where(ta => ta.SeasonId == seasonId)
                                                .Select(ta => ta.Id).ToList();
            if (applicantIds.Count > 0)
            {
                db.TeamApplicantPlayers.RemoveRange(
                    db.TeamApplicantPlayers.Where(p => applicantIds.Contains(p.TeamApplicantId)));
                db.TeamApplicantDaySlots.RemoveRange(
                    db.TeamApplicantDaySlots.Where(d => applicantIds.Contains(d.TeamApplicantId)));
                db.TeamApplicantTimeSlots.RemoveRange(
                    db.TeamApplicantTimeSlots.Where(t => applicantIds.Contains(t.TeamApplicantId)));
                db.TeamApplicants.RemoveRange(db.TeamApplicants.Where(ta => ta.SeasonId == seasonId));
            }

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
}

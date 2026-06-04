using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.Services;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Panels;

public class DivisionPanel : UserControl
{
    private bool _isLoadingData = false;
    private bool _isEditMode = false;

    // ── State ────────────────────────────────────────────────────────────────
    private int? _selectedLeagueId;
    private int? _selectedSeasonId;
    private int? _selectedDivisionId;
    private int? _leagueIdToRestore;
    private int? _seasonIdToRestore;
    private int? _currentTeamId;

    // ── Header ───────────────────────────────────────────────────────────────
    private ComboBox _leagueCombo   = null!;
    private ComboBox _seasonCombo   = null!;
    private ComboBox _divisionCombo = null!;

    // ── Editor tab ───────────────────────────────────────────────────────────
    private TextBox       _txtName       = null!;
    private Label         _lblSystemName = null!;
    private Label         _lblSortKey    = null!;
    private ComboBox      _cmbDay        = null!;
    private ComboBox      _cmbTime       = null!;
    private CheckBox      _chkActive     = null!;
    private Label         _lblCreated    = null!;

    // ── Parameters tab ───────────────────────────────────────────────────────
    private NumericUpDown _numTeamsInDiv  = null!;
    private NumericUpDown _numPlayersMin  = null!;
    private NumericUpDown _numPlayersMax  = null!;

    // ── Teams tab ────────────────────────────────────────────────────────────
    private DataGridView _teamsGrid      = null!;
    private DataGridView _playersGrid    = null!;
    private Label        _lblTeamTitle   = null!;
    private ComboBox     _cmbCaptain     = null!;
    private Button       _btnAddTeam     = null!;
    private Button       _btnDeleteTeam  = null!;
    private Button       _btnAddPlayer   = null!;
    private Button       _btnRemovePlayer = null!;

    // ── Shared toolbar ───────────────────────────────────────────────────────
    private Button _btnEdit   = null!;
    private Button _btnSave   = null!;
    private Button _btnDelete = null!;
    private Button _btnCancel = null!;

    // ─────────────────────────────────────────────────────────────────────────

    public DivisionPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;
        BuildUI();
        LoadLeagueList();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { }
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
            SelectLeagueInCombo(s.LeagueId);
            SelectSeasonInCombo(d.SeasonId);
            SelectDivisionInCombo(divisionId);
        }
        catch { }
    }

    // ── Build UI ─────────────────────────────────────────────────────────────

    private void BuildUI()
    {
        var header  = BuildHeader();
        var toolbar = BuildSaveToolbar();
        var tabs    = BuildTabs();
        Controls.Add(tabs);
        Controls.Add(toolbar);
        Controls.Add(header);
    }

    private Panel BuildHeader()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Top, Height = 54,
            BackColor = AppTheme.Surface, Padding = new Padding(12, 8, 12, 8)
        };

        int x = 12;

        var lblL = NavLabel("League:", x, 17); panel.Controls.Add(lblL); x += lblL.PreferredWidth + 6;
        _leagueCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = AppTheme.FontDefault, Width = 200, Location = new Point(x, 13) };
        _leagueCombo.SelectedIndexChanged += OnLeagueSelected;
        panel.Controls.Add(_leagueCombo); x += 208;

        var lblS = NavLabel("Season:", x, 17); panel.Controls.Add(lblS); x += lblS.PreferredWidth + 6;
        _seasonCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = AppTheme.FontDefault, Width = 210, Location = new Point(x, 13) };
        _seasonCombo.SelectedIndexChanged += OnSeasonSelected;
        panel.Controls.Add(_seasonCombo); x += 218;

        var lblD = NavLabel("Division:", x, 17); panel.Controls.Add(lblD); x += lblD.PreferredWidth + 6;
        _divisionCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = AppTheme.FontDefault, Width = 230, Location = new Point(x, 13) };
        _divisionCombo.SelectedIndexChanged += OnDivisionSelected;
        panel.Controls.Add(_divisionCombo); x += 238;

        var btnNew = new Button
        {
            Text = "+ New Division", Location = new Point(x, 12), Size = new Size(130, 30),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.ButtonSuccess, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 }
        };
        btnNew.Click += (_, _) => StartNewDivision();
        panel.Controls.Add(btnNew);

        return panel;
    }

    private TabControl BuildTabs()
    {
        var tabs = new TabControl { Dock = DockStyle.Fill, Font = AppTheme.FontDefault, Padding = new Point(16, 6) };
        tabs.TabPages.Add(BuildEditorTab());
        tabs.TabPages.Add(BuildParametersTab());
        tabs.TabPages.Add(BuildTeamsTab());
        return tabs;
    }

    // ── Editor Tab ───────────────────────────────────────────────────────────

    private TabPage BuildEditorTab()
    {
        var page   = new TabPage("  Editor  ");
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = AppTheme.ContentBackground };

        const int lx = 20, ix = 210, iw = 420;
        int y = 20;
        var cc = new List<Control>();
        void Add(params Control[] items) => cc.AddRange(items);

        Add(Lbl("Name", lx, y));
        _txtName = new TextBox { Location = new Point(ix, y), Size = new Size(iw, 26), Font = AppTheme.FontDefault, BackColor = AppTheme.ContentBackground, ForeColor = AppTheme.TextPrimary };
        Add(_txtName); y += 44;

        Add(Lbl("Day Slot", lx, y));
        _cmbDay = new ComboBox { Location = new Point(ix, y), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList, Font = AppTheme.FontDefault };
        _cmbDay.SelectedIndexChanged += OnSlotChanged;
        Add(_cmbDay); y += 44;

        Add(Lbl("Time Slot", lx, y));
        _cmbTime = new ComboBox { Location = new Point(ix, y), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList, Font = AppTheme.FontDefault };
        _cmbTime.SelectedIndexChanged += OnSlotChanged;
        Add(_cmbTime); y += 44;

        Add(Sep(lx, y, iw + ix - lx)); y += 10;
        Add(SecHdr("Computed Names  (update automatically when Day/Time changes)", lx, y)); y += 34;

        Add(Lbl("System Name", lx, y));
        _lblSystemName = new Label { Location = new Point(ix, y + 3), AutoSize = true, Font = AppTheme.FontDefaultBold, ForeColor = AppTheme.TextPrimary };
        Add(_lblSystemName, Hint("Mo-0900 format — used in team names", ix + 120, y + 4)); y += 38;

        Add(Lbl("Sort Key", lx, y));
        _lblSortKey = new Label { Location = new Point(ix, y + 3), AutoSize = true, Font = AppTheme.FontDefaultBold, ForeColor = AppTheme.TextPrimary };
        Add(_lblSortKey, Hint("1-0900 format — used for ordering", ix + 120, y + 4)); y += 38;

        Add(Sep(lx, y, iw + ix - lx)); y += 10;
        Add(SecHdr("Status", lx, y)); y += 34;

        Add(Lbl("Active", lx, y));
        _chkActive = new CheckBox { Location = new Point(ix, y), AutoSize = true, Font = AppTheme.FontDefault, ForeColor = AppTheme.TextPrimary, Checked = true };
        Add(_chkActive); y += 38;

        Add(Lbl("Created", lx, y));
        _lblCreated = new Label { Location = new Point(ix, y + 3), AutoSize = true, Font = AppTheme.FontDefault, ForeColor = AppTheme.TextSecondary };
        Add(_lblCreated); y += 44;

        scroll.Controls.AddRange([.. cc]);
        page.Controls.Add(scroll);
        LoadSlotCombos();
        return page;
    }

    // ── Parameters Tab ───────────────────────────────────────────────────────

    private TabPage BuildParametersTab()
    {
        var page   = new TabPage("  Parameters  ");
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = AppTheme.ContentBackground };

        const int lx = 20, ix = 220, iw = 400;
        int y = 20;
        var cc = new List<Control>();
        void Add(params Control[] items) => cc.AddRange(items);

        Add(SecHdr("Division Parameters", lx, y)); y += 34;

        Add(Lbl("Teams in Division", lx, y));
        _numTeamsInDiv = Num(ix, y, 0, 99);
        Add(_numTeamsInDiv, Hint("Max/target teams for this division. 0 = inherit from season or league. Even preferred; odd = one bye per round.", ix + 100, y + 4)); y += 44;

        Add(Sep(lx, y, iw + ix - lx)); y += 10;
        Add(SecHdr("Player Limits  (override season values when set)", lx, y)); y += 34;

        Add(Lbl("Players / Team Min", lx, y));
        _numPlayersMin = Num(ix, y, 0, 99);
        Add(_numPlayersMin, Hint("0 = use season default", ix + 100, y + 4)); y += 38;

        Add(Lbl("Players / Team Max", lx, y));
        _numPlayersMax = Num(ix, y, 0, 99);
        Add(_numPlayersMax, Hint("0 = use season default", ix + 100, y + 4)); y += 38;

        scroll.Controls.AddRange([.. cc]);
        page.Controls.Add(scroll);
        return page;
    }

    // ── Teams Tab ────────────────────────────────────────────────────────────

    private TabPage BuildTeamsTab()
    {
        var page = new TabPage("  Teams  ");

        // ── Teams grid ───────────────────────────────────────────────────────
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

        // ── Players sub-panel ─────────────────────────────────────────────────
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
        playerBtns.Controls.AddRange([_btnAddPlayer, _btnRemovePlayer]);

        _lblTeamTitle = new Label { Dock = DockStyle.Top, Height = 28, Font = AppTheme.FontSectionHeading, ForeColor = AppTheme.Accent, Text = "Players  —  select a team above", Padding = new Padding(2, 4, 0, 0) };

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

        _btnDeleteTeam = new Button
        {
            Text = "Delete Team", Location = new Point(144, 8), Size = new Size(120, 30),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.ButtonDanger, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 }, Enabled = false
        };
        _btnDeleteTeam.Click += (_, _) => DeleteTeam();

        var teamHint = new Label
        {
            Text = "Teams are lettered A, B, C… automatically. Deleting re-sequences the remaining teams.",
            Location = new Point(278, 16), AutoSize = true,
            Font = AppTheme.FontSmall, ForeColor = AppTheme.TextMuted
        };
        teamToolbar.Controls.AddRange([_btnAddTeam, _btnDeleteTeam, teamHint]);

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

    // ── Save Toolbar ─────────────────────────────────────────────────────────

    private Panel BuildSaveToolbar()
    {
        var toolbar = new Panel
        {
            Dock = DockStyle.Bottom, Height = 54,
            BackColor = AppTheme.Surface, Padding = new Padding(12, 10, 12, 10)
        };

        _btnEdit = new Button
        {
            Text = "Edit Division", Location = new Point(12, 10), Size = new Size(130, 32),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.Accent, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 },
            Visible = false
        };
        _btnEdit.Click += (_, _) => EnterEditMode();

        _btnDelete = new Button
        {
            Text = "Delete Division", Location = new Point(150, 10), Size = new Size(140, 32),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.ButtonDanger, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand,
            FlatAppearance = { BorderSize = 0 }, Enabled = false, Visible = false
        };
        _btnDelete.Click += (_, _) => DeleteDivision();

        _btnSave = new Button
        {
            Text = "Save Division", Location = new Point(12, 10), Size = new Size(130, 32),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.Accent, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 },
            Visible = false
        };
        _btnSave.Click += (_, _) => SaveDivision();

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

    // ── Data Loading ─────────────────────────────────────────────────────────

    private void LoadLeagueList()
    {
        _isLoadingData = true;
        try
        {
            _leagueCombo.SelectedIndexChanged -= OnLeagueSelected;
            _leagueCombo.Items.Clear();

            int? defaultLeagueId = null;
            try
            {
                using var db = new BocceDbContext();
                foreach (var l in db.Leagues.OrderBy(l => l.Name).ToList())
                    _leagueCombo.Items.Add(new IntItem(l.Id, l.Name + (l.IsActive ? "" : " (inactive)")));

                defaultLeagueId = AppParameterService.GetDefaultLeagueId(db);
            }
            catch { }

            _leagueCombo.SelectedIndexChanged += OnLeagueSelected;

            // Restore default from database
            if (defaultLeagueId.HasValue)
            {
                int idx = _leagueCombo.Items.Cast<IntItem>().ToList().FindIndex(item => item.Id == defaultLeagueId);
                if (idx >= 0)
                    _leagueCombo.SelectedIndex = idx;
                else
                    ClearEditor();
            }
            else
                ClearEditor();
        }
        finally
        {
            _isLoadingData = false;
        }
    }

    private void OnLeagueSelected(object? sender, EventArgs e)
    {
        if (_leagueCombo.SelectedItem is IntItem item)
        {
            _selectedLeagueId = item.Id;
            _leagueIdToRestore = item.Id;  // Save for persistence across reloads
            LoadSeasonList(item.Id);
            // Update default league only if user selected (not during data load)
            if (!_isLoadingData)
            {
                using var db = new BocceDbContext();
                AppParameterService.SetDefaultLeagueId(db, item.Id);

                // If this league has no seasons, clear the default season
                var hasSeasons = db.Seasons.Any(s => s.LeagueId == item.Id && s.IsActive);
                if (!hasSeasons)
                {
                    AppParameterService.SetDefaultSeasonId(db, null);
                }
            }
        }
        else ClearEditor();
    }

    private void LoadSeasonList(int leagueId)
    {
        _isLoadingData = true;
        try
        {
            _seasonCombo.SelectedIndexChanged -= OnSeasonSelected;
            _seasonCombo.Items.Clear();

            int? defaultSeasonId = null;
            try
            {
                using var db = new BocceDbContext();
                foreach (var s in db.Seasons.Where(s => s.LeagueId == leagueId)
                    .OrderByDescending(s => s.IsCurrent).ThenByDescending(s => s.StartDate).ToList())
                {
                    _seasonCombo.Items.Add(new IntItem(s.Id,
                        s.Name + (s.IsCurrent ? "  ★" : "") + (s.IsActive ? "" : " (inactive)")));
                }

                defaultSeasonId = AppParameterService.GetDefaultSeasonId(db);
            }
            catch { }

            _seasonCombo.SelectedIndexChanged += OnSeasonSelected;
            if (_seasonCombo.Items.Count > 0)
            {
                // Try to select default season; fall back to first
                if (defaultSeasonId.HasValue)
                {
                    int idx = _seasonCombo.Items.Cast<IntItem>().ToList().FindIndex(item => item.Id == defaultSeasonId);
                    _seasonCombo.SelectedIndex = idx >= 0 ? idx : 0;
                }
                else
                {
                    _seasonCombo.SelectedIndex = 0;
                }
            }
            else
            {
                // No seasons: clear divisions too
                _divisionCombo.SelectedIndexChanged -= OnDivisionSelected;
                _divisionCombo.Items.Clear();
                _divisionCombo.SelectedIndexChanged += OnDivisionSelected;
                ClearEditor();
            }
        }
        finally
        {
            _isLoadingData = false;
        }
    }

    private void OnSeasonSelected(object? sender, EventArgs e)
    {
        if (_seasonCombo.SelectedItem is IntItem item)
        {
            _selectedSeasonId = item.Id;
            LoadDivisionList(item.Id);
            // Update default season only if user selected (not during data load)
            if (!_isLoadingData)
            {
                using var db = new BocceDbContext();
                AppParameterService.SetDefaultSeasonId(db, item.Id);
            }
        }
        else ClearEditor();
    }

    private void LoadDivisionList(int seasonId)
    {
        _divisionCombo.SelectedIndexChanged -= OnDivisionSelected;
        _divisionCombo.Items.Clear();
        try
        {
            using var db = new BocceDbContext();
            foreach (var d in db.Divisions.Where(d => d.SeasonId == seasonId)
                .OrderBy(d => d.SortName).ThenBy(d => d.Name).ToList())
            {
                _divisionCombo.Items.Add(new IntItem(d.Id,
                    d.Name + (d.IsActive ? "" : " (inactive)")));
            }
        }
        catch { }
        _divisionCombo.SelectedIndexChanged += OnDivisionSelected;
        if (_divisionCombo.Items.Count > 0) _divisionCombo.SelectedIndex = 0;
        else ClearEditor();
    }

    private void OnDivisionSelected(object? sender, EventArgs e)
    {
        if (_divisionCombo.SelectedItem is IntItem item) LoadDivision(item.Id);
        else ClearEditor();
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
            foreach (var d in db.DaySlots.Where(d => d.IsActive).OrderBy(d => d.DayNbr).ToList())
                _cmbDay.Items.Add(new SlotItem(d.Id, d.DayName));
            foreach (var t in db.TimeSlots.Where(t => t.IsActive).OrderBy(t => t.SortOrder).ToList())
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
        _isEditMode = false;

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

            _txtName.Text    = d.Name;
            _chkActive.Checked = d.IsActive;
            _lblCreated.Text   = ""; // Division has no CreatedAt — show blank

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
        SetEditModeUI(false);  // Start in read-only mode
    }

    private void ClearEditor()
    {
        _selectedDivisionId = null;
        _currentTeamId      = null;
        _txtName.Text = "";
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
        _btnAddTeam.Enabled = false;
        _btnDeleteTeam.Enabled   = false;
        _btnAddPlayer.Enabled    = false;
        _btnRemovePlayer.Enabled = false;
        _teamsGrid.Rows.Clear();
        ClearPlayersPanel();
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
            _txtName.Text = "";
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
            _txtName.Text = $"{day.DayName} {time.Timeslot12h}";
        }
        catch { }
    }

    // ── New Division ──────────────────────────────────────────────────────────

    private void StartNewDivision()
    {
        if (!_selectedSeasonId.HasValue)
        {
            MessageBox.Show("Select a league and season first.", "BocceManager", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        _divisionCombo.SelectedIndexChanged -= OnDivisionSelected;
        _divisionCombo.SelectedIndex = -1;
        _divisionCombo.SelectedIndexChanged += OnDivisionSelected;
        ClearEditor();
        _txtName.Focus();
    }

    // ── Edit Mode ─────────────────────────────────────────────────────────────

    private void EnterEditMode()
    {
        if (_selectedDivisionId == null) return;
        _isEditMode = true;
        SetEditModeUI(true);
    }

    private void ExitEditMode()
    {
        _isEditMode = false;
        SetEditModeUI(false);
        // Reload to discard changes
        if (_selectedDivisionId.HasValue)
            LoadDivision(_selectedDivisionId.Value);
    }

    private void SetEditModeUI(bool editMode)
    {
        // Division name is always read-only (calculated from Day + Time)
        _txtName.ReadOnly = true;

        // Other controls editable in edit mode
        _cmbDay.Enabled = editMode;
        _cmbTime.Enabled = editMode;
        _chkActive.Enabled = editMode;
        _numTeamsInDiv.ReadOnly = !editMode;
        _numPlayersMin.ReadOnly = !editMode;
        _numPlayersMax.ReadOnly = !editMode;

        // Button visibility: Edit/Delete in view mode, Save/Cancel in edit mode
        _btnEdit.Visible = !editMode && _selectedDivisionId.HasValue;
        _btnDelete.Visible = !editMode && _selectedDivisionId.HasValue;
        _btnSave.Visible = editMode;
        _btnCancel.Visible = editMode;

        // Teams/Players editing only in view mode
        _btnAddTeam.Enabled = !editMode && _selectedDivisionId.HasValue;
        _btnDeleteTeam.Enabled = !editMode && _currentTeamId.HasValue;
        _btnAddPlayer.Enabled = !editMode && _currentTeamId.HasValue;
        _btnRemovePlayer.Enabled = !editMode && _playersGrid.SelectedRows.Count > 0;
    }

    // ── Save ──────────────────────────────────────────────────────────────────

    private void SaveDivision()
    {
        if (!_selectedSeasonId.HasValue)
        {
            MessageBox.Show("Select a season first.", "BocceManager", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Get selected day and time
        string dayName = _cmbDay.SelectedItem is SlotItem ds && ds.Id > 0 ? ds.Display : null;
        string timeName = _cmbTime.SelectedItem is SlotItem ts && ts.Id > 0 ? ts.Display : null;

        // For new divisions, day and time are required
        if (!_selectedDivisionId.HasValue && (string.IsNullOrEmpty(dayName) || string.IsNullOrEmpty(timeName)))
        {
            MessageBox.Show("Select a Day and Time for the division.", "BocceManager", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            name = _txtName.Text.Trim();
            if (string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(dayName) && !string.IsNullOrEmpty(timeName))
                name = $"{dayName} {timeName}";
        }

        if (string.IsNullOrEmpty(name))
        {
            MessageBox.Show("Division name is required.", "BocceManager", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            MessageBox.Show($"Save failed:\n\n{ex.Message}", "BocceManager", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _selectedDivisionId = savedId;
        _btnDelete.Enabled  = true;
        _btnAddTeam.Enabled = true;
        MessageBox.Show("Division saved.", "BocceManager", MessageBoxButtons.OK, MessageBoxIcon.Information);
        LoadDivisionList(_selectedSeasonId!.Value);
        SelectDivisionInCombo(savedId);
        LoadTeams(savedId);
    }

    // ── Delete Division ───────────────────────────────────────────────────────

    private void DeleteDivision()
    {
        if (!_selectedDivisionId.HasValue) return;
        int divId = _selectedDivisionId.Value;
        string divName = _txtName.Text.Trim();

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
            sb.AppendLine("Players are NOT deleted — only their team assignments.");
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
                foreach (var team in div.Teams)
                    db.TeamPlayers.RemoveRange(team.TeamPlayers);
                db.Teams.RemoveRange(div.Teams);
                db.Divisions.Remove(div);
                db.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Delete failed:\n{ex.Message}", "BocceManager", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        MessageBox.Show("Division deleted.", "BocceManager", MessageBoxButtons.OK, MessageBoxIcon.Information);
        _selectedDivisionId = null;
        if (_selectedSeasonId.HasValue) LoadDivisionList(_selectedSeasonId.Value);
        if (_divisionCombo.Items.Count == 0) ClearEditor();
    }

    // ── Teams ─────────────────────────────────────────────────────────────────

    private void LoadTeams(int divisionId)
    {
        _teamsGrid.SelectionChanged -= OnTeamSelected;
        _teamsGrid.Rows.Clear();
        try
        {
            using var db = new BocceDbContext();
            var teams = db.Teams
                .Where(t => t.DivisionId == divisionId)
                .OrderBy(t => t.TeamLetter)
                .Select(t => new
                {
                    t.Id, t.TeamLetter, t.SystemName,
                    Display  = t.DisplayName ?? t.SystemName,
                    Captain  = t.Captain != null ? t.Captain.LastName + ", " + t.Captain.FirstName : "",
                    Players  = db.TeamPlayers.Count(tp => tp.TeamId == t.Id),
                    t.IsActive
                }).ToList();

            foreach (var t in teams)
                _teamsGrid.Rows.Add(t.Id, t.TeamLetter, t.SystemName, t.Display, t.Captain, t.Players, t.IsActive);
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
            _btnDeleteTeam.Enabled   = false;
            _btnAddPlayer.Enabled    = false;
            _btnRemovePlayer.Enabled = false;
            ClearPlayersPanel();
            return;
        }
        var row = _teamsGrid.SelectedRows[0];
        if (row.Cells["TmId"].Value == null) return;
        _currentTeamId = Convert.ToInt32(row.Cells["TmId"].Value);
        _btnDeleteTeam.Enabled   = true;
        _btnAddPlayer.Enabled    = _selectedDivisionId.HasValue;
        _btnRemovePlayer.Enabled = false;
        string displayName = row.Cells["TmDisplay"].Value?.ToString() ?? "";
        _lblTeamTitle.Text = $"Players  —  Team {displayName}";
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
            if (div.TeamsInDivision > 0) return div.TeamsInDivision;

            var season = db.Seasons.Find(div.SeasonId);
            if (season?.MaxTeamsInDivision > 0) return season.MaxTeamsInDivision;

            var league = db.Leagues.Find(season?.LeagueId ?? 0);
            return league?.MaxTeamsInDivision ?? 0;
        }
        catch { return 0; }
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
            MessageBox.Show($"Could not add team:\n{ex.Message}", "BocceManager", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        LoadTeams(divId);
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
        try { using var db = new BocceDbContext(); playerCount = db.TeamPlayers.Count(tp => tp.TeamId == teamId); }
        catch { }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Delete team \"{teamName}\"?");
        if (playerCount > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"  Player assignments removed: {playerCount}");
            sb.AppendLine();
            sb.AppendLine("Players are NOT deleted — only their team assignments.");
        }
        sb.AppendLine();
        sb.AppendLine("Remaining teams will be re-lettered (A, B, C…).");
        sb.AppendLine("This cannot be undone. Continue?");

        if (MessageBox.Show(sb.ToString(), "Confirm Delete",
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
            MessageBox.Show($"Delete failed:\n{ex.Message}", "BocceManager", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _currentTeamId = null;
        LoadTeams(divId);
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

    // ── Players ───────────────────────────────────────────────────────────────

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
        _cmbCaptain.Enabled = playerItems.Count > 1;
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
        _lblTeamTitle.Text = "Players  —  select a team above";
        _cmbCaptain.SelectedIndexChanged -= OnCaptainChanged;
        _cmbCaptain.Items.Clear();
        _cmbCaptain.Enabled = false;
        _cmbCaptain.SelectedIndexChanged += OnCaptainChanged;
        _btnRemovePlayer.Enabled = false;
    }

    private void AddPlayerToTeam()
    {
        if (_currentTeamId == null) return;
        int teamId = _currentTeamId.Value;

        var existing = new HashSet<int>();
        try
        {
            using var db = new BocceDbContext();
            existing = db.TeamPlayers.Where(tp => tp.TeamId == teamId).Select(tp => tp.PlayerId).ToHashSet();
        }
        catch { }

        var playerIds = PickPlayersMultiple(existing);
        if (playerIds.Count == 0) return;

        try
        {
            using var db = new BocceDbContext();
            int count = 0;
            foreach (var playerId in playerIds)
            {
                // Check if player not already on team
                var alreadyOnTeam = db.TeamPlayers.Any(tp => tp.TeamId == teamId && tp.PlayerId == playerId);
                if (!alreadyOnTeam)
                {
                    db.TeamPlayers.Add(new TeamPlayer
                    {
                        TeamId    = teamId,
                        PlayerId  = playerId,
                        Role      = "player",
                        JoinedDate = DateOnly.FromDateTime(DateTime.Today)
                    });
                    count++;
                }
            }
            if (count > 0)
            {
                db.SaveChanges();
                MessageBox.Show($"Added {count} player(s) to team.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not add player(s):\n{ex.Message}", "BocceManager", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

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

        if (MessageBox.Show($"Remove \"{name}\" from this team?\n\nThe player is NOT deleted — only the team assignment.",
            "Confirm Remove", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;

        try
        {
            using var db = new BocceDbContext();
            var tp = db.TeamPlayers.FirstOrDefault(x => x.TeamId == teamId && x.PlayerId == playerId);
            if (tp != null) { db.TeamPlayers.Remove(tp); db.SaveChanges(); }

            // Clear captain if it was this player
            var team = db.Teams.Find(teamId);
            if (team?.CaptainPlayerId == playerId)
            {
                team.CaptainPlayerId = null;
                team.DisplayName     = null;
                db.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Remove failed:\n{ex.Message}", "BocceManager", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

    // ── Player picker dialog ──────────────────────────────────────────────────

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
            PlaceholderText = "Search by name…", BackColor = AppTheme.Surface,
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
                bool matches = false;
                if (string.IsNullOrWhiteSpace(q))
                    matches = true;
                else if (q.Contains('|'))
                {
                    // Pipe-delimited OR search: "Hans|Hami|James" matches any term
                    var terms = q.Split('|').Select(t => t.Trim()).Where(t => !string.IsNullOrWhiteSpace(t));
                    matches = terms.Any(term => name.Contains(term, StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    // Single term: substring match
                    matches = name.Contains(q, StringComparison.OrdinalIgnoreCase);
                }

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

    // ── Navigation helpers ────────────────────────────────────────────────────

    private void SelectLeagueInCombo(int leagueId)
    {
        for (int i = 0; i < _leagueCombo.Items.Count; i++)
            if (_leagueCombo.Items[i] is IntItem ci && ci.Id == leagueId)
            { _leagueCombo.SelectedIndex = i; return; }
    }

    private void SelectSeasonInCombo(int seasonId)
    {
        for (int i = 0; i < _seasonCombo.Items.Count; i++)
            if (_seasonCombo.Items[i] is IntItem ci && ci.Id == seasonId)
            { _seasonCombo.SelectedIndex = i; return; }
    }

    private void SelectDivisionInCombo(int divisionId)
    {
        for (int i = 0; i < _divisionCombo.Items.Count; i++)
            if (_divisionCombo.Items[i] is IntItem ci && ci.Id == divisionId)
            { _divisionCombo.SelectedIndex = i; return; }
    }

    // ── Control factories ─────────────────────────────────────────────────────

    private static string BuildShortName(string dayAbbr, string time24h)
    {
        var prefix = dayAbbr.Length >= 2
            ? $"{char.ToUpper(dayAbbr[0])}{char.ToLower(dayAbbr[1])}"
            : dayAbbr;
        return $"{prefix}-{time24h}";
    }

    private static Label NavLabel(string text, int x, int y) => new()
    {
        Text = text, Font = AppTheme.FontDefaultBold, ForeColor = AppTheme.TextPrimary,
        AutoSize = true, Location = new Point(x, y)
    };

    private static Label Lbl(string text, int x, int y) => new()
    {
        Text = text, Font = AppTheme.FontDefaultBold, ForeColor = AppTheme.TextPrimary,
        AutoSize = true, Location = new Point(x, y + 3)
    };

    private static NumericUpDown Num(int x, int y, decimal min, decimal max, decimal def = 0) => new()
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

    // ── Multi-select player picker ────────────────────────────────────────────

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
        try
        {
            using var db = new BocceDbContext();
            allPlayers = db.Players
                .Where(p => p.IsActive && !excludeIds.Contains(p.Id))
                .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
                .ToList()
                .Select(p => (p.Id, $"{p.LastName}, {p.FirstName}"))
                .ToList();
        }
        catch { }

        // Search box
        var searchBox = new TextBox
        {
            Location = new Point(10, 8), Width = 300, Height = 28,
            Font = AppTheme.FontDefault, PlaceholderText = "Search... (use | for OR: Hans|Hami)",
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

        // Filter function with pipe-delimited OR search
        void RefreshAvailable(string query)
        {
            cmbAvailable.Items.Clear();
            foreach (var (id, name) in allPlayers)
            {
                // Skip if already in selected
                if (cmbSelected.Items.Cast<IntItem>().Any(x => x.Id == id)) continue;

                bool matches = false;
                if (string.IsNullOrWhiteSpace(query))
                    matches = true;
                else if (query.Contains('|'))
                {
                    var terms = query.Split('|').Select(t => t.Trim()).Where(t => !string.IsNullOrWhiteSpace(t));
                    matches = terms.Any(term => name.Contains(term, StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    matches = name.Contains(query, StringComparison.OrdinalIgnoreCase);
                }

                if (matches) cmbAvailable.Items.Add(new IntItem(id, name));
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

    private sealed record IntItem(int Id, string Name)    { public override string ToString() => Name; }
    private sealed record SlotItem(int Id, string Display) { public override string ToString() => Display; }
}

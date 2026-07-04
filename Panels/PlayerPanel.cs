using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.Services;
using BocceManager.UI.Controls;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Panels;

public class PlayerPanel : UserControl
{
    private enum PlayerMode { View, Create }

    private sealed class PlayerListItem
    {
        public int Id { get; set; }
        public string Label { get; set; } = "";
        public override string ToString() => Label;
    }

    private sealed class PartnerItem
    {
        public int? Id { get; set; }
        public string Name { get; set; } = "";
        public override string ToString() => Name;
    }

    private sealed class RoleItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public override string ToString() => Name;
    }

    private sealed class FilterItem
    {
        public int? Id { get; set; }
        public string Name { get; set; } = "";
        public override string ToString() => Name;
    }

    private sealed class PlayerLookup
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? LotNumber { get; set; }
        public bool IsActive { get; set; }

        public string FullName => string.IsNullOrWhiteSpace(FirstName)
            ? LastName.Trim()
            : string.IsNullOrWhiteSpace(LastName)
                ? FirstName.Trim()
                : $"{FirstName} {LastName}".Trim();

        public string DisplayName => string.IsNullOrWhiteSpace(LastName)
            ? FirstName.Trim()
            : string.IsNullOrWhiteSpace(FirstName)
                ? LastName.Trim()
                : $"{LastName}, {FirstName}".Trim();
    }

    private readonly List<PlayerLookup> _allPlayers = [];
    private SplitContainer _mainSplit = null!;
    private const int PreferredLookupWidth = 330;

    private int? _selectedPlayerId;
    private PlayerMode _mode = PlayerMode.View;
    private bool _isDirty = false;
    private bool _isCreatingNew = false;
    private bool _isLoadingData = false;
    private bool _isSearching = false;
    private bool _isLoadingFilters = false;

    private List<(int PlayerId, int TeamId, int DivisionId, int SeasonId)> _teamMemberships = [];
    private HashSet<int> _sparePlayerIds = [];
    private List<(int PlayerId, int? SeasonId)> _lookingForEntries = [];

    private SearchBoxControl _txtSearch = null!;
    private ListBox _lstPlayers = null!;

    private ComboBox _cmbFilterSeason = null!;
    private ComboBox _cmbFilterDivision = null!;
    private ComboBox _cmbFilterTeam = null!;
    private ComboBox _cmbFilterOnTeam = null!;
    private ComboBox _cmbFilterSpare = null!;
    private ComboBox _cmbFilterLookingForTeam = null!;

    private TextBox _txtFirstName = null!;
    private TextBox _txtLastName = null!;
    private TextBox _txtEmail = null!;
    private TextBox _txtPhone = null!;
    private TextBox _txtLotNumber = null!;
    private CheckBox _chkIsActive = null!;
    private ComboBox _cmbPartner = null!;
    private ComboBox _cmbRole = null!;
    private Label _lblModeHint = null!;
    private Label _lblLeagueContext = null!;

    private Label _lblLookingForTeamsContent = null!;
    private Label _lblSpareListContent = null!;
    private ListBox _lstTeams = null!;
    private Label _lblTeamsContent = null!;

    private List<Control> _fieldControls = [];

    private Button _btnNew = null!;
    private Button _btnSave = null!;
    private Button _btnClear = null!;
    private Button _btnDelete = null!;

    public PlayerPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;

        BuildUi();
        LoadRoleOptions();

        _isLoadingFilters = true;
        try
        {
            LoadSeasonFilterOptions();
            LoadDivisionFilterOptions();
            LoadTeamFilterOptions();
        }
        finally { _isLoadingFilters = false; }

        LoadPlayerLookup();
        SetFieldsVisible(false);
        SetMode(PlayerMode.View);
    }

    private void BuildUi()
    {
        _mainSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.ContentBackground,
            Panel1MinSize = 0,
            Panel2MinSize = 0
        };

        _mainSplit.Panel1.Controls.Add(BuildLookupPanel());
        _mainSplit.Panel2.Controls.Add(BuildEditorPanel());
        Controls.Add(_mainSplit);

        _mainSplit.SizeChanged += (_, _) => SafeApplySplitDistance();
        _mainSplit.HandleCreated += (_, _) => BeginInvoke(new Action(SafeApplySplitDistance));
    }

    private void SafeApplySplitDistance()
    {
        if (_mainSplit.Width <= 1) return;

        // Assign min sizes only after we know the measured width.
        const int desiredLeftMin = 220;
        const int desiredRightMin = 320;
        int maxTotalMin = Math.Max(0, _mainSplit.Width - 1);

        int leftMin = desiredLeftMin;
        int rightMin = desiredRightMin;
        if (leftMin + rightMin > maxTotalMin)
        {
            if (maxTotalMin == 0)
            {
                leftMin = 0;
                rightMin = 0;
            }
            else
            {
                double leftRatio = desiredLeftMin / (double)(desiredLeftMin + desiredRightMin);
                leftMin = (int)Math.Floor(maxTotalMin * leftRatio);
                rightMin = maxTotalMin - leftMin;
            }
        }

        _mainSplit.Panel1MinSize = leftMin;
        _mainSplit.Panel2MinSize = rightMin;

        int minLeft = _mainSplit.Panel1MinSize;
        int maxLeft = _mainSplit.Width - _mainSplit.Panel2MinSize;
        if (maxLeft < minLeft)
            maxLeft = minLeft;

        int clamped = Math.Min(PreferredLookupWidth, maxLeft);
        clamped = Math.Max(minLeft, clamped);

        if (clamped > 0)
            _mainSplit.SplitterDistance = clamped;
    }

    private static Panel MakeFilterRow(string labelText, ComboBox combo)
    {
        var row = new Panel { Dock = DockStyle.Top, Height = 24 };
        var lbl = new Label
        {
            Text = labelText,
            Dock = DockStyle.Left,
            Width = 90,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = AppTheme.FontSmall,
            ForeColor = AppTheme.TextSecondary
        };
        combo.Dock = DockStyle.Fill;
        combo.Font = AppTheme.FontSmall;
        combo.DropDownStyle = ComboBoxStyle.DropDownList;
        row.Controls.Add(combo);
        row.Controls.Add(lbl);
        return row;
    }

    private Control BuildLookupPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Surface,
            Padding = new Padding(12)
        };

        var title = new Label
        {
            Text = "Player Lookup",
            Dock = DockStyle.Top,
            Height = 28,
            Font = AppTheme.FontSectionHeading,
            ForeColor = AppTheme.TextPrimary
        };

        _cmbFilterSeason = new ComboBox();
        _cmbFilterDivision = new ComboBox();
        _cmbFilterTeam = new ComboBox();
        _cmbFilterOnTeam = new ComboBox();
        _cmbFilterOnTeam.Items.AddRange(["All", "On Team", "Off Team"]);
        _cmbFilterOnTeam.SelectedIndex = 0;
        _cmbFilterSpare = new ComboBox();
        _cmbFilterSpare.Items.AddRange(["All", "Spare List", "Not Spare"]);
        _cmbFilterSpare.SelectedIndex = 0;
        _cmbFilterLookingForTeam = new ComboBox();
        _cmbFilterLookingForTeam.Items.AddRange(["All", "Looking For Team", "Not Looking"]);
        _cmbFilterLookingForTeam.SelectedIndex = 0;

        _cmbFilterSeason.SelectedIndexChanged += (_, _) => OnSeasonFilterChanged();
        _cmbFilterDivision.SelectedIndexChanged += (_, _) => OnDivisionFilterChanged();
        _cmbFilterTeam.SelectedIndexChanged += (_, _) => { if (!_isLoadingFilters) ApplySearchFilter(); };
        _cmbFilterOnTeam.SelectedIndexChanged += (_, _) => { if (!_isLoadingFilters) ApplySearchFilter(); };
        _cmbFilterSpare.SelectedIndexChanged += (_, _) => { if (!_isLoadingFilters) ApplySearchFilter(); };
        _cmbFilterLookingForTeam.SelectedIndexChanged += (_, _) => { if (!_isLoadingFilters) ApplySearchFilter(); };

        var rowLft = MakeFilterRow("Looking For:", _cmbFilterLookingForTeam);
        var rowSpare = MakeFilterRow("Spare List:", _cmbFilterSpare);
        var rowOnTeam = MakeFilterRow("Team Status:", _cmbFilterOnTeam);
        var rowTeam = MakeFilterRow("Team:", _cmbFilterTeam);
        var rowDivision = MakeFilterRow("Division:", _cmbFilterDivision);
        var rowSeason = MakeFilterRow("Season:", _cmbFilterSeason);

        var filtersHeader = new Label
        {
            Text = "Filters",
            Dock = DockStyle.Top,
            Height = 18,
            Font = AppTheme.FontSmallBold,
            ForeColor = AppTheme.TextPrimary
        };

        var filterPanel = new Panel { Dock = DockStyle.Top, Height = 24 * 6 + 18 };
        filterPanel.Controls.AddRange([rowLft, rowSpare, rowOnTeam, rowTeam, rowDivision, rowSeason]);
        filterPanel.Controls.Add(filtersHeader);

        var searchHint = new Label
        {
            Text = "Tip: delimiters |  \\  /  :  ; are supported",
            Dock = DockStyle.Top,
            Height = 20,
            Font = AppTheme.FontSmall,
            ForeColor = AppTheme.TextMuted,
            Padding = new Padding(1, 2, 0, 0)
        };

        _txtSearch = new SearchBoxControl("Search name, email, phone, lot")
        {
            Dock = DockStyle.Top,
            Height = 30
        };
        _txtSearch.SearchTextChanged += (_, _) => ApplySearchFilter();

        _btnNew = new Button
        {
            Text = "+ New Player",
            Dock = DockStyle.Top,
            Height = 34,
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.ButtonSuccess,
            ForeColor = Color.White,
            Font = AppTheme.FontButton,
            Cursor = Cursors.Hand
        };
        _btnNew.FlatAppearance.BorderSize = 0;
        _btnNew.Click += (_, _) => AddNewPlayer();

        _lstPlayers = new ListBox
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.FontDefault,
            BorderStyle = BorderStyle.FixedSingle,
            IntegralHeight = false
        };
        _lstPlayers.SelectedIndexChanged += (_, _) => OnPlayerSelectedFromLookup();

        panel.Controls.Add(_lstPlayers);
        panel.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 10, BackColor = AppTheme.Surface });
        panel.Controls.Add(_btnNew);
        panel.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 8, BackColor = AppTheme.Surface });
        panel.Controls.Add(searchHint);
        panel.Controls.Add(_txtSearch);
        panel.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 8, BackColor = AppTheme.Surface });
        panel.Controls.Add(filterPanel);
        panel.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 8, BackColor = AppTheme.Surface });
        panel.Controls.Add(title);

        return panel;
    }

    private Control BuildEditorPanel()
    {
        var root = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.ContentBackground
        };

        var toolbar = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 54,
            BackColor = AppTheme.Surface,
            Padding = new Padding(12, 10, 12, 10)
        };

        _btnSave = MakeButton("Save Player", AppTheme.Accent, Color.White, new Point(12, 10), 120);
        _btnSave.Click += (_, _) => SavePlayer();

        _btnClear = MakeButton("Clear", AppTheme.Surface, AppTheme.TextPrimary, new Point(140, 10), 100);
        _btnClear.FlatAppearance.BorderSize = 1;
        _btnClear.FlatAppearance.BorderColor = AppTheme.Separator;
        _btnClear.Click += (_, _) => ReturnToEmptyState();

        _btnDelete = MakeButton("Delete Player", AppTheme.ButtonDanger, Color.White, new Point(250, 10), 130);
        _btnDelete.Click += (_, _) => DeletePlayer();

        toolbar.Controls.AddRange([_btnSave, _btnClear, _btnDelete]);

        var scroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = AppTheme.ContentBackground
        };

        const int labelX = 24;
        const int inputX = 230;
        const int inputW = 420;
        int y = 24;

        Label MakeLabel(string text, int top) => new()
        {
            Text = text,
            Location = new Point(labelX, top + 4),
            AutoSize = true,
            Font = AppTheme.FontDefaultBold,
            ForeColor = AppTheme.TextPrimary
        };

        _lblModeHint = new Label
        {
            Text = "Select a player from the left list, or create a new one.",
            Location = new Point(labelX, y),
            AutoSize = true,
            Font = AppTheme.FontSmall,
            ForeColor = AppTheme.TextMuted
        };
        y += 28;

        var lblFirstName = MakeLabel("First Name *", y);
        _txtFirstName = MakeInput(inputX, y, inputW);
        _txtFirstName.TextChanged += (_, _) => MarkDirty();
        y += 42;

        var lblLastName = MakeLabel("Last Name *", y);
        _txtLastName = MakeInput(inputX, y, inputW);
        _txtLastName.TextChanged += (_, _) => MarkDirty();
        y += 42;

        var lblEmail = MakeLabel("Email", y);
        _txtEmail = MakeInput(inputX, y, inputW);
        _txtEmail.TextChanged += (_, _) => MarkDirty();
        y += 42;

        var lblPhone = MakeLabel("Phone", y);
        _txtPhone = MakeInput(inputX, y, inputW);
        _txtPhone.TextChanged += (_, _) => MarkDirty();
        y += 42;

        var lblLot = MakeLabel("Lot Number", y);
        _txtLotNumber = MakeInput(inputX, y, 180);
        _txtLotNumber.TextChanged += (_, _) => MarkDirty();
        y += 42;

        var lblPartner = MakeLabel("Spouse / Partner", y);
        _cmbPartner = new ComboBox
        {
            Location = new Point(inputX, y),
            Size = new Size(inputW, 28),
            DropDownStyle = ComboBoxStyle.DropDown,
            Font = AppTheme.FontDefault,
            AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            AutoCompleteSource = AutoCompleteSource.ListItems
        };
        _cmbPartner.SelectedIndexChanged += (_, _) => MarkDirty();
        y += 42;

        var lblRole = MakeLabel("Role", y);
        _cmbRole = new ComboBox
        {
            Location = new Point(inputX, y),
            Size = new Size(220, 28),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = AppTheme.FontDefault
        };
        _cmbRole.SelectedIndexChanged += (_, _) => MarkDirty();
        y += 42;

        var lblActive = MakeLabel("Active", y);
        _chkIsActive = new CheckBox
        {
            Location = new Point(inputX, y + 3),
            AutoSize = true,
            Font = AppTheme.FontDefault,
            ForeColor = AppTheme.TextPrimary,
            Checked = true
        };
        _chkIsActive.CheckedChanged += (_, _) => MarkDirty();
        y += 40;

        // Teams section
        var lblTeamsHeader = new Label
        {
            Text = "Teams",
            Font = AppTheme.FontSmallBold,
            ForeColor = AppTheme.TextPrimary,
            AutoSize = true,
            Location = new Point(labelX, y)
        };
        scroll.Controls.Add(lblTeamsHeader);
        y += 20;

        _lstTeams = new ListBox
        {
            Location = new Point(labelX, y),
            Size = new Size(inputW, 80),
            BackColor = AppTheme.ContentBackground,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.FontSmall,
            IntegralHeight = false
        };
        _lstTeams.DoubleClick += (_, _) => NavigateToSelectedTeam();
        scroll.Controls.Add(_lstTeams);

        _lblTeamsContent = new Label
        {
            Location = new Point(labelX, y + 82),
            AutoSize = true,
            Font = AppTheme.FontSmall,
            ForeColor = AppTheme.TextMuted,
            Text = "Double-click to open team editor"
        };
        scroll.Controls.Add(_lblTeamsContent);

        y += 105;

        // League Status section (read-only, shown only if data exists)
        const int col1X = 24;
        const int col2X = 330;

        var lblLftHeader = new Label
        {
            Text = "Looking For Team",
            Font = AppTheme.FontSmallBold,
            ForeColor = AppTheme.TextPrimary,
            AutoSize = true,
            Location = new Point(col1X, y)
        };
        scroll.Controls.Add(lblLftHeader);

        var lblSpareHeader = new Label
        {
            Text = "Spare List",
            Font = AppTheme.FontSmallBold,
            ForeColor = AppTheme.TextPrimary,
            AutoSize = true,
            Location = new Point(col2X, y)
        };
        scroll.Controls.Add(lblSpareHeader);

        y += 20;

        _lblLookingForTeamsContent = new Label
        {
            Location = new Point(col1X, y),
            Size = new Size(280, 80),
            BackColor = AppTheme.ContentBackground,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.FontSmall,
            AutoSize = false,
            Visible = false
        };
        scroll.Controls.Add(_lblLookingForTeamsContent);

        _lblSpareListContent = new Label
        {
            Location = new Point(col2X, y),
            Size = new Size(280, 80),
            BackColor = AppTheme.ContentBackground,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.FontSmall,
            AutoSize = false,
            Visible = false
        };
        scroll.Controls.Add(_lblSpareListContent);

        y += 90;

        _lblLeagueContext = new Label
        {
            Text = "Uses default league context.",
            Location = new Point(inputX, y + 2),
            AutoSize = true,
            Font = AppTheme.FontSmall,
            ForeColor = AppTheme.TextMuted
        };

        scroll.Controls.AddRange([
            _lblModeHint,
            lblFirstName, _txtFirstName,
            lblLastName, _txtLastName,
            lblEmail, _txtEmail,
            lblPhone, _txtPhone,
            lblLot, _txtLotNumber,
            lblPartner, _cmbPartner,
            lblRole, _cmbRole,
            lblActive, _chkIsActive,
            _lblTeamsContent,
            _lblLookingForTeamsContent,
            _lblSpareListContent
        ]);

        _fieldControls =
        [
            lblFirstName, _txtFirstName,
            lblLastName, _txtLastName,
            lblEmail, _txtEmail,
            lblPhone, _txtPhone,
            lblLot, _txtLotNumber,
            lblPartner, _cmbPartner,
            lblRole, _cmbRole,
            lblActive, _chkIsActive,
            lblTeamsHeader, _lstTeams, _lblTeamsContent,
            lblLftHeader, lblSpareHeader, _lblLookingForTeamsContent, _lblSpareListContent
        ];

        root.Controls.Add(scroll);
        root.Controls.Add(toolbar);
        return root;
    }

    private static TextBox MakeInput(int x, int y, int width)
    {
        return new TextBox
        {
            Location = new Point(x, y),
            Size = new Size(width, 28),
            Font = AppTheme.FontDefault
        };
    }

    private static Button MakeButton(string text, Color backColor, Color foreColor, Point location, int width)
    {
        var btn = new Button
        {
            Text = text,
            Location = location,
            Size = new Size(width, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = backColor,
            ForeColor = foreColor,
            Font = AppTheme.FontButton,
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    private void SetFieldsVisible(bool visible)
    {
        foreach (var c in _fieldControls)
            c.Visible = visible;
    }

    private void LoadRoleOptions()
    {
        using var db = new BocceDbContext();
        var roles = db.PlayerRoles
            .AsNoTracking()
            .OrderBy(r => r.Id)
            .Select(r => new RoleItem { Id = r.Id, Name = r.RoleName })
            .ToList();

        _cmbRole.DataSource = roles;
        _cmbRole.DisplayMember = "Name";
        _cmbRole.ValueMember = "Id";
    }

    private int GetSelectedRoleId() =>
        _cmbRole.SelectedItem is RoleItem ri ? ri.Id : 0;

    private void SetSelectedRoleId(int roleId)
    {
        foreach (var item in _cmbRole.Items)
        {
            if (item is RoleItem ri && ri.Id == roleId)
            {
                _cmbRole.SelectedItem = ri;
                return;
            }
        }
    }

    // ── Filter option loading (Season / Division / Team cascading dropdowns) ──

    private void LoadSeasonFilterOptions()
    {
        using var db = new BocceDbContext();
        var leagueId = AppParameterService.GetDefaultLeagueId(db);

        var items = new List<FilterItem> { new() { Id = null, Name = "All Seasons" } };
        if (leagueId.HasValue)
        {
            items.AddRange(db.Seasons
                .Where(s => s.LeagueId == leagueId.Value)
                .OrderByDescending(s => s.StartDate)
                .Select(s => new { s.Id, s.Name })
                .AsEnumerable()
                .Select(s => new FilterItem { Id = s.Id, Name = s.Name }));
        }

        _cmbFilterSeason.DataSource = items;
        _cmbFilterSeason.DisplayMember = nameof(FilterItem.Name);
        _cmbFilterSeason.ValueMember = nameof(FilterItem.Id);
        _cmbFilterSeason.SelectedIndex = 0;
    }

    private void LoadDivisionFilterOptions()
    {
        int? seasonId = (_cmbFilterSeason.SelectedItem as FilterItem)?.Id;

        using var db = new BocceDbContext();
        var items = new List<FilterItem> { new() { Id = null, Name = "All Divisions" } };
        if (seasonId.HasValue)
        {
            items.AddRange(db.Divisions
                .Where(d => d.SeasonId == seasonId.Value)
                .OrderBy(d => d.SortName).ThenBy(d => d.Name)
                .Select(d => new { d.Id, d.Name })
                .AsEnumerable()
                .Select(d => new FilterItem { Id = d.Id, Name = d.Name }));
        }

        _cmbFilterDivision.DataSource = items;
        _cmbFilterDivision.DisplayMember = nameof(FilterItem.Name);
        _cmbFilterDivision.ValueMember = nameof(FilterItem.Id);
        _cmbFilterDivision.SelectedIndex = 0;
        _cmbFilterDivision.Enabled = seasonId.HasValue;
    }

    private void LoadTeamFilterOptions()
    {
        int? divisionId = (_cmbFilterDivision.SelectedItem as FilterItem)?.Id;

        using var db = new BocceDbContext();
        var items = new List<FilterItem> { new() { Id = null, Name = "All Teams" } };
        if (divisionId.HasValue)
        {
            items.AddRange(db.Teams
                .Where(t => t.DivisionId == divisionId.Value)
                .OrderBy(t => t.TeamLetter)
                .Select(t => new { t.Id, t.TeamLetter, t.DisplayName, t.SystemName })
                .AsEnumerable()
                .Select(t => new FilterItem { Id = t.Id, Name = t.DisplayName ?? t.SystemName }));
        }

        _cmbFilterTeam.DataSource = items;
        _cmbFilterTeam.DisplayMember = nameof(FilterItem.Name);
        _cmbFilterTeam.ValueMember = nameof(FilterItem.Id);
        _cmbFilterTeam.SelectedIndex = 0;
        _cmbFilterTeam.Enabled = divisionId.HasValue;
    }

    private void OnSeasonFilterChanged()
    {
        if (_isLoadingFilters) return;
        _isLoadingFilters = true;
        try
        {
            LoadDivisionFilterOptions();
            LoadTeamFilterOptions();
        }
        finally { _isLoadingFilters = false; }
        ApplySearchFilter();
    }

    private void OnDivisionFilterChanged()
    {
        if (_isLoadingFilters) return;
        _isLoadingFilters = true;
        try
        {
            LoadTeamFilterOptions();
        }
        finally { _isLoadingFilters = false; }
        ApplySearchFilter();
    }

    private void LoadFilterContextData()
    {
        using var db = new BocceDbContext();
        var leagueId = AppParameterService.GetDefaultLeagueId(db);

        _teamMemberships.Clear();
        _sparePlayerIds.Clear();
        _lookingForEntries.Clear();
        if (!leagueId.HasValue) return;

        _teamMemberships = db.TeamPlayers
            .Where(tp => tp.Team.Division.Season.LeagueId == leagueId.Value)
            .Select(tp => new { tp.PlayerId, tp.TeamId, DivisionId = tp.Team.DivisionId, SeasonId = tp.Team.Division.SeasonId })
            .AsEnumerable()
            .Select(x => (x.PlayerId, x.TeamId, x.DivisionId, x.SeasonId))
            .ToList();

        _sparePlayerIds = db.SpareLists
            .Where(s => s.LeagueId == leagueId.Value && s.IsActive)
            .Select(s => s.PlayerId)
            .ToHashSet();

        _lookingForEntries = db.LookingForTeams
            .Where(l => l.LeagueId == leagueId.Value)
            .Select(l => new { l.PlayerId, l.SeasonId })
            .AsEnumerable()
            .Select(x => (x.PlayerId, x.SeasonId))
            .ToList();
    }

    private bool PassesFilters(PlayerLookup p)
    {
        int? seasonId = (_cmbFilterSeason.SelectedItem as FilterItem)?.Id;
        int? divisionId = (_cmbFilterDivision.SelectedItem as FilterItem)?.Id;
        int? teamId = (_cmbFilterTeam.SelectedItem as FilterItem)?.Id;
        int onTeamIdx = _cmbFilterOnTeam.SelectedIndex;       // 0 All, 1 On Team, 2 Off Team
        int spareIdx = _cmbFilterSpare.SelectedIndex;         // 0 All, 1 Spare List, 2 Not Spare
        int lftIdx = _cmbFilterLookingForTeam.SelectedIndex;  // 0 All, 1 Looking, 2 Not Looking

        if (teamId.HasValue)
        {
            if (!_teamMemberships.Any(m => m.PlayerId == p.Id && m.TeamId == teamId.Value)) return false;
        }
        else if (divisionId.HasValue)
        {
            if (!_teamMemberships.Any(m => m.PlayerId == p.Id && m.DivisionId == divisionId.Value)) return false;
        }
        else if (onTeamIdx != 0)
        {
            bool onTeam = seasonId.HasValue
                ? _teamMemberships.Any(m => m.PlayerId == p.Id && m.SeasonId == seasonId.Value)
                : _teamMemberships.Any(m => m.PlayerId == p.Id);
            if (onTeamIdx == 1 && !onTeam) return false;
            if (onTeamIdx == 2 && onTeam) return false;
        }

        if (spareIdx != 0)
        {
            bool isSpare = _sparePlayerIds.Contains(p.Id);
            if (spareIdx == 1 && !isSpare) return false;
            if (spareIdx == 2 && isSpare) return false;
        }

        if (lftIdx != 0)
        {
            bool isLooking = seasonId.HasValue
                ? _lookingForEntries.Any(e => e.PlayerId == p.Id && e.SeasonId == seasonId.Value)
                : _lookingForEntries.Any(e => e.PlayerId == p.Id);
            if (lftIdx == 1 && !isLooking) return false;
            if (lftIdx == 2 && isLooking) return false;
        }

        return true;
    }

    private void LoadPlayerLookup(int? selectPlayerId = null)
    {
        using var db = new BocceDbContext();
        _allPlayers.Clear();

        _allPlayers.AddRange(db.Players
            .AsNoTracking()
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Select(p => new PlayerLookup
            {
                Id = p.Id,
                FirstName = p.FirstName,
                LastName = p.LastName,
                Email = p.Email,
                Phone = p.Phone,
                LotNumber = p.LotNumber,
                IsActive = p.IsActive
            })
            .ToList());

        LoadFilterContextData();
        ApplySearchFilter(selectPlayerId);
    }

    private int? GetSelectedPartnerId()
    {
        if (_cmbPartner.SelectedItem is PartnerItem pi)
            return pi.Id;
        return null;
    }

    private void ApplySearchFilter(int? preferredPlayerId = null)
    {
        _isSearching = true;
        try
        {
            var currentId = preferredPlayerId ?? ((_lstPlayers.SelectedItem as PlayerListItem)?.Id);
            var query = _txtSearch.SearchText;

            var filtered = _allPlayers
                .Where(PassesFilters)
                .Where(p => SearchQueryService.MatchesAnyTerm($"{p.DisplayName} {p.FullName} {p.Email} {p.Phone} {p.LotNumber}", query))
                .Select(p => new PlayerListItem
                {
                    Id = p.Id,
                    Label = $"{p.DisplayName}{(p.IsActive ? "" : " (inactive)")}"
                })
                .ToList();

            _lstPlayers.BeginUpdate();
            _lstPlayers.DataSource = null;
            _lstPlayers.DataSource = filtered;
            _lstPlayers.EndUpdate();

            if (currentId.HasValue)
            {
                for (int i = 0; i < _lstPlayers.Items.Count; i++)
                {
                    if (_lstPlayers.Items[i] is PlayerListItem item && item.Id == currentId.Value)
                    {
                        _lstPlayers.SelectedIndex = i;
                        break;
                    }
                }
            }
        }
        finally
        {
            _isSearching = false;
        }
    }

    private void OnPlayerSelectedFromLookup()
    {
        if (_mode == PlayerMode.Create) return;
        if (_isSearching) return;
        if (_lstPlayers.SelectedItem is not PlayerListItem item) return;

        LoadPlayerForView(item.Id);
    }

    private void LoadPlayerForView(int playerId)
    {
        _isLoadingData = true;
        try
        {
            using var db = new BocceDbContext();
            var p = db.Players.AsNoTracking().FirstOrDefault(x => x.Id == playerId);
            if (p == null) return;

            _selectedPlayerId = p.Id;
            SetFieldsVisible(true);
            _txtFirstName.Text = p.FirstName;
            _txtLastName.Text = p.LastName;
            _txtEmail.Text = p.Email ?? "";
            _txtPhone.Text = p.Phone ?? "";
            _txtLotNumber.Text = p.LotNumber ?? "";
            _chkIsActive.Checked = p.IsActive;
            SetSelectedRoleId(p.Role);

            PopulatePartnerLookup(p.Id, p.PartnerPlayerId);
            LoadPlayerTeams(p.Id);
            LoadPlayerLeagueStatus(p.Id);
        }
        finally
        {
            _isLoadingData = false;
            SetMode(PlayerMode.View);
        }
    }

    private void PopulatePartnerLookup(int? selfPlayerId, int? selectedPartnerId)
    {
        var partners = _allPlayers
            .Where(p => !selfPlayerId.HasValue || p.Id != selfPlayerId.Value)
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Select(p => new PartnerItem { Id = p.Id, Name = p.DisplayName })
            .ToList();

        partners.Insert(0, new PartnerItem { Id = null, Name = "(none)" });

        _cmbPartner.DataSource = null;
        _cmbPartner.DisplayMember = nameof(PartnerItem.Name);
        _cmbPartner.ValueMember = nameof(PartnerItem.Id);
        _cmbPartner.DataSource = partners;

        int selectedIndex = 0;
        if (selectedPartnerId.HasValue)
        {
            for (int i = 0; i < partners.Count; i++)
            {
                if (partners[i].Id == selectedPartnerId)
                {
                    selectedIndex = i;
                    break;
                }
            }
        }
        _cmbPartner.SelectedIndex = selectedIndex;
    }

    private void AddNewPlayer()
    {
        _isLoadingData = true;
        try
        {
            _selectedPlayerId = null;
            SetFieldsVisible(true);
            ClearEditor();
        }
        finally
        {
            _isLoadingData = false;
            SetMode(PlayerMode.Create);
        }
    }

    private void ClearEditor()
    {
        _txtFirstName.Text = "";
        _txtLastName.Text = "";
        _txtEmail.Text = "";
        _txtPhone.Text = "";
        _txtLotNumber.Text = "";
        _chkIsActive.Checked = true;
        SetSelectedRoleId(0);
        PopulatePartnerLookup(null, null);
        _lstTeams.Items.Clear();
        _lstTeams.Visible = false;
        _lblTeamsContent.Visible = false;
        _lblLookingForTeamsContent.Visible = false;
        _lblSpareListContent.Visible = false;
    }

    private void ReturnToEmptyState()
    {
        _selectedPlayerId = null;
        _isDirty = false;
        _mode = PlayerMode.View;
        _isCreatingNew = false;
        ClearEditor();
        SetFieldsVisible(false);
        UpdateButtonVisibility();
        _lblModeHint.Text = "Select a player from the list, or create a new one.";

        _lstPlayers.ClearSelected();
    }

    private void SetMode(PlayerMode mode)
    {
        _mode = mode;
        _isCreatingNew = (mode == PlayerMode.Create);

        UpdateButtonVisibility();

        _lblModeHint.Text = mode switch
        {
            PlayerMode.Create => BuildCreateModeHint(),
            _ when _selectedPlayerId.HasValue => "Editing player. Remember to Save.",
            _ => "Select a player to edit, or create a new one."
        };
    }

    private void MarkDirty()
    {
        if (_isLoadingData || _isSearching) return;
        _isDirty = true;
    }

    private void UpdateButtonVisibility()
    {
        bool editing = _isCreatingNew || _selectedPlayerId.HasValue;

        _btnNew.Visible    = !_isCreatingNew;
        _btnSave.Visible   = editing;
        _btnSave.Text      = _isCreatingNew ? "Create Player" : "Save Player";
        _btnClear.Visible  = editing;
        _btnDelete.Visible = _selectedPlayerId.HasValue && !_isCreatingNew;
    }

    private static string BuildCreateModeHint()
    {
        using var db = new BocceDbContext();
        var defaultLeagueId = AppParameterService.GetDefaultLeagueId(db);
        if (!defaultLeagueId.HasValue)
            return "Creating a new player. No default league is set, so league status checkboxes are disabled.";

        var leagueName = db.Leagues.AsNoTracking().Where(l => l.Id == defaultLeagueId.Value).Select(l => l.Name).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(leagueName))
            return "Creating a new player. League status checkboxes use your default league.";

        return $"Creating a new player. League status checkboxes use default league: {leagueName}.";
    }

    private sealed class TeamDisplay
    {
        public int TeamId { get; set; }
        public string DisplayText { get; set; } = "";
        public override string ToString() => DisplayText;
    }

    private void LoadPlayerTeams(int playerId)
    {
        try
        {
            using var db = new BocceDbContext();
            int? currentSeasonId = AppParameterService.GetDefaultSeasonId(db);

            var teams = db.TeamPlayers
                .Where(tp => tp.PlayerId == playerId)
                .Include(tp => tp.Team)
                .ThenInclude(t => t.Division)
                .ThenInclude(d => d.Season)
                .AsNoTracking()
                .ToList();

            // Filter to current season only
            if (currentSeasonId.HasValue)
            {
                teams = teams.Where(tp => tp.Team.Division.Season.Id == currentSeasonId.Value).ToList();
            }

            _lstTeams.Items.Clear();
            if (teams.Count == 0)
            {
                _lstTeams.Visible = false;
                _lblTeamsContent.Visible = false;
                return;
            }

            _lstTeams.Visible = true;
            _lblTeamsContent.Visible = true;

            foreach (var tp in teams)
            {
                var displayText = $"{tp.Team.Division.Name} - {tp.Team.EffectiveDisplayName}";
                _lstTeams.Items.Add(new TeamDisplay { TeamId = tp.Team.Id, DisplayText = displayText });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading player teams: {ex.Message}");
        }
    }

    private void LoadPlayerLeagueStatus(int playerId)
    {
        try
        {
            using var db = new BocceDbContext();
            int? currentSeasonId = AppParameterService.GetDefaultSeasonId(db);

            // Get Looking for Team entries (filtered to current season)
            var lftEntries = db.LookingForTeams
                .Where(l => l.PlayerId == playerId)
                .Include(l => l.Season)
                .Include(l => l.League)
                .AsNoTracking()
                .ToList();

            if (currentSeasonId.HasValue)
            {
                lftEntries = lftEntries.Where(l => l.SeasonId == currentSeasonId.Value).ToList();
            }

            // Get Spare List entries (no season filter, but showing league name)
            var spareEntries = db.SpareLists
                .Where(s => s.PlayerId == playerId && s.IsActive)
                .Include(s => s.League)
                .AsNoTracking()
                .ToList();

            // Display LFT if any entries
            if (lftEntries.Count > 0)
            {
                _lblLookingForTeamsContent.Visible = true;
                var lftText = string.Join("\n", lftEntries.Select(l => l.League.Name));
                _lblLookingForTeamsContent.Text = lftText;
            }
            else
            {
                _lblLookingForTeamsContent.Visible = false;
            }

            // Display Spare List if any entries
            if (spareEntries.Count > 0)
            {
                _lblSpareListContent.Visible = true;
                var spareText = string.Join("\n", spareEntries.Select(s => s.League.Name));
                _lblSpareListContent.Text = spareText;
            }
            else
            {
                _lblSpareListContent.Visible = false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading player league status: {ex.Message}");
        }
    }

    private void NavigateToSelectedTeam()
    {
        if (_lstTeams.SelectedItem is not TeamDisplay selectedTeam || !_selectedPlayerId.HasValue)
            return;

        try
        {
            if (FindParentMainForm() is MainForm mainForm)
                mainForm.NavigateToTeam(selectedTeam.TeamId);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unable to navigate to team: {ex.Message}", "Navigation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private MainForm? FindParentMainForm()
    {
        var control = Parent;
        while (control != null)
        {
            if (control is MainForm mainForm)
                return mainForm;
            control = control.Parent;
        }
        return null;
    }

    private void SavePlayer()
    {
        string firstName = _txtFirstName.Text.Trim();
        string lastName = _txtLastName.Text.Trim();

        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
        {
            MessageBox.Show("First Name and Last Name are required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            using var db = new BocceDbContext();

            var selectedPartnerId = GetSelectedPartnerId();
            if (_mode == PlayerMode.Create)
            {
                var player = new Player
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = NullIfEmpty(_txtEmail.Text),
                    Phone = NullIfEmpty(_txtPhone.Text),
                    LotNumber = NullIfEmpty(_txtLotNumber.Text),
                    IsActive = _chkIsActive.Checked,
                    Role = GetSelectedRoleId(),
                    CreatedAt = DateTime.UtcNow
                };

                db.Players.Add(player);
                db.SaveChanges();

                FeeService.EnsureInitiationFee(db, player.Id);

                UpdatePartnerLink(db, player, selectedPartnerId);
                db.SaveChanges();
            }
            else if (_mode == PlayerMode.View && _selectedPlayerId.HasValue)
            {
                var player = db.Players.FirstOrDefault(p => p.Id == _selectedPlayerId.Value);
                if (player == null) return;

                player.FirstName = firstName;
                player.LastName = lastName;
                player.Email = NullIfEmpty(_txtEmail.Text);
                player.Phone = NullIfEmpty(_txtPhone.Text);
                player.LotNumber = NullIfEmpty(_txtLotNumber.Text);
                player.IsActive = _chkIsActive.Checked;
                player.Role = GetSelectedRoleId();

                UpdatePartnerLink(db, player, selectedPartnerId);
                db.SaveChanges();
            }
            else
            {
                return;
            }

            MessageBox.Show("Player saved.", "Players", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ReturnToEmptyState();
            LoadPlayerLookup();
        }
        catch (Exception ex)
        {
            string fullError = ex.Message;
            if (ex.InnerException != null) fullError += $"\n\nInner: {ex.InnerException.Message}";
            MessageBox.Show($"Unable to save player.\n\n{fullError}", "Players", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void UpdatePartnerLink(BocceDbContext db, Player player, int? newPartnerId)
    {
        if (newPartnerId == player.Id)
            newPartnerId = null;

        int? oldPartnerId = player.PartnerPlayerId;
        if (oldPartnerId.HasValue && oldPartnerId.Value != newPartnerId)
        {
            var oldPartner = db.Players.FirstOrDefault(p => p.Id == oldPartnerId.Value);
            if (oldPartner != null && oldPartner.PartnerPlayerId == player.Id)
                oldPartner.PartnerPlayerId = null;
        }

        player.PartnerPlayerId = null;

        if (!newPartnerId.HasValue)
            return;

        var newPartner = db.Players.FirstOrDefault(p => p.Id == newPartnerId.Value);
        if (newPartner == null)
            return;

        if (newPartner.PartnerPlayerId.HasValue && newPartner.PartnerPlayerId.Value != player.Id)
        {
            var displaced = db.Players.FirstOrDefault(p => p.Id == newPartner.PartnerPlayerId.Value);
            if (displaced != null && displaced.PartnerPlayerId == newPartner.Id)
                displaced.PartnerPlayerId = null;
        }

        newPartner.PartnerPlayerId = player.Id;
        player.PartnerPlayerId = newPartner.Id;
    }

    private void DeletePlayer()
    {
        if (!_selectedPlayerId.HasValue)
            return;

        var confirm = MessageBox.Show(
            "Delete this player? This may fail if the player is referenced by teams, fees, or other records.",
            "Confirm Delete",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes)
            return;

        try
        {
            using var db = new BocceDbContext();
            var player = db.Players.FirstOrDefault(p => p.Id == _selectedPlayerId.Value);
            if (player == null) return;

            UpdatePartnerLink(db, player, null);
            db.Players.Remove(player);
            db.SaveChanges();

            ReturnToEmptyState();
            LoadPlayerLookup();
        }
        catch (DbUpdateException)
        {
            MessageBox.Show(
                "Player cannot be deleted because it is referenced by other records. Remove those links first.",
                "Delete Blocked",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unable to delete player.\n\n{ex.Message}", "Players", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string? NullIfEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

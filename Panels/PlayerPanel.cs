using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.Services;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Panels;

public class PlayerPanel : UserControl
{
    private enum PlayerMode { View, Edit, Create }

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

    private TextBox _txtSearch = null!;
    private ListBox _lstPlayers = null!;

    private TextBox _txtFirstName = null!;
    private TextBox _txtLastName = null!;
    private TextBox _txtEmail = null!;
    private TextBox _txtPhone = null!;
    private TextBox _txtLotNumber = null!;
    private CheckBox _chkIsActive = null!;
    private ComboBox _cmbPartner = null!;
    private Label _lblCreatedAt = null!;
    private Label _lblModeHint = null!;
    private Label _lblLeagueContext = null!;

    private CheckedListBox _lstLookingForTeams = null!;
    private CheckedListBox _lstSpareList = null!;
    private List<(int LeagueId, int SeasonId)> _lookingForTeamItems = [];
    private List<int> _spareListItems = [];

    private Button _btnNew = null!;
    private Button _btnEdit = null!;
    private Button _btnSave = null!;
    private Button _btnCancel = null!;
    private Button _btnDelete = null!;

    public PlayerPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;

        BuildUi();
        LoadPlayerLookup();
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

        _txtSearch = new TextBox
        {
            Dock = DockStyle.Top,
            Height = 30,
            Font = AppTheme.FontDefault,
            PlaceholderText = "Search name, email, phone, lot"
        };
        _txtSearch.TextChanged += (_, _) => ApplySearchFilter();

        var searchHint = new Label
        {
            Text = "Tip: delimiters |  \\  /  :  ; are supported",
            Dock = DockStyle.Top,
            Height = 20,
            Font = AppTheme.FontSmall,
            ForeColor = AppTheme.TextMuted,
            Padding = new Padding(1, 2, 0, 0)
        };

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

        _btnEdit = MakeButton("Edit Player", AppTheme.Accent, Color.White, new Point(12, 10), 120);
        _btnEdit.Click += (_, _) => SetMode(PlayerMode.Edit);

        _btnSave = MakeButton("Save Player", AppTheme.Accent, Color.White, new Point(12, 10), 120);
        _btnSave.Click += (_, _) => SavePlayer();

        _btnCancel = MakeButton("Cancel", AppTheme.Surface, AppTheme.TextPrimary, new Point(140, 10), 100);
        _btnCancel.FlatAppearance.BorderSize = 1;
        _btnCancel.FlatAppearance.BorderColor = AppTheme.Separator;
        _btnCancel.Click += (_, _) => CancelEdit();

        _btnDelete = MakeButton("Delete Player", AppTheme.ButtonDanger, Color.White, new Point(250, 10), 130);
        _btnDelete.Click += (_, _) => DeletePlayer();

        toolbar.Controls.AddRange([_btnEdit, _btnSave, _btnCancel, _btnDelete]);

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
        y += 38;

        var lblCreated = MakeLabel("Created", y);
        _lblCreatedAt = new Label
        {
            Location = new Point(inputX, y + 4),
            AutoSize = true,
            Font = AppTheme.FontDefault,
            ForeColor = AppTheme.TextSecondary
        };
        y += 40;

        // Column headers for League Status section
        const int col1X = 230;
        const int col2X = 500;

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

        // Divider lines
        var divider1 = new Label
        {
            Text = "============",
            Font = AppTheme.FontSmall,
            ForeColor = AppTheme.TextMuted,
            AutoSize = true,
            Location = new Point(col1X, y)
        };
        scroll.Controls.Add(divider1);

        var divider2 = new Label
        {
            Text = "=======",
            Font = AppTheme.FontSmall,
            ForeColor = AppTheme.TextMuted,
            AutoSize = true,
            Location = new Point(col2X, y)
        };
        scroll.Controls.Add(divider2);

        y += 18;

        // Looking for Teams listbox with checkboxes
        _lstLookingForTeams = new CheckedListBox
        {
            Location = new Point(col1X, y),
            Size = new Size(420, 80),
            BackColor = AppTheme.ContentBackground,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.FontSmall,
            CheckOnClick = true,
            Visible = false
        };
        _lstLookingForTeams.ItemCheck += (_, _) => MarkDirty();
        scroll.Controls.Add(_lstLookingForTeams);

        y += 95;

        // Spare List listbox with checkboxes
        _lstSpareList = new CheckedListBox
        {
            Location = new Point(col1X, y),
            Size = new Size(420, 80),
            BackColor = AppTheme.ContentBackground,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.FontSmall,
            CheckOnClick = true,
            Visible = false
        };
        _lstSpareList.ItemCheck += (_, _) => MarkDirty();
        scroll.Controls.Add(_lstSpareList);

        y += 95;

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
            lblActive, _chkIsActive,
            lblCreated, _lblCreatedAt,
            _lblLeagueContext
        ]);

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

        ApplySearchFilter(selectPlayerId);
        PopulatePartnerLookup(_selectedPlayerId, GetSelectedPartnerId());
        RefreshLeagueContextAndStatus(_selectedPlayerId);
    }

    private int? GetSelectedPartnerId()
    {
        if (_cmbPartner.SelectedItem is PartnerItem pi)
            return pi.Id;
        return null;
    }

    private void ApplySearchFilter(int? preferredPlayerId = null)
    {
        var currentId = preferredPlayerId ?? ((_lstPlayers.SelectedItem as PlayerListItem)?.Id);
        var query = _txtSearch.Text;

        var filtered = _allPlayers
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
                    PopulatePartnerLookup(_selectedPlayerId, GetSelectedPartnerId());
                    return;
                }
            }
        }

        if (_lstPlayers.Items.Count > 0)
            _lstPlayers.SelectedIndex = 0;
        else
            ClearEditor();

        PopulatePartnerLookup(_selectedPlayerId, GetSelectedPartnerId());
    }

    private void OnPlayerSelectedFromLookup()
    {
        if (_mode == PlayerMode.Create) return;
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
            _txtFirstName.Text = p.FirstName;
            _txtLastName.Text = p.LastName;
            _txtEmail.Text = p.Email ?? "";
            _txtPhone.Text = p.Phone ?? "";
            _txtLotNumber.Text = p.LotNumber ?? "";
            _chkIsActive.Checked = p.IsActive;
            _lblCreatedAt.Text = p.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

            PopulatePartnerLookup(p.Id, p.PartnerPlayerId);
            RefreshLeagueContextAndStatus(p.Id);
        }
        finally
        {
            _isLoadingData = false;
            SetMode(PlayerMode.View);
        }
    }

    private void PopulatePartnerLookup(int? selfPlayerId, int? selectedPartnerId)
    {
        var visiblePlayerIds = _lstPlayers.Items
            .OfType<PlayerListItem>()
            .Select(i => i.Id)
            .ToHashSet();

        var partners = _allPlayers
            .Where(p => visiblePlayerIds.Count == 0 || visiblePlayerIds.Contains(p.Id))
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
            ClearEditor();
            PopulatePartnerLookup(null, null);
            RefreshLeagueContextAndStatus(null);
            BuildLeagueSelectionListboxes();
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
        _lblCreatedAt.Text = "(new)";
        ClearLeagueSelections();
    }

    private void SetMode(PlayerMode mode)
    {
        _mode = mode;
        _isCreatingNew = (mode == PlayerMode.Create);
        bool hasSelection = _selectedPlayerId.HasValue;

        // Fields are always editable - no ReadOnly mode
        _txtFirstName.ReadOnly = false;
        _txtLastName.ReadOnly = false;
        _txtEmail.ReadOnly = false;
        _txtPhone.ReadOnly = false;
        _txtLotNumber.ReadOnly = false;

        _chkIsActive.Enabled = true;
        _cmbPartner.Enabled = true;

        // Build and populate league selection listboxes
        BuildLeagueSelectionListboxes();
        _lstLookingForTeams.Visible = true;
        _lstSpareList.Visible = true;

        // Populate with current player's data if viewing/editing existing player
        if (_selectedPlayerId.HasValue)
            PopulateLeagueSelectionFromPlayer(_selectedPlayerId.Value);

        _btnNew.Visible = true;
        _btnEdit.Visible = false;
        _btnDelete.Visible = hasSelection;

        _btnSave.Visible = true;
        _btnSave.Enabled = _isCreatingNew;
        _btnSave.Text = mode == PlayerMode.Create ? "Create Player" : "Save Player";

        _btnCancel.Visible = _isCreatingNew;

        _txtSearch.Enabled = true;
        _lstPlayers.Enabled = true;

        if (mode != PlayerMode.Create)
            ClearDirty();

        _lblModeHint.Text = mode switch
        {
            PlayerMode.Create => BuildCreateModeHint(),
            _ => "Select a player to edit, or create a new one."
        };
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

    private bool HasDefaultLeagueContext()
    {
        using var db = new BocceDbContext();
        return AppParameterService.GetDefaultLeagueId(db).HasValue;
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

    private void RefreshLeagueContextAndStatus(int? playerId)
    {
        _lblLeagueContext.Text = "";
    }

    private void CancelEdit()
    {
        if (_selectedPlayerId.HasValue)
            LoadPlayerForView(_selectedPlayerId.Value);
        else
        {
            ClearEditor();
            SetMode(PlayerMode.View);
        }
    }

    private List<(int LeagueId, int SeasonId)> GetCheckedLookingForTeamEntries()
    {
        var result = new List<(int, int)>();
        for (int i = 0; i < _lstLookingForTeams.CheckedItems.Count; i++)
        {
            if (_lstLookingForTeams.GetItemChecked(i) && i < _lookingForTeamItems.Count)
                result.Add(_lookingForTeamItems[i]);
        }
        return result;
    }

    private List<int> GetCheckedSpareLeagues()
    {
        var result = new List<int>();
        for (int i = 0; i < _lstSpareList.Items.Count; i++)
        {
            if (_lstSpareList.GetItemChecked(i) && i < _spareListItems.Count)
                result.Add(_spareListItems[i]);
        }
        return result;
    }

    private void ClearLeagueSelections()
    {
        _lookingForTeamItems.Clear();
        _spareListItems.Clear();
        _lstLookingForTeams.Items.Clear();
        _lstSpareList.Items.Clear();
    }

    private void BuildLeagueSelectionListboxes()
    {
        ClearLeagueSelections();

        try
        {
            using var db = new BocceDbContext();

            // Build Looking for Team listbox (League/Season pairs - current and next season)
            var leagues = db.Leagues.OrderBy(l => l.Name).ToList();

            foreach (var league in leagues)
            {
                var seasons = db.Seasons.Where(s => s.LeagueId == league.Id)
                    .OrderByDescending(s => s.IsCurrent)
                    .ThenByDescending(s => s.StartDate)
                    .Take(2)  // Current + next
                    .ToList();

                foreach (var season in seasons)
                {
                    var displayText = $"{league.Name} - {season.Name}" + (season.IsCurrent ? " ★" : "");
                    _lstLookingForTeams.Items.Add((league.Id, season.Id), false);
                    int index = _lstLookingForTeams.Items.Count - 1;
                    _lstLookingForTeams.Items[index] = displayText;
                    _lookingForTeamItems.Add((league.Id, season.Id));
                }
            }

            // Build Spare List listbox (Leagues)
            foreach (var league in leagues)
            {
                _lstSpareList.Items.Add(league.Name, false);
                _spareListItems.Add(league.Id);
            }
        }
        catch { }
    }

    private void PopulateLeagueSelectionFromPlayer(int playerId)
    {
        if (_lookingForTeamItems.Count == 0 && _spareListItems.Count == 0)
            return;

        try
        {
            using var db = new BocceDbContext();

            // Get player's current LookingForTeam entries
            var playerLft = db.LookingForTeams
                .Where(l => l.PlayerId == playerId)
                .Select(l => new { l.LeagueId, l.SeasonId })
                .ToList();

            // Get player's current SpareList entries
            var playerSpare = db.SpareLists
                .Where(s => s.PlayerId == playerId && s.IsActive)
                .Select(s => s.LeagueId)
                .ToList();

            // Check Looking for Team items
            for (int i = 0; i < _lookingForTeamItems.Count; i++)
            {
                var (leagueId, seasonId) = _lookingForTeamItems[i];
                if (playerLft.Any(l => l.LeagueId == leagueId && l.SeasonId == seasonId))
                    _lstLookingForTeams.SetItemChecked(i, true);
            }

            // Check Spare List items
            for (int i = 0; i < _spareListItems.Count; i++)
            {
                if (playerSpare.Contains(_spareListItems[i]))
                    _lstSpareList.SetItemChecked(i, true);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error populating league selections: {ex.Message}");
        }
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
                    CreatedAt = DateTime.UtcNow
                };

                db.Players.Add(player);
                db.SaveChanges();

                UpdatePartnerLink(db, player, selectedPartnerId);
                ApplyLeagueListStatus(db, player, GetCheckedLookingForTeamEntries(), GetCheckedSpareLeagues());

                db.SaveChanges();
                _selectedPlayerId = player.Id;
            }
            else if (_mode == PlayerMode.Edit && _selectedPlayerId.HasValue)
            {
                var player = db.Players.FirstOrDefault(p => p.Id == _selectedPlayerId.Value);
                if (player == null)
                {
                    MessageBox.Show("Player no longer exists.", "Players", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                player.FirstName = firstName;
                player.LastName = lastName;
                player.Email = NullIfEmpty(_txtEmail.Text);
                player.Phone = NullIfEmpty(_txtPhone.Text);
                player.LotNumber = NullIfEmpty(_txtLotNumber.Text);
                player.IsActive = _chkIsActive.Checked;

                UpdatePartnerLink(db, player, selectedPartnerId);
                ApplyLeagueListStatus(db, player, GetCheckedLookingForTeamEntries(), GetCheckedSpareLeagues());
                db.SaveChanges();
            }
            else
            {
                return;
            }

            LoadPlayerLookup(_selectedPlayerId);
            if (_selectedPlayerId.HasValue)
                LoadPlayerForView(_selectedPlayerId.Value);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unable to save player.\n\n{ex.Message}", "Players", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ApplyLeagueListStatus(BocceDbContext db, Player player, List<(int LeagueId, int SeasonId)> lookingForTeamEntries, List<int> spareLeagueIds)
    {
        // Remove all existing looking for teams for this player
        var existingLft = db.LookingForTeams.Where(l => l.PlayerId == player.Id).ToList();
        foreach (var lft in existingLft)
            db.LookingForTeams.Remove(lft);

        // Add new looking for team entries with season binding
        foreach (var (leagueId, seasonId) in lookingForTeamEntries)
        {
            db.LookingForTeams.Add(new LookingForTeam
            {
                LeagueId = leagueId,
                PlayerId = player.Id,
                SeasonId = seasonId,
                TeamId = null
            });
        }

        // Remove all existing spare lists for this player
        var existingSpares = db.SpareLists.Where(s => s.PlayerId == player.Id).ToList();
        foreach (var spare in existingSpares)
            db.SpareLists.Remove(spare);

        // Add new spare list entries
        foreach (var leagueId in spareLeagueIds)
        {
            db.SpareLists.Add(new SpareList
            {
                LeagueId = leagueId,
                PlayerId = player.Id,
                IsActive = true
            });
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

            _selectedPlayerId = null;
            LoadPlayerLookup();
            ClearEditor();
            SetMode(PlayerMode.View);
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

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
    private Label _lblLeagueStatus = null!;
    private Label _lblLeagueContext = null!;

    private Label _lblLookingForTeams = null!;
    private Label _lblSpareListLeagues = null!;
    private Panel _pnlLookingForTeamsCheckboxes = null!;
    private Panel _pnlSpareListCheckboxes = null!;
    private Dictionary<string, CheckBox> _lookingForTeamCheckboxes = new();
    private Dictionary<int, CheckBox> _spareListCheckboxes = new();

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
        _btnNew.Click += (_, _) => StartCreateMode();

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
        y += 42;

        var lblLastName = MakeLabel("Last Name *", y);
        _txtLastName = MakeInput(inputX, y, inputW);
        y += 42;

        var lblEmail = MakeLabel("Email", y);
        _txtEmail = MakeInput(inputX, y, inputW);
        y += 42;

        var lblPhone = MakeLabel("Phone", y);
        _txtPhone = MakeInput(inputX, y, inputW);
        y += 42;

        var lblLot = MakeLabel("Lot Number", y);
        _txtLotNumber = MakeInput(inputX, y, 180);
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

        // Values
        _lblLookingForTeams = new Label
        {
            Text = "(none)",
            Font = AppTheme.FontSmall,
            ForeColor = AppTheme.TextMuted,
            AutoSize = false,
            Size = new Size(250, 40),
            Location = new Point(col1X, y)
        };
        scroll.Controls.Add(_lblLookingForTeams);

        _lblSpareListLeagues = new Label
        {
            Text = "(none)",
            Font = AppTheme.FontSmall,
            ForeColor = AppTheme.TextMuted,
            AutoSize = false,
            Size = new Size(250, 40),
            Location = new Point(col2X, y)
        };
        scroll.Controls.Add(_lblSpareListLeagues);

        y += 50;

        // Looking for Teams checkboxes (for create mode)
        _pnlLookingForTeamsCheckboxes = new Panel
        {
            Location = new Point(col1X, y),
            Size = new Size(250, 100),
            BackColor = AppTheme.ContentBackground,
            AutoScroll = true,
            Visible = false
        };
        scroll.Controls.Add(_pnlLookingForTeamsCheckboxes);

        // Spare List checkboxes (for create mode)
        _pnlSpareListCheckboxes = new Panel
        {
            Location = new Point(col2X, y),
            Size = new Size(250, 100),
            BackColor = AppTheme.ContentBackground,
            AutoScroll = true,
            Visible = false
        };
        scroll.Controls.Add(_pnlSpareListCheckboxes);

        y += 110;
        _lblLeagueStatus = null;

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
        SetMode(PlayerMode.View);
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

    private void StartCreateMode()
    {
        _selectedPlayerId = null;
        ClearEditor();
        PopulatePartnerLookup(null, null);
        RefreshLeagueContextAndStatus(null);
        BuildLeagueSelectionPanels();

        SetMode(PlayerMode.Create);
    }

    private void ClearEditor()
    {
        _txtFirstName.Text = "";
        _txtLastName.Text = "";
        _txtEmail.Text = "";
        _txtPhone.Text = "";
        _txtLotNumber.Text = "";
        _chkIsActive.Checked = true;
        _lblLookingForTeams.Text = "(none)";
        _lblSpareListLeagues.Text = "(none)";
        _lblCreatedAt.Text = "(new)";
        ClearLeagueSelections();
    }

    private void SetMode(PlayerMode mode)
    {
        _mode = mode;
        bool editing = mode != PlayerMode.View;
        bool hasSelection = _selectedPlayerId.HasValue;
        bool hasDefaultLeague = HasDefaultLeagueContext();

        _txtFirstName.ReadOnly = !editing;
        _txtLastName.ReadOnly = !editing;
        _txtEmail.ReadOnly = !editing;
        _txtPhone.ReadOnly = !editing;
        _txtLotNumber.ReadOnly = !editing;

        _chkIsActive.Enabled = editing;
        _cmbPartner.Enabled = editing;

        // Show league selection panels only when creating or editing a player
        if (mode == PlayerMode.Create || mode == PlayerMode.Edit)
        {
            BuildLeagueSelectionPanels();
            _pnlLookingForTeamsCheckboxes.Visible = true;
            _pnlSpareListCheckboxes.Visible = true;

            // If editing, populate checkboxes with current player's data
            if (mode == PlayerMode.Edit && _selectedPlayerId.HasValue)
                PopulateLeagueSelectionFromPlayer(_selectedPlayerId.Value);
        }
        else
        {
            _pnlLookingForTeamsCheckboxes.Visible = false;
            _pnlSpareListCheckboxes.Visible = false;
        }

        _btnEdit.Visible = mode == PlayerMode.View && hasSelection;
        _btnDelete.Visible = mode == PlayerMode.View && hasSelection;

        _btnSave.Visible = editing;
        _btnCancel.Visible = editing;
        _btnSave.Text = mode == PlayerMode.Create ? "Create Player" : "Save Player";

        _txtSearch.Enabled = !editing;
        _lstPlayers.Enabled = !editing;
        _btnNew.Enabled = !editing;

        _lblModeHint.Text = mode switch
        {
            PlayerMode.Create => BuildCreateModeHint(),
            PlayerMode.Edit => "Edit the selected player and click Save Player.",
            _ => "Select a player from the left list, or create a new one."
        };
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
        using var db = new BocceDbContext();

        if (!playerId.HasValue)
        {
            _lblLookingForTeams.Text = "(none)";
            _lblSpareListLeagues.Text = "(none)";
            _lblLeagueContext.Text = "";
            return;
        }

        // Load Looking for Teams (Leagues)
        var lookingForTeams = db.LookingForTeams
            .Where(l => l.PlayerId == playerId.Value)
            .Include(l => l.League)
            .OrderBy(l => l.League.Name)
            .ToList();

        if (lookingForTeams.Count == 0)
            _lblLookingForTeams.Text = "(none)";
        else
            _lblLookingForTeams.Text = string.Join("\n",
                lookingForTeams.Select(x => x.League.Name));

        // Load Spare Lists (Leagues)
        var spareLists = db.SpareLists
            .Where(s => s.PlayerId == playerId.Value && s.IsActive)
            .Include(s => s.League)
            .OrderBy(s => s.League.Name)
            .ToList();

        if (spareLists.Count == 0)
            _lblSpareListLeagues.Text = "(none)";
        else
            _lblSpareListLeagues.Text = string.Join("\n", spareLists.Select(s => s.League.Name));

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

    private List<int> GetCheckedLookingForLeagues()
    {
        var result = new List<int>();
        var seenLeagues = new HashSet<int>();
        foreach (var kvp in _lookingForTeamCheckboxes)
        {
            if (kvp.Value.Checked)
            {
                var parts = kvp.Key.Split('_');
                if (parts.Length == 2 && int.TryParse(parts[0], out int leagueId))
                {
                    if (seenLeagues.Add(leagueId))
                        result.Add(leagueId);
                }
            }
        }
        return result;
    }

    private List<int> GetCheckedSpareLeagues()
    {
        var result = new List<int>();
        foreach (var kvp in _spareListCheckboxes)
        {
            if (kvp.Value.Checked)
                result.Add(kvp.Key);
        }
        return result;
    }

    private void ClearLeagueSelections()
    {
        _lookingForTeamCheckboxes.Clear();
        _spareListCheckboxes.Clear();
        _pnlLookingForTeamsCheckboxes.Controls.Clear();
        _pnlSpareListCheckboxes.Controls.Clear();
    }

    private void BuildLeagueSelectionPanels()
    {
        ClearLeagueSelections();

        try
        {
            using var db = new BocceDbContext();

            // Build Looking for Team checkboxes (League/Season pairs - current and next season)
            var leagues = db.Leagues.OrderBy(l => l.Name).ToList();
            int yPos = 4;

            foreach (var league in leagues)
            {
                var seasons = db.Seasons.Where(s => s.LeagueId == league.Id)
                    .OrderByDescending(s => s.IsCurrent)
                    .ThenByDescending(s => s.StartDate)
                    .Take(2)  // Current + next
                    .ToList();

                if (seasons.Count > 0)
                {
                    var leagueLabel = new Label
                    {
                        Text = $"{league.Name}:",
                        Font = AppTheme.FontSmallBold,
                        ForeColor = AppTheme.TextPrimary,
                        Location = new Point(4, yPos),
                        AutoSize = true
                    };
                    _pnlLookingForTeamsCheckboxes.Controls.Add(leagueLabel);
                    yPos += 20;

                    foreach (var season in seasons)
                    {
                        var chk = new CheckBox
                        {
                            Text = $"{season.Name}" + (season.IsCurrent ? " ★" : ""),
                            Location = new Point(16, yPos),
                            AutoSize = true,
                            Font = AppTheme.FontSmall,
                            ForeColor = AppTheme.TextPrimary
                        };
                        _pnlLookingForTeamsCheckboxes.Controls.Add(chk);
                        _lookingForTeamCheckboxes[$"{league.Id}_{season.Id}"] = chk;
                        yPos += 22;
                    }
                }
            }

            // Build Spare List checkboxes (Leagues)
            yPos = 4;
            foreach (var league in leagues)
            {
                var chk = new CheckBox
                {
                    Text = league.Name,
                    Location = new Point(4, yPos),
                    AutoSize = true,
                    Font = AppTheme.FontSmall,
                    ForeColor = AppTheme.TextPrimary
                };
                _pnlSpareListCheckboxes.Controls.Add(chk);
                _spareListCheckboxes[league.Id] = chk;
                yPos += 22;
            }
        }
        catch { }
    }

    private void PopulateLeagueSelectionFromPlayer(int playerId)
    {
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
                .Where(s => s.PlayerId == playerId)
                .Select(s => s.LeagueId)
                .ToList();

            // Check the Looking for Team boxes
            foreach (var lft in playerLft)
            {
                string key = $"{lft.LeagueId}_{lft.SeasonId}";
                if (_lookingForTeamCheckboxes.TryGetValue(key, out var chk))
                    chk.Checked = true;
            }

            // Check the Spare List boxes
            foreach (var leagueId in playerSpare)
            {
                if (_spareListCheckboxes.TryGetValue(leagueId, out var chk))
                    chk.Checked = true;
            }
        }
        catch { }
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
                ApplyLeagueListStatus(db, player, GetCheckedLookingForLeagues(), GetCheckedSpareLeagues());

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
                ApplyLeagueListStatus(db, player, GetCheckedLookingForLeagues(), GetCheckedSpareLeagues());
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

    private void ApplyLeagueListStatus(BocceDbContext db, Player player, List<int> lookingForLeagueIds, List<int> spareLeagueIds)
    {
        // Remove all existing looking for teams for this player
        var existingLft = db.LookingForTeams.Where(l => l.PlayerId == player.Id).ToList();
        foreach (var lft in existingLft)
            db.LookingForTeams.Remove(lft);

        // Add new looking for team entries
        foreach (var leagueId in lookingForLeagueIds)
        {
            db.LookingForTeams.Add(new LookingForTeam
            {
                LeagueId = leagueId,
                PlayerId = player.Id,
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

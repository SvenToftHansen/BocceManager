using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.Services;
using BocceManager.UI.Controls;
using BocceManager.UI.Theme;

namespace BocceManager.Panels;

// "Create groups from the userbase": custom named recipient groups (EmailList/EmailListMember)
// that Compose can target alongside the built-in division/team/all-members scopes.
public class EmailListsPanel : UserControl
{
    private sealed record ListItem(int Id, string Name, bool IsActive)
    {
        public override string ToString() => IsActive ? Name : $"{Name} (inactive)";
    }

    private sealed record PlayerItem(int Id, string Name, string SearchBlob)
    {
        public override string ToString() => Name;
    }

    private sealed record PlayerRow(int Id, string Display)
    {
        public override string ToString() => Display;
    }

    private sealed record ValueOption(int Id, string Name)
    {
        public override string ToString() => Name;
    }

    private enum PlayerFilterKind
    {
        None, OnTeam, SpecificTeam, OnSpareList, LookingForTeam,
        PlaysOnDay, PlaysAtTime, DivisionLeaders, PlayoffQualified, CaptainsOnly
    }

    private sealed record FilterOption(PlayerFilterKind Kind, string Label)
    {
        public override string ToString() => Label;
    }

    private ListBox _lstLists = null!;
    private SearchBoxControl _txtSearch = null!;
    private ComboBox _cboFilter = null!;
    private ComboBox _cboFilterValue = null!;
    private ListBox _lstMembers = null!;
    private ListBox _lstAvailable = null!;
    private Button _btnAdd = null!, _btnRename = null!, _btnDelete = null!, _btnToggleActive = null!;

    private List<PlayerItem> _allPlayers = [];
    private HashSet<int> _memberIds = [];
    private Dictionary<int, List<string>> _listsByPlayer = [];
    private Dictionary<int, List<(int TeamId, string Name)>> _teamsByPlayer = [];
    private HashSet<int> _sparePlayerIds = [];
    private HashSet<int> _lookingForTeamPlayerIds = [];
    private HashSet<int> _captainPlayerIds = [];
    private HashSet<int> _divisionLeaderPlayerIds = [];
    private HashSet<int> _playoffQualifiedPlayerIds = [];
    private Dictionary<int, HashSet<int>> _daySlotIdsByPlayer = [];
    private Dictionary<int, HashSet<int>> _timeSlotIdsByPlayer = [];
    private List<ValueOption> _teamOptions = [];
    private List<ValueOption> _dayOptions = [];
    private List<ValueOption> _timeOptions = [];
    private int? _leagueId;

    public EmailListsPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;
        BuildUI();
        LoadAll();
    }

    private void BuildUI()
    {
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Panel1MinSize = 0,
            Panel2MinSize = 0,
            BackColor = AppTheme.Separator
        };
        // Panel1MinSize/Panel2MinSize/SplitterDistance can't be set to their real values
        // until the control has a real size — WinForms recomputes SplitterDistance
        // internally when the min sizes are set, and throws if Width is still 0.
        split.SizeChanged += (_, _) => SafeApplySplitDistance(split, 220, 260, 260);
        split.HandleCreated += (_, _) => BeginInvoke(new Action(() => SafeApplySplitDistance(split, 220, 260, 260)));

        // -- Left: lists --
        var leftLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, BackColor = AppTheme.Surface };
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

        var toolbar1 = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(6, 4, 6, 4) };
        _btnAdd = MakeButton("Add", AppTheme.Accent);
        _btnAdd.Click += OnAdd;
        toolbar1.Controls.Add(_btnAdd);
        leftLayout.Controls.Add(toolbar1, 0, 0);

        _lstLists = new ListBox
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.FontDefault,
            BorderStyle = BorderStyle.None,
            ItemHeight = 26
        };
        _lstLists.SelectedIndexChanged += (_, _) => OnListSelected();
        leftLayout.Controls.Add(_lstLists, 0, 1);

        var toolbar2 = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(6, 4, 6, 4) };
        _btnRename = MakeButton("Rename", Color.FromArgb(100, 116, 139));
        _btnToggleActive = MakeButton("Toggle Active", Color.FromArgb(100, 116, 139));
        _btnRename.Click += OnRename;
        _btnToggleActive.Click += OnToggleActive;
        toolbar2.Controls.AddRange([_btnRename, _btnToggleActive]);
        leftLayout.Controls.Add(toolbar2, 0, 2);

        var toolbar3 = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(6, 4, 6, 4) };
        _btnDelete = MakeButton("Delete", AppTheme.ButtonDanger);
        _btnDelete.Click += OnDelete;
        toolbar3.Controls.Add(_btnDelete);
        leftLayout.Controls.Add(toolbar3, 0, 3);

        split.Panel1.Controls.Add(leftLayout);

        // -- Right: members --
        var rightLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4, BackColor = AppTheme.Surface };
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        rightLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        rightLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        var searchPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 6, 8, 6) };
        _txtSearch = new SearchBoxControl("Search name, email, phone, lot") { Dock = DockStyle.Fill };
        _txtSearch.SearchTextChanged += (_, _) => RefreshPlayerLists();
        searchPanel.Controls.Add(_txtSearch);
        rightLayout.Controls.Add(searchPanel, 0, 0);
        rightLayout.SetColumnSpan(searchPanel, 2);

        var filterPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(8, 4, 8, 4), WrapContents = false };
        var lblFilter = new Label { Text = "Filter:", AutoSize = true, ForeColor = AppTheme.TextPrimary, Font = AppTheme.FontDefault, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 8, 6, 0) };
        _cboFilter = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = AppTheme.FontDefault, Width = 190, Margin = new Padding(0, 4, 8, 0) };
        _cboFilter.Items.AddRange(
        [
            new FilterOption(PlayerFilterKind.None, "All Players"),
            new FilterOption(PlayerFilterKind.OnTeam, "On a Team"),
            new FilterOption(PlayerFilterKind.SpecificTeam, "Specific Team..."),
            new FilterOption(PlayerFilterKind.OnSpareList, "On Spare List"),
            new FilterOption(PlayerFilterKind.LookingForTeam, "Looking for a Team"),
            new FilterOption(PlayerFilterKind.PlaysOnDay, "Plays on a Certain Day..."),
            new FilterOption(PlayerFilterKind.PlaysAtTime, "Plays at a Certain Time..."),
            new FilterOption(PlayerFilterKind.DivisionLeaders, "Division Leaders"),
            new FilterOption(PlayerFilterKind.PlayoffQualified, "Qualified for Playoffs"),
            new FilterOption(PlayerFilterKind.CaptainsOnly, "Captains Only")
        ]);
        _cboFilter.SelectedIndex = 0;
        _cboFilter.SelectedIndexChanged += (_, _) => { UpdateFilterValueOptions(); RefreshPlayerLists(); };

        _cboFilterValue = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = AppTheme.FontDefault, Width = 170, Margin = new Padding(0, 4, 0, 0), Visible = false };
        _cboFilterValue.SelectedIndexChanged += (_, _) => RefreshPlayerLists();

        filterPanel.Controls.AddRange([lblFilter, _cboFilter, _cboFilterValue]);
        rightLayout.Controls.Add(filterPanel, 0, 1);
        rightLayout.SetColumnSpan(filterPanel, 2);

        var lblMembers = new Label { Text = "On this list (click to remove)", Dock = DockStyle.Fill, ForeColor = AppTheme.TextSecondary, Font = AppTheme.FontSmall, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0) };
        var lblAvailable = new Label { Text = "Not on this list (click to add)", Dock = DockStyle.Fill, ForeColor = AppTheme.TextSecondary, Font = AppTheme.FontSmall, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0) };
        rightLayout.Controls.Add(lblMembers, 0, 2);
        rightLayout.Controls.Add(lblAvailable, 1, 2);

        _lstMembers = new ListBox
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.FontDefault,
            BorderStyle = BorderStyle.None,
            IntegralHeight = false
        };
        _lstMembers.MouseClick += (_, e) => OnRowClick(_lstMembers, e, add: false);
        rightLayout.Controls.Add(_lstMembers, 0, 3);

        _lstAvailable = new ListBox
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.FontDefault,
            BorderStyle = BorderStyle.None,
            IntegralHeight = false
        };
        _lstAvailable.MouseClick += (_, e) => OnRowClick(_lstAvailable, e, add: true);
        rightLayout.Controls.Add(_lstAvailable, 1, 3);

        split.Panel2.Controls.Add(rightLayout);

        Controls.Add(split);
    }

    private static void SafeApplySplitDistance(SplitContainer split, int desiredFirstMin, int desiredSecondMin, int preferred)
    {
        int extent = split.Orientation == Orientation.Horizontal ? split.Height : split.Width;
        if (extent <= 1) return;

        int maxTotalMin = Math.Max(0, extent - 1);
        int firstMin = desiredFirstMin, secondMin = desiredSecondMin;
        if (firstMin + secondMin > maxTotalMin)
        {
            if (maxTotalMin == 0)
            {
                firstMin = 0;
                secondMin = 0;
            }
            else
            {
                double ratio = desiredFirstMin / (double)(desiredFirstMin + desiredSecondMin);
                firstMin = (int)Math.Floor(maxTotalMin * ratio);
                secondMin = maxTotalMin - firstMin;
            }
        }

        split.Panel1MinSize = firstMin;
        split.Panel2MinSize = secondMin;

        int maxFirst = extent - secondMin;
        if (maxFirst < firstMin) maxFirst = firstMin;

        int clamped = Math.Min(preferred, maxFirst);
        clamped = Math.Max(firstMin, clamped);

        if (clamped > 0)
            split.SplitterDistance = clamped;
    }

    private static Button MakeButton(string text, Color back) => new()
    {
        Text = text,
        AutoSize = true,
        Padding = new Padding(10, 4, 10, 4),
        FlatStyle = FlatStyle.Flat,
        BackColor = back,
        ForeColor = Color.White,
        Font = AppTheme.FontButton,
        Cursor = Cursors.Hand,
        Margin = new Padding(0, 0, 6, 0),
        FlatAppearance = { BorderSize = 0 }
    };

    private void LoadAll()
    {
        try
        {
            using var db = new BocceDbContext();
            _leagueId = AppParameterService.GetDefaultLeagueId(db);

            var lists = _leagueId.HasValue
                ? db.EmailLists.Where(l => l.LeagueId == _leagueId.Value).OrderBy(l => l.Name).ToList()
                : [];

            _allPlayers = db.Players
                .Where(p => p.IsActive)
                .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
                .Select(p => new PlayerItem(p.Id, $"{p.LastName}, {p.FirstName}",
                    $"{p.LastName} {p.FirstName} {p.Email} {p.Phone} {p.LotNumber}"))
                .ToList();

            LoadListsByPlayer(db);
            LoadFilterData(db);
            UpdateFilterValueOptions();

            _lstLists.Items.Clear();
            foreach (var l in lists)
                _lstLists.Items.Add(new ListItem(l.Id, l.Name, l.IsActive));

            if (_lstLists.Items.Count > 0)
                _lstLists.SelectedIndex = 0;
            else
                RefreshPlayerLists();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading email lists:\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadListsByPlayer(BocceDbContext db)
    {
        _listsByPlayer = _leagueId.HasValue
            ? db.EmailListMembers
                .Where(m => m.IsActive && m.EmailList.LeagueId == _leagueId.Value)
                .Select(m => new { m.PlayerId, ListName = m.EmailList.Name })
                .AsEnumerable()
                .GroupBy(x => x.PlayerId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ListName).OrderBy(n => n).ToList())
            : [];
    }

    private void LoadFilterData(BocceDbContext db)
    {
        _teamsByPlayer = [];
        _sparePlayerIds = [];
        _lookingForTeamPlayerIds = [];
        _captainPlayerIds = [];
        _divisionLeaderPlayerIds = [];
        _playoffQualifiedPlayerIds = [];
        _daySlotIdsByPlayer = [];
        _timeSlotIdsByPlayer = [];
        _teamOptions = [];
        _dayOptions = [];
        _timeOptions = [];

        if (!_leagueId.HasValue) return;

        _sparePlayerIds = db.SpareLists
            .Where(s => s.LeagueId == _leagueId.Value && s.IsActive)
            .Select(s => s.PlayerId)
            .ToHashSet();

        var seasonId = AppParameterService.GetDefaultSeasonId(db);
        if (!seasonId.HasValue) return;

        _teamsByPlayer = db.TeamPlayers
            .Where(tp => tp.IsActive && tp.Team.Division.SeasonId == seasonId.Value)
            .Select(tp => new { tp.PlayerId, tp.TeamId, TeamName = tp.Team.SystemName })
            .AsEnumerable()
            .GroupBy(x => x.PlayerId)
            .ToDictionary(g => g.Key, g => g.Select(x => (x.TeamId, x.TeamName)).OrderBy(t => t.TeamName).ToList());

        _captainPlayerIds = db.TeamPlayers
            .Where(tp => tp.IsActive && tp.Role == "captain" && tp.Team.Division.SeasonId == seasonId.Value)
            .Select(tp => tp.PlayerId)
            .ToHashSet();

        _teamOptions = db.Teams
            .Where(t => t.IsActive && t.Division.SeasonId == seasonId.Value)
            .Select(t => new { t.Id, t.DisplayName, t.SystemName })
            .AsEnumerable()
            .Select(t => new ValueOption(t.Id,
                t.DisplayName != null ? $"{t.DisplayName} ({t.SystemName})" : t.SystemName))
            .OrderBy(t => t.Name)
            .ToList();

        _lookingForTeamPlayerIds = db.LookingForTeams
            .Where(l => l.LeagueId == _leagueId.Value && l.SeasonId == seasonId.Value && l.TeamId == null)
            .Select(l => l.PlayerId)
            .ToHashSet();

        var divisionSlots = db.Divisions
            .Where(d => d.SeasonId == seasonId.Value)
            .Select(d => new { d.Id, d.DaySlotId, d.TimeSlotId })
            .ToList();

        var daySlotIds = divisionSlots.Where(d => d.DaySlotId.HasValue).Select(d => d.DaySlotId!.Value).Distinct().ToList();
        var timeSlotIds = divisionSlots.Where(d => d.TimeSlotId.HasValue).Select(d => d.TimeSlotId!.Value).Distinct().ToList();

        _dayOptions = db.DaySlots
            .Where(d => daySlotIds.Contains(d.Id))
            .OrderBy(d => d.DayNbr)
            .Select(d => new ValueOption(d.Id, d.DayName))
            .ToList();

        _timeOptions = db.TimeSlots
            .Where(t => timeSlotIds.Contains(t.Id))
            .OrderBy(t => t.SortOrder)
            .Select(t => new ValueOption(t.Id, t.Timeslot12h))
            .ToList();

        var divisionDayMap = divisionSlots.Where(d => d.DaySlotId.HasValue).ToDictionary(d => d.Id, d => d.DaySlotId!.Value);
        var divisionTimeMap = divisionSlots.Where(d => d.TimeSlotId.HasValue).ToDictionary(d => d.Id, d => d.TimeSlotId!.Value);

        var playerDivisions = db.TeamPlayers
            .Where(tp => tp.IsActive && tp.Team.Division.SeasonId == seasonId.Value)
            .Select(tp => new { tp.PlayerId, DivisionId = tp.Team.DivisionId })
            .ToList();

        foreach (var pd in playerDivisions)
        {
            if (divisionDayMap.TryGetValue(pd.DivisionId, out var dayId))
            {
                if (!_daySlotIdsByPlayer.TryGetValue(pd.PlayerId, out var daySet))
                    _daySlotIdsByPlayer[pd.PlayerId] = daySet = [];
                daySet.Add(dayId);
            }
            if (divisionTimeMap.TryGetValue(pd.DivisionId, out var timeId))
            {
                if (!_timeSlotIdsByPlayer.TryGetValue(pd.PlayerId, out var timeSet))
                    _timeSlotIdsByPlayer[pd.PlayerId] = timeSet = [];
                timeSet.Add(timeId);
            }
        }

        var season = db.Seasons.Find(seasonId.Value);
        var standings = db.Standings.Where(s => s.SeasonId == seasonId.Value).ToList();

        var leaderTeamIds = standings.Where(s => s.DivisionRank == 1).Select(s => s.TeamId).ToHashSet();
        _divisionLeaderPlayerIds = db.TeamPlayers
            .Where(tp => tp.IsActive && leaderTeamIds.Contains(tp.TeamId))
            .Select(tp => tp.PlayerId)
            .ToHashSet();

        if (season != null && season.TeamsInPlayoffs > 0)
        {
            var qualifiedTeamIds = standings.Where(s => s.SeasonSeed <= season.TeamsInPlayoffs).Select(s => s.TeamId).ToHashSet();
            _playoffQualifiedPlayerIds = db.TeamPlayers
                .Where(tp => tp.IsActive && qualifiedTeamIds.Contains(tp.TeamId))
                .Select(tp => tp.PlayerId)
                .ToHashSet();
        }
    }

    private void UpdateFilterValueOptions()
    {
        var kind = SelectedFilterKind();
        _cboFilterValue.Items.Clear();

        List<ValueOption>? options = kind switch
        {
            PlayerFilterKind.SpecificTeam => _teamOptions,
            PlayerFilterKind.PlaysOnDay => _dayOptions,
            PlayerFilterKind.PlaysAtTime => _timeOptions,
            _ => null
        };

        if (options != null)
        {
            foreach (var o in options)
                _cboFilterValue.Items.Add(o);
        }

        _cboFilterValue.Visible = options != null;
        if (options is { Count: > 0 })
            _cboFilterValue.SelectedIndex = 0;
    }

    private PlayerFilterKind SelectedFilterKind() =>
        _cboFilter.SelectedItem is FilterOption fo ? fo.Kind : PlayerFilterKind.None;

    private HashSet<int>? ComputeFilterIds()
    {
        return SelectedFilterKind() switch
        {
            PlayerFilterKind.None => null,
            PlayerFilterKind.OnTeam => _teamsByPlayer.Keys.ToHashSet(),
            PlayerFilterKind.SpecificTeam => _cboFilterValue.SelectedItem is ValueOption vt
                ? _teamsByPlayer.Where(kv => kv.Value.Any(t => t.TeamId == vt.Id)).Select(kv => kv.Key).ToHashSet()
                : [],
            PlayerFilterKind.OnSpareList => _sparePlayerIds,
            PlayerFilterKind.LookingForTeam => _lookingForTeamPlayerIds,
            PlayerFilterKind.PlaysOnDay => _cboFilterValue.SelectedItem is ValueOption vd
                ? _daySlotIdsByPlayer.Where(kv => kv.Value.Contains(vd.Id)).Select(kv => kv.Key).ToHashSet()
                : [],
            PlayerFilterKind.PlaysAtTime => _cboFilterValue.SelectedItem is ValueOption vtm
                ? _timeSlotIdsByPlayer.Where(kv => kv.Value.Contains(vtm.Id)).Select(kv => kv.Key).ToHashSet()
                : [],
            PlayerFilterKind.DivisionLeaders => _divisionLeaderPlayerIds,
            PlayerFilterKind.PlayoffQualified => _playoffQualifiedPlayerIds,
            PlayerFilterKind.CaptainsOnly => _captainPlayerIds,
            _ => null
        };
    }

    private void OnListSelected()
    {
        if (_lstLists.SelectedItem is not ListItem item)
        {
            _memberIds = [];
            RefreshPlayerLists();
            return;
        }

        try
        {
            using var db = new BocceDbContext();
            _memberIds = db.EmailListMembers
                .Where(m => m.EmailListId == item.Id && m.IsActive)
                .Select(m => m.PlayerId)
                .ToHashSet();
        }
        catch (Exception ex)
        {
            AppLogger.Error(ex, "Failed to load email list members");
            _memberIds = [];
        }

        RefreshPlayerLists();
    }

    private void RefreshPlayerLists()
    {
        var filter = _txtSearch.SearchText.Trim();
        IEnumerable<PlayerItem> visible = filter.Length == 0
            ? _allPlayers
            : _allPlayers.Where(p => SearchQueryService.MatchesAnyTerm(p.SearchBlob, filter));

        var filterIds = ComputeFilterIds();
        if (filterIds != null)
            visible = visible.Where(p => filterIds.Contains(p.Id));

        var visibleList = visible.ToList();

        _lstMembers.Items.Clear();
        _lstAvailable.Items.Clear();

        foreach (var p in visibleList)
        {
            bool isMember = _memberIds.Contains(p.Id);
            var row = new PlayerRow(p.Id, FormatPlayerRow(p, includeTeamAndSpare: !isMember));
            if (isMember)
                _lstMembers.Items.Add(row);
            else
                _lstAvailable.Items.Add(row);
        }

        bool hasSelection = _lstLists.SelectedItem is ListItem;
        _lstMembers.Enabled = hasSelection;
        _lstAvailable.Enabled = hasSelection;
    }

    private string FormatPlayerRow(PlayerItem player, bool includeTeamAndSpare)
    {
        var segments = new List<string>();

        if (_listsByPlayer.TryGetValue(player.Id, out var lists) && lists.Count > 0)
            segments.Add(string.Join(", ", lists));

        if (includeTeamAndSpare)
        {
            if (_teamsByPlayer.TryGetValue(player.Id, out var teams) && teams.Count > 0)
                segments.Add(string.Join(", ", teams.Select(t => t.Name)));

            if (_sparePlayerIds.Contains(player.Id))
                segments.Add("Spare List");
        }

        return segments.Count > 0
            ? $"{player.Name}  —  {string.Join("  |  ", segments)}"
            : player.Name;
    }

    private void OnRowClick(ListBox listBox, MouseEventArgs e, bool add)
    {
        if (_lstLists.SelectedItem is not ListItem list) return;

        int index = listBox.IndexFromPoint(e.Location);
        if (index < 0 || index >= listBox.Items.Count) return;
        if (listBox.Items[index] is not PlayerRow row) return;

        ToggleMember(list.Id, row.Id, add);
    }

    private void ToggleMember(int listId, int playerId, bool add)
    {
        try
        {
            using var db = new BocceDbContext();
            var existing = db.EmailListMembers.FirstOrDefault(m => m.EmailListId == listId && m.PlayerId == playerId);
            if (add)
            {
                if (existing == null)
                    db.EmailListMembers.Add(new EmailListMember { EmailListId = listId, PlayerId = playerId, IsActive = true });
                else
                    existing.IsActive = true;

                _memberIds.Add(playerId);
            }
            else
            {
                if (existing != null) existing.IsActive = false;
                _memberIds.Remove(playerId);
            }
            db.SaveChanges();

            LoadListsByPlayer(db);
        }
        catch (Exception ex)
        {
            AppLogger.Error(ex, "Failed to update email list member");
        }

        RefreshPlayerLists();
    }

    private void OnAdd(object? sender, EventArgs e)
    {
        var name = PromptForText("New Email List", "List Name:", "");
        if (name == null) return;

        try
        {
            using var db = new BocceDbContext();
            var leagueId = AppParameterService.GetDefaultLeagueId(db);
            if (!leagueId.HasValue)
            {
                MessageBox.Show("Select a default league before creating email lists.", "Email Lists", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            db.EmailLists.Add(new EmailList { LeagueId = leagueId.Value, Name = name, IsActive = true });
            db.SaveChanges();
            LoadAll();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error creating email list:\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnRename(object? sender, EventArgs e)
    {
        if (_lstLists.SelectedItem is not ListItem item) return;
        var name = PromptForText("Rename Email List", "List Name:", item.Name);
        if (name == null) return;

        try
        {
            using var db = new BocceDbContext();
            var list = db.EmailLists.Find(item.Id);
            if (list != null)
            {
                list.Name = name;
                db.SaveChanges();
                LoadAll();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error renaming email list:\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnToggleActive(object? sender, EventArgs e)
    {
        if (_lstLists.SelectedItem is not ListItem item) return;

        try
        {
            using var db = new BocceDbContext();
            var list = db.EmailLists.Find(item.Id);
            if (list != null)
            {
                list.IsActive = !list.IsActive;
                db.SaveChanges();
                LoadAll();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error updating email list:\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnDelete(object? sender, EventArgs e)
    {
        if (_lstLists.SelectedItem is not ListItem item) return;

        var confirm = MessageBox.Show($"Delete the email list \"{item.Name}\"? This cannot be undone.",
            "Delete Email List", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes) return;

        try
        {
            using var db = new BocceDbContext();
            var list = db.EmailLists.Find(item.Id);
            if (list != null)
            {
                db.EmailLists.Remove(list);
                db.SaveChanges();
                LoadAll();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error deleting email list:\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private string? PromptForText(string title, string label, string initial)
    {
        using var dialog = new Form
        {
            Text = title,
            Width = 360,
            Height = 160,
            StartPosition = FormStartPosition.CenterParent,
            BackColor = AppTheme.ContentBackground,
            Font = AppTheme.FontDefault,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var lbl = new Label { Text = label, Location = new Point(10, 15), Size = new Size(200, 20), ForeColor = AppTheme.TextPrimary };
        var txt = new TextBox
        {
            Location = new Point(10, 38),
            Size = new Size(320, 28),
            Text = initial,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle
        };
        var btnOk = new Button { Text = "OK", Location = new Point(174, 78), Size = new Size(75, 28), DialogResult = DialogResult.OK };
        var btnCancel = new Button { Text = "Cancel", Location = new Point(255, 78), Size = new Size(75, 28), DialogResult = DialogResult.Cancel };

        dialog.Controls.AddRange([lbl, txt, btnOk, btnCancel]);
        dialog.AcceptButton = btnOk;
        dialog.CancelButton = btnCancel;

        if (dialog.ShowDialog(this) != DialogResult.OK) return null;

        var value = txt.Text.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            MessageBox.Show("Name cannot be blank.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }
        return value;
    }
}

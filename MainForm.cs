using BocceManager.Panels;
using BocceManager.Services;
using BocceManager.UI.Theme;

namespace BocceManager;

public partial class MainForm : Form
{

    private enum NavSection
    {
        Dashboard,
        Leagues, Seasons, Divisions,
        Players, Teams, TeamApplicants, LookingForTeam,
        ScoreEntry, Schedule,
        Standings, Playoffs, PlayoffSetup, PlayoffSchedule,
        ReportTeamListing, ReportScheduleGeneric, ReportSpareLists,
        SpareLists, Announcements, Fees, EmailLists, EmailCompose, EmailHistory, Documents, Parameters, Courts, Utilities, Theme
    }

    private NavSection _currentSection = NavSection.Dashboard;
    private readonly Dictionary<NavSection, Label> _navItems = [];
    private readonly List<(Label Header, List<Label> Items)> _navGroups = [];
    private int _openGroupIndex = -1;
    private UserControl? _currentPanel;

    private List<(int Id, string Name)> _topBarLeagues = [];
    private List<(int Id, string Name)> _topBarSeasons = [];

    public MainForm()
    {
        InitializeComponent();
        BuildNavigation();
        if (_navGroups.Count > 0) ToggleGroup(0);  // open CLUB group on startup
        SetupTopBarInteraction();
        AppParameterService.DefaultsChanged += OnDefaultsChanged;
        FormClosed += OnMainFormClosed;
        UpdateNavigationAvailability();
        Navigate(GetStartupSection());
        UpdateStatusBar();
        UpdateContextBar();
    }

    private void OnMainFormClosed(object? sender, FormClosedEventArgs e)
    {
        AppParameterService.DefaultsChanged -= OnDefaultsChanged;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        AppTheme.ApplyDisabledStylesToAll(this);
    }

    private void SetupTopBarInteraction()
    {
        lblCtxLeague.Click += (_, _) => ShowLeagueMenu();
        lblCtxSeason.Click += (_, _) => ShowSeasonMenu();
    }

    private void LoadTopBarItems()
    {
        try
        {
            using var db = new Data.BocceDbContext();
            var leagueId = AppParameterService.GetDefaultLeagueId(db);

            _topBarLeagues = db.Leagues
                .OrderBy(l => l.Name)
                .Select(l => new { l.Id, l.Name })
                .AsEnumerable()
                .Select(l => (l.Id, l.Name))
                .ToList();

            _topBarSeasons = leagueId.HasValue
                ? db.Seasons
                    .Where(s => s.LeagueId == leagueId.Value)
                    .OrderByDescending(s => s.Name)
                    .Select(s => new { s.Id, s.Name })
                    .AsEnumerable()
                    .Select(s => (s.Id, s.Name))
                    .ToList()
                : [];
        }
        catch
        {
            _topBarLeagues = [];
            _topBarSeasons = [];
        }
    }

    private void ShowLeagueMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Font = new Font("Segoe UI", 10f);

        if (_topBarLeagues.Count == 0)
        {
            menu.Items.Add("(no leagues)").Enabled = false;
        }
        else
        {
            int? currentId;
            try
            {
                using var db = new Data.BocceDbContext();
                currentId = AppParameterService.GetDefaultLeagueId(db);
            }
            catch { currentId = null; }

            foreach (var (id, name) in _topBarLeagues)
            {
                var item = menu.Items.Add(name);
                item.Tag = id;
                if (id == currentId)
                    item.Font = new Font(menu.Font, FontStyle.Bold);
                item.Click += (_, _) => SelectLeague((int)item.Tag!);
            }
        }

        menu.Show(lblCtxLeague, new Point(0, lblCtxLeague.Height));
    }

    private void ShowSeasonMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Font = new Font("Segoe UI", 10f);

        int? currentId;
        try
        {
            using var db = new Data.BocceDbContext();
            currentId = AppParameterService.GetDefaultSeasonId(db);
        }
        catch { currentId = null; }

        if (_topBarSeasons.Count == 0)
        {
            menu.Items.Add("(no seasons)").Enabled = false;
        }
        else
        {
            foreach (var (id, name) in _topBarSeasons)
            {
                var item = menu.Items.Add(name);
                item.Tag = id;
                if (id == currentId)
                    item.Font = new Font(menu.Font, FontStyle.Bold);
                item.Click += (_, _) => SelectSeason((int)item.Tag!);
            }
        }

        menu.Show(lblCtxSeason, new Point(0, lblCtxSeason.Height));
    }

    private void SelectLeague(int id)
    {
        try
        {
            using var db = new Data.BocceDbContext();
            AppParameterService.SetDefaultLeagueId(db, id);
            var seasonId = AppParameterService.GetDefaultSeasonId(db);
            if (seasonId.HasValue)
            {
                var season = db.Seasons.Find(seasonId.Value);
                if (season == null || season.LeagueId != id)
                    AppParameterService.SetDefaultSeasonId(db, null);
            }
        }
        catch { }
    }

    private void SelectSeason(int id)
    {
        try
        {
            using var db = new Data.BocceDbContext();
            AppParameterService.SetDefaultSeasonId(db, id);
        }
        catch { }
    }

    private void OnDefaultsChanged(object? sender, DefaultsChangedEventArgs e)
    {
        UpdateContextBar();
        UpdateNavigationAvailability();
    }

    private static NavSection GetStartupSection()
    {
        try
        {
            using var db = new Data.BocceDbContext();
            bool hasLeagues = db.Leagues.Any();
            if (!hasLeagues) return NavSection.Leagues;

            bool hasSeasons = db.Seasons.Any();
            if (!hasSeasons) return NavSection.Seasons;
        }
        catch
        {
            // Fall back to dashboard if startup data checks fail.
        }

        return NavSection.Dashboard;
    }

    private void BuildNavigation()
    {
        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = AppTheme.NavBackground,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };
        pnlNav.Controls.Add(flow);

        void AddStandaloneItem(string text, NavSection section)
        {
            var item = new Label
            {
                Text = "  " + text,
                Width = 220, Height = 40,
                ForeColor = AppTheme.NavText,
                BackColor = AppTheme.NavBackground,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0),
                Tag = section
            };
            item.MouseEnter += (s, _) => { if ((NavSection)((Label)s!).Tag! != _currentSection) ((Label)s!).BackColor = AppTheme.NavHover; };
            item.MouseLeave += (s, _) => { if ((NavSection)((Label)s!).Tag! != _currentSection) ((Label)s!).BackColor = AppTheme.NavBackground; };
            item.Click += (s, _) => Navigate((NavSection)((Label)s!).Tag!);
            flow.Controls.Add(item);
            _navItems[section] = item;
        }

        void AddGroup(string groupName, Action<Action<string, NavSection>> populate)
        {
            int groupIndex = _navGroups.Count;
            List<Label> groupItems = [];

            var header = new Label
            {
                Text = "\u25B6  " + groupName,
                Tag = groupName,
                Width = 220, Height = 28,
                ForeColor = AppTheme.NavText,
                BackColor = AppTheme.NavBackground,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Font = AppTheme.FontNavHeader,
                Cursor = Cursors.Hand,
                Margin = new Padding(0)
            };
            header.MouseEnter += (s, _) => { };
            header.MouseLeave += (s, _) => { };
            header.Click += (s, _) => ToggleGroup(groupIndex);
            flow.Controls.Add(header);

            populate((text, section) =>
            {
                var item = new Label
                {
                    Text = "        ● " + text,
                    Width = 220, Height = 32,
                    ForeColor = AppTheme.NavText,
                    BackColor = AppTheme.NavBackground,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Margin = new Padding(0),
                    Tag = section,
                    Visible = false
                };
                item.MouseEnter += (s, _) => { if ((NavSection)((Label)s!).Tag! != _currentSection) ((Label)s!).BackColor = AppTheme.NavHover; };
                item.MouseLeave += (s, _) => { if ((NavSection)((Label)s!).Tag! != _currentSection) ((Label)s!).BackColor = AppTheme.NavBackground; };
                item.Click += (s, _) => Navigate((NavSection)((Label)s!).Tag!);
                flow.Controls.Add(item);
                _navItems[section] = item;
                groupItems.Add(item);
            });

            _navGroups.Add((header, groupItems));
        }

        AddStandaloneItem("Dashboard", NavSection.Dashboard);

        AddGroup("CLUB", add => {
            add("Leagues",   NavSection.Leagues);
            add("Seasons",   NavSection.Seasons);
            add("Divisions", NavSection.Divisions);
            add("Spare Lists", NavSection.SpareLists);
        });

        AddGroup("ROSTER", add => {
            add("Players",          NavSection.Players);
            add("Teams",            NavSection.Teams);
            add("Team Applicants",  NavSection.TeamApplicants);
            add("Looking For Team", NavSection.LookingForTeam);
        });

        AddGroup("LEAGUE", add => {
            add("Score Entry", NavSection.ScoreEntry);
            add("Schedule",    NavSection.Schedule);
            add("Standings",   NavSection.Standings);
        });

        AddGroup("PLAYOFFS", add => {
            add("Setup",       NavSection.PlayoffSetup);
            add("Schedule",    NavSection.PlayoffSchedule);
        });

        AddGroup("REPORTS", add => {
            add("Team Listing", NavSection.ReportTeamListing);
            add("Schedules",    NavSection.ReportScheduleGeneric);
            add("Spare List",   NavSection.ReportSpareLists);
        });

        AddGroup("EMAIL", add => {
            add("Email Lists",    NavSection.EmailLists);
            add("Compose",        NavSection.EmailCompose);
            add("Sent History",   NavSection.EmailHistory);
        });

        AddGroup("ADMINISTRATION", add => {
            add("Announcements",  NavSection.Announcements);
            add("Fees",           NavSection.Fees);
            add("Documents",      NavSection.Documents);
            add("Parameters",     NavSection.Parameters);
            add("Courts",         NavSection.Courts);
        });

        AddGroup("UTILITIES", add => {
            add("Backup",         NavSection.Utilities);
            add("Theme",          NavSection.Theme);
        });
    }

    private void ToggleGroup(int index)
    {
        bool wasOpen = _openGroupIndex == index;

        foreach (var (header, items) in _navGroups)
        {
            foreach (var item in items)
                item.Visible = false;
            header.Text = "\u25B6  " + (string)header.Tag!;
        }
        _openGroupIndex = -1;

        if (!wasOpen)
        {
            var (header, items) = _navGroups[index];
            foreach (var item in items)
                item.Visible = true;
            header.Text = "\u25BC  " + (string)header.Tag!;
            _openGroupIndex = index;
        }
    }

    private void ExpandGroupContaining(NavSection section)
    {
        for (int i = 0; i < _navGroups.Count; i++)
        {
            if (_navGroups[i].Items.Any(item => (NavSection)item.Tag! == section))
            {
                if (_openGroupIndex != i)
                    ToggleGroup(i);
                return;
            }
        }
    }

    private void Navigate(NavSection section)
    {
        UpdateNavigationAvailability();
        ExpandGroupContaining(section);

        if (_navItems.TryGetValue(_currentSection, out var prev))
            prev.BackColor = AppTheme.NavBackground;

        _currentSection = section;

        if (_navItems.TryGetValue(section, out var cur))
            cur.BackColor = AppTheme.NavSelected;

        _currentPanel?.Dispose();
        _currentPanel = CreatePanel(section);
        _currentPanel.Dock = DockStyle.Fill;

        pnlContent.Controls.Clear();
        pnlContent.Controls.Add(_currentPanel);
        AppTheme.ApplyControlStyles(_currentPanel);

        lblCtxPageTitle.Text = SectionTitle(section);
        pnlNav.Refresh();
    }

    private static UserControl CreatePanel(NavSection section) => section switch
    {
        NavSection.Dashboard  => new DashboardPanel(),
        NavSection.Leagues    => new LeaguePanel(),
        NavSection.Seasons    => new SeasonPanel(),
        NavSection.Divisions  => new DivisionPanel(),
        NavSection.Players    => new PlayerPanel(),
        NavSection.Teams           => new DivisionPanel(teamsOnly: true),
        NavSection.TeamApplicants  => new TeamApplicantsPanel(),
        NavSection.LookingForTeam  => new LookingForTeamPanel(),
        NavSection.Documents  => new DocumentsPanel(),
        NavSection.Parameters => new ParametersPanel(),
        NavSection.Courts     => new CourtPanel(),
        NavSection.ScoreEntry            => new ScoreEntryPanel(),
        NavSection.Schedule              => new SchedulePanel(),
        NavSection.Standings             => new StandingsPanel(),
        NavSection.PlayoffSetup          => new PlayoffSetupPanel(),
        NavSection.PlayoffSchedule       => new PlayoffSchedulePanel(),
        NavSection.ReportTeamListing     => new ReportTeamListingPanel(),
        NavSection.ReportScheduleGeneric => new ReportSchedulePanel(),
        NavSection.ReportSpareLists      => new ReportSpareListPanel(),
        NavSection.SpareLists => new SpareListPanel(),
        NavSection.Fees       => new FeesPanel(),
        NavSection.Utilities  => new UtilitiesPanel(),
        NavSection.Theme      => new ThemePanel(),
        _ => new PlaceholderPanel(SectionTitle(section))
    };

    // Disable navigation sidebar during long operations (e.g. restore) without
    // graying out the content panel where progress is displayed.
    public void LockNavigation()   => pnlNav.Enabled = false;
    public void UnlockNavigation() => pnlNav.Enabled = true;

    // Called by panels that need to jump to the Seasons section with a pre-selected season.
    // seasonId will be used to pre-select once SeasonPanel is built.
    public void NavigateToDivision(int? divisionId = null)
    {
        Navigate(NavSection.Divisions);
        if (divisionId.HasValue && _currentPanel is DivisionPanel dp)
            dp.SelectDivision(divisionId.Value);
    }

    public void NavigateToTeam(int teamId)
    {
        Navigate(NavSection.Teams);
        if (_currentPanel is DivisionPanel dp)
            dp.SelectTeam(teamId);
    }

    public void NavigateToSeasons(int? seasonId = null)
    {
        Navigate(NavSection.Seasons);
        if (seasonId.HasValue && _currentPanel is SeasonPanel sp)
            sp.SelectSeason(seasonId.Value);
    }

    private static string SectionTitle(NavSection section) => section switch
    {
        NavSection.Dashboard     => "Dashboard",
        NavSection.Leagues       => "Leagues",
        NavSection.Seasons       => "Seasons",
        NavSection.Divisions     => "Divisions",
        NavSection.Players       => "Players",
        NavSection.Teams           => "Teams",
        NavSection.TeamApplicants  => "Team Applicants",
        NavSection.LookingForTeam  => "Looking For Team",
        NavSection.ScoreEntry    => "Score Entry",
        NavSection.Schedule              => "Schedule",
        NavSection.Standings             => "Standings",
        NavSection.ReportTeamListing     => "Team Listing",
        NavSection.ReportScheduleGeneric => "Schedules",
        NavSection.ReportSpareLists      => "Spare List",
        NavSection.Playoffs           => "Playoffs",
        NavSection.PlayoffSetup       => "Playoff Setup",
        NavSection.PlayoffSchedule    => "Playoff Schedule",
        NavSection.SpareLists    => "Spare Lists",
        NavSection.Announcements => "Announcements",
        NavSection.Fees          => "Fees",
        NavSection.EmailLists    => "Email Lists",
        NavSection.EmailCompose  => "Compose Email",
        NavSection.EmailHistory  => "Sent History",
        NavSection.Documents     => "Documents",
        NavSection.Parameters    => "Parameters",
        NavSection.Courts        => "Courts",
        NavSection.Theme         => "Theme",
        _ => section.ToString()
    };

    private void UpdateStatusBar()
    {
        lblDbPath.Text = "DB: PostgreSQL (localhost:5432)";
    }

    private void UpdateContextBar()
    {
        LoadTopBarItems();
        try
        {
            using var db = new Data.BocceDbContext();

            var leagueId = AppParameterService.GetDefaultLeagueId(db);
            var seasonId = AppParameterService.GetDefaultSeasonId(db);

            string leagueName = leagueId.HasValue
                ? db.Leagues.Where(l => l.Id == leagueId.Value).Select(l => l.Name).FirstOrDefault() ?? "(missing)"
                : "(not set)";

            string seasonName = seasonId.HasValue
                ? db.Seasons.Where(s => s.Id == seasonId.Value).Select(s => s.Name).FirstOrDefault() ?? "(missing)"
                : "(not set)";

            lblCtxLeague.Text = $"League: {leagueName}";
            lblCtxSeason.Text = $"Season: {seasonName}";
        }
        catch
        {
            lblCtxLeague.Text = "League: (unavailable)";
            lblCtxSeason.Text = "Season: (unavailable)";
        }
    }

    private void UpdateNavigationAvailability()
    {
        try
        {
            using var db = new Data.BocceDbContext();
            bool hasLeagues = db.Leagues.Any();
            bool hasSeasons = db.Seasons.Any();
            int? defaultSeasonId = AppParameterService.GetDefaultSeasonId(db);
            bool hasDefaultSeason = defaultSeasonId.HasValue;

            // If no leagues: only Dashboard and Leagues enabled
            // If no seasons: only Leagues and Seasons enabled
            // If no default season selected: disable Divisions and dependent sections
            // Otherwise: all enabled

            var sectionsToEnable = new HashSet<NavSection>
            {
                NavSection.Dashboard,
                NavSection.Leagues,
                NavSection.Parameters,
                NavSection.Documents,
                NavSection.Courts,
                NavSection.Utilities,
                NavSection.Theme
            };
            if (hasLeagues)
                sectionsToEnable.Add(NavSection.Seasons);
            if (hasLeagues && hasSeasons && hasDefaultSeason)
            {
                sectionsToEnable.Add(NavSection.Divisions);
                sectionsToEnable.Add(NavSection.Players);
                sectionsToEnable.Add(NavSection.Teams);
                sectionsToEnable.Add(NavSection.TeamApplicants);
                sectionsToEnable.Add(NavSection.LookingForTeam);
                sectionsToEnable.Add(NavSection.ScoreEntry);
                sectionsToEnable.Add(NavSection.Schedule);
                sectionsToEnable.Add(NavSection.Standings);
                sectionsToEnable.Add(NavSection.Playoffs);
                sectionsToEnable.Add(NavSection.PlayoffSetup);
                sectionsToEnable.Add(NavSection.PlayoffSchedule);
                sectionsToEnable.Add(NavSection.ReportTeamListing);
                sectionsToEnable.Add(NavSection.ReportScheduleGeneric);
                sectionsToEnable.Add(NavSection.ReportSpareLists);
                sectionsToEnable.Add(NavSection.SpareLists);
                sectionsToEnable.Add(NavSection.Announcements);
                sectionsToEnable.Add(NavSection.Fees);
                sectionsToEnable.Add(NavSection.EmailLists);
                sectionsToEnable.Add(NavSection.EmailCompose);
                sectionsToEnable.Add(NavSection.EmailHistory);
            }

            foreach (var section in _navItems.Keys)
            {
                var item    = _navItems[section];
                bool enable = sectionsToEnable.Contains(section);
                item.Enabled   = enable;
                // Always restore nav colors — ApplyDisabledStylesToAll overwrites them with
                // content-area colors (white bg / dark text) which persist after re-enabling.
                item.ForeColor = AppTheme.NavText;
                item.BackColor = section == _currentSection
                    ? AppTheme.NavSelected
                    : AppTheme.NavBackground;
            }
        }
        catch { }
    }
}


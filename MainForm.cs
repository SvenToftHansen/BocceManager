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
        Players, Teams,
        ScoreEntry, Schedule,
        Standings, Playoffs,
        SpareLists, Announcements, Fees, EmailLists, Documents, Parameters, Utilities, Theme
    }

    private NavSection _currentSection = NavSection.Dashboard;
    private readonly Dictionary<NavSection, Label> _navItems = [];
    private readonly List<(Label Header, List<Label> Items)> _navGroups = [];
    private int _openGroupIndex = -1;
    private UserControl? _currentPanel;

    public MainForm()
    {
        InitializeComponent();
        BuildNavigation();
        AppParameterService.DefaultsChanged += OnDefaultsChanged;
        FormClosed += OnMainFormClosed;
        Navigate(GetStartupSection());
        UpdateStatusBar();
        UpdateContextBar();
    }

    private void OnMainFormClosed(object? sender, FormClosedEventArgs e)
    {
        AppParameterService.DefaultsChanged -= OnDefaultsChanged;
    }

    private void OnDefaultsChanged(object? sender, DefaultsChangedEventArgs e)
    {
        UpdateContextBar();
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

        flow.Controls.Add(new Label
        {
            Text = "Golden Vista\r\nBocce League Master",
            Width = 220, Height = 72,
            ForeColor = AppTheme.NavText,
            BackColor = AppTheme.NavTitleBackground,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = AppTheme.FontNavTitle,
            Margin = new Padding(0)
        });

        void AddStandaloneItem(string text, NavSection section)
        {
            var item = new Label
            {
                Text = "  " + text,
                Width = 220, Height = 38,
                ForeColor = AppTheme.NavText,
                BackColor = AppTheme.NavBackground,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = AppTheme.FontNavItem,
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
                ForeColor = AppTheme.NavHeader,
                BackColor = AppTheme.NavBackground,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Font = AppTheme.FontNavHeader,
                Cursor = Cursors.Hand,
                Margin = new Padding(0)
            };
            header.MouseEnter += (s, _) => ((Label)s!).ForeColor = AppTheme.NavText;
            header.MouseLeave += (s, _) => ((Label)s!).ForeColor = AppTheme.NavHeader;
            header.Click += (s, _) => ToggleGroup(groupIndex);
            flow.Controls.Add(header);

            populate((text, section) =>
            {
                var item = new Label
                {
                    Text = "  " + text,
                    Width = 220, Height = 38,
                    ForeColor = AppTheme.NavText,
                    BackColor = AppTheme.NavBackground,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = AppTheme.FontNavItem,
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

        AddGroup("SETUP", add => {
            add("Leagues",   NavSection.Leagues);
            add("Seasons",   NavSection.Seasons);
            add("Divisions", NavSection.Divisions);
        });

        AddGroup("TEAMS & PLAYERS", add => {
            add("Players", NavSection.Players);
            add("Teams",   NavSection.Teams);
        });

        AddGroup("OPERATIONS", add => {
            add("Score Entry", NavSection.ScoreEntry);
            add("Schedule",    NavSection.Schedule);
        });

        AddGroup("REPORTS", add => {
            add("Standings", NavSection.Standings);
            add("Playoffs",  NavSection.Playoffs);
        });

        AddGroup("ADMINISTRATION", add => {
            add("Spare Lists",    NavSection.SpareLists);
            add("Announcements",  NavSection.Announcements);
            add("Fees",           NavSection.Fees);
            add("Email Lists",    NavSection.EmailLists);
            add("Documents",      NavSection.Documents);
            add("Parameters",     NavSection.Parameters);
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
        pnlContent.Controls.Add(pnlContextBar);
        pnlContent.Controls.Add(pnlHeader);

        lblSection.Text = SectionTitle(section);
    }

    private static UserControl CreatePanel(NavSection section) => section switch
    {
        NavSection.Dashboard  => new DashboardPanel(),
        NavSection.Leagues    => new LeaguePanel(),
        NavSection.Seasons    => new SeasonPanel(),
        NavSection.Divisions  => new DivisionPanel(),
        NavSection.Players    => new PlayerPanel(),
        NavSection.Documents  => new DocumentsPanel(),
        NavSection.Parameters => new ParametersPanel(),
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
        NavSection.Teams         => "Teams",
        NavSection.ScoreEntry    => "Score Entry",
        NavSection.Schedule      => "Schedule",
        NavSection.Standings     => "Standings",
        NavSection.Playoffs      => "Playoffs",
        NavSection.SpareLists    => "Spare Lists",
        NavSection.Announcements => "Announcements",
        NavSection.Fees          => "Fees",
        NavSection.EmailLists    => "Email Lists",
        NavSection.Documents     => "Documents",
        NavSection.Parameters    => "Parameters",
        NavSection.Theme         => "Theme",
        _ => section.ToString()
    };

    private void UpdateStatusBar()
    {
        lblDbPath.Text = "DB: PostgreSQL (localhost:5432)";
    }

    private void UpdateContextBar()
    {
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
}


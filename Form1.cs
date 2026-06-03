using BocceManager.Panels;
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
        SpareLists, Announcements, Fees, EmailLists, Parameters
    }

    private NavSection _currentSection = NavSection.Dashboard;
    private readonly Dictionary<NavSection, Label> _navItems = [];
    private UserControl? _currentPanel;

    public MainForm()
    {
        InitializeComponent();
        BuildNavigation();
        Navigate(NavSection.Dashboard);
        UpdateStatusBar();
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
            Text = "BocceManager",
            Width = 220, Height = 64,
            ForeColor = AppTheme.NavText,
            BackColor = AppTheme.NavTitleBackground,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = AppTheme.FontNavTitle,
            Margin = new Padding(0)
        });

        void AddHeader(string text)
        {
            flow.Controls.Add(new Label
            {
                Text = text,
                Width = 220, Height = 28,
                ForeColor = AppTheme.NavHeader,
                BackColor = AppTheme.NavBackground,
                TextAlign = ContentAlignment.BottomLeft,
                Padding = new Padding(14, 0, 0, 4),
                Font = AppTheme.FontNavHeader,
                Margin = new Padding(0)
            });
        }

        void AddItem(string text, NavSection section)
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

        AddItem("Dashboard", NavSection.Dashboard);

        AddHeader("SETUP");
        AddItem("Leagues", NavSection.Leagues);
        AddItem("Seasons", NavSection.Seasons);
        AddItem("Divisions", NavSection.Divisions);

        AddHeader("TEAMS & PLAYERS");
        AddItem("Players", NavSection.Players);
        AddItem("Teams", NavSection.Teams);

        AddHeader("OPERATIONS");
        AddItem("Score Entry", NavSection.ScoreEntry);
        AddItem("Schedule", NavSection.Schedule);

        AddHeader("REPORTS");
        AddItem("Standings", NavSection.Standings);
        AddItem("Playoffs", NavSection.Playoffs);

        AddHeader("ADMINISTRATION");
        AddItem("Spare Lists", NavSection.SpareLists);
        AddItem("Announcements", NavSection.Announcements);
        AddItem("Fees", NavSection.Fees);
        AddItem("Email Lists", NavSection.EmailLists);
        AddItem("Parameters", NavSection.Parameters);
    }

    private void Navigate(NavSection section)
    {
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

        lblSection.Text = SectionTitle(section);
    }

    private static UserControl CreatePanel(NavSection section) => section switch
    {
        NavSection.Dashboard  => new DashboardPanel(),
        NavSection.Parameters => new ParametersPanel(),
        _ => new PlaceholderPanel(SectionTitle(section))
    };

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
        NavSection.Parameters    => "Parameters",
        _ => section.ToString()
    };

    private void UpdateStatusBar()
    {
        lblDbPath.Text = $"DB: {Data.BocceDbContext.DbPath}";
    }
}

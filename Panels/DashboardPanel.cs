using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.Services;
using BocceManager.UI.Theme;

namespace BocceManager.Panels;

public class DashboardPanel : UserControl
{
    private Panel? _statsPanel;

    public DashboardPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;
        BuildUI();
        AppParameterService.DefaultsChanged += OnDefaultsChanged;
    }

    private void OnDefaultsChanged(object? sender, DefaultsChangedEventArgs e)
    {
        RefreshStatsPanel();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            AppParameterService.DefaultsChanged -= OnDefaultsChanged;
        base.Dispose(disposing);
    }

    private void BuildUI()
    {
        var scroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = AppTheme.ContentBackground,
            Padding = new Padding(40, 30, 40, 40)
        };

        var flow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Location = new Point(0, 0),
            BackColor = AppTheme.ContentBackground
        };

        int flowWidth = 700;

        Label FlowLabel(string text, Font font, Color color, int bottomPad = 6) => new()
        {
            Text = text,
            Font = font,
            ForeColor = color,
            AutoSize = false,
            Size = new Size(flowWidth, font.Height + 12),
            Padding = new Padding(0, 0, 0, bottomPad),
            BackColor = AppTheme.ContentBackground
        };

        flow.Controls.Add(FlowLabel("Dashboard",
            AppTheme.FontPageTitle, AppTheme.TextPrimary, bottomPad: 16));

        flow.Controls.Add(new Panel
        {
            Size = new Size(flowWidth, 1),
            BackColor = AppTheme.Separator,
            Margin = new Padding(0, 0, 0, 16)
        });

        _statsPanel = BuildStatsPanel(flowWidth);
        _statsPanel.Margin = new Padding(0, 0, 0, 28);
        flow.Controls.Add(_statsPanel);

        scroll.Controls.Add(flow);
        Controls.Add(scroll);
    }

    private Panel BuildStatsPanel(int width)
    {
        int leagues = 0, players = 0, teams = 0, sparePlayers = 0, lookingForTeam = 0, pendingMatches = 0;
        try
        {
            using var db = new BocceDbContext();
            var leagueId = AppParameterService.GetDefaultLeagueId(db);
            var seasonId = AppParameterService.GetDefaultSeasonId(db);

            leagues = db.Leagues.Count(l => l.IsActive);
            players = db.Players.Count(p => p.IsActive);

            IQueryable<Team> teamsQuery = db.Teams.Where(t => t.IsActive);
            if (leagueId.HasValue)
                teamsQuery = teamsQuery.Where(t => t.Division.Season.LeagueId == leagueId.Value);
            if (seasonId.HasValue)
                teamsQuery = teamsQuery.Where(t => t.Division.SeasonId == seasonId.Value);
            teams = teamsQuery.Count();

            IQueryable<SpareList> spareQuery = db.SpareLists.Where(s => s.IsActive);
            if (leagueId.HasValue)
                spareQuery = spareQuery.Where(s => s.LeagueId == leagueId.Value);
            sparePlayers = spareQuery.Count();

            IQueryable<LookingForTeam> lftQuery = db.LookingForTeams.Where(l => l.TeamId == null);
            if (leagueId.HasValue)
                lftQuery = lftQuery.Where(l => l.LeagueId == leagueId.Value);
            lookingForTeam = lftQuery.Count();

            IQueryable<BocceMatch> matchQuery = db.Matches.Where(m => m.Status == "scheduled");
            if (leagueId.HasValue)
                matchQuery = matchQuery.Where(m => m.ScheduleWeek.Division.Season.LeagueId == leagueId.Value);
            if (seasonId.HasValue)
                matchQuery = matchQuery.Where(m => m.ScheduleWeek.Division.SeasonId == seasonId.Value);
            pendingMatches = matchQuery.Count();
        }
        catch { }

        (string label, int value, Color accent)[] stats =
        [
            ("Leagues",          leagues,        Color.FromArgb(46,  204, 113)),
            ("Players",          players,        Color.FromArgb(155, 89,  182)),
            ("Teams",            teams,          Color.FromArgb(230, 126, 34)),
            ("Spare Players",    sparePlayers,   Color.FromArgb(52,  152, 219)),
            ("Looking for Team", lookingForTeam, Color.FromArgb(231, 76,  60)),
            ("Pending Matches",  pendingMatches, Color.FromArgb(127, 140, 141)),
        ];

        var panel = new Panel
        {
            Size = new Size(width, 70),
            BackColor = AppTheme.ContentBackground
        };

        int x = 0;
        foreach (var (label, value, accent) in stats)
        {
            var card = new Panel
            {
                Size = new Size(108, 66),
                Location = new Point(x, 0),
                BackColor = AppTheme.Surface
            };
            card.Controls.Add(new Label
            {
                Text = value.ToString(),
                Font = AppTheme.FontStatValue,
                ForeColor = accent,
                AutoSize = false,
                Size = new Size(108, 36),
                Location = new Point(0, 8),
                TextAlign = ContentAlignment.MiddleCenter
            });
            card.Controls.Add(new Label
            {
                Text = label,
                Font = AppTheme.FontStatLabel,
                ForeColor = AppTheme.TextMuted,
                AutoSize = false,
                Size = new Size(108, 20),
                Location = new Point(0, 44),
                TextAlign = ContentAlignment.MiddleCenter
            });
            panel.Controls.Add(card);
            x += 116;
        }

        return panel;
    }

    private void RefreshStatsPanel()
    {
        if (_statsPanel == null)
            return;

        var parent = _statsPanel.Parent;
        if (parent == null)
            return;

        var margin = _statsPanel.Margin;
        int index = parent.Controls.IndexOf(_statsPanel);
        int width = _statsPanel.Width;

        parent.Controls.Remove(_statsPanel);
        _statsPanel.Dispose();

        _statsPanel = BuildStatsPanel(width);
        _statsPanel.Margin = margin;
        parent.Controls.Add(_statsPanel);
        parent.Controls.SetChildIndex(_statsPanel, index);
    }
}

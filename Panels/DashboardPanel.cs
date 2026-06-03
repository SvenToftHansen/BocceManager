using BocceManager.Data;
using BocceManager.Services;
using BocceManager.UI.Theme;

namespace BocceManager.Panels;

public class DashboardPanel : UserControl
{
    private BocceDbContext _db = new();
    private ComboBox? _leagueCombo;
    private ComboBox? _seasonCombo;

    public DashboardPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;
        BuildUI();
        AppParameterService.DefaultsChanged += OnDefaultsChanged;
    }

    private void OnDefaultsChanged(object? sender, DefaultsChangedEventArgs e)
    {
        LoadDefaultsUI();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            AppParameterService.DefaultsChanged -= OnDefaultsChanged;
        base.Dispose(disposing);
    }

    private void BuildUI()
    {
        // Outer scroll container so content is never clipped on small windows
        var scroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = AppTheme.ContentBackground,
            Padding = new Padding(40, 30, 40, 40)
        };

        // Vertical flow — items stack top-to-bottom, each taking its natural height
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

        // Title
        flow.Controls.Add(FlowLabel("BocceManager",
            AppTheme.FontPageTitle, AppTheme.TextPrimary, bottomPad: 4));

        // Subtitle
        flow.Controls.Add(FlowLabel("Bocce Ball League Administration",
            AppTheme.FontPageSubtitle, AppTheme.TextSecondary, bottomPad: 16));

        // Separator
        flow.Controls.Add(new Panel
        {
            Size = new Size(flowWidth, 1),
            BackColor = AppTheme.Separator,
            Margin = new Padding(0, 0, 0, 16)
        });

        // DB path row
        var dbRow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            BackColor = AppTheme.ContentBackground,
            Margin = new Padding(0, 0, 0, 24)
        };
        dbRow.Controls.Add(new Label
        {
            Text = "Database:",
            Font = AppTheme.FontSmallBold,
            ForeColor = AppTheme.TextPrimary,
            AutoSize = true
        });
        dbRow.Controls.Add(new Label
        {
            Text = "  " + BocceDbContext.DbPath,
            Font = AppTheme.FontSmall,
            ForeColor = AppTheme.TextMuted,
            AutoSize = true
        });
        flow.Controls.Add(dbRow);

        // Stats cards
        var statsPanel = BuildStatsPanel(flowWidth);
        statsPanel.Margin = new Padding(0, 0, 0, 28);
        flow.Controls.Add(statsPanel);

        // Separator
        flow.Controls.Add(new Panel
        {
            Size = new Size(flowWidth, 1),
            BackColor = AppTheme.Separator,
            Margin = new Padding(0, 0, 0, 16)
        });

        // Default League and Season section
        flow.Controls.Add(FlowLabel("Default League and Season",
            AppTheme.FontSectionHeading, AppTheme.TextPrimary, bottomPad: 12));

        var defaultsPanel = BuildDefaultsPanel(flowWidth);
        defaultsPanel.Margin = new Padding(0, 0, 0, 24);
        flow.Controls.Add(defaultsPanel);

        scroll.Controls.Add(flow);
        Controls.Add(scroll);
    }

    private static Panel BuildStatsPanel(int width)
    {
        int leagues = 0, players = 0, teams = 0, pendingMatches = 0;
        try
        {
            using var db = new BocceDbContext();
            leagues        = db.Leagues.Count(l => l.IsActive);
            players        = db.Players.Count(p => p.IsActive);
            teams          = db.Teams.Count(t => t.IsActive);
            pendingMatches = db.Matches.Count(m => m.Status == "scheduled");
        }
        catch { }

        (string label, int value, Color accent)[] stats =
        [
            ("Leagues",         leagues,        Color.FromArgb(46,  204, 113)),
            ("Players",         players,        Color.FromArgb(155, 89,  182)),
            ("Teams",           teams,          Color.FromArgb(230, 126, 34)),
            ("Pending Matches", pendingMatches, Color.FromArgb(231, 76,  60)),
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

    private Panel BuildDefaultsPanel(int width)
    {
        var panel = new Panel
        {
            Size = new Size(width, 100),
            BackColor = AppTheme.ContentBackground
        };

        // League label and combo
        panel.Controls.Add(new Label
        {
            Text = "League:",
            Font = AppTheme.FontSmall,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(0, 0),
            AutoSize = true
        });

        _leagueCombo = new ComboBox
        {
            Location = new Point(0, 20),
            Width = 250,
            Height = 24,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.FontDefault
        };
        _leagueCombo.SelectedIndexChanged += LeagueCombo_SelectedIndexChanged;
        panel.Controls.Add(_leagueCombo);

        // Season label and combo
        panel.Controls.Add(new Label
        {
            Text = "Season:",
            Font = AppTheme.FontSmall,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(280, 0),
            AutoSize = true
        });

        _seasonCombo = new ComboBox
        {
            Location = new Point(280, 20),
            Width = 250,
            Height = 24,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.FontDefault,
            Enabled = false
        };
        _seasonCombo.SelectedIndexChanged += SeasonCombo_SelectedIndexChanged;
        panel.Controls.Add(_seasonCombo);

        LoadDefaultsUI();

        return panel;
    }

    private void LoadDefaultsUI()
    {
        var allSeasons = _db.Seasons.Where(s => s.IsActive).ToList();
        var leaguesWithSeasons = _db.Leagues
            .Where(l => l.IsActive && _db.Seasons.Any(s => s.LeagueId == l.Id && s.IsActive))
            .OrderBy(l => l.Name)
            .ToList();

        _leagueCombo!.DataSource = leaguesWithSeasons;
        _leagueCombo!.DisplayMember = "Name";
        _leagueCombo!.ValueMember = "Id";

        // If exactly one season exists, auto-select it and its league
        if (allSeasons.Count == 1)
        {
            var season = allSeasons.First();
            _leagueCombo!.SelectedValue = season.LeagueId;
            _seasonCombo!.SelectedValue = season.Id;
            AppParameterService.SetDefaultLeagueId(_db, season.LeagueId);
            AppParameterService.SetDefaultSeasonId(_db, season.Id);
        }
        else
        {
            // Otherwise load saved defaults
            var defaultLeagueId = AppParameterService.GetDefaultLeagueId(_db);
            if (defaultLeagueId.HasValue && leaguesWithSeasons.Any(l => l.Id == defaultLeagueId))
            {
                _leagueCombo!.SelectedValue = defaultLeagueId;
            }
            else if (leaguesWithSeasons.Count > 0)
            {
                _leagueCombo!.SelectedIndex = 0;
            }
        }
    }

    private void LeagueCombo_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_leagueCombo?.SelectedValue is int leagueId)
        {
            var seasons = _db.Seasons
                .Where(s => s.LeagueId == leagueId && s.IsActive)
                .OrderByDescending(s => s.Name)
                .ToList();

            _seasonCombo!.DataSource = seasons;
            _seasonCombo!.DisplayMember = "Name";
            _seasonCombo!.ValueMember = "Id";
            _seasonCombo!.Enabled = seasons.Any();

            var defaultSeasonId = AppParameterService.GetDefaultSeasonId(_db);
            if (defaultSeasonId.HasValue && seasons.Any(s => s.Id == defaultSeasonId))
            {
                _seasonCombo!.SelectedValue = defaultSeasonId;
            }
            else if (seasons.Count > 0)
            {
                _seasonCombo!.SelectedIndex = 0;
            }
        }
    }

    private void SeasonCombo_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_leagueCombo?.SelectedValue is int leagueId && _seasonCombo?.SelectedValue is int seasonId)
        {
            AppParameterService.SetDefaultLeagueId(_db, leagueId);
            AppParameterService.SetDefaultSeasonId(_db, seasonId);
        }
    }
}

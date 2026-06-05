using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.Services;
using BocceManager.UI.Theme;

namespace BocceManager.Panels;

public class DashboardPanel : UserControl
{
    private ComboBox? _leagueCombo;
    private ComboBox? _seasonCombo;
    private Label? _leagueDisplay;
    private Label? _seasonDisplay;
    private Panel? _statsPanel;
    private bool _isLoadingDefaults;

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
        // Outer scroll container so content is never clipped on small windows
        var scroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = AppTheme.ContentBackground,
            Padding = new Padding(40, 30, 40, 40)
        };

        // Vertical flow - items stack top-to-bottom, each taking its natural height
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
        flow.Controls.Add(FlowLabel("Dashboard",
            AppTheme.FontPageTitle, AppTheme.TextPrimary, bottomPad: 16));

        // Separator
        flow.Controls.Add(new Panel
        {
            Size = new Size(flowWidth, 1),
            BackColor = AppTheme.Separator,
            Margin = new Padding(0, 0, 0, 16)
        });

        // Stats cards
        _statsPanel = BuildStatsPanel(flowWidth);
        _statsPanel.Margin = new Padding(0, 0, 0, 28);
        flow.Controls.Add(_statsPanel);

        // Separator
        flow.Controls.Add(new Panel
        {
            Size = new Size(flowWidth, 1),
            BackColor = AppTheme.Separator,
            Margin = new Padding(0, 0, 0, 16)
        });

        // Current League and Season section
        flow.Controls.Add(FlowLabel("Current League and Season",
            AppTheme.FontSectionHeading, AppTheme.TextPrimary, bottomPad: 12));

        var defaultsPanel = BuildDefaultsPanel(flowWidth);
        defaultsPanel.Margin = new Padding(0, 0, 0, 24);
        flow.Controls.Add(defaultsPanel);

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

            leagues        = db.Leagues.Count(l => l.IsActive);
            players        = db.Players.Count(p => p.IsActive);

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

    private Panel BuildDefaultsPanel(int width)
    {
        var panel = new Panel
        {
            Size = new Size(width, 58),
            BackColor = AppTheme.ContentBackground
        };

        // League selector
        var leagueHeader = new Label
        {
            Text = "League:",
            Font = AppTheme.FontSmallBold,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(0, 0),
            AutoSize = true,
            Cursor = Cursors.Hand
        };
        leagueHeader.Click += (_, _) => OpenSelectorMenu(_leagueCombo, _leagueDisplay ?? leagueHeader, 260);
        panel.Controls.Add(leagueHeader);

        _leagueDisplay = new Label
        {
            Location = new Point(0, 16),
            Size = new Size(250, 22),
            Font = AppTheme.FontSmall,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.ContentBackground,
            TextAlign = ContentAlignment.MiddleLeft,
            Cursor = Cursors.Hand
        };
        _leagueDisplay.Click += (_, _) => OpenSelectorMenu(_leagueCombo, _leagueDisplay, 260);
        panel.Controls.Add(_leagueDisplay);

        _leagueCombo = new ComboBox
        {
            Location = new Point(0, 20),
            Width = 260,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = AppTheme.FontSmall,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.ContentBackground,
            Visible = false
        };
        _leagueCombo.SelectedIndexChanged += OnDefaultLeagueSelected;
        panel.Controls.Add(_leagueCombo);

        // Season selector
        var seasonHeader = new Label
        {
            Text = "Season:",
            Font = AppTheme.FontSmallBold,
            ForeColor = AppTheme.TextPrimary,
            Location = new Point(270, 0),
            AutoSize = true,
            Cursor = Cursors.Hand
        };
        seasonHeader.Click += (_, _) => OpenSelectorMenu(_seasonCombo, _seasonDisplay ?? seasonHeader, 320);
        panel.Controls.Add(seasonHeader);

        _seasonDisplay = new Label
        {
            Location = new Point(270, 16),
            Size = new Size(300, 22),
            Font = AppTheme.FontSmall,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.ContentBackground,
            TextAlign = ContentAlignment.MiddleLeft,
            Cursor = Cursors.Hand
        };
        _seasonDisplay.Click += (_, _) => OpenSelectorMenu(_seasonCombo, _seasonDisplay, 320);
        panel.Controls.Add(_seasonDisplay);

        _seasonCombo = new ComboBox
        {
            Location = new Point(280, 20),
            Width = 320,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = AppTheme.FontSmall,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.ContentBackground,
            Visible = false
        };
        _seasonCombo.SelectedIndexChanged += OnDefaultSeasonSelected;
        panel.Controls.Add(_seasonCombo);

        LoadDefaultsUI();

        return panel;
    }

    private void OpenSelectorMenu(ComboBox? combo, Control anchor, int width)
    {
        if (combo == null || combo.Items.Count == 0) return;

        var menu = new ContextMenuStrip
        {
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            ShowImageMargin = false
        };

        for (int i = 0; i < combo.Items.Count; i++)
        {
            int index = i;
            var item = new ToolStripMenuItem(combo.Items[i]?.ToString() ?? string.Empty)
            {
                Checked = index == combo.SelectedIndex
            };
            item.Click += (_, _) => combo.SelectedIndex = index;
            menu.Items.Add(item);
        }

        menu.Width = width;
        menu.Show(anchor, new Point(0, anchor.Height));
    }

    private void RefreshSelectorDisplay()
    {
        if (_leagueDisplay != null && _leagueCombo != null)
            _leagueDisplay.Text = _leagueCombo.SelectedItem?.ToString() ?? "(none)";

        if (_seasonDisplay != null && _seasonCombo != null)
            _seasonDisplay.Text = _seasonCombo.SelectedItem?.ToString() ?? "(none)";
    }

    private void OnDefaultLeagueSelected(object? sender, EventArgs e)
    {
        if (_isLoadingDefaults || _leagueCombo == null) return;

        using var db = new BocceDbContext();
        int? leagueId = _leagueCombo.SelectedItem is IntItem li ? li.Id : null;
        AppParameterService.SetDefaultLeagueId(db, leagueId);

        if (leagueId.HasValue)
        {
            // Clear season default when not valid for selected league.
            var seasonId = AppParameterService.GetDefaultSeasonId(db);
            if (!seasonId.HasValue || !db.Seasons.Any(s => s.Id == seasonId.Value && s.LeagueId == leagueId.Value))
                AppParameterService.SetDefaultSeasonId(db, null);
        }
        else
        {
            AppParameterService.SetDefaultSeasonId(db, null);
        }

        LoadDefaultsUI();
    }

    private void OnDefaultSeasonSelected(object? sender, EventArgs e)
    {
        if (_isLoadingDefaults || _seasonCombo == null) return;
        using var db = new BocceDbContext();
        int? seasonId = _seasonCombo.SelectedItem is IntItem si ? si.Id : null;
        AppParameterService.SetDefaultSeasonId(db, seasonId);
        RefreshSelectorDisplay();
    }

    private void LoadDefaultsUI()
    {
        if (_leagueCombo == null || _seasonCombo == null) return;

        _isLoadingDefaults = true;
        using var db = new BocceDbContext();
        var leagueId = AppParameterService.GetDefaultLeagueId(db);
        var seasonId = AppParameterService.GetDefaultSeasonId(db);

        _leagueCombo.Items.Clear();
        foreach (var l in db.Leagues.OrderBy(l => l.Name).ToList())
            _leagueCombo.Items.Add(new IntItem(l.Id, l.Name + (l.IsActive ? "" : " (inactive)")));

        if (_leagueCombo.Items.Count == 0)
        {
            _seasonCombo.Items.Clear();
            _isLoadingDefaults = false;
            return;
        }

        int leagueIndex = -1;
        if (leagueId.HasValue)
            leagueIndex = _leagueCombo.Items.Cast<IntItem>().ToList().FindIndex(i => i.Id == leagueId.Value);
        _leagueCombo.SelectedIndex = leagueIndex >= 0 ? leagueIndex : 0;

        var selectedLeagueId = (_leagueCombo.SelectedItem as IntItem)?.Id;
        _seasonCombo.Items.Clear();
        if (selectedLeagueId.HasValue)
        {
            foreach (var s in db.Seasons.Where(s => s.LeagueId == selectedLeagueId.Value)
                .OrderByDescending(s => s.IsCurrent)
                .ThenByDescending(s => s.StartDate)
                .ToList())
            {
                _seasonCombo.Items.Add(new IntItem(s.Id, s.Name + (s.IsCurrent ? "  ★" : "") + (s.IsActive ? "" : " (inactive)")));
            }
        }

        if (_seasonCombo.Items.Count > 0)
        {
            int seasonIndex = -1;
            if (seasonId.HasValue)
                seasonIndex = _seasonCombo.Items.Cast<IntItem>().ToList().FindIndex(i => i.Id == seasonId.Value);
            _seasonCombo.SelectedIndex = seasonIndex >= 0 ? seasonIndex : 0;
        }

        RefreshSelectorDisplay();
        _isLoadingDefaults = false;
        RefreshStatsPanel();
    }

    private sealed record IntItem(int Id, string Name)
    {
        public override string ToString() => Name;
    }
}


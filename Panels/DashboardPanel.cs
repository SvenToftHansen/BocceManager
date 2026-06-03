using BocceManager.Data;
using BocceManager.UI.Theme;

namespace BocceManager.Panels;

public class DashboardPanel : UserControl
{
    public DashboardPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;
        BuildUI();
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

        // Getting started heading
        flow.Controls.Add(FlowLabel("Getting Started",
            AppTheme.FontSectionHeading, AppTheme.TextPrimary, bottomPad: 8));

        // Steps
        flow.Controls.Add(FlowLabel(
            "1. Add a Park  →  2. Create a League  →  3. Set up a Season  →  4. Add Divisions & Teams  →  5. Enter Scores",
            AppTheme.FontDefault, AppTheme.TextSecondary, bottomPad: 0));

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
}

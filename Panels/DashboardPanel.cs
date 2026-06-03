using BocceManager.Data;
using BocceManager.UI.Theme;

namespace BocceManager.Panels;

public class DashboardPanel : UserControl
{
    public DashboardPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;
        Padding = new Padding(40);
        BuildUI();
    }

    private void BuildUI()
    {
        var lblTitle = new Label
        {
            Text = "BocceManager",
            Font = AppTheme.FontPageTitle,
            ForeColor = AppTheme.TextPrimary,
            AutoSize = true,
            Location = new Point(40, 40)
        };

        var lblSubtitle = new Label
        {
            Text = "Bocce Ball League Administration",
            Font = AppTheme.FontPageSubtitle,
            ForeColor = AppTheme.TextSecondary,
            AutoSize = true,
            Location = new Point(42, 85)
        };

        var separator = new Panel
        {
            BackColor = AppTheme.Separator,
            Location = new Point(40, 120),
            Size = new Size(500, 1)
        };

        var lblDbLabel = new Label
        {
            Text = "Database:",
            Font = AppTheme.FontSmall,
            ForeColor = AppTheme.TextPrimary,
            AutoSize = true,
            Location = new Point(40, 135)
        };

        var lblDbPath = new Label
        {
            Text = BocceDbContext.DbPath,
            Font = AppTheme.FontSmall,
            ForeColor = AppTheme.TextMuted,
            AutoSize = true,
            Location = new Point(100, 135)
        };

        var statsPanel = BuildStatsPanel();
        statsPanel.Location = new Point(40, 175);

        var lblGettingStarted = new Label
        {
            Text = "Getting Started",
            Font = AppTheme.FontSectionHeading,
            ForeColor = AppTheme.TextPrimary,
            AutoSize = true,
            Location = new Point(40, statsPanel.Location.Y + statsPanel.Height + 30)
        };

        var lblSteps = new Label
        {
            Text = "1. Add a Park  →  2. Create a League  →  3. Set up a Season  →  4. Add Divisions & Teams  →  5. Enter Scores",
            Font = AppTheme.FontDefault,
            ForeColor = AppTheme.TextSecondary,
            AutoSize = true,
            Location = new Point(40, lblGettingStarted.Location.Y + 28)
        };

        Controls.AddRange([lblTitle, lblSubtitle, separator, lblDbLabel, lblDbPath, statsPanel, lblGettingStarted, lblSteps]);
    }

    private Panel BuildStatsPanel()
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
        catch { /* db may be empty on first run */ }

        var panel = new Panel { Size = new Size(480, 70), BackColor = AppTheme.ContentBackground };

        (string label, int value, Color accent)[] stats =
        [
            ("Leagues",         leagues,        Color.FromArgb(46, 204, 113)),
            ("Players",         players,        Color.FromArgb(155, 89, 182)),
            ("Teams",           teams,          Color.FromArgb(230, 126, 34)),
            ("Pending Matches", pendingMatches, Color.FromArgb(231, 76, 60)),
        ];

        int x = 0;
        foreach (var (label, value, accent) in stats)
        {
            var card = new Panel
            {
                Size = new Size(108, 66),
                Location = new Point(x, 0),
                BackColor = AppTheme.Surface,
                Cursor = Cursors.Default
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

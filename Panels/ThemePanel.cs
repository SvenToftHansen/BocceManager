using BocceManager.UI.Theme;

namespace BocceManager.Panels;

public class ThemePanel : UserControl
{
    public ThemePanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;
        BuildUI();
    }

    private void BuildUI()
    {
        var scroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = AppTheme.ContentBackground,
            Padding = new Padding(32, 24, 32, 32)
        };

        var heading = new Label
        {
            Text = "Choose a Theme",
            Font = AppTheme.FontSectionHeading,
            ForeColor = AppTheme.TextPrimary,
            AutoSize = true,
            Location = new Point(32, 24)
        };

        var subtitle = new Label
        {
            Text = "Click a theme to apply it. The app will restart to load the new colors.",
            Font = AppTheme.FontDefault,
            ForeColor = AppTheme.TextSecondary,
            AutoSize = true,
            Location = new Point(32, heading.Bottom + 6)
        };

        var cards = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoSize = true,
            BackColor = AppTheme.ContentBackground,
            Location = new Point(32, subtitle.Bottom + 20),
            Padding = new Padding(0)
        };

        foreach (ThemePreset preset in Enum.GetValues<ThemePreset>())
            cards.Controls.Add(BuildCard(preset));

        scroll.Controls.AddRange([heading, subtitle, cards]);
        Controls.Add(scroll);
    }

    private static Panel BuildCard(ThemePreset preset)
    {
        bool isCurrent = preset == AppTheme.Current;
        var colors = PreviewColors(preset);

        var card = new Panel
        {
            Size = new Size(190, 160),
            Margin = new Padding(0, 0, 16, 16),
            Cursor = Cursors.Hand,
            BackColor = colors.Content
        };

        // Colored border — thicker + accent color when selected
        card.Paint += (s, e) =>
        {
            var g = e.Graphics;
            var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
            using var pen = new Pen(isCurrent ? colors.Accent : Color.FromArgb(200, 200, 200), isCurrent ? 3 : 1);
            g.DrawRectangle(pen, rect);
        };

        // Nav color band at top
        var navBand = new Panel
        {
            Size = new Size(190, 40),
            Location = new Point(0, 0),
            BackColor = colors.Nav
        };

        // Theme name on the band
        var lblName = new Label
        {
            Text = AppTheme.DisplayName(preset),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = colors.NavText,
            AutoSize = false,
            Size = new Size(190, 40),
            Location = new Point(0, 0),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };
        navBand.Controls.Add(lblName);

        // Swatch row
        var swatchRow = new Panel
        {
            Size = new Size(158, 22),
            Location = new Point(16, 52),
            BackColor = Color.Transparent
        };

        (Color color, string tip)[] swatches =
        [
            (colors.Accent,  "Accent"),
            (colors.Success, "Success"),
            (colors.Danger,  "Danger"),
            (colors.Text,    "Text"),
            (colors.Content, "Background"),
        ];

        int sx = 0;
        foreach (var (color, tip) in swatches)
        {
            var sw = new Panel
            {
                Size = new Size(26, 22),
                Location = new Point(sx, 0),
                BackColor = color
            };
            new ToolTip().SetToolTip(sw, tip);
            swatchRow.Controls.Add(sw);
            sx += 30;
        }

        // Selected checkmark
        var lblCheck = new Label
        {
            Text = isCurrent ? "✓  Current" : "",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = colors.Accent,
            AutoSize = false,
            Size = new Size(190, 22),
            Location = new Point(0, 86),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };

        // Click to apply
        void Apply(object? s, EventArgs e)
        {
            if (preset == AppTheme.Current) return;
            AppTheme.Save(preset);
            Application.Restart();
        }
        card.Click    += Apply;
        navBand.Click += Apply;
        lblName.Click += Apply;
        swatchRow.Click += Apply;
        foreach (Control sw in swatchRow.Controls) sw.Click += Apply;

        card.Controls.AddRange([navBand, swatchRow, lblCheck]);
        return card;
    }

    // ── Preview color palettes ────────────────────────────────────────────────

    private sealed record ThemePreview(
        Color Nav, Color NavText, Color Content, Color Accent, Color Text, Color Success, Color Danger);

    private static ThemePreview PreviewColors(ThemePreset preset) => preset switch
    {
        ThemePreset.Dark => new(
            C(25,25,28), C(210,210,215), C(32,32,35),
            C(0,120,215), C(230,230,235), C(39,174,96), C(192,57,43)),

        ThemePreset.Classic => new(
            C(100,100,100), C(255,255,255), C(240,240,240),
            C(0,84,166), C(0,0,0), C(0,128,0), C(180,0,0)),

        ThemePreset.BocceGreen => new(
            C(27,79,45), C(220,240,228), C(245,252,247),
            C(39,174,96), C(27,79,45), C(39,174,96), C(192,57,43)),

        ThemePreset.MidnightBlue => new(
            C(13,27,62), C(180,210,255), C(18,35,75),
            C(0,200,210), C(200,220,255), C(0,180,160), C(180,50,80)),

        ThemePreset.Slate => new(
            C(52,62,72), C(215,220,228), C(244,245,248),
            C(220,100,30), C(52,62,72), C(70,170,100), C(192,57,43)),

        ThemePreset.HighContrast => new(
            C(0,0,0), C(255,255,0), C(0,0,0),
            C(255,255,0), C(255,255,255), C(0,200,0), C(255,0,0)),

        _ => new( // Light (default)
            C(44,62,80), C(236,240,241), Color.White,
            C(41,128,185), C(44,62,80), C(46,204,113), C(231,76,60))
    };

    private static Color C(int r, int g, int b) => Color.FromArgb(r, g, b);
}

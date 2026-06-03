namespace BocceManager;

public class SplashForm : Form
{
    private readonly Label _lblTitle;
    private readonly Label _lblSubtitle;
    private readonly Panel _separator;
    private readonly Label _lblCredit;

    public SplashForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition   = FormStartPosition.CenterScreen;
        ClientSize      = new Size(540, 290);
        BackColor       = Color.FromArgb(22, 36, 50);
        ShowInTaskbar   = false;

        // Left accent bar
        Controls.Add(new Panel
        {
            Dock      = DockStyle.Left,
            Width     = 5,
            BackColor = Color.FromArgb(41, 128, 185)
        });

        _lblTitle = new Label
        {
            Text      = "GOLDEN VISTA",
            Font      = new Font("Segoe UI", 30f, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize  = true,
            BackColor = Color.Transparent
        };

        _lblSubtitle = new Label
        {
            Text      = "Bocce League Manager",
            Font      = new Font("Segoe UI", 16f),
            ForeColor = Color.FromArgb(41, 128, 185),
            AutoSize  = true,
            BackColor = Color.Transparent
        };

        _separator = new Panel
        {
            Size      = new Size(380, 1),
            BackColor = Color.FromArgb(52, 73, 94)
        };

        _lblCredit = new Label
        {
            Text      = "written by Sven Hansen  ©  2026",
            Font      = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(100, 120, 140),
            AutoSize  = true,
            BackColor = Color.Transparent
        };

        Controls.AddRange([_lblTitle, _lblSubtitle, _separator, _lblCredit]);

        Resize += (_, _) => PositionControls();
        Load   += (_, _) => PositionControls();
    }

    private void PositionControls()
    {
        int cx = (ClientSize.Width + 5) / 2; // offset for the accent bar

        int blockHeight = _lblTitle.PreferredHeight + 10 + _lblSubtitle.PreferredHeight;
        int titleY      = (ClientSize.Height / 2) - (blockHeight / 2) - 20;

        _lblTitle.Location    = new Point(cx - _lblTitle.PreferredWidth / 2,  titleY);
        _lblSubtitle.Location = new Point(cx - _lblSubtitle.PreferredWidth / 2, titleY + _lblTitle.PreferredHeight + 10);

        _separator.Location = new Point(cx - _separator.Width / 2, _lblSubtitle.Bottom + 24);

        _lblCredit.Location = new Point(cx - _lblCredit.PreferredWidth / 2, ClientSize.Height - _lblCredit.PreferredHeight - 18);
    }
}

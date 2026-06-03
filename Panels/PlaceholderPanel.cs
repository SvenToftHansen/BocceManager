namespace BocceManager.Panels;

public class PlaceholderPanel : UserControl
{
    public PlaceholderPanel(string sectionName)
    {
        BackColor = Color.White;
        Dock = DockStyle.Fill;

        var lbl = new Label
        {
            Text = sectionName,
            Font = new Font("Segoe UI", 22f, FontStyle.Bold),
            ForeColor = Color.FromArgb(200, 210, 220),
            AutoSize = true,
            Anchor = AnchorStyles.None
        };

        var sub = new Label
        {
            Text = "This section is under construction.",
            Font = new Font("Segoe UI", 11f),
            ForeColor = Color.FromArgb(200, 210, 220),
            AutoSize = true,
            Anchor = AnchorStyles.None
        };

        Controls.Add(lbl);
        Controls.Add(sub);

        Resize += (_, _) =>
        {
            lbl.Location = new Point(
                (Width - lbl.Width) / 2,
                (Height - lbl.Height - sub.Height - 8) / 2);
            sub.Location = new Point(
                (Width - sub.Width) / 2,
                lbl.Bottom + 8);
        };
    }
}

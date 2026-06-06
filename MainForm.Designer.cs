namespace BocceManager;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    private Panel pnlNav = null!;
    private Panel pnlContent = null!;
    private Panel pnlHeader = null!;
    private Panel pnlContextBar = null!;
    private Label lblSection = null!;
    private Label lblCtxLeague = null!;
    private Label lblCtxSeason = null!;
    private Label lblCtxDivider = null!;
    private StatusStrip statusStrip = null!;
    private ToolStripStatusLabel lblDbPath = null!;
    private ToolStripStatusLabel lblSpacer = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        pnlNav     = new Panel();
        pnlContent = new Panel();
        pnlHeader  = new Panel();
        pnlContextBar = new Panel();
        lblSection = new Label();
        lblCtxLeague = new Label();
        lblCtxSeason = new Label();
        lblCtxDivider = new Label();
        statusStrip = new StatusStrip();
        lblDbPath   = new ToolStripStatusLabel();
        lblSpacer   = new ToolStripStatusLabel();

        SuspendLayout();

        // Header strip (top of content area, shows current section name)
        lblSection.Text = "Dashboard";
        lblSection.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
        lblSection.ForeColor = Color.FromArgb(44, 62, 80);
        lblSection.AutoSize = true;
        lblSection.Location = new Point(16, 10);
        pnlHeader.Controls.Add(lblSection);
        pnlHeader.Dock = DockStyle.Top;
        pnlHeader.Height = 38;
        pnlHeader.BackColor = Color.FromArgb(245, 248, 250);
        pnlHeader.Padding = new Padding(4, 0, 0, 0);

        // Global context bar (League / Season)
        lblCtxLeague.Text = "League: (not set)";
        lblCtxLeague.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        lblCtxLeague.ForeColor = Color.FromArgb(44, 62, 80);
        lblCtxLeague.AutoSize = true;
        lblCtxLeague.Location = new Point(16, 9);

        lblCtxDivider.Text = "|";
        lblCtxDivider.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
        lblCtxDivider.ForeColor = Color.FromArgb(127, 140, 141);
        lblCtxDivider.AutoSize = true;
        lblCtxDivider.Location = new Point(320, 9);

        lblCtxSeason.Text = "Season: (not set)";
        lblCtxSeason.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        lblCtxSeason.ForeColor = Color.FromArgb(44, 62, 80);
        lblCtxSeason.AutoSize = true;
        lblCtxSeason.Location = new Point(340, 9);

        pnlContextBar.Controls.Add(lblCtxLeague);
        pnlContextBar.Controls.Add(lblCtxDivider);
        pnlContextBar.Controls.Add(lblCtxSeason);
        pnlContextBar.Dock = DockStyle.Top;
        pnlContextBar.Height = 30;
        pnlContextBar.BackColor = Color.FromArgb(236, 242, 247);
        pnlContextBar.Padding = new Padding(4, 0, 0, 0);

        // Status strip
        lblSpacer.Spring = true;
        lblDbPath.Text = "";
        lblDbPath.ForeColor = Color.Gray;
        statusStrip.Items.AddRange(new ToolStripItem[] { lblSpacer, lblDbPath });
        statusStrip.SizingGrip = false;
        statusStrip.BackColor = Color.FromArgb(236, 240, 241);

        // Nav panel
        pnlNav.Dock = DockStyle.Left;
        pnlNav.Width = 220;
        pnlNav.BackColor = Color.FromArgb(44, 62, 80);

        // Content panel
        pnlContent.Dock = DockStyle.Fill;
        pnlContent.BackColor = Color.White;
        pnlContent.Controls.Add(pnlContextBar);
        pnlContent.Controls.Add(pnlHeader);

        // Form
        Controls.Add(pnlContent);
        Controls.Add(pnlNav);
        Controls.Add(statusStrip);

        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1280, 800);
        MinimumSize = new Size(1000, 640);
        Text = "Golden Vista Bocce League Master";
        StartPosition = FormStartPosition.CenterScreen;

        ResumeLayout(false);
        PerformLayout();
    }
}


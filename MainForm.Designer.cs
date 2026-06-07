using BocceManager.UI.Theme;

namespace BocceManager;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    private Panel pnlNav = null!;
    private Panel pnlTopBar = null!;
    private Panel pnlContent = null!;
    private Label lblNavTitle = null!;
    private Label lblCtxLeague = null!;
    private Label lblCtxSeason = null!;
    private Label lblCtxDivider = null!;
    private Label lblCtxPageTitle = null!;
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
        pnlTopBar  = new Panel();
        pnlContent = new Panel();
        lblNavTitle = new Label();
        lblCtxLeague = new Label();
        lblCtxSeason = new Label();
        lblCtxDivider = new Label();
        lblCtxPageTitle = new Label();
        statusStrip = new StatusStrip();
        lblDbPath   = new ToolStripStatusLabel();
        lblSpacer   = new ToolStripStatusLabel();

        SuspendLayout();

        // Global top bar (Title + League/Season + Page Title)
        lblNavTitle.Text = "Golden Vista\r\nBocce League Master";
        lblNavTitle.Font = AppTheme.FontNavTitle;
        lblNavTitle.ForeColor = AppTheme.NavText;
        lblNavTitle.BackColor = AppTheme.NavTitleBackground;
        lblNavTitle.TextAlign = ContentAlignment.MiddleCenter;
        lblNavTitle.Dock = DockStyle.Left;
        lblNavTitle.Width = 220;
        lblNavTitle.Margin = new Padding(0);

        lblCtxLeague.Text = "League: (not set)";
        lblCtxLeague.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
        lblCtxLeague.ForeColor = AppTheme.NavText;
        lblCtxLeague.AutoSize = false;
        lblCtxLeague.TextAlign = ContentAlignment.MiddleLeft;
        lblCtxLeague.Size = new Size(300, 72);
        lblCtxLeague.Location = new Point(236, 0);
        lblCtxLeague.Cursor = Cursors.Hand;

        lblCtxDivider.Text = "|";
        lblCtxDivider.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
        lblCtxDivider.ForeColor = AppTheme.NavText;
        lblCtxDivider.AutoSize = false;
        lblCtxDivider.TextAlign = ContentAlignment.MiddleCenter;
        lblCtxDivider.Size = new Size(20, 72);
        lblCtxDivider.Location = new Point(536, 0);

        lblCtxSeason.Text = "Season: (not set)";
        lblCtxSeason.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
        lblCtxSeason.ForeColor = AppTheme.NavText;
        lblCtxSeason.AutoSize = false;
        lblCtxSeason.TextAlign = ContentAlignment.MiddleLeft;
        lblCtxSeason.Size = new Size(300, 72);
        lblCtxSeason.Location = new Point(556, 0);
        lblCtxSeason.Cursor = Cursors.Hand;

        lblCtxPageTitle.Text = "Dashboard";
        lblCtxPageTitle.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
        lblCtxPageTitle.ForeColor = AppTheme.NavText;
        lblCtxPageTitle.AutoSize = false;
        lblCtxPageTitle.TextAlign = ContentAlignment.MiddleRight;
        lblCtxPageTitle.Dock = DockStyle.Right;
        lblCtxPageTitle.Padding = new Padding(0, 0, 40, 0);
        lblCtxPageTitle.MinimumSize = new Size(300, 72);

        pnlTopBar.Controls.Add(lblCtxPageTitle);
        pnlTopBar.Controls.Add(lblCtxSeason);
        pnlTopBar.Controls.Add(lblCtxDivider);
        pnlTopBar.Controls.Add(lblCtxLeague);
        pnlTopBar.Controls.Add(lblNavTitle);
        pnlTopBar.Dock = DockStyle.Top;
        pnlTopBar.Height = 72;
        pnlTopBar.BackColor = AppTheme.NavTitleBackground;
        pnlTopBar.Padding = new Padding(0);

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
        pnlNav.BackColor = AppTheme.NavBackground;

        // Content panel
        pnlContent.Dock = DockStyle.Fill;
        pnlContent.BackColor = Color.White;

        // Form
        Controls.Add(pnlContent);
        Controls.Add(pnlNav);
        Controls.Add(pnlTopBar);
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


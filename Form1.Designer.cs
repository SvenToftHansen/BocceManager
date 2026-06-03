namespace BocceManager;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    private Panel pnlNav = null!;
    private Panel pnlContent = null!;
    private Panel pnlHeader = null!;
    private Label lblSection = null!;
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
        lblSection = new Label();
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
        pnlContent.Controls.Add(pnlHeader);

        // Form
        Controls.Add(pnlContent);
        Controls.Add(pnlNav);
        Controls.Add(statusStrip);

        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1280, 800);
        MinimumSize = new Size(1000, 640);
        Text = "BocceManager";
        StartPosition = FormStartPosition.CenterScreen;

        ResumeLayout(false);
        PerformLayout();
    }
}

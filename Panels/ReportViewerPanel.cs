using BocceManager.Data;
using BocceManager.Services;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;
using Microsoft.Reporting.WinForms;

namespace BocceManager.Panels;

public class ReportViewerPanel : UserControl
{
    private ListBox _reportList = null!;
    private ReportViewer _reportViewer = null!;
    private Button _btnPrint = null!;
    private Button _btnPdf = null!;
    private Button _btnWeb = null!;
    private Label _lblStatus = null!;

    public ReportViewerPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;
        BuildUI();
        AppParameterService.DefaultsChanged += OnDefaultsChanged;
        HandleCreated += (_, _) => BeginInvoke(new Action(LoadReports));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            AppParameterService.DefaultsChanged -= OnDefaultsChanged;
        }
        base.Dispose(disposing);
    }

    private void OnDefaultsChanged(object? sender, DefaultsChangedEventArgs e)
        => BeginInvoke(new Action(RefreshCurrentReport));

    private void BuildUI()
    {
        var mainContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            BorderStyle = BorderStyle.None,
            BackColor = AppTheme.ContentBackground
        };

        // Left sidebar with report list
        var leftPanel = new Panel
        {
            Dock = DockStyle.Left,
            Width = 220,
            BackColor = AppTheme.ContentBackground
        };

        var leftHeader = new Label
        {
            Dock = DockStyle.Top,
            Text = "Reports",
            Font = new Font(AppTheme.FontDefault.FontFamily, 11f, FontStyle.Bold),
            ForeColor = AppTheme.TextPrimary,
            Padding = new Padding(8, 8, 0, 4),
            AutoSize = false,
            Height = 32,
            BackColor = AppTheme.ContentBackground
        };
        leftPanel.Controls.Add(leftHeader);

        _reportList = new ListBox
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            BorderStyle = BorderStyle.None,
            Font = AppTheme.FontDefault
        };
        _reportList.SelectedIndexChanged += OnReportSelected;
        leftPanel.Controls.Add(_reportList);

        // Center: ReportViewer
        var centerPanel = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.ContentBackground };

        _reportViewer = new ReportViewer
        {
            Dock = DockStyle.Fill,
            ProcessingMode = ProcessingMode.Local
        };
        centerPanel.Controls.Add(_reportViewer);

        // Bottom bar with controls
        var bottomBar = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 50,
            BackColor = AppTheme.Surface,
            Padding = new Padding(12)
        };

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.RightToLeft
        };

        _btnWeb = CreateButton("Web", OnClickWeb);
        _btnPdf = CreateButton("PDF", OnClickPdf);
        _btnPrint = CreateButton("Print", OnClickPrint);

        buttonPanel.Controls.Add(_btnWeb);
        buttonPanel.Controls.Add(_btnPdf);
        buttonPanel.Controls.Add(_btnPrint);

        _lblStatus = new Label
        {
            Dock = DockStyle.Left,
            Text = "Select a report",
            Font = AppTheme.FontDefault,
            ForeColor = AppTheme.TextSecondary,
            AutoSize = true,
            Padding = new Padding(4, 12, 0, 0)
        };

        bottomBar.Controls.Add(buttonPanel);
        bottomBar.Controls.Add(_lblStatus);

        // Wire up the split container
        mainContainer.Panel1.Controls.Add(leftPanel);
        mainContainer.Panel2.Controls.Add(centerPanel);

        Controls.Add(mainContainer);
        Controls.Add(bottomBar);
    }

    private Button CreateButton(string text, EventHandler onClick)
    {
        var btn = new Button
        {
            Text = text,
            Size = new Size(80, 32),
            Font = AppTheme.FontDefaultBold,
            BackColor = AppTheme.Accent,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(4, 0, 0, 0)
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.Click += onClick;
        return btn;
    }

    private void LoadReports()
    {
        _reportList.Items.Clear();
        try
        {
            using var db = new BocceDbContext();
            var reports = ReportService.GetActiveReports(db);
            foreach (var report in reports)
            {
                _reportList.Items.Add(report);
            }

            if (_reportList.Items.Count > 0)
                _reportList.SelectedIndex = 0;
            else
                _lblStatus.Text = "No reports available";
        }
        catch (Exception ex)
        {
            _lblStatus.Text = $"Error loading reports: {ex.Message}";
        }
    }

    private void OnReportSelected(object? sender, EventArgs e)
    {
        RefreshCurrentReport();
    }

    private void RefreshCurrentReport()
    {
        if (_reportList.SelectedItem is not Data.Entities.Report report)
            return;

        try
        {
            _lblStatus.Text = $"Loading {report.Name}…";
            LoadReportData(report);
            _lblStatus.Text = report.Name;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading report:\n\n{ex.Message}", report.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            _lblStatus.Text = $"Error: {ex.Message}";
        }
    }

    private void LoadReportData(Data.Entities.Report report)
    {
        try
        {
            using var db = new BocceDbContext();
            var seasonId = AppParameterService.GetDefaultSeasonId(db);

            if (!seasonId.HasValue)
            {
                MessageBox.Show("No default season selected.", report.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var resourceName = $"BocceManager.{report.ReportPath.Replace('/', '.')}";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                MessageBox.Show($"Report file not found: {resourceName}", report.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _reportViewer.LocalReport.LoadReportDefinition(stream);

            var reportParams = new ReportParameter("SeasonId", seasonId.Value.ToString());
            _reportViewer.LocalReport.SetParameters(reportParams);

            _reportViewer.RefreshReport();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading report:\n\n{ex.Message}", report.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnClickPrint(object? sender, EventArgs e)
    {
        if (_reportList.SelectedItem is not Data.Entities.Report report)
            return;

        try
        {
            _reportViewer.PrintDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error printing report:\n\n{ex.Message}", "Print", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnClickPdf(object? sender, EventArgs e)
    {
        if (_reportList.SelectedItem is not Data.Entities.Report report)
            return;

        try
        {
            using var db = new BocceDbContext();
            var savePath = ReportService.GetDefaultReportPdfLocation(db);

            var dialog = new SaveFileDialog
            {
                InitialDirectory = savePath,
                FileName = $"{report.Name.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf",
                Filter = "PDF Files (*.pdf)|*.pdf"
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                byte[] pdfBytes = _reportViewer.LocalReport.Render("PDF");
                File.WriteAllBytes(dialog.FileName, pdfBytes);
                MessageBox.Show($"PDF saved to:\n{dialog.FileName}", "PDF Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error exporting to PDF:\n\n{ex.Message}", "PDF Export", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnClickWeb(object? sender, EventArgs e)
    {
        if (_reportList.SelectedItem is not Data.Entities.Report report)
            return;

        MessageBox.Show($"Web upload for {report.Name} coming soon.\n\nThis is a placeholder for future integration with your website.", "Web Upload", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}

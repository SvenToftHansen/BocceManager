using BocceManager.Data;
using BocceManager.Services;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Panels;

public class ReportViewerPanel : UserControl
{
    private ListBox _reportList = null!;
    private Button _btnPrint = null!;
    private Button _btnPdf = null!;
    private Button _btnWeb = null!;
    private Label _lblStatus = null!;
    private Label _lblPreview = null!;
    private List<Data.Entities.Report> _reports = new();

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
        // Left sidebar with report list
        var leftPanel = new Panel
        {
            Dock = DockStyle.Left,
            Width = 180,
            BackColor = AppTheme.ContentBackground,
            Padding = new Padding(0, 8, 0, 0)
        };

        _reportList = new ListBox
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            BorderStyle = BorderStyle.None,
            Font = AppTheme.FontDefault,
            IntegralHeight = false
        };
        _reportList.SelectedIndexChanged += OnReportSelected;
        leftPanel.Controls.Add(_reportList);

        // Center: Info Area
        var centerPanel = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.ContentBackground, Padding = new Padding(16) };

        _lblPreview = new Label
        {
            Dock = DockStyle.Top,
            Text = "Select a report from the list",
            TextAlign = ContentAlignment.TopLeft,
            Font = AppTheme.FontDefault,
            ForeColor = AppTheme.TextSecondary,
            AutoSize = true
        };
        centerPanel.Controls.Add(_lblPreview);

        // Bottom bar with controls and status
        var bottomBar = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            BackColor = AppTheme.Surface,
            Padding = new Padding(12, 8, 12, 8)
        };

        _btnPrint = CreateButton("Print", OnClickPrint);
        _btnPdf = CreateButton("PDF", OnClickPdf);
        _btnWeb = CreateButton("Web", OnClickWeb);

        _lblStatus = new Label
        {
            Dock = DockStyle.Left,
            Text = "Ready",
            Font = AppTheme.FontDefault,
            ForeColor = AppTheme.TextSecondary,
            AutoSize = true,
            Padding = new Padding(0, 10, 8, 0)
        };

        bottomBar.Controls.Add(_btnPrint);
        bottomBar.Controls.Add(_btnPdf);
        bottomBar.Controls.Add(_btnWeb);
        bottomBar.Controls.Add(_lblStatus);

        Controls.Add(bottomBar);
        Controls.Add(leftPanel);
        Controls.Add(centerPanel);
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
        _reports.Clear();
        try
        {
            using var db = new BocceDbContext();
            _reports = ReportService.GetActiveReports(db).ToList();

            foreach (var report in _reports)
            {
                _reportList.Items.Add(report.Name);
            }

            if (_reportList.Items.Count > 0)
                _reportList.SelectedIndex = 0;
            else
                _lblStatus.Text = "No reports available";
        }
        catch (Exception ex)
        {
            _lblStatus.Text = $"Error: {ex.Message}";
        }
    }

    private void OnReportSelected(object? sender, EventArgs e)
    {
        RefreshCurrentReport();
    }

    private void RefreshCurrentReport()
    {
        if (_reportList.SelectedIndex < 0 || _reportList.SelectedIndex >= _reports.Count)
            return;

        var report = _reports[_reportList.SelectedIndex];

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
                _lblPreview.Text = "No default season selected";
                _lblStatus.Text = "No season";
                return;
            }

            _lblPreview.Text = $"✓ {report.Name}\n\nClick 'Print' to preview and print\nClick 'PDF' to export to file\nClick 'Web' to upload";
            _lblStatus.Text = report.Name;
        }
        catch (Exception ex)
        {
            _lblPreview.Text = $"Error: {ex.Message}";
            _lblStatus.Text = "Error";
        }
    }

    private void OnClickPrint(object? sender, EventArgs e)
    {
        if (_reportList.SelectedIndex < 0 || _reportList.SelectedIndex >= _reports.Count)
            return;

        var report = _reports[_reportList.SelectedIndex];

        try
        {
            using var db = new BocceDbContext();
            var seasonId = AppParameterService.GetDefaultSeasonId(db);
            if (!seasonId.HasValue) return;

            if (report.Name.Contains("Team"))
            {
                var sections = TeamsPrintService.BuildSections(seasonId.Value);
                if (sections.Count == 0)
                {
                    MessageBox.Show("No data available for this report.", report.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                var doc = TeamsPrintService.BuildDocument(sections);
                doc.DocumentName = report.Name;
                TeamsPrintService.ShowPrintPreview(this, doc);
            }
            else if (report.Name.Contains("Schedule"))
            {
                var sections = SchedulePrintService.BuildSections(seasonId.Value);
                if (sections.Count == 0)
                {
                    MessageBox.Show("No data available for this report.", report.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                var doc = SchedulePrintService.BuildDocument(sections);
                doc.DocumentName = report.Name;
                SchedulePrintService.ShowPrintPreview(this, doc);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error:\n\n{ex.Message}", "Print", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnClickPdf(object? sender, EventArgs e)
    {
        if (_reportList.SelectedIndex < 0 || _reportList.SelectedIndex >= _reports.Count)
            return;

        var report = _reports[_reportList.SelectedIndex];

        try
        {
            using var db = new BocceDbContext();
            var seasonId = AppParameterService.GetDefaultSeasonId(db);
            if (!seasonId.HasValue) return;

            var savePath = ReportService.GetDefaultReportPdfLocation(db);
            var dialog = new SaveFileDialog
            {
                InitialDirectory = savePath,
                FileName = $"{report.Name.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf",
                Filter = "PDF Files (*.pdf)|*.pdf"
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                MessageBox.Show("PDF export feature coming soon.", "PDF Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // TODO: Implement PDF export using print services
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error:\n\n{ex.Message}", "PDF Export", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnClickWeb(object? sender, EventArgs e)
    {
        if (_reportList.SelectedIndex < 0 || _reportList.SelectedIndex >= _reports.Count)
            return;

        var report = _reports[_reportList.SelectedIndex];

        MessageBox.Show($"Web upload for {report.Name} coming soon.\n\nThis is a placeholder for future integration with your website.", "Web Upload", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}

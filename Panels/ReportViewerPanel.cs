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
    private DataGridView _dataGrid = null!;
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
            Width = 200,
            BackColor = AppTheme.ContentBackground
        };

        var leftHeader = new Label
        {
            Dock = DockStyle.Top,
            Text = "Reports",
            Font = new Font(AppTheme.FontDefault.FontFamily, 11f, FontStyle.Bold),
            ForeColor = AppTheme.TextPrimary,
            Padding = new Padding(8, 6, 0, 4),
            AutoSize = false,
            Height = 30,
            BackColor = AppTheme.ContentBackground
        };
        leftPanel.Controls.Add(leftHeader);

        _reportList = new ListBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            ForeColor = Color.Black,
            BorderStyle = BorderStyle.FixedSingle,
            Font = AppTheme.FontDefault,
            IntegralHeight = false
        };
        _reportList.SelectedIndexChanged += OnReportSelected;
        leftPanel.Controls.Add(_reportList);

        // Center: Preview Area
        var centerPanel = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.ContentBackground };

        _lblPreview = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Select a report from the list to preview",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = AppTheme.FontDefault,
            ForeColor = AppTheme.TextSecondary,
            AutoSize = false
        };
        centerPanel.Controls.Add(_lblPreview);

        _dataGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = AppTheme.Surface,
            BorderStyle = BorderStyle.None,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToOrderColumns = false,
            AllowUserToResizeRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = true,
            Visible = false
        };
        _dataGrid.DefaultCellStyle.BackColor = AppTheme.Surface;
        _dataGrid.DefaultCellStyle.ForeColor = AppTheme.TextPrimary;
        _dataGrid.DefaultCellStyle.Font = AppTheme.FontDefault;
        _dataGrid.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.ContentBackground;
        _dataGrid.ColumnHeadersDefaultCellStyle.ForeColor = AppTheme.TextPrimary;
        _dataGrid.ColumnHeadersDefaultCellStyle.Font = AppTheme.FontDefaultBold;
        centerPanel.Controls.Add(_dataGrid);

        // Bottom bar with controls
        var bottomBar = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 56,
            BackColor = AppTheme.Surface,
            Padding = new Padding(12, 8, 12, 8)
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
                var name = report.Name ?? "Unknown";
                _reportList.Items.Add($"• {name}");
            }

            _lblStatus.Text = $"Loaded {_reportList.Items.Count} reports";

            if (_reportList.Items.Count > 0)
                _reportList.SelectedIndex = 0;
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
                _lblPreview.Visible = true;
                _dataGrid.Visible = false;
                _lblPreview.Text = "No default season selected";
                return;
            }

            if (report.Name.Contains("Team"))
            {
                LoadTeamListingData(db, seasonId.Value);
            }
            else if (report.Name.Contains("Schedule"))
            {
                LoadScheduleData(db, seasonId.Value);
            }

            _lblPreview.Visible = false;
            _dataGrid.Visible = true;
        }
        catch (Exception ex)
        {
            _lblPreview.Visible = true;
            _dataGrid.Visible = false;
            _lblPreview.Text = $"Error: {ex.Message}";
        }
    }

    private void LoadTeamListingData(BocceDbContext db, int seasonId)
    {
        var teams = db.Teams
            .Where(t => t.Division.SeasonId == seasonId)
            .OrderBy(t => t.Division.Name)
            .ThenBy(t => t.SystemName)
            .Select(t => new
            {
                Division = t.Division.Name,
                Team = t.EffectiveDisplayName,
                Captain = (t.Captain != null ? t.Captain.FirstName + " " + t.Captain.LastName : ""),
                PlayerCount = t.TeamPlayers.Count(tp => tp.IsActive)
            })
            .ToList();

        _dataGrid.DataSource = teams;
        _dataGrid.AutoResizeColumns();
    }

    private void LoadScheduleData(BocceDbContext db, int seasonId)
    {
        var schedules = db.Matches
            .Where(m => m.ScheduleWeek.Division.SeasonId == seasonId)
            .OrderBy(m => m.ScheduleWeek.Division.Name)
            .ThenBy(m => m.ScheduleWeek.WeekNumber)
            .ThenBy(m => m.CourtId)
            .Select(m => new
            {
                Division = m.ScheduleWeek.Division.Name,
                Week = m.ScheduleWeek.WeekNumber,
                Date = m.ScheduledDate.HasValue ? m.ScheduledDate.Value.ToString("MMM dd") : "",
                Time = m.ScheduledTime.HasValue ? m.ScheduledTime.Value.ToString("h:mm tt") : "",
                Court = m.Court != null ? m.Court.CourtNumber.ToString() : "TBD",
                Team1 = m.Team1.EffectiveDisplayName,
                Team2 = m.Team2.EffectiveDisplayName,
                Status = m.Status
            })
            .ToList();

        _dataGrid.DataSource = schedules;
        _dataGrid.AutoResizeColumns();
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

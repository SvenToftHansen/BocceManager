using BocceManager.Data;
using BocceManager.Services;
using BocceManager.UI.Theme;

namespace BocceManager.Panels;

public class ReportAllPlayersPanel : UserControl
{
    private Label _lblMessage = null!;
    private ComboBox _cboPaidFilter = null!;
    private CheckBox _chkMissingPhone = null!;
    private CheckBox _chkMissingEmail = null!;
    private CheckBox _chkMissingLot = null!;
    private Button _btnShowReport = null!;

    public ReportAllPlayersPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;

        var toolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 46,
            BackColor = AppTheme.ContentBackground,
            Padding = new Padding(12, 8, 12, 8)
        };

        var lblPaidFilter = new Label
        {
            Text = "Fee Status:",
            Left = 12, Top = 12, Width = 70, Height = 22,
            Font = AppTheme.FontDefault, ForeColor = AppTheme.TextPrimary,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _cboPaidFilter = new ComboBox
        {
            Left = 86, Top = 8, Width = 110, Height = 30,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = AppTheme.FontDefault
        };
        _cboPaidFilter.Items.AddRange(["All", "Paid", "Unpaid"]);
        _cboPaidFilter.SelectedIndex = 0;

        _chkMissingPhone = new CheckBox
        {
            Text = "Missing Phone", Left = 210, Top = 12, Width = 130, Height = 22,
            Font = AppTheme.FontDefault, ForeColor = AppTheme.TextPrimary
        };
        _chkMissingEmail = new CheckBox
        {
            Text = "Missing Email", Left = 350, Top = 12, Width = 130, Height = 22,
            Font = AppTheme.FontDefault, ForeColor = AppTheme.TextPrimary
        };
        _chkMissingLot = new CheckBox
        {
            Text = "Missing Lot #", Left = 490, Top = 12, Width = 130, Height = 22,
            Font = AppTheme.FontDefault, ForeColor = AppTheme.TextPrimary
        };

        _btnShowReport = new Button
        {
            Text = "Show Report",
            Left = 630, Top = 8, Width = 130, Height = 30,
            FlatStyle = FlatStyle.Flat, Font = AppTheme.FontButton
        };

        toolbar.Controls.AddRange([lblPaidFilter, _cboPaidFilter, _chkMissingPhone, _chkMissingEmail, _chkMissingLot, _btnShowReport]);

        _lblMessage = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Choose filters above and click Show Report",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = AppTheme.FontDefault,
            ForeColor = AppTheme.TextSecondary,
            AutoSize = false
        };

        Controls.Add(_lblMessage);
        Controls.Add(toolbar);

        _btnShowReport.Click += (_, _) => ShowReport();
    }

    private void ShowReport()
    {
        try
        {
            using var db = new BocceDbContext();
            var leagueId = AppParameterService.GetDefaultLeagueId(db);
            var seasonId = AppParameterService.GetDefaultSeasonId(db);

            if (!leagueId.HasValue)
            {
                _lblMessage.Text = "No default league selected";
                return;
            }

            var league = db.Leagues.FirstOrDefault(l => l.Id == leagueId.Value);
            var season = seasonId.HasValue ? db.Seasons.FirstOrDefault(s => s.Id == seasonId.Value) : null;

            var paidFilter = _cboPaidFilter.SelectedIndex switch
            {
                1 => AllPlayersReportService.PaidFilter.Paid,
                2 => AllPlayersReportService.PaidFilter.Unpaid,
                _ => AllPlayersReportService.PaidFilter.All
            };
            var filter = new AllPlayersReportService.ReportFilter(
                paidFilter, _chkMissingPhone.Checked, _chkMissingEmail.Checked, _chkMissingLot.Checked);

            var data = AllPlayersReportService.BuildData(leagueId.Value, seasonId, filter);

            if (data.Count == 0)
            {
                _lblMessage.Text = "No players match the selected filters";
                return;
            }

            var clubName = AppParameterService.GetAppParameter(db, "ClubName") ?? "Bocce League";
            var docHeader = $"{clubName} – {league?.Name} – {season?.Name ?? "N/A"} - ALL PLAYERS";

            var doc = AllPlayersReportService.BuildDocument(docHeader, filter.Describe(), data);
            doc.DocumentName = "All Players";

            AllPlayersReportService.ShowPrintPreview(this, doc, data);
        }
        catch (Exception ex)
        {
            _lblMessage.Text = $"Error: {ex.Message}";
        }
    }
}

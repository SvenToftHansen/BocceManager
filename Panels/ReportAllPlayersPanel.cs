using BocceManager.Data;
using BocceManager.Services;
using BocceManager.UI.Controls;
using BocceManager.UI.Theme;

namespace BocceManager.Panels;

public class ReportAllPlayersPanel : UserControl
{
    private const string FilterPaid            = "Paid";
    private const string FilterUnpaid          = "Unpaid";
    private const string FilterMissingPhone    = "Missing Phone";
    private const string FilterMissingEmail    = "Missing Email";
    private const string FilterMissingLot      = "Missing Lot #";
    private const string FilterOnTeam          = "On a Team";
    private const string FilterNotOnTeam       = "Not on a Team";
    private const string FilterOnSpareList     = "On Spare List";
    private const string FilterNotOnSpareList  = "Not on Spare List";

    private Label _lblMessage = null!;
    private MultiSelectDropDown _dropFilters = null!;
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

        var lblFilters = new Label
        {
            Text = "Filters:",
            Left = 12, Top = 12, Width = 50, Height = 22,
            Font = AppTheme.FontDefault, ForeColor = AppTheme.TextPrimary,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _dropFilters = new MultiSelectDropDown("All Players")
        {
            Left = 66, Top = 8, Width = 200, Height = 30,
            Font = AppTheme.FontDefault
        };
        _dropFilters.SetItems([
            FilterPaid, FilterUnpaid,
            FilterOnTeam, FilterNotOnTeam,
            FilterOnSpareList, FilterNotOnSpareList,
            FilterMissingPhone, FilterMissingEmail, FilterMissingLot
        ]);

        _btnShowReport = new Button
        {
            Text = "Show Report",
            Left = 280, Top = 8, Width = 130, Height = 30,
            FlatStyle = FlatStyle.Flat, Font = AppTheme.FontButton
        };

        toolbar.Controls.AddRange([lblFilters, _dropFilters, _btnShowReport]);

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

            var filter = new AllPlayersReportService.ReportFilter(
                _dropFilters.IsChecked(FilterPaid), _dropFilters.IsChecked(FilterUnpaid),
                _dropFilters.IsChecked(FilterMissingPhone), _dropFilters.IsChecked(FilterMissingEmail), _dropFilters.IsChecked(FilterMissingLot),
                _dropFilters.IsChecked(FilterOnTeam), _dropFilters.IsChecked(FilterNotOnTeam),
                _dropFilters.IsChecked(FilterOnSpareList), _dropFilters.IsChecked(FilterNotOnSpareList));

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

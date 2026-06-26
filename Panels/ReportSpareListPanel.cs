using BocceManager.Data;
using BocceManager.Services;
using BocceManager.UI.Theme;

namespace BocceManager.Panels;

public class ReportSpareListPanel : UserControl
{
    private Label _lblMessage = null!;

    public ReportSpareListPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;

        _lblMessage = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Loading Spare List Report...",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = AppTheme.FontDefault,
            ForeColor = AppTheme.TextSecondary,
            AutoSize = false
        };

        Controls.Add(_lblMessage);
        HandleCreated += (_, _) => BeginInvoke(new Action(ShowReport));
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
            if (league == null)
            {
                _lblMessage.Text = "League not found";
                return;
            }

            var season = seasonId.HasValue ? db.Seasons.FirstOrDefault(s => s.Id == seasonId.Value) : null;

            var data = SpareListReportService.BuildSpareListData(leagueId.Value);

            if (data.Count == 0)
            {
                _lblMessage.Text = "No players on the spare list for the selected league";
                return;
            }

            var clubName = AppParameterService.GetAppParameter(db, "ClubName") ?? "Bocce League";
            var doc = SpareListReportService.BuildDocument(clubName, league.Name, season?.Name ?? "N/A", data);
            doc.DocumentName = $"{league.Name} - Spare List";
            SpareListReportService.ShowPrintPreview(this, doc, league.Name, data);
        }
        catch (Exception ex)
        {
            _lblMessage.Text = $"Error: {ex.Message}";
        }
    }
}

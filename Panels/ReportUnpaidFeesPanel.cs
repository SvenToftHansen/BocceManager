using BocceManager.Services;
using BocceManager.UI.Theme;

namespace BocceManager.Panels;

public class ReportUnpaidFeesPanel : UserControl
{
    private Label _lblMessage = null!;

    public ReportUnpaidFeesPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;

        _lblMessage = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Loading Unpaid Fees...",
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
            using var db = new Data.BocceDbContext();
            var seasonId = AppParameterService.GetDefaultSeasonId(db);
            var leagueId = AppParameterService.GetDefaultLeagueId(db);

            string leagueName = "";
            string seasonName = "";
            if (leagueId.HasValue)
                leagueName = db.Leagues.FirstOrDefault(l => l.Id == leagueId.Value)?.Name ?? "";
            if (seasonId.HasValue)
                seasonName = db.Seasons.FirstOrDefault(s => s.Id == seasonId.Value)?.Name ?? "";

            var clubName = AppParameterService.GetAppParameter(db, "ClubName") ?? "Bocce League";
            string docHeader = string.IsNullOrEmpty(leagueName)
                ? $"{clubName} - Unpaid Fees"
                : $"{clubName}  –  {leagueName}  –  {seasonName} - Unpaid Fees";

            var sections = UnpaidFeesReportService.BuildSections(seasonId);
            if (sections.All(s => s.Rows.Count == 0))
            {
                _lblMessage.Text = "No unpaid fees — everyone is paid up";
                return;
            }

            var doc = UnpaidFeesReportService.BuildDocument(docHeader, sections);
            doc.DocumentName = "Unpaid Fees";

            UnpaidFeesReportService.ShowPrintPreview(this, doc, "UnpaidFees", docHeader, sections);
        }
        catch (Exception ex)
        {
            _lblMessage.Text = $"Error: {ex.Message}";
        }
    }
}

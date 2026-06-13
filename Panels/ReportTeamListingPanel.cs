using BocceManager.Services;
using BocceManager.UI.Theme;

namespace BocceManager.Panels;

public class ReportTeamListingPanel : UserControl
{
    private Label _lblMessage = null!;

    public ReportTeamListingPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;

        _lblMessage = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Loading Team Listing...",
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

            if (!seasonId.HasValue)
            {
                _lblMessage.Text = "No default season selected";
                return;
            }

            var sections = TeamsPrintService.BuildSections(seasonId.Value);
            if (sections.Count == 0)
            {
                _lblMessage.Text = "No teams available for this season";
                return;
            }

            var doc = TeamsPrintService.BuildDocument(sections);
            doc.DocumentName = "Team Listing";
            TeamsPrintService.ShowPrintPreview(this, doc);
        }
        catch (Exception ex)
        {
            _lblMessage.Text = $"Error: {ex.Message}";
        }
    }
}

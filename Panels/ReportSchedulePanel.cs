using BocceManager.Services;
using BocceManager.UI.Theme;

namespace BocceManager.Panels;

public class ReportSchedulePanel : UserControl
{
    private Label _lblMessage = null!;

    public ReportSchedulePanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;

        _lblMessage = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Loading Schedules...",
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

            var sections = SchedulePrintService.BuildSections(seasonId.Value);
            if (sections.Count == 0)
            {
                _lblMessage.Text = "No schedules available for this season";
                return;
            }

            var doc = SchedulePrintService.BuildDocument(sections);
            doc.DocumentName = "Schedules - Generic";
            SchedulePrintService.ShowPrintPreview(this, doc);
        }
        catch (Exception ex)
        {
            _lblMessage.Text = $"Error: {ex.Message}";
        }
    }
}

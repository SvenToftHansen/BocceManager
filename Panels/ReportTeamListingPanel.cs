using BocceManager.Services;
using BocceManager.UI.Theme;

namespace BocceManager.Panels;

public class ReportTeamListingPanel : UserControl
{
    private Label _lblMessage = null!;
    private Button _btnCompact = null!;
    private Button _btnFullRoster = null!;

    public ReportTeamListingPanel()
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

        _btnCompact = new Button
        {
            Text = "Compact (Players in One Column)",
            Left = 12, Top = 8, Width = 220, Height = 30,
            FlatStyle = FlatStyle.Flat, Font = AppTheme.FontButton
        };
        _btnFullRoster = new Button
        {
            Text = "Full Roster (One Player per Column)",
            Left = 240, Top = 8, Width = 240, Height = 30,
            FlatStyle = FlatStyle.Flat, Font = AppTheme.FontButton
        };

        toolbar.Controls.Add(_btnCompact);
        toolbar.Controls.Add(_btnFullRoster);

        _lblMessage = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Choose a Team Listing format above",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = AppTheme.FontDefault,
            ForeColor = AppTheme.TextSecondary,
            AutoSize = false
        };

        Controls.Add(_lblMessage);
        Controls.Add(toolbar);

        _btnCompact.Click += (_, _) => ShowReport(TeamsPrintService.ListingMode.Compact);
        _btnFullRoster.Click += (_, _) => ShowReport(TeamsPrintService.ListingMode.FullRoster);
    }

    private void ShowReport(TeamsPrintService.ListingMode mode)
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

            var doc = TeamsPrintService.BuildDocument(sections, mode);
            doc.DocumentName = mode == TeamsPrintService.ListingMode.Compact
                ? "Team Listing"
                : "Team Listing (Full Roster)";

            TeamsPrintService.ShowPrintPreview(this, doc, "TeamListing", sections, mode);
        }
        catch (Exception ex)
        {
            _lblMessage.Text = $"Error: {ex.Message}";
        }
    }
}

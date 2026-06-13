using BocceManager.Data;
using BocceManager.Services;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Panels;

public class ScheduleReportPanel : UserControl
{
    private Label  _lblContext = null!;
    private Button _btnPreview = null!;

    public ScheduleReportPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock      = DockStyle.Fill;
        BuildUI();
        AppParameterService.DefaultsChanged += OnDefaultsChanged;
        HandleCreated += (_, _) => BeginInvoke(new Action(LoadContext));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) AppParameterService.DefaultsChanged -= OnDefaultsChanged;
        base.Dispose(disposing);
    }

    private void OnDefaultsChanged(object? sender, DefaultsChangedEventArgs e)
        => BeginInvoke(new Action(LoadContext));

    private void LoadContext()
    {
        _lblContext.Text    = "Loading…";
        _btnPreview.Enabled = false;
        try
        {
            using var db     = new BocceDbContext();
            var seasonId     = AppParameterService.GetDefaultSeasonId(db);
            if (!seasonId.HasValue) { _lblContext.Text = "No default season selected."; return; }

            var season = db.Seasons.Include(s => s.League).FirstOrDefault(s => s.Id == seasonId.Value);
            if (season == null) { _lblContext.Text = "Season not found."; return; }

            int count       = db.ScheduleTemplates.Count(t => t.SeasonId == seasonId.Value);
            string tmplInfo = count == 0 ? "no templates" : $"{count} template{(count == 1 ? "" : "s")}";
            _lblContext.Text    = $"{season.League.Name}  —  {season.Name}  ({tmplInfo})";
            _btnPreview.Enabled = count > 0;
        }
        catch (Exception ex)
        {
            _lblContext.Text = $"Error: {ex.Message}";
        }
    }

    private void OnPreview(object? sender, EventArgs e)
    {
        try
        {
            using var db = new BocceDbContext();
            var seasonId = AppParameterService.GetDefaultSeasonId(db);
            if (!seasonId.HasValue) return;

            var sections = SchedulePrintService.BuildSections(seasonId.Value);
            if (sections.Count == 0)
            {
                MessageBox.Show("No schedule templates found for this season.",
                    "Schedules – Generic", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var doc = SchedulePrintService.BuildDocument(sections);
            doc.DocumentName = "Schedule Templates";
            SchedulePrintService.ShowPrintPreview(this, doc);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not build report:\n\n{ex.Message}",
                "Schedules – Generic", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BuildUI()
    {
        var stack = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents  = false,
            Padding       = new Padding(48, 40, 0, 0)
        };

        stack.Controls.Add(new Label
        {
            Text      = "Schedules — Generic",
            Font      = new Font(AppTheme.FontDefault.FontFamily, 16f, FontStyle.Bold),
            AutoSize  = true,
            ForeColor = AppTheme.TextPrimary,
            Margin    = new Padding(0, 0, 0, 6)
        });

        _lblContext = new Label
        {
            Text      = "Loading…",
            AutoSize  = true,
            Font      = AppTheme.FontDefault,
            ForeColor = AppTheme.TextSecondary,
            Margin    = new Padding(0, 0, 0, 4)
        };
        stack.Controls.Add(_lblContext);

        stack.Controls.Add(new Label
        {
            Text      = "Prints all schedule templates for the current season, showing weekly court assignments.",
            AutoSize  = true,
            Font      = AppTheme.FontDefault,
            ForeColor = AppTheme.TextMuted,
            Margin    = new Padding(0, 0, 0, 20)
        });

        _btnPreview = new Button
        {
            Text      = "Preview / Print…",
            Size      = new Size(160, 36),
            Font      = AppTheme.FontDefaultBold,
            BackColor = AppTheme.Accent,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Enabled   = false,
            Margin    = new Padding(0)
        };
        _btnPreview.FlatAppearance.BorderSize = 0;
        _btnPreview.Click += OnPreview;
        stack.Controls.Add(_btnPreview);

        Controls.Add(stack);
    }
}

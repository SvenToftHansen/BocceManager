using BocceManager.Data;
using BocceManager.Services;
using BocceManager.UI.Theme;

namespace BocceManager.Panels;

public class EmailHistoryPanel : UserControl
{
    private ListView _lvLogs = null!;
    private ListView _lvRecipients = null!;
    private SplitContainer _split = null!;

    public EmailHistoryPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;
        BuildUI();
        LoadLogs();
    }

    private void BuildUI()
    {
        _split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            Panel1MinSize = 0,
            Panel2MinSize = 0,
            BackColor = AppTheme.Separator
        };
        // Panel1MinSize/Panel2MinSize/SplitterDistance can't be set to their real values
        // until the control has a real size — WinForms recomputes SplitterDistance
        // internally when the min sizes are set, and throws if Height is still 0.
        _split.SizeChanged += (_, _) => SafeApplySplitDistance(_split, 150, 150, 260);
        _split.HandleCreated += (_, _) => BeginInvoke(new Action(() => SafeApplySplitDistance(_split, 150, 150, 260)));

        _lvLogs = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = false,
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.FontDefault,
            BorderStyle = BorderStyle.None
        };
        _lvLogs.Columns.Add("Sent At", 140);
        _lvLogs.Columns.Add("Subject", 300);
        _lvLogs.Columns.Add("Recipients", 90);
        _lvLogs.Columns.Add("Sent By", 140);
        _lvLogs.SelectedIndexChanged += (_, _) => LoadRecipients();
        _split.Panel1.Controls.Add(_lvLogs);

        _lvRecipients = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = false,
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.FontDefault,
            BorderStyle = BorderStyle.None
        };
        _lvRecipients.Columns.Add("Email", 240);
        _lvRecipients.Columns.Add("Status", 80);
        _lvRecipients.Columns.Add("Error", 400);
        _split.Panel2.Controls.Add(_lvRecipients);

        Controls.Add(_split);
    }

    private static void SafeApplySplitDistance(SplitContainer split, int desiredFirstMin, int desiredSecondMin, int preferred)
    {
        int extent = split.Orientation == Orientation.Horizontal ? split.Height : split.Width;
        if (extent <= 1) return;

        int maxTotalMin = Math.Max(0, extent - 1);
        int firstMin = desiredFirstMin, secondMin = desiredSecondMin;
        if (firstMin + secondMin > maxTotalMin)
        {
            if (maxTotalMin == 0)
            {
                firstMin = 0;
                secondMin = 0;
            }
            else
            {
                double ratio = desiredFirstMin / (double)(desiredFirstMin + desiredSecondMin);
                firstMin = (int)Math.Floor(maxTotalMin * ratio);
                secondMin = maxTotalMin - firstMin;
            }
        }

        split.Panel1MinSize = firstMin;
        split.Panel2MinSize = secondMin;

        int maxFirst = extent - secondMin;
        if (maxFirst < firstMin) maxFirst = firstMin;

        int clamped = Math.Min(preferred, maxFirst);
        clamped = Math.Max(firstMin, clamped);

        if (clamped > 0)
            split.SplitterDistance = clamped;
    }

    private void LoadLogs()
    {
        try
        {
            using var db = new BocceDbContext();
            var logs = db.EmailLogs.OrderByDescending(l => l.SentAt).Take(200).ToList();

            _lvLogs.Items.Clear();
            foreach (var log in logs)
            {
                var item = new ListViewItem(log.SentAt.ToLocalTime().ToString("g")) { Tag = log.Id };
                item.SubItems.Add(log.Subject);
                item.SubItems.Add((log.RecipientCount ?? 0).ToString());
                item.SubItems.Add(log.SentBy ?? "");
                _lvLogs.Items.Add(item);
            }

            if (_lvLogs.Items.Count > 0) _lvLogs.Items[0].Selected = true;
            else _lvRecipients.Items.Clear();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading sent history:\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadRecipients()
    {
        _lvRecipients.Items.Clear();
        if (_lvLogs.SelectedItems.Count == 0 || _lvLogs.SelectedItems[0].Tag is not int logId) return;

        try
        {
            using var db = new BocceDbContext();
            var recipients = db.EmailLogRecipients
                .Where(r => r.EmailLogId == logId)
                .OrderBy(r => r.Email)
                .ToList();

            foreach (var r in recipients)
            {
                var item = new ListViewItem(r.Email);
                item.SubItems.Add(r.Status);
                item.SubItems.Add(r.ErrorMessage ?? "");
                _lvRecipients.Items.Add(item);
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error(ex, "Failed to load email log recipients");
        }
    }
}

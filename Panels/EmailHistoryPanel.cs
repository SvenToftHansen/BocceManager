using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.Services;
using BocceManager.UI.Theme;
using Microsoft.Web.WebView2.WinForms;

namespace BocceManager.Panels;

public class EmailHistoryPanel : UserControl
{
    private ListView _lvLogs = null!;
    private ListView _lvRecipients = null!;
    private WebView2 _webView = null!;
    private SplitContainer _outerSplit = null!;
    private SplitContainer _leftSplit = null!;
    private bool _webViewReady;
    private string? _pendingNavHtml;

    public EmailHistoryPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;
        BuildUI();
        Load += async (_, _) => await InitWebViewAsync();
        LoadLogs();
    }

    private void BuildUI()
    {
        // Left/right: the log + recipient lists vs. the selected email's body, for reference.
        _outerSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            Panel1MinSize = 0,
            Panel2MinSize = 0,
            BackColor = AppTheme.Separator
        };
        _outerSplit.SizeChanged += (_, _) => SafeApplySplitDistance(_outerSplit, 260, 320, 420);
        _outerSplit.HandleCreated += (_, _) => BeginInvoke(new Action(() => SafeApplySplitDistance(_outerSplit, 260, 320, 420)));

        // Top/bottom, within the left side: sent-email log, then its recipients.
        _leftSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            Panel1MinSize = 0,
            Panel2MinSize = 0,
            BackColor = AppTheme.Separator
        };
        _leftSplit.SizeChanged += (_, _) => SafeApplySplitDistance(_leftSplit, 150, 150, 260);
        _leftSplit.HandleCreated += (_, _) => BeginInvoke(new Action(() => SafeApplySplitDistance(_leftSplit, 150, 150, 260)));

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
        _lvLogs.SelectedIndexChanged += (_, _) => OnLogSelectionChanged();
        _leftSplit.Panel1.Controls.Add(_lvLogs);

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
        _leftSplit.Panel2.Controls.Add(_lvRecipients);

        _outerSplit.Panel1.Controls.Add(_leftSplit);

        _webView = new WebView2 { Dock = DockStyle.Fill };
        _outerSplit.Panel2.Controls.Add(_webView);

        Controls.Add(_outerSplit);
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

    private async Task InitWebViewAsync()
    {
        try
        {
            await _webView.EnsureCoreWebView2Async();
            _webViewReady = true;
            if (_pendingNavHtml != null)
            {
                _webView.CoreWebView2.NavigateToString(_pendingNavHtml);
                _pendingNavHtml = null;
            }
        }
        catch { }
    }

    private void NavigateWebView(string html)
    {
        if (_webViewReady && _webView.CoreWebView2 != null)
            _webView.CoreWebView2.NavigateToString(html);
        else
            _pendingNavHtml = html;
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
                var item = new ListViewItem(log.SentAt.ToLocalTime().ToString("g")) { Tag = log };
                item.SubItems.Add(log.Subject);
                item.SubItems.Add((log.RecipientCount ?? 0).ToString());
                item.SubItems.Add(log.SentBy ?? "");
                _lvLogs.Items.Add(item);
            }

            if (_lvLogs.Items.Count > 0) _lvLogs.Items[0].Selected = true;
            else
            {
                _lvRecipients.Items.Clear();
                NavigateWebView("<html><body></body></html>");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading sent history:\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnLogSelectionChanged()
    {
        LoadRecipients();
        LoadBodyPreview();
    }

    private void LoadRecipients()
    {
        _lvRecipients.Items.Clear();
        if (_lvLogs.SelectedItems.Count == 0 || _lvLogs.SelectedItems[0].Tag is not EmailLog log) return;

        try
        {
            using var db = new BocceDbContext();
            var recipients = db.EmailLogRecipients
                .Where(r => r.EmailLogId == log.Id)
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

    private void LoadBodyPreview()
    {
        if (_lvLogs.SelectedItems.Count == 0 || _lvLogs.SelectedItems[0].Tag is not EmailLog log)
        {
            NavigateWebView("<html><body></body></html>");
            return;
        }

        var header = $"""
            <div style="font-family:Segoe UI,Arial,sans-serif;border-bottom:1px solid #ddd;padding-bottom:8px;margin-bottom:12px;">
            <div style="font-size:14px;font-weight:bold;">{System.Net.WebUtility.HtmlEncode(log.Subject)}</div>
            <div style="font-size:12px;color:#666;">Sent {log.SentAt.ToLocalTime():dddd, MMM d, yyyy h:mm tt} by {System.Net.WebUtility.HtmlEncode(log.SentBy ?? "")} to {log.RecipientCount ?? 0} recipient(s)</div>
            </div>
            """;

        string bodyHtml = string.IsNullOrWhiteSpace(log.Body)
            ? "<p style=\"color:#999;\">(no body recorded)</p>"
            : log.Body;

        NavigateWebView($"<html><body style=\"padding:16px;\">{header}{bodyHtml}</body></html>");
    }
}

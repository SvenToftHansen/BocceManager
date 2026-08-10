using BocceManager.Services;
using BocceManager.UI.Theme;
using Microsoft.Web.WebView2.WinForms;

namespace BocceManager.Panels;

public class EmailRepliesPanel : UserControl
{
    private ListView _listView = null!;
    private WebView2 _webView = null!;
    private SplitContainer _mainSplit = null!;
    private Button _btnRefresh = null!;
    private Button _btnArchive = null!;
    private Label _lblStatus = null!;
    private bool _webViewReady;
    private string? _pendingNavHtml;
    private const int PreferredSplitDistance = 420;

    private List<InboxMessage> _messages = [];
    private InboxMessage? _selected;

    public EmailRepliesPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;
        BuildUI();
        Load += async (_, _) => { await InitWebViewAsync(); await RefreshAsync(); };
    }

    private void BuildUI()
    {
        var toolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 48,
            BackColor = AppTheme.Surface,
            Padding = new Padding(8, 8, 8, 8)
        };

        _btnRefresh = MakeButton("Refresh", AppTheme.Accent);
        _btnArchive = MakeButton("Archive", Color.FromArgb(100, 116, 139));
        _btnArchive.Enabled = false;

        _btnRefresh.Click += async (_, _) => await RefreshAsync();
        _btnArchive.Click += async (_, _) => await OnArchiveAsync();

        int x = 0;
        foreach (var btn in new[] { _btnRefresh, _btnArchive })
        {
            btn.Location = new Point(x, 8);
            toolbar.Controls.Add(btn);
            x += btn.Width + 8;
        }

        _lblStatus = new Label
        {
            AutoSize = true, Font = AppTheme.FontSmall, ForeColor = AppTheme.TextMuted,
            Location = new Point(x + 8, 14)
        };
        toolbar.Controls.Add(_lblStatus);

        _mainSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Panel1MinSize = 0,
            Panel2MinSize = 0,
            BackColor = AppTheme.Separator
        };
        _mainSplit.SizeChanged += (_, _) => SafeApplySplitDistance();
        _mainSplit.HandleCreated += (_, _) => BeginInvoke(new Action(SafeApplySplitDistance));

        _listView = new ListView
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
        _listView.Columns.Add("From", 150);
        _listView.Columns.Add("Subject", 220);
        _listView.Columns.Add("Date", 110);
        _listView.SelectedIndexChanged += OnSelectionChanged;
        _mainSplit.Panel1.Controls.Add(_listView);

        _webView = new WebView2 { Dock = DockStyle.Fill };
        _mainSplit.Panel2.Controls.Add(_webView);

        Controls.Add(_mainSplit);
        Controls.Add(toolbar);
    }

    private void SafeApplySplitDistance()
    {
        if (_mainSplit.Width <= 1) return;

        const int desiredLeftMin = 330;
        const int desiredRightMin = 320;
        int maxTotalMin = Math.Max(0, _mainSplit.Width - 1);

        int leftMin = desiredLeftMin;
        int rightMin = desiredRightMin;
        if (leftMin + rightMin > maxTotalMin)
        {
            if (maxTotalMin == 0) { leftMin = 0; rightMin = 0; }
            else
            {
                double leftRatio = desiredLeftMin / (double)(desiredLeftMin + desiredRightMin);
                leftMin = (int)Math.Floor(maxTotalMin * leftRatio);
                rightMin = maxTotalMin - leftMin;
            }
        }

        _mainSplit.Panel1MinSize = leftMin;
        _mainSplit.Panel2MinSize = rightMin;

        int minLeft = _mainSplit.Panel1MinSize;
        int maxLeft = _mainSplit.Width - _mainSplit.Panel2MinSize;
        if (maxLeft < minLeft) maxLeft = minLeft;

        int clamped = Math.Min(PreferredSplitDistance, maxLeft);
        clamped = Math.Max(minLeft, clamped);

        if (clamped > 0) _mainSplit.SplitterDistance = clamped;
    }

    private static Button MakeButton(string text, Color back) => new()
    {
        Text = text,
        AutoSize = true,
        Padding = new Padding(12, 0, 12, 0),
        Height = 30,
        FlatStyle = FlatStyle.Flat,
        BackColor = back,
        ForeColor = Color.White,
        Font = AppTheme.FontButton,
        Cursor = Cursors.Hand
    };

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

    private async Task RefreshAsync()
    {
        if (!GmailAuthService.IsConnected())
        {
            _lblStatus.Text = "Connect Gmail in Parameters to view replies.";
            _listView.Items.Clear();
            return;
        }

        _btnRefresh.Enabled = false;
        _lblStatus.Text = "Loading...";
        try
        {
            var credential = await GmailAuthService.GetCredentialAsync();
            _messages = await GmailReadService.ListInboxAsync(credential);

            _listView.Items.Clear();
            foreach (var m in _messages)
            {
                var from = m.PlayerName != null ? $"{m.PlayerName} <{m.FromEmail}>" : m.FromDisplay;
                var item = new ListViewItem(from);
                item.SubItems.Add(m.Subject);
                item.SubItems.Add(m.Date == DateTime.MinValue ? "" : m.Date.ToString("MM/dd/yy h:mm tt"));
                item.Tag = m;
                if (m.IsUnread) item.Font = AppTheme.FontDefaultBold;
                _listView.Items.Add(item);
            }
            _lblStatus.Text = $"{_messages.Count} message(s).";
        }
        catch (Exception ex)
        {
            AppLogger.Error(ex, "Failed to load Gmail inbox");
            _lblStatus.Text = "Could not load inbox.";
            MessageBox.Show($"Could not load Gmail inbox:\n\n{ex.Message}", "Email Replies", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnRefresh.Enabled = true;
        }
    }

    private async void OnSelectionChanged(object? sender, EventArgs e)
    {
        if (_listView.SelectedItems.Count == 0)
        {
            _selected = null;
            _btnArchive.Enabled = false;
            return;
        }

        var msg = (InboxMessage)_listView.SelectedItems[0].Tag!;
        _selected = msg;
        _btnArchive.Enabled = true;

        var header = $"""
            <div style="font-family:Segoe UI,Arial,sans-serif;border-bottom:1px solid #ddd;padding-bottom:8px;margin-bottom:12px;">
            <div style="font-size:14px;font-weight:bold;">{System.Net.WebUtility.HtmlEncode(msg.Subject)}</div>
            <div style="font-size:12px;color:#666;">From: {System.Net.WebUtility.HtmlEncode(msg.PlayerName != null ? $"{msg.PlayerName} <{msg.FromEmail}>" : msg.FromDisplay)}</div>
            <div style="font-size:12px;color:#666;">{msg.Date:dddd, MMM d, yyyy h:mm tt}</div>
            </div>
            """;
        NavigateWebView($"<html><body style=\"padding:16px;\">{header}<p>Loading message…</p></body></html>");

        try
        {
            var credential = await GmailAuthService.GetCredentialAsync();
            var bodyHtml = await GmailReadService.GetMessageBodyHtmlAsync(credential, msg.Id);
            if (_selected?.Id != msg.Id) return; // selection changed while loading

            NavigateWebView($"<html><body style=\"padding:16px;\">{header}{bodyHtml}</body></html>");

            if (msg.IsUnread)
            {
                await GmailReadService.MarkAsReadAsync(credential, msg.Id);
                msg = msg with { IsUnread = false };
                var idx = _messages.FindIndex(m => m.Id == msg.Id);
                if (idx >= 0) _messages[idx] = msg;
                foreach (ListViewItem item in _listView.Items)
                {
                    if (item.Tag is InboxMessage im && im.Id == msg.Id)
                    {
                        item.Tag = msg;
                        item.Font = AppTheme.FontDefault;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error(ex, "Failed to load Gmail message body");
            NavigateWebView($"<html><body style=\"padding:16px;\">{header}<p style=\"color:red;\">Could not load message: {System.Net.WebUtility.HtmlEncode(ex.Message)}</p></body></html>");
        }
    }

    private async Task OnArchiveAsync()
    {
        if (_selected == null) return;
        var msg = _selected;

        try
        {
            var credential = await GmailAuthService.GetCredentialAsync();
            await GmailReadService.ArchiveAsync(credential, msg.Id);
            _messages.RemoveAll(m => m.Id == msg.Id);
            foreach (ListViewItem item in _listView.Items)
            {
                if (item.Tag is InboxMessage im && im.Id == msg.Id)
                {
                    _listView.Items.Remove(item);
                    break;
                }
            }
            _selected = null;
            _btnArchive.Enabled = false;
            NavigateWebView("<html><body></body></html>");
            _lblStatus.Text = $"{_messages.Count} message(s).";
        }
        catch (Exception ex)
        {
            AppLogger.Error(ex, "Failed to archive Gmail message");
            MessageBox.Show($"Could not archive message:\n\n{ex.Message}", "Email Replies", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

using System.Text.Json;
using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.Services;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;
using Microsoft.Web.WebView2.WinForms;

namespace BocceManager.Panels;

public class EmailComposePanel : UserControl
{
    private sealed record ComboItem(int Id, string Text)
    {
        public override string ToString() => Text;
    }

    // Gmail's per-account daily sending cap is ~500 (regular) / ~2000 (Workspace); warn
    // before a send would push the day's total past this conservative shared floor.
    private const int DailySendWarningThreshold = 480;

    private ComboBox _cmbScope = null!;
    private ComboBox _cmbTarget = null!;
    private Label _lblRecipientCount = null!;
    private TextBox _txtSubject = null!;
    private WebView2 _webView = null!;
    private Button _btnSend = null!;
    private Label _lblStatus = null!;
    private bool _webViewReady;

    private List<EmailRecipient> _currentRecipients = [];

    public EmailComposePanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;
        BuildUI();
        Load += async (_, _) => await InitWebViewAsync();
        LoadTargets();
    }

    private void BuildUI()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, BackColor = AppTheme.ContentBackground };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));

        // -- Row 1: recipient scope --
        var scopeRow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(8, 6, 8, 0) };
        scopeRow.Controls.Add(new Label { Text = "Send To:", AutoSize = true, ForeColor = AppTheme.TextSecondary, Font = AppTheme.FontDefault, Margin = new Padding(0, 8, 6, 0) });

        _cmbScope = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150, Font = AppTheme.FontDefault };
        _cmbScope.Items.AddRange(["All Members", "Division", "Team", "Email List", "Individual"]);
        _cmbScope.SelectedIndex = 0;
        _cmbScope.SelectedIndexChanged += (_, _) => { LoadTargets(); RefreshRecipientPreview(); };
        scopeRow.Controls.Add(_cmbScope);

        _cmbTarget = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDown,
            AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            AutoCompleteSource = AutoCompleteSource.ListItems,
            Width = 260,
            Font = AppTheme.FontDefault,
            Margin = new Padding(8, 0, 0, 0)
        };
        _cmbTarget.SelectedIndexChanged += (_, _) => RefreshRecipientPreview();
        scopeRow.Controls.Add(_cmbTarget);

        _lblRecipientCount = new Label { Text = "0 recipient(s)", AutoSize = true, ForeColor = AppTheme.TextMuted, Font = AppTheme.FontDefault, Margin = new Padding(16, 8, 0, 0) };
        scopeRow.Controls.Add(_lblRecipientCount);
        layout.Controls.Add(scopeRow, 0, 0);

        // -- Row 2: subject --
        var subjectRow = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 4, 8, 4) };
        _txtSubject = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.FontDefault,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            PlaceholderText = "Subject (supports {FirstName}, {LastName}, {TeamName}, {Email})"
        };
        subjectRow.Controls.Add(_txtSubject);
        layout.Controls.Add(subjectRow, 0, 1);

        // -- Row 3: merge field insert buttons --
        var mergeRow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(8, 0, 8, 0) };
        mergeRow.Controls.Add(new Label { Text = "Insert into body:", AutoSize = true, ForeColor = AppTheme.TextMuted, Font = AppTheme.FontSmall, Margin = new Padding(0, 6, 6, 0) });
        foreach (var field in new[] { "{FirstName}", "{LastName}", "{TeamName}", "{Email}" })
        {
            var btn = MakeButton(field, Color.FromArgb(100, 116, 139));
            btn.Click += async (_, _) => await InsertMergeFieldAsync(field);
            mergeRow.Controls.Add(btn);
        }
        layout.Controls.Add(mergeRow, 0, 2);

        // -- Row 4: body editor --
        var bodyPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 8, 0) };
        _webView = new WebView2 { Dock = DockStyle.Fill };
        bodyPanel.Controls.Add(_webView);
        layout.Controls.Add(bodyPanel, 0, 3);

        // -- Row 5: send --
        var sendRow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(8) };
        _btnSend = MakeButton("Send", AppTheme.ButtonSuccess);
        _btnSend.Click += OnSend;
        sendRow.Controls.Add(_btnSend);
        _lblStatus = new Label { AutoSize = true, ForeColor = AppTheme.TextMuted, Font = AppTheme.FontDefault, Margin = new Padding(12, 8, 0, 0) };
        sendRow.Controls.Add(_lblStatus);
        layout.Controls.Add(sendRow, 0, 4);

        Controls.Add(layout);
    }

    private static Button MakeButton(string text, Color back) => new()
    {
        Text = text,
        AutoSize = true,
        Padding = new Padding(10, 4, 10, 4),
        FlatStyle = FlatStyle.Flat,
        BackColor = back,
        ForeColor = Color.White,
        Font = AppTheme.FontButton,
        Cursor = Cursors.Hand,
        Margin = new Padding(0, 0, 6, 0),
        FlatAppearance = { BorderSize = 0 }
    };

    private async Task InitWebViewAsync()
    {
        try
        {
            await _webView.EnsureCoreWebView2Async();
            var editorPath = Path.Combine(AppContext.BaseDirectory, "Resources", "EmailEditor", "editor.html");
            _webView.CoreWebView2.NavigationCompleted += (_, _) => _webViewReady = true;
            _webView.CoreWebView2.Navigate(new Uri(editorPath).AbsoluteUri);
        }
        catch (Exception ex)
        {
            AppLogger.Error(ex, "Failed to initialize compose body editor");
        }
    }

    private async Task InsertMergeFieldAsync(string field)
    {
        if (!_webViewReady) return;
        await _webView.CoreWebView2.ExecuteScriptAsync($"window.insertTextAtCursor('{field}')");
    }

    private void LoadTargets()
    {
        _cmbTarget.Items.Clear();
        _cmbTarget.Enabled = _cmbScope.SelectedIndex != 0; // not "All Members"

        try
        {
            using var db = new BocceDbContext();
            var leagueId = AppParameterService.GetDefaultLeagueId(db);
            var seasonId = AppParameterService.GetDefaultSeasonId(db);
            if (!seasonId.HasValue) return;

            switch (_cmbScope.SelectedIndex)
            {
                case 1: // Division
                    foreach (var d in db.Divisions.Where(d => d.SeasonId == seasonId.Value && d.IsActive).OrderBy(d => d.SortName))
                        _cmbTarget.Items.Add(new ComboItem(d.Id, d.Name));
                    break;

                case 2: // Team
                    var teams = db.Teams.Include(t => t.Division)
                        .Where(t => t.Division.SeasonId == seasonId.Value && t.IsActive)
                        .OrderBy(t => t.SortOrder)
                        .ToList();
                    foreach (var t in teams)
                        _cmbTarget.Items.Add(new ComboItem(t.Id, $"{t.Division.ShortName} - {t.EffectiveDisplayName}"));
                    break;

                case 3: // Email List
                    if (leagueId.HasValue)
                        foreach (var l in db.EmailLists.Where(l => l.LeagueId == leagueId.Value && l.IsActive).OrderBy(l => l.Name))
                            _cmbTarget.Items.Add(new ComboItem(l.Id, l.Name));
                    break;

                case 4: // Individual
                    foreach (var p in db.Players.Where(p => p.IsActive).OrderBy(p => p.LastName).ThenBy(p => p.FirstName))
                        _cmbTarget.Items.Add(new ComboItem(p.Id, $"{p.LastName}, {p.FirstName}"));
                    break;
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error(ex, "Failed to load compose recipient targets");
        }

        if (_cmbTarget.Items.Count > 0) _cmbTarget.SelectedIndex = 0;
    }

    private (EmailRecipientScope Scope, int? ScopeId)? GetCurrentScope() => _cmbScope.SelectedIndex switch
    {
        0 => (EmailRecipientScope.AllMembers, (int?)null),
        1 when _cmbTarget.SelectedItem is ComboItem d => (EmailRecipientScope.Division, d.Id),
        2 when _cmbTarget.SelectedItem is ComboItem t => (EmailRecipientScope.Team, t.Id),
        3 when _cmbTarget.SelectedItem is ComboItem l => (EmailRecipientScope.EmailList, l.Id),
        4 when _cmbTarget.SelectedItem is ComboItem p => (EmailRecipientScope.Individual, p.Id),
        _ => null
    };

    private void RefreshRecipientPreview()
    {
        try
        {
            using var db = new BocceDbContext();
            var seasonId = AppParameterService.GetDefaultSeasonId(db);
            var scope = GetCurrentScope();
            if (!seasonId.HasValue || scope == null)
            {
                _currentRecipients = [];
            }
            else
            {
                _currentRecipients = EmailRecipientService.GetRecipients(db, scope.Value.Scope, scope.Value.ScopeId, seasonId.Value);
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error(ex, "Failed to compute recipient preview");
            _currentRecipients = [];
        }

        _lblRecipientCount.Text = $"{_currentRecipients.Count} recipient(s)";
    }

    private async void OnSend(object? sender, EventArgs e)
    {
        if (!GmailAuthService.IsConnected())
        {
            MessageBox.Show("Connect a Gmail account in Parameters before sending email.", "Not Connected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        RefreshRecipientPreview();
        if (_currentRecipients.Count == 0)
        {
            MessageBox.Show("No recipients match the current selection.", "Compose Email", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var subjectTemplate = _txtSubject.Text.Trim();
        if (subjectTemplate.Length == 0)
        {
            MessageBox.Show("Enter a subject before sending.", "Compose Email", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!_webViewReady)
        {
            MessageBox.Show("The email editor is still loading — try again in a moment.", "Compose Email", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (MessageBox.Show($"Send this email to {_currentRecipients.Count} recipient(s)?", "Confirm Send",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        int leagueId;
        using (var db = new BocceDbContext())
        {
            var today = DateTime.UtcNow.Date;
            var sentToday = db.EmailLogRecipients.Count(r => r.Status == "Sent" && r.SentAt.Date == today);
            if (sentToday + _currentRecipients.Count > DailySendWarningThreshold)
            {
                if (MessageBox.Show(
                        $"This send would bring today's total to {sentToday + _currentRecipients.Count}, close to Gmail's daily sending limit. Continue anyway?",
                        "Approaching Daily Send Limit", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;
            }

            leagueId = AppParameterService.GetDefaultLeagueId(db) ?? 0;
        }

        var bodyTemplateRaw = await _webView.CoreWebView2.ExecuteScriptAsync("window.getBody()");
        var bodyTemplate = JsonSerializer.Deserialize<string>(bodyTemplateRaw) ?? "";

        _btnSend.Enabled = false;
        try
        {
            var credential = await GmailAuthService.GetCredentialAsync();
            var fromEmail = await GmailAuthService.GetConnectedAccountAsync(credential);

            int sent = 0, failed = 0;
            var results = new List<(EmailRecipient Recipient, bool Success, string? Error)>();

            for (int i = 0; i < _currentRecipients.Count; i++)
            {
                var recipient = _currentRecipients[i];
                _lblStatus.Text = $"Sending {i + 1}/{_currentRecipients.Count}...";

                try
                {
                    var player = new Player { FirstName = recipient.FirstName, LastName = recipient.LastName, Email = recipient.Email };
                    var subject = ApplyMerge(subjectTemplate, player, recipient.TeamName);
                    var body = ApplyMerge(bodyTemplate, player, recipient.TeamName);

                    await GmailSendService.SendAsync(credential, fromEmail, recipient.Email, subject, body);
                    results.Add((recipient, true, null));
                    sent++;
                }
                catch (Exception ex)
                {
                    AppLogger.Error(ex, "Failed to send email to {Email}", recipient.Email);
                    results.Add((recipient, false, ex.Message));
                    failed++;
                }

                await Task.Delay(250);
            }

            using (var db = new BocceDbContext())
            {
                var log = new EmailLog
                {
                    LeagueId = leagueId == 0 ? null : leagueId,
                    Subject = subjectTemplate,
                    Body = bodyTemplate,
                    RecipientCount = sent,
                    SentBy = Environment.UserName,
                    SentAt = DateTime.UtcNow
                };
                db.EmailLogs.Add(log);
                db.SaveChanges();

                foreach (var (recipient, success, error) in results)
                {
                    db.EmailLogRecipients.Add(new EmailLogRecipient
                    {
                        EmailLogId = log.Id,
                        PlayerId = recipient.PlayerId,
                        Email = recipient.Email,
                        Status = success ? "Sent" : "Failed",
                        ErrorMessage = error,
                        SentAt = DateTime.UtcNow
                    });
                }
                db.SaveChanges();
            }

            _lblStatus.Text = "";
            MessageBox.Show($"Sent: {sent}    Failed: {failed}", "Send Complete", MessageBoxButtons.OK,
                failed > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            AppLogger.Error(ex, "Compose send failed");
            MessageBox.Show($"Could not send email:\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnSend.Enabled = true;
            _lblStatus.Text = "";
        }
    }

    private static string ApplyMerge(string template, Player player, string? teamName) =>
        MergeFieldService.Apply(template, player, teamName);
}

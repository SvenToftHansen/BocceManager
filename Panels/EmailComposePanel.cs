using System.Text.Json;
using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.Services;
using BocceManager.UI.Controls;
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
    private TextBox _txtManualEmails = null!;
    private Button _btnSelectIndividuals = null!;
    private Label _lblSelectedIndividuals = null!;
    private Label _lblRecipientCount = null!;
    private CheckBox _chkUniqueEmail = null!;
    private TextBox _txtSubject = null!;
    private WebView2 _webView = null!;
    private FlowLayoutPanel _attachmentsPanel = null!;
    private Button _btnSend = null!;
    private Button _btnPreview = null!;
    private Label _lblStatus = null!;
    private bool _webViewReady;

    private List<EmailRecipient> _currentRecipients = [];
    private readonly List<(string FileName, byte[] Bytes, string MimeType)> _attachments = [];
    private readonly HashSet<int> _selectedIndividualIds = [];

    private FlowLayoutPanel _mergeRow = null!;
    private bool _inPreviewMode;
    private string? _preservedBodyHtml;
    private TaskCompletionSource? _pendingNavigation;

    public EmailComposePanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;
        BuildUI();
        Load += async (_, _) => await InitWebViewAsync();
        UpdateScopeControlsVisibility();
        LoadTargets();
    }

    private void BuildUI()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, BackColor = AppTheme.ContentBackground };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));

        // -- Row 1: recipient scope --
        var scopeRow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(8, 6, 8, 0) };
        scopeRow.Controls.Add(new Label { Text = "Send To:", AutoSize = true, ForeColor = AppTheme.TextSecondary, Font = AppTheme.FontDefault, Margin = new Padding(0, 8, 6, 0) });

        _cmbScope = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 170, Font = AppTheme.FontDefault };
        _cmbScope.Items.AddRange([
            "All Active Players", "Players on Teams", "Division", "Team",
            "Players on Spare List", "Unpaid Players", "Email List", "Individual(s)", "Manual Email Address(es)"
        ]);
        _cmbScope.SelectedIndex = 0;
        _cmbScope.SelectedIndexChanged += (_, _) => { UpdateScopeControlsVisibility(); LoadTargets(); RefreshRecipientPreview(); };
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

        _txtManualEmails = new TextBox
        {
            Width = 320,
            Font = AppTheme.FontDefault,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            PlaceholderText = "name@example.com;other@example.com",
            Margin = new Padding(8, 0, 0, 0),
            Visible = false
        };
        _txtManualEmails.TextChanged += (_, _) => RefreshRecipientPreview();
        scopeRow.Controls.Add(_txtManualEmails);

        _btnSelectIndividuals = MakeButton("Select Players...", Color.FromArgb(100, 116, 139));
        _btnSelectIndividuals.Margin = new Padding(8, 0, 0, 0);
        _btnSelectIndividuals.Click += OnSelectIndividuals;
        scopeRow.Controls.Add(_btnSelectIndividuals);

        _lblSelectedIndividuals = new Label { Text = "No players selected", AutoSize = true, ForeColor = AppTheme.TextMuted, Font = AppTheme.FontDefault, Margin = new Padding(8, 8, 0, 0) };
        scopeRow.Controls.Add(_lblSelectedIndividuals);

        _lblRecipientCount = new Label { Text = "0 recipient(s)", AutoSize = true, ForeColor = AppTheme.TextMuted, Font = AppTheme.FontDefault, Margin = new Padding(16, 8, 0, 0) };
        scopeRow.Controls.Add(_lblRecipientCount);

        _chkUniqueEmail = new CheckBox
        {
            Text = "Unique email addresses only", Checked = true, AutoSize = true,
            ForeColor = AppTheme.TextSecondary, Font = AppTheme.FontDefault, Margin = new Padding(16, 8, 0, 0)
        };
        _chkUniqueEmail.CheckedChanged += (_, _) => RefreshRecipientPreview();
        scopeRow.Controls.Add(_chkUniqueEmail);
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

        // -- Row 3: merge field insert buttons + insert report --
        _mergeRow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(8, 0, 8, 0) };
        _mergeRow.Controls.Add(new Label { Text = "Insert into body:", AutoSize = true, ForeColor = AppTheme.TextMuted, Font = AppTheme.FontSmall, Margin = new Padding(0, 6, 6, 0) });
        foreach (var field in new[] { "{FirstName}", "{LastName}", "{TeamName}", "{Email}" })
        {
            var btn = MakeButton(field, Color.FromArgb(100, 116, 139));
            btn.Click += async (_, _) => await InsertMergeFieldAsync(field);
            _mergeRow.Controls.Add(btn);
        }
        var btnInsertReport = MakeButton("Insert Report...", AppTheme.Accent);
        btnInsertReport.Margin = new Padding(16, 0, 6, 0);
        btnInsertReport.Click += OnInsertReport;
        _mergeRow.Controls.Add(btnInsertReport);
        var btnAttachDocument = MakeButton("Attach Document...", AppTheme.Accent);
        btnAttachDocument.Click += OnAttachDocument;
        _mergeRow.Controls.Add(btnAttachDocument);
        var btnClearContent = MakeButton("Clear Content", AppTheme.ButtonDanger);
        btnClearContent.Margin = new Padding(16, 0, 6, 0);
        btnClearContent.Click += OnClearContent;
        _mergeRow.Controls.Add(btnClearContent);
        layout.Controls.Add(_mergeRow, 0, 2);

        // -- Row 4: attachment chips --
        _attachmentsPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(8, 2, 8, 2), AutoScroll = true };
        layout.Controls.Add(_attachmentsPanel, 0, 3);

        // -- Row 5: body editor --
        var bodyPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 8, 0) };
        _webView = new WebView2 { Dock = DockStyle.Fill };
        bodyPanel.Controls.Add(_webView);
        layout.Controls.Add(bodyPanel, 0, 4);

        // -- Row 6: send --
        var sendRow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(8) };
        _btnSend = MakeButton("Send", AppTheme.ButtonSuccess);
        _btnSend.Click += OnSend;
        sendRow.Controls.Add(_btnSend);
        _btnPreview = MakeButton("Preview", Color.FromArgb(100, 116, 139));
        _btnPreview.Click += OnPreview;
        sendRow.Controls.Add(_btnPreview);
        _lblStatus = new Label { AutoSize = true, ForeColor = AppTheme.TextMuted, Font = AppTheme.FontDefault, Margin = new Padding(12, 8, 0, 0) };
        sendRow.Controls.Add(_lblStatus);
        layout.Controls.Add(sendRow, 0, 5);

        var tabs = new TabControl { Dock = DockStyle.Fill, Font = AppTheme.FontDefault };

        var composeTab = new TabPage("Compose") { BackColor = AppTheme.ContentBackground };
        composeTab.Controls.Add(layout);
        tabs.TabPages.Add(composeTab);

        var repliesTab = new TabPage("Replies") { BackColor = AppTheme.ContentBackground };
        repliesTab.Controls.Add(new EmailRepliesPanel());
        tabs.TabPages.Add(repliesTab);

        Controls.Add(tabs);
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
            _webView.CoreWebView2.NavigationCompleted += (_, _) =>
            {
                _webViewReady = true;
                _pendingNavigation?.TrySetResult();
                _pendingNavigation = null;
            };
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

    private async void OnInsertReport(object? sender, EventArgs e)
    {
        if (!_webViewReady)
        {
            MessageBox.Show("The email editor is still loading — try again in a moment.", "Insert Report", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var report = ShowReportPickerDialog();
        if (report == null) return;

        int? leagueId;
        int? seasonId;
        using (var db = new BocceDbContext())
        {
            leagueId = AppParameterService.GetDefaultLeagueId(db);
            seasonId = AppParameterService.GetDefaultSeasonId(db);
        }

        if (!leagueId.HasValue)
        {
            MessageBox.Show("Select a default league before inserting a report.", "Insert Report", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var html = ReportAttachmentService.BuildHtmlContent(report.Value, leagueId.Value, seasonId);
            await _webView.CoreWebView2.ExecuteScriptAsync($"window.insertHtmlAtCursor({JsonSerializer.Serialize(html)})");
        }
        catch (Exception ex)
        {
            AppLogger.Error(ex, "Failed to insert report into compose email");
            MessageBox.Show($"Could not build report:\n\n{ex.Message}", "Insert Report", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void OnClearContent(object? sender, EventArgs e)
    {
        if (!_webViewReady)
        {
            MessageBox.Show("The email editor is still loading — try again in a moment.", "Clear Content", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (MessageBox.Show("Clear all content from the email body?", "Clear Content", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        await _webView.CoreWebView2.ExecuteScriptAsync("window.setBody('')");
    }

    private void OnAttachDocument(object? sender, EventArgs e)
    {
        List<ClubDocument> docs;
        try
        {
            using var db = new BocceDbContext();
            docs = db.ClubDocuments
                .Where(d => d.DocType == "pdf" || d.DocType == "docx")
                .OrderBy(d => d.Title)
                .ToList();
        }
        catch (Exception ex)
        {
            AppLogger.Error(ex, "Failed to load documents for attachment picker");
            MessageBox.Show($"Could not load documents:\n\n{ex.Message}", "Attach Document", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (docs.Count == 0)
        {
            MessageBox.Show("No PDF or Word documents are available in the Documents folder.", "Attach Document", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new Form
        {
            Text = "Attach Document",
            Width = 420,
            Height = 480,
            StartPosition = FormStartPosition.CenterParent,
            BackColor = AppTheme.ContentBackground,
            Font = AppTheme.FontDefault,
            MinimizeBox = false,
            MaximizeBox = false
        };

        var clb = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true, DisplayMember = "Title" };
        foreach (var doc in docs)
            clb.Items.Add(doc, _attachments.Any(a => a.FileName == doc.FileName));

        var btnOk = new Button { Text = "Attach", DialogResult = DialogResult.OK, Dock = DockStyle.Bottom, Height = 32 };

        dialog.Controls.Add(clb);
        dialog.Controls.Add(btnOk);
        dialog.AcceptButton = btnOk;

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        foreach (var checkedItem in clb.CheckedItems.Cast<ClubDocument>())
        {
            if (_attachments.Any(a => a.FileName == checkedItem.FileName)) continue;

            try
            {
                var bytes = File.ReadAllBytes(DocumentService.GetFilePath(checkedItem));
                var mimeType = checkedItem.DocType == "pdf"
                    ? "application/pdf"
                    : "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                _attachments.Add((checkedItem.FileName, bytes, mimeType));
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Failed to attach document {FileName}", checkedItem.FileName);
                MessageBox.Show($"Could not attach \"{checkedItem.Title}\":\n\n{ex.Message}", "Attach Document", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        RefreshAttachmentChips();
    }

    private AttachableReport? ShowReportPickerDialog()
    {
        var options = ReportAttachmentService.GetOptions();

        using var dialog = new Form
        {
            Text = "Insert Report",
            Width = 380,
            Height = 152,
            StartPosition = FormStartPosition.CenterParent,
            BackColor = AppTheme.ContentBackground,
            Font = AppTheme.FontDefault,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var lblReport = new Label { Text = "Report:", Location = new Point(10, 15), Size = new Size(100, 20), ForeColor = AppTheme.TextPrimary };
        var cmbReport = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(10, 38), Size = new Size(340, 26) };
        foreach (var opt in options) cmbReport.Items.Add(opt);
        cmbReport.SelectedIndex = 0;

        var btnOk = new Button { Text = "Insert", Location = new Point(174, 78), Size = new Size(75, 28), DialogResult = DialogResult.OK };
        var btnCancel = new Button { Text = "Cancel", Location = new Point(255, 78), Size = new Size(75, 28), DialogResult = DialogResult.Cancel };

        dialog.Controls.AddRange([lblReport, cmbReport, btnOk, btnCancel]);
        dialog.AcceptButton = btnOk;
        dialog.CancelButton = btnCancel;

        if (dialog.ShowDialog(this) != DialogResult.OK) return null;

        return ((AttachableReportOption)cmbReport.SelectedItem!).Report;
    }

    private void RefreshAttachmentChips()
    {
        _attachmentsPanel.Controls.Clear();
        foreach (var (fileName, _, _) in _attachments)
        {
            var chip = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.FromArgb(230, 230, 235),
                Padding = new Padding(6, 3, 2, 3),
                Margin = new Padding(0, 2, 6, 2)
            };
            chip.Controls.Add(new Label { Text = fileName, AutoSize = true, Font = AppTheme.FontSmall, ForeColor = AppTheme.TextPrimary, Margin = new Padding(0, 3, 4, 0) });
            var btnRemove = new Button
            {
                Text = "✕", Width = 20, Height = 20, FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent, ForeColor = AppTheme.TextMuted, Cursor = Cursors.Hand,
                Margin = new Padding(0)
            };
            btnRemove.FlatAppearance.BorderSize = 0;
            var capturedName = fileName;
            btnRemove.Click += (_, _) =>
            {
                _attachments.RemoveAll(a => a.FileName == capturedName);
                RefreshAttachmentChips();
            };
            chip.Controls.Add(btnRemove);
            _attachmentsPanel.Controls.Add(chip);
        }
    }

    // Preview reuses the compose editor's own WebView2 instead of spinning up a second one —
    // creating a second CoreWebView2 controller in this app reliably threw E_ABORT (WebView2
    // controller creation appears to be unreliable for a second control in this process/host),
    // so instead of fighting that, Preview temporarily navigates the same control to a
    // rendered, read-only version of the email and back.
    private async void OnPreview(object? sender, EventArgs e)
    {
        if (_inPreviewMode)
        {
            await ExitPreviewModeAsync();
            return;
        }

        if (!_webViewReady)
        {
            MessageBox.Show("The email editor is still loading — try again in a moment.", "Preview", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var bodyRaw = await _webView.CoreWebView2.ExecuteScriptAsync("window.getBody()");
        var bodyTemplate = JsonSerializer.Deserialize<string>(bodyRaw) ?? "";
        var subjectTemplate = _txtSubject.Text.Trim();

        Player samplePlayer;
        string? sampleTeam;
        if (_currentRecipients.Count > 0)
        {
            var r = _currentRecipients[0];
            samplePlayer = new Player { FirstName = r.FirstName, LastName = r.LastName, Email = r.Email };
            sampleTeam = r.TeamName;
        }
        else
        {
            samplePlayer = new Player { FirstName = "Jane", LastName = "Doe", Email = "jane@example.com" };
            sampleTeam = "Sample Team";
        }

        var subject = MergeFieldService.Apply(subjectTemplate, samplePlayer, sampleTeam);
        var mergedBody = MergeFieldService.Apply(bodyTemplate, samplePlayer, sampleTeam);

        var attachmentNote = _attachments.Count > 0
            ? $"<p style=\"color:#666;font-size:12px;\">Attachments: {string.Join(", ", _attachments.Select(a => System.Net.WebUtility.HtmlEncode(a.FileName)))}</p>"
            : "";

        var html = $"""
            <html><body style="font-family:'Segoe UI',sans-serif;padding:16px;">
            <p style="color:#b45309;font-size:12px;margin-top:0;">Preview — click "Close Preview" to keep editing.</p>
            <h3 style="border-bottom:1px solid #ccc;padding-bottom:8px;">{System.Net.WebUtility.HtmlEncode(subject)}</h3>
            <div>{mergedBody}</div>
            {attachmentNote}
            </body></html>
            """;

        _preservedBodyHtml = bodyTemplate;
        _inPreviewMode = true;
        _btnPreview.Text = "Close Preview";
        _txtSubject.Enabled = false;
        _btnSend.Enabled = false;
        _mergeRow.Enabled = false;

        await NavigateAndWaitAsync(html: html);
    }

    private async Task ExitPreviewModeAsync()
    {
        _inPreviewMode = false;
        _btnPreview.Text = "Preview";
        _txtSubject.Enabled = true;
        _btnSend.Enabled = true;
        _mergeRow.Enabled = true;

        var editorPath = Path.Combine(AppContext.BaseDirectory, "Resources", "EmailEditor", "editor.html");
        await NavigateAndWaitAsync(uri: new Uri(editorPath).AbsoluteUri);

        if (_preservedBodyHtml != null)
            await _webView.CoreWebView2.ExecuteScriptAsync($"window.setBody({JsonSerializer.Serialize(_preservedBodyHtml)})");
    }

    private Task NavigateAndWaitAsync(string? uri = null, string? html = null)
    {
        _pendingNavigation = new TaskCompletionSource();
        if (html != null) _webView.CoreWebView2.NavigateToString(html);
        else _webView.CoreWebView2.Navigate(uri!);
        return _pendingNavigation.Task;
    }

    // Scope indices: 0 All Active Players, 1 Players on Teams, 2 Division, 3 Team,
    // 4 Players on Spare List, 5 Unpaid Players, 6 Email List, 7 Individual(s), 8 Manual Email Address(es).
    private void UpdateScopeControlsVisibility()
    {
        int idx = _cmbScope.SelectedIndex;
        bool needsTarget = idx is 2 or 3 or 6;
        bool isIndividual = idx == 7;
        bool isManual = idx == 8;

        _cmbTarget.Visible = needsTarget;
        _btnSelectIndividuals.Visible = isIndividual;
        _lblSelectedIndividuals.Visible = isIndividual;
        _txtManualEmails.Visible = isManual;
    }

    private void LoadTargets()
    {
        _cmbTarget.Items.Clear();

        try
        {
            using var db = new BocceDbContext();
            var leagueId = AppParameterService.GetDefaultLeagueId(db);
            var seasonId = AppParameterService.GetDefaultSeasonId(db);
            if (!seasonId.HasValue) return;

            switch (_cmbScope.SelectedIndex)
            {
                case 2: // Division
                    foreach (var d in db.Divisions.Where(d => d.SeasonId == seasonId.Value && d.IsActive).OrderBy(d => d.SortName))
                        _cmbTarget.Items.Add(new ComboItem(d.Id, d.Name));
                    break;

                case 3: // Team
                    var teams = db.Teams.Include(t => t.Division)
                        .Where(t => t.Division.SeasonId == seasonId.Value && t.IsActive)
                        .OrderBy(t => t.SortOrder)
                        .ToList();
                    foreach (var t in teams)
                        _cmbTarget.Items.Add(new ComboItem(t.Id, $"{t.Division.ShortName} - {t.EffectiveDisplayName}"));
                    break;

                case 6: // Email List
                    if (leagueId.HasValue)
                        foreach (var l in db.EmailLists.Where(l => l.LeagueId == leagueId.Value && l.IsActive).OrderBy(l => l.Name))
                            _cmbTarget.Items.Add(new ComboItem(l.Id, l.Name));
                    break;
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error(ex, "Failed to load compose recipient targets");
        }

        if (_cmbTarget.Items.Count > 0) _cmbTarget.SelectedIndex = 0;
    }

    private void OnSelectIndividuals(object? sender, EventArgs e)
    {
        using var dialog = new Form
        {
            Text = "Select Players",
            Width = 420,
            Height = 520,
            StartPosition = FormStartPosition.CenterParent,
            BackColor = AppTheme.ContentBackground,
            Font = AppTheme.FontDefault,
            MinimizeBox = false,
            MaximizeBox = false
        };

        var search = new SearchBoxControl("Search name, email, phone, lot") { Dock = DockStyle.Top, Margin = new Padding(8) };
        var clb = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true };
        var btnOk = new Button { Text = "Done", DialogResult = DialogResult.OK, Dock = DockStyle.Bottom, Height = 32 };

        List<(int Id, string Display, string SearchBlob)> allPlayers;
        using (var db = new BocceDbContext())
        {
            allPlayers = db.Players.Where(p => p.IsActive)
                .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
                .Select(p => new { p.Id, p.FirstName, p.LastName, p.Email, p.Phone, p.LotNumber })
                .ToList()
                .Select(p => (p.Id, Display: $"{p.LastName}, {p.FirstName}",
                    SearchBlob: $"{p.LastName} {p.FirstName} {p.Email} {p.Phone} {p.LotNumber}"))
                .ToList();
        }

        void RefreshList()
        {
            var filter = search.SearchText.Trim();
            var visible = filter.Length == 0
                ? allPlayers
                : allPlayers.Where(p => SearchQueryService.MatchesAnyTerm(p.SearchBlob, filter)).ToList();

            clb.Items.Clear();
            foreach (var p in visible)
                clb.Items.Add(new ComboItem(p.Id, p.Display), _selectedIndividualIds.Contains(p.Id));
        }

        search.SearchTextChanged += (_, _) => RefreshList();
        clb.ItemCheck += (_, e2) =>
        {
            if (clb.Items[e2.Index] is not ComboItem item) return;
            bool willCheck = e2.NewValue == CheckState.Checked;
            BeginInvoke(new Action(() =>
            {
                if (willCheck) _selectedIndividualIds.Add(item.Id);
                else _selectedIndividualIds.Remove(item.Id);
                UpdateIndividualsLabel();
            }));
        };

        RefreshList();

        dialog.Controls.Add(clb);
        dialog.Controls.Add(btnOk);
        dialog.Controls.Add(search);
        dialog.AcceptButton = btnOk;

        dialog.ShowDialog(this);
        UpdateIndividualsLabel();
        RefreshRecipientPreview();
    }

    private void UpdateIndividualsLabel()
    {
        _lblSelectedIndividuals.Text = _selectedIndividualIds.Count == 0
            ? "No players selected"
            : $"{_selectedIndividualIds.Count} player(s) selected";
    }

    private (EmailRecipientScope Scope, int[] ScopeIds)? GetCurrentScope() => _cmbScope.SelectedIndex switch
    {
        0 => (EmailRecipientScope.AllActivePlayers, []),
        1 => (EmailRecipientScope.PlayersOnTeams, []),
        2 when _cmbTarget.SelectedItem is ComboItem d => (EmailRecipientScope.Division, [d.Id]),
        3 when _cmbTarget.SelectedItem is ComboItem t => (EmailRecipientScope.Team, [t.Id]),
        4 => (EmailRecipientScope.SpareList, []),
        5 => (EmailRecipientScope.UnpaidPlayers, []),
        6 when _cmbTarget.SelectedItem is ComboItem l => (EmailRecipientScope.EmailList, [l.Id]),
        7 => (EmailRecipientScope.Individual, _selectedIndividualIds.ToArray()),
        _ => null
    };

    private void RefreshRecipientPreview()
    {
        if (_cmbScope.SelectedIndex == 8)
        {
            _currentRecipients = ParseManualEmails(_txtManualEmails.Text);
            _lblRecipientCount.Text = $"{_currentRecipients.Count} recipient(s)";
            return;
        }

        try
        {
            using var db = new BocceDbContext();
            var leagueId = AppParameterService.GetDefaultLeagueId(db);
            var seasonId = AppParameterService.GetDefaultSeasonId(db);
            var scope = GetCurrentScope();
            if (!seasonId.HasValue || scope == null)
            {
                _currentRecipients = [];
            }
            else
            {
                _currentRecipients = EmailRecipientService.GetRecipients(db, scope.Value.Scope, scope.Value.ScopeIds, leagueId, seasonId.Value, _chkUniqueEmail.Checked);
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error(ex, "Failed to compute recipient preview");
            _currentRecipients = [];
        }

        _lblRecipientCount.Text = $"{_currentRecipients.Count} recipient(s)";
    }

    private static readonly System.Text.RegularExpressions.Regex EmailPattern =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", System.Text.RegularExpressions.RegexOptions.Compiled);

    // Manually typed addresses aren't tied to a Player row, so PlayerId is left null —
    // EmailRecipient.PlayerId is nullable for exactly this case.
    private static List<EmailRecipient> ParseManualEmails(string text) =>
        text.Split([';', ',', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(a => EmailPattern.IsMatch(a))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(a => new EmailRecipient(null, "", "", a, null))
            .ToList();

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

            List<EmailAttachment>? attachments = _attachments.Count == 0
                ? null
                : _attachments.Select(a => new EmailAttachment(a.FileName, a.MimeType, a.Bytes)).ToList();

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

                    await GmailSendService.SendAsync(credential, fromEmail, recipient.Email, subject, body, attachments);
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

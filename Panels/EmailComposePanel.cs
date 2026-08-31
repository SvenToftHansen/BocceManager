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

    private enum FocusTarget { Subject, Body }

    private ComboBox _cmbScope = null!;
    private ComboBox _cmbTarget = null!;
    private TextBox _txtManualEmails = null!;
    private Button _btnSelectIndividuals = null!;
    private Label _lblSelectedIndividuals = null!;
    private Label _lblRecipientCount = null!;
    private ListBox _lstRecipients = null!;
    private CheckBox _chkUniqueEmail = null!;
    private TextBox _txtSubject = null!;
    private WebView2 _webView = null!;
    private FlowLayoutPanel _attachmentsPanel = null!;
    private Button _btnSend = null!;
    private Button _btnPreview = null!;
    private Button _btnSaveDraft = null!;
    private Button _btnLoadDraft = null!;
    private Label _lblStatus = null!;
    private bool _webViewReady;

    private List<EmailRecipient> _currentRecipients = [];
    private readonly List<(string FileName, byte[] Bytes, string MimeType)> _attachments = [];
    private readonly HashSet<int> _selectedIndividualIds = [];
    private int? _loadedDraftId;

    private FlowLayoutPanel _mergeRow = null!;
    private TableLayoutPanel _composeLayout = null!;
    private const int ManualRowIndex = 1;
    private FocusTarget _lastFocusTarget = FocusTarget.Body;
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
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Panel1MinSize = 0,
            Panel2MinSize = 0,
            BackColor = AppTheme.Separator
        };
        split.SizeChanged += (_, _) => SafeApplySplitDistance(split, 160, 420, 220);
        split.HandleCreated += (_, _) => BeginInvoke(new Action(() => SafeApplySplitDistance(split, 160, 420, 220)));

        // -- Left: recipients in the currently selected group --
        var leftLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = AppTheme.Surface };
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _lblRecipientCount = new Label
        {
            Text = "Recipients (0)", Dock = DockStyle.Fill, ForeColor = AppTheme.TextSecondary,
            Font = AppTheme.FontDefaultBold, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0)
        };
        leftLayout.Controls.Add(_lblRecipientCount, 0, 0);

        _lstRecipients = new ListBox
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.FontDefault,
            BorderStyle = BorderStyle.None,
            IntegralHeight = false
        };
        _lstRecipients.Enter += (_, _) => _lastFocusTarget = FocusTarget.Body;
        leftLayout.Controls.Add(_lstRecipients, 0, 1);

        split.Panel1.Controls.Add(leftLayout);

        // -- Right: compose form --
        _composeLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 9, BackColor = AppTheme.ContentBackground };
        _composeLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44)); // 0: Send To
        _composeLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));  // 1: manual email addresses (toggled)
        _composeLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32)); // 2: unique email checkbox
        _composeLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 1));  // 3: separator
        _composeLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44)); // 4: subject
        _composeLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // 5: merge/insert buttons
        _composeLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36)); // 6: attachment chips
        _composeLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // 7: body editor
        _composeLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56)); // 8: send

        // -- Row 0: recipient scope --
        var scopeRow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(8, 6, 8, 0), WrapContents = false };
        scopeRow.Controls.Add(new Label { Text = "Send To:", AutoSize = true, ForeColor = AppTheme.TextSecondary, Font = AppTheme.FontDefault, Margin = new Padding(0, 8, 6, 0) });

        _cmbScope = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 190, Font = AppTheme.FontDefault };
        _cmbScope.Items.AddRange([
            "All Active Players", "Players on Teams", "Division", "Team",
            "Players on Spare List", "Unpaid Players", "Email List", "Individual(s)", "Manual Email Address(es)"
        ]);
        _cmbScope.SelectedIndex = 0;
        _cmbScope.SelectedIndexChanged += (_, _) => { UpdateScopeControlsVisibility(); LoadTargets(); RefreshRecipientPreview(); };
        _cmbScope.Enter += (_, _) => _lastFocusTarget = FocusTarget.Body;
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
        _cmbTarget.Enter += (_, _) => _lastFocusTarget = FocusTarget.Body;
        scopeRow.Controls.Add(_cmbTarget);

        _btnSelectIndividuals = MakeButton("Select Players...", Color.FromArgb(100, 116, 139));
        _btnSelectIndividuals.Margin = new Padding(8, 0, 0, 0);
        _btnSelectIndividuals.Click += OnSelectIndividuals;
        _btnSelectIndividuals.Enter += (_, _) => _lastFocusTarget = FocusTarget.Body;
        scopeRow.Controls.Add(_btnSelectIndividuals);

        _lblSelectedIndividuals = new Label { Text = "No players selected", AutoSize = true, ForeColor = AppTheme.TextMuted, Font = AppTheme.FontDefault, Margin = new Padding(8, 8, 0, 0) };
        scopeRow.Controls.Add(_lblSelectedIndividuals);
        _composeLayout.Controls.Add(scopeRow, 0, 0);

        // -- Row 1: manual email addresses, own line --
        var manualRow = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 4, 8, 4) };
        _txtManualEmails = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.FontDefault,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            PlaceholderText = "name@example.com; other@example.com",
            Visible = false
        };
        _txtManualEmails.TextChanged += (_, _) => RefreshRecipientPreview();
        _txtManualEmails.Enter += (_, _) => _lastFocusTarget = FocusTarget.Body;
        manualRow.Controls.Add(_txtManualEmails);
        _composeLayout.Controls.Add(manualRow, 0, ManualRowIndex);

        // -- Row 2: unique email checkbox, own line --
        var uniqueRow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(8, 4, 8, 0) };
        _chkUniqueEmail = new CheckBox
        {
            Text = "Unique email addresses only", Checked = true, AutoSize = true,
            ForeColor = AppTheme.TextSecondary, Font = AppTheme.FontDefault
        };
        _chkUniqueEmail.CheckedChanged += (_, _) => RefreshRecipientPreview();
        _chkUniqueEmail.Enter += (_, _) => _lastFocusTarget = FocusTarget.Body;
        uniqueRow.Controls.Add(_chkUniqueEmail);
        _composeLayout.Controls.Add(uniqueRow, 0, 2);

        // -- Row 3: separator --
        var separator = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Separator, Margin = new Padding(0) };
        _composeLayout.Controls.Add(separator, 0, 3);

        // -- Row 4: subject --
        var subjectRow = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 4, 8, 4) };
        _txtSubject = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.FontDefault,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            PlaceholderText = "Subject (supports {FirstName}, {LastName}, {TeamName}, {Email}, {Division}, {Day}, {Time})"
        };
        _txtSubject.Enter += (_, _) => _lastFocusTarget = FocusTarget.Subject;
        subjectRow.Controls.Add(_txtSubject);
        _composeLayout.Controls.Add(subjectRow, 0, 4);

        // -- Row 5: merge field insert buttons + insert report --
        _mergeRow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(8, 0, 8, 0) };
        _mergeRow.Controls.Add(new Label { Text = "Insert placeholder:", AutoSize = true, ForeColor = AppTheme.TextMuted, Font = AppTheme.FontSmall, Margin = new Padding(0, 6, 6, 0) });
        foreach (var field in new[] { "{FirstName}", "{LastName}", "{TeamName}", "{Email}", "{Division}", "{Day}", "{Time}" })
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
        _composeLayout.Controls.Add(_mergeRow, 0, 5);

        // -- Row 6: attachment chips --
        _attachmentsPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(8, 2, 8, 2), AutoScroll = true };
        _composeLayout.Controls.Add(_attachmentsPanel, 0, 6);

        // -- Row 7: body editor --
        var bodyPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 8, 0) };
        _webView = new WebView2 { Dock = DockStyle.Fill };
        bodyPanel.Controls.Add(_webView);
        _composeLayout.Controls.Add(bodyPanel, 0, 7);

        // -- Row 8: send --
        var sendRow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(8) };
        _btnSend = MakeButton("Send", AppTheme.ButtonSuccess);
        _btnSend.Click += OnSend;
        sendRow.Controls.Add(_btnSend);
        _btnPreview = MakeButton("Preview", Color.FromArgb(100, 116, 139));
        _btnPreview.Click += OnPreview;
        sendRow.Controls.Add(_btnPreview);
        _btnSaveDraft = MakeButton("Save Draft", Color.FromArgb(100, 116, 139));
        _btnSaveDraft.Margin = new Padding(16, 0, 6, 0);
        _btnSaveDraft.Click += OnSaveDraft;
        sendRow.Controls.Add(_btnSaveDraft);
        _btnLoadDraft = MakeButton("Load Draft...", Color.FromArgb(100, 116, 139));
        _btnLoadDraft.Click += OnLoadDraft;
        sendRow.Controls.Add(_btnLoadDraft);
        _lblStatus = new Label { AutoSize = true, ForeColor = AppTheme.TextMuted, Font = AppTheme.FontDefault, Margin = new Padding(12, 8, 0, 0) };
        sendRow.Controls.Add(_lblStatus);
        _composeLayout.Controls.Add(sendRow, 0, 8);

        split.Panel2.Controls.Add(_composeLayout);

        var tabs = new TabControl { Dock = DockStyle.Fill, Font = AppTheme.FontDefault };

        var composeTab = new TabPage("Compose") { BackColor = AppTheme.ContentBackground };
        composeTab.Controls.Add(split);
        tabs.TabPages.Add(composeTab);

        var repliesTab = new TabPage("Replies") { BackColor = AppTheme.ContentBackground };
        repliesTab.Controls.Add(new EmailRepliesPanel());
        tabs.TabPages.Add(repliesTab);

        Controls.Add(tabs);
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
            // The body editor's actual keyboard focus lives in the Chromium child window the
            // WebView2 control hosts, not the WinForms control itself, so Control.GotFocus never
            // fires when the user clicks into the page. editor.html posts a message on focus
            // instead, which is the reliable signal for "user is in the body".
            _webView.CoreWebView2.WebMessageReceived += (_, e) =>
            {
                if (e.TryGetWebMessageAsString() == "editor-focus")
                    _lastFocusTarget = FocusTarget.Body;
            };
            _webView.CoreWebView2.Navigate(new Uri(editorPath).AbsoluteUri);
        }
        catch (Exception ex)
        {
            AppLogger.Error(ex, "Failed to initialize compose body editor");
        }
    }

    // Inserts at the caret in whichever field last had focus before the placeholder button was
    // clicked (tracked via _lastFocusTarget, since by the time this runs focus has already
    // moved to the button itself) — Subject if that's where the user was, Body otherwise.
    private async Task InsertMergeFieldAsync(string field)
    {
        if (_lastFocusTarget == FocusTarget.Subject)
        {
            int pos = Math.Max(0, Math.Min(_txtSubject.SelectionStart, _txtSubject.Text.Length));
            _txtSubject.Text = _txtSubject.Text.Insert(pos, field);
            _txtSubject.SelectionStart = pos + field.Length;
            _txtSubject.Focus();
            return;
        }

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
        string? sampleTeam, sampleDivision, sampleDay, sampleTime;
        if (_currentRecipients.Count > 0)
        {
            var r = _currentRecipients[0];
            samplePlayer = new Player { FirstName = r.FirstName, LastName = r.LastName, Email = r.Email };
            sampleTeam = r.TeamName;
            sampleDivision = r.DivisionName;
            sampleDay = r.DayName;
            sampleTime = r.TimeName;
        }
        else
        {
            samplePlayer = new Player { FirstName = "Jane", LastName = "Doe", Email = "jane@example.com" };
            sampleTeam = "Sample Team";
            sampleDivision = "Sample Division";
            sampleDay = "Monday";
            sampleTime = "6:00 PM";
        }

        var subject = MergeFieldService.Apply(subjectTemplate, samplePlayer, sampleTeam, sampleDivision, sampleDay, sampleTime);
        var mergedBody = MergeFieldService.Apply(bodyTemplate, samplePlayer, sampleTeam, sampleDivision, sampleDay, sampleTime);

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

        _composeLayout.RowStyles[ManualRowIndex].Height = isManual ? 40 : 0;
        _composeLayout.PerformLayout();
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

    // ── Drafts ─────────────────────────────────────────────────────────────────
    // Lets the admin step away mid-compose and pick back up later. Recipient selection is
    // stored as the raw scope/target rather than resolved emails, since the roster underneath
    // (team assignments, spare lists, etc.) may have changed by the time the draft is reloaded.

    private async void OnSaveDraft(object? sender, EventArgs e)
    {
        if (!_webViewReady)
        {
            MessageBox.Show("The email editor is still loading — try again in a moment.", "Save Draft", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var bodyRaw = await _webView.CoreWebView2.ExecuteScriptAsync("window.getBody()");
        var bodyHtml = JsonSerializer.Deserialize<string>(bodyRaw) ?? "";

        int? targetId = (_cmbScope.SelectedIndex is 2 or 3 or 6) && _cmbTarget.SelectedItem is ComboItem t ? t.Id : null;

        try
        {
            using var db = new BocceDbContext();
            var draft = _loadedDraftId.HasValue
                ? db.EmailDrafts.FirstOrDefault(d => d.Id == _loadedDraftId.Value)
                : null;
            if (draft == null)
            {
                draft = new EmailDraft();
                db.EmailDrafts.Add(draft);
            }

            draft.Subject = _txtSubject.Text.Trim();
            draft.BodyHtml = bodyHtml;
            draft.ScopeIndex = _cmbScope.SelectedIndex;
            draft.TargetId = targetId;
            draft.ManualEmails = _txtManualEmails.Text;
            draft.UniqueEmailOnly = _chkUniqueEmail.Checked;
            draft.SelectedIndividualIds = _selectedIndividualIds.Count == 0 ? null : string.Join(",", _selectedIndividualIds);
            draft.SavedBy = Environment.UserName;
            draft.SavedAt = DateTime.UtcNow;

            db.SaveChanges();
            _loadedDraftId = draft.Id;

            _lblStatus.Text = "Draft saved.";
        }
        catch (Exception ex)
        {
            AppLogger.Error(ex, "Failed to save email draft");
            MessageBox.Show($"Could not save draft:\n\n{ex.Message}", "Save Draft", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnLoadDraft(object? sender, EventArgs e)
    {
        List<EmailDraft> drafts;
        try
        {
            using var db = new BocceDbContext();
            drafts = db.EmailDrafts.OrderByDescending(d => d.SavedAt).ToList();
        }
        catch (Exception ex)
        {
            AppLogger.Error(ex, "Failed to load email drafts");
            MessageBox.Show($"Could not load drafts:\n\n{ex.Message}", "Load Draft", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (drafts.Count == 0)
        {
            MessageBox.Show("No saved drafts.", "Load Draft", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new Form
        {
            Text = "Load Draft",
            Width = 480,
            Height = 420,
            StartPosition = FormStartPosition.CenterParent,
            BackColor = AppTheme.ContentBackground,
            Font = AppTheme.FontDefault,
            MinimizeBox = false,
            MaximizeBox = false
        };

        var list = new ListBox { Dock = DockStyle.Fill, Font = AppTheme.FontDefault, IntegralHeight = false };
        foreach (var d in drafts)
        {
            string subject = string.IsNullOrWhiteSpace(d.Subject) ? "(no subject)" : d.Subject;
            list.Items.Add(new DraftListItem(d, $"{subject}  —  {d.SavedAt.ToLocalTime():g}"));
        }
        list.SelectedIndex = 0;

        var buttonRow = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
        var btnLoad = new Button { Text = "Load", DialogResult = DialogResult.OK, Width = 90, Height = 28 };
        var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 90, Height = 28 };
        var btnDelete = new Button { Text = "Delete", Width = 90, Height = 28, ForeColor = AppTheme.ButtonDanger };
        btnDelete.Click += (_, _) =>
        {
            if (list.SelectedItem is not DraftListItem item) return;
            if (MessageBox.Show("Delete this draft?", "Delete Draft", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            try
            {
                using var db = new BocceDbContext();
                var d = db.EmailDrafts.FirstOrDefault(x => x.Id == item.Draft.Id);
                if (d != null) { db.EmailDrafts.Remove(d); db.SaveChanges(); }
                if (_loadedDraftId == item.Draft.Id) _loadedDraftId = null;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Failed to delete email draft");
            }
            list.Items.Remove(item);
            if (list.Items.Count == 0) dialog.DialogResult = DialogResult.Cancel;
        };

        buttonRow.Controls.AddRange([btnCancel, btnLoad, btnDelete]);
        dialog.Controls.Add(list);
        dialog.Controls.Add(buttonRow);
        dialog.AcceptButton = btnLoad;
        dialog.CancelButton = btnCancel;

        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        if (list.SelectedItem is not DraftListItem selected) return;

        _ = ApplyDraftAsync(selected.Draft);
    }

    private sealed record DraftListItem(EmailDraft Draft, string Label)
    {
        public override string ToString() => Label;
    }

    private async Task ApplyDraftAsync(EmailDraft draft)
    {
        if (!_webViewReady)
        {
            MessageBox.Show("The email editor is still loading — try again in a moment.", "Load Draft", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _txtSubject.Text = draft.Subject;

        _cmbScope.SelectedIndex = draft.ScopeIndex;
        LoadTargets();
        if (draft.TargetId.HasValue)
        {
            for (int i = 0; i < _cmbTarget.Items.Count; i++)
                if (_cmbTarget.Items[i] is ComboItem t && t.Id == draft.TargetId.Value) { _cmbTarget.SelectedIndex = i; break; }
        }

        _txtManualEmails.Text = draft.ManualEmails ?? "";
        _chkUniqueEmail.Checked = draft.UniqueEmailOnly;

        _selectedIndividualIds.Clear();
        if (!string.IsNullOrWhiteSpace(draft.SelectedIndividualIds))
            foreach (var part in draft.SelectedIndividualIds.Split(',', StringSplitOptions.RemoveEmptyEntries))
                if (int.TryParse(part, out int id)) _selectedIndividualIds.Add(id);
        UpdateIndividualsLabel();

        UpdateScopeControlsVisibility();

        await _webView.CoreWebView2.ExecuteScriptAsync($"window.setBody({JsonSerializer.Serialize(draft.BodyHtml)})");

        _loadedDraftId = draft.Id;
        RefreshRecipientPreview();
        _lblStatus.Text = "Draft loaded.";
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
            UpdateRecipientDisplay();
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

        UpdateRecipientDisplay();
    }

    private void UpdateRecipientDisplay()
    {
        _lblRecipientCount.Text = $"Recipients ({_currentRecipients.Count})";

        _lstRecipients.Items.Clear();
        foreach (var r in _currentRecipients)
        {
            bool hasName = !string.IsNullOrWhiteSpace(r.FirstName) || !string.IsNullOrWhiteSpace(r.LastName);
            _lstRecipients.Items.Add(hasName ? $"{r.LastName}, {r.FirstName}  —  {r.Email}" : r.Email);
        }
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
                    var subject = ApplyMerge(subjectTemplate, player, recipient.TeamName, recipient.DivisionName, recipient.DayName, recipient.TimeName);
                    var body = ApplyMerge(bodyTemplate, player, recipient.TeamName, recipient.DivisionName, recipient.DayName, recipient.TimeName);

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

                // The draft this send came from (if any) is now obsolete.
                if (_loadedDraftId.HasValue)
                {
                    var draft = db.EmailDrafts.FirstOrDefault(d => d.Id == _loadedDraftId.Value);
                    if (draft != null) db.EmailDrafts.Remove(draft);
                    db.SaveChanges();
                    _loadedDraftId = null;
                }
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

    private static string ApplyMerge(string template, Player player, string? teamName,
        string? divisionName = null, string? dayName = null, string? timeName = null) =>
        MergeFieldService.Apply(template, player, teamName, divisionName, dayName, timeName);
}

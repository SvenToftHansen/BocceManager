using System.Drawing.Printing;
using System.Text.Json;
using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.Services;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;
using Microsoft.Web.WebView2.WinForms;

namespace BocceManager.Panels;

public class AnnouncementsPanel : UserControl
{
    private ListView _listView = null!;
    private WebView2 _webView = null!;
    private TextBox _txtTitle = null!;
    private ComboBox _cmbLeague = null!;
    private DateTimePicker _dtpPublished = null!;
    private DateTimePicker _dtpExpires = null!;
    private CheckBox _chkActive = null!;
    private Button _btnDelete = null!;
    private Button _btnSave = null!;
    private Button _btnPrint = null!;
    private bool _webViewReady;
    private bool _suppressFieldEvents;
    private int? _currentId;
    private static readonly string DesignerHtmlPath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "NoticeDesigner", "designer.html");

    public AnnouncementsPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;
        BuildUI();
        Load += async (_, _) => await InitWebViewAsync();
        LoadAnnouncements();
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

        var btnNew   = MakeButton("New Notice", AppTheme.Accent);
        _btnSave     = MakeButton("Save",        AppTheme.ButtonSuccess);
        _btnDelete   = MakeButton("Delete",      AppTheme.ButtonDanger);
        _btnPrint    = MakeButton("Print",       Color.FromArgb(100, 116, 139));

        btnNew.Click      += (_, _) => NewNotice();
        _btnSave.Click    += (_, _) => SaveCurrent();
        _btnDelete.Click  += (_, _) => DeleteCurrent();
        _btnPrint.Click   += async (_, _) => await PrintCurrentAsync();

        int x = 0;
        foreach (var btn in new[] { btnNew, _btnSave, _btnDelete, _btnPrint })
        {
            btn.Location = new Point(x, 8);
            toolbar.Controls.Add(btn);
            x += btn.Width + 8;
        }

        var mainSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Separator
        };

        void SafeApplySplitDistance()
        {
            if (mainSplit.Width <= 1) return;

            const int desiredLeftMin = 220;
            const int desiredRightMin = 400;
            int maxTotalMin = Math.Max(0, mainSplit.Width - 1);

            int leftMin = desiredLeftMin;
            int rightMin = desiredRightMin;
            if (leftMin + rightMin > maxTotalMin)
            {
                if (maxTotalMin == 0)
                {
                    leftMin = 0;
                    rightMin = 0;
                }
                else
                {
                    double leftRatio = desiredLeftMin / (double)(desiredLeftMin + desiredRightMin);
                    leftMin = (int)Math.Floor(maxTotalMin * leftRatio);
                    rightMin = maxTotalMin - leftMin;
                }
            }

            mainSplit.Panel1MinSize = leftMin;
            mainSplit.Panel2MinSize = rightMin;

            int minLeft = mainSplit.Panel1MinSize;
            int maxLeft = mainSplit.Width - mainSplit.Panel2MinSize;
            if (maxLeft < minLeft) maxLeft = minLeft;

            int clamped = Math.Min(280, maxLeft);
            clamped = Math.Max(minLeft, clamped);

            if (clamped > 0) mainSplit.SplitterDistance = clamped;
        }

        mainSplit.SizeChanged += (_, _) => SafeApplySplitDistance();
        mainSplit.HandleCreated += (_, _) => BeginInvoke(new Action(SafeApplySplitDistance));

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
        _listView.Columns.Add("Title",  160);
        _listView.Columns.Add("League",  90);
        _listView.Columns.Add("Active",  55);
        _listView.SelectedIndexChanged += (_, _) => OnSelectionChanged();
        mainSplit.Panel1.Controls.Add(_listView);

        var detailPanel = new Panel { Dock = DockStyle.Top, Height = 108, BackColor = AppTheme.Surface, Padding = new Padding(10, 8, 10, 8) };

        var lblTitle = new Label { Text = "Title", Font = AppTheme.FontDefaultBold, ForeColor = AppTheme.TextPrimary, AutoSize = true, Location = new Point(0, 4) };
        _txtTitle = new TextBox { Location = new Point(60, 0), Width = 260, Font = AppTheme.FontDefault };

        var lblLeague = new Label { Text = "League", Font = AppTheme.FontDefaultBold, ForeColor = AppTheme.TextPrimary, AutoSize = true, Location = new Point(336, 4) };
        _cmbLeague = new ComboBox { Location = new Point(400, 0), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList, Font = AppTheme.FontDefault };

        _chkActive = new CheckBox { Text = "Active", Location = new Point(600, 2), AutoSize = true, Font = AppTheme.FontDefault, ForeColor = AppTheme.TextPrimary, Checked = true };

        var lblPublished = new Label { Text = "Published", Font = AppTheme.FontDefaultBold, ForeColor = AppTheme.TextPrimary, AutoSize = true, Location = new Point(0, 38) };
        _dtpPublished = new DateTimePicker { Location = new Point(80, 34), Width = 160, Format = DateTimePickerFormat.Short, Font = AppTheme.FontDefault, ShowCheckBox = true, Checked = false };

        var lblExpires = new Label { Text = "Expires", Font = AppTheme.FontDefaultBold, ForeColor = AppTheme.TextPrimary, AutoSize = true, Location = new Point(260, 38) };
        _dtpExpires = new DateTimePicker { Location = new Point(330, 34), Width = 160, Format = DateTimePickerFormat.Short, Font = AppTheme.FontDefault, ShowCheckBox = true, Checked = false };

        detailPanel.Controls.AddRange([lblTitle, _txtTitle, lblLeague, _cmbLeague, _chkActive, lblPublished, _dtpPublished, lblExpires, _dtpExpires]);

        _webView = new WebView2 { Dock = DockStyle.Fill };

        mainSplit.Panel2.Controls.Add(_webView);
        mainSplit.Panel2.Controls.Add(detailPanel);

        Controls.Add(mainSplit);
        Controls.Add(toolbar);

        LoadLeagues();
        SetFieldsEnabled(false);
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
            _webView.CoreWebView2.Navigate("file:///" + DesignerHtmlPath.Replace('\\', '/'));
            _webView.CoreWebView2.NavigationCompleted += async (_, _) =>
            {
                _webViewReady = true;
                if (_currentId.HasValue) await LoadDesignIntoCanvas();
            };
        }
        catch { }
    }

    private void LoadLeagues()
    {
        _cmbLeague.Items.Clear();
        _cmbLeague.Items.Add(new LeagueItem(null, "(All Leagues)"));
        try
        {
            using var db = new BocceDbContext();
            foreach (var league in db.Leagues.Where(l => l.IsActive).OrderBy(l => l.Name).ToList())
                _cmbLeague.Items.Add(new LeagueItem(league.Id, league.Name));
        }
        catch { }
        _cmbLeague.SelectedIndex = 0;
    }

    private void LoadAnnouncements()
    {
        _listView.Items.Clear();
        try
        {
            using var db = new BocceDbContext();
            foreach (var a in db.Announcements.Include(x => x.League).OrderByDescending(x => x.CreatedAt).ToList())
            {
                var item = new ListViewItem(a.Title);
                item.SubItems.Add(a.League?.Name ?? "All");
                item.SubItems.Add(a.IsActive ? "Yes" : "No");
                item.Tag = a.Id;
                _listView.Items.Add(item);
            }
        }
        catch { }
    }

    private void OnSelectionChanged()
    {
        if (_listView.SelectedItems.Count == 0) return;
        var id = (int)_listView.SelectedItems[0].Tag!;
        LoadAnnouncement(id);
    }

    private void LoadAnnouncement(int id)
    {
        Announcement? a;
        try
        {
            using var db = new BocceDbContext();
            a = db.Announcements.Find(id);
        }
        catch { return; }
        if (a == null) return;

        _currentId = id;
        _suppressFieldEvents = true;
        _txtTitle.Text = a.Title;
        SelectLeague(a.LeagueId);
        _chkActive.Checked = a.IsActive;
        if (a.PublishedAt.HasValue) { _dtpPublished.Checked = true; _dtpPublished.Value = a.PublishedAt.Value; }
        else _dtpPublished.Checked = false;
        if (a.ExpiresAt.HasValue) { _dtpExpires.Checked = true; _dtpExpires.Value = a.ExpiresAt.Value; }
        else _dtpExpires.Checked = false;
        _suppressFieldEvents = false;

        SetFieldsEnabled(true);
        _ = LoadDesignIntoCanvas();
    }

    private async Task LoadDesignIntoCanvas()
    {
        if (!_webViewReady || !_currentId.HasValue) return;
        string? json;
        try
        {
            using var db = new BocceDbContext();
            json = db.Announcements.Find(_currentId.Value)?.DesignJson;
        }
        catch { json = null; }

        var arg = JsonSerializer.Serialize(json);
        await _webView.ExecuteScriptAsync($"loadDesign({arg})");
    }

    private void SelectLeague(int? leagueId)
    {
        foreach (LeagueItem item in _cmbLeague.Items)
        {
            if (item.Id == leagueId) { _cmbLeague.SelectedItem = item; return; }
        }
        _cmbLeague.SelectedIndex = 0;
    }

    private void NewNotice()
    {
        _currentId = null;
        _listView.SelectedItems.Clear();
        _suppressFieldEvents = true;
        _txtTitle.Text = "";
        _cmbLeague.SelectedIndex = 0;
        _chkActive.Checked = true;
        _dtpPublished.Checked = false;
        _dtpExpires.Checked = false;
        _suppressFieldEvents = false;

        SetFieldsEnabled(true);
        if (_webViewReady) _ = _webView.ExecuteScriptAsync("loadDesign(null)");
    }

    private async void SaveCurrent()
    {
        if (string.IsNullOrWhiteSpace(_txtTitle.Text))
        {
            MessageBox.Show("Please enter a title.", "Title Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string? designJson = null;
        if (_webViewReady)
        {
            try
            {
                var raw = await _webView.ExecuteScriptAsync("getDesignJson()");
                designJson = JsonSerializer.Deserialize<string>(raw);
            }
            catch { }
        }

        var leagueId = (_cmbLeague.SelectedItem as LeagueItem)?.Id;

        try
        {
            using var db = new BocceDbContext();
            Announcement a;
            if (_currentId.HasValue)
            {
                a = db.Announcements.Find(_currentId.Value) ?? new Announcement();
                if (a.Id == 0) db.Announcements.Add(a);
            }
            else
            {
                a = new Announcement();
                db.Announcements.Add(a);
            }

            a.Title = _txtTitle.Text.Trim();
            a.LeagueId = leagueId;
            a.IsActive = _chkActive.Checked;
            a.PublishedAt = _dtpPublished.Checked ? _dtpPublished.Value : null;
            a.ExpiresAt = _dtpExpires.Checked ? _dtpExpires.Value : null;
            if (designJson != null) a.DesignJson = designJson;

            db.SaveChanges();
            _currentId = a.Id;

            LoadAnnouncements();
            SelectListItem(a.Id);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save notice:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void DeleteCurrent()
    {
        if (!_currentId.HasValue) return;
        if (MessageBox.Show("Delete this notice?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        try
        {
            using var db = new BocceDbContext();
            var a = db.Announcements.Find(_currentId.Value);
            if (a != null) { db.Announcements.Remove(a); db.SaveChanges(); }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to delete:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _currentId = null;
        SetFieldsEnabled(false);
        LoadAnnouncements();
        if (_webViewReady) _ = _webView.ExecuteScriptAsync("loadDesign(null)");
    }

    private async Task PrintCurrentAsync()
    {
        if (!_webViewReady || !_currentId.HasValue)
        {
            MessageBox.Show("Select or create a notice first.", "Nothing to Print", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        string dataUrl;
        try
        {
            var raw = await _webView.ExecuteScriptAsync("getPngDataUrl()");
            dataUrl = JsonSerializer.Deserialize<string>(raw) ?? "";
        }
        catch { return; }

        var comma = dataUrl.IndexOf(',');
        if (comma < 0) return;
        var bytes = Convert.FromBase64String(dataUrl[(comma + 1)..]);
        using var ms = new MemoryStream(bytes);
        using var image = Image.FromStream(ms);

        var doc = new PrintDocument { DocumentName = _txtTitle.Text };
        doc.PrintPage += (_, e) =>
        {
            var bounds = e.MarginBounds;
            var ratio = Math.Min((double)bounds.Width / image.Width, (double)bounds.Height / image.Height);
            var w = (int)(image.Width * ratio);
            var h = (int)(image.Height * ratio);
            e.Graphics!.DrawImage(image, bounds.X, bounds.Y, w, h);
        };
        PrintPreviewService.ShowPrintPreview(this, doc);
    }

    private void SelectListItem(int id)
    {
        foreach (ListViewItem item in _listView.Items)
        {
            if ((int)item.Tag! == id)
            {
                item.Selected = true;
                item.EnsureVisible();
                _listView.Focus();
                break;
            }
        }
    }

    private void SetFieldsEnabled(bool enabled)
    {
        _txtTitle.Enabled = enabled;
        _cmbLeague.Enabled = enabled;
        _chkActive.Enabled = enabled;
        _dtpPublished.Enabled = enabled;
        _dtpExpires.Enabled = enabled;
        _btnSave.Enabled = enabled;
        _btnDelete.Enabled = enabled && _currentId.HasValue;
        _btnPrint.Enabled = enabled;
    }

    private sealed record LeagueItem(int? Id, string Name)
    {
        public override string ToString() => Name;
    }
}

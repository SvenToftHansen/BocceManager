using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.Services;
using BocceManager.UI.Theme;
using Microsoft.Web.WebView2.WinForms;

namespace BocceManager.Panels;

public class DocumentsPanel : UserControl
{
    private ListView _listView = null!;
    private WebView2 _webView = null!;
    private Button _btnDelete = null!;
    private bool _webViewReady;
    private string? _pendingNavUrl;

    public DocumentsPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;
        BuildUI();
        Load += async (_, _) => await InitWebViewAsync();
        LoadDocuments();
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

        var btnAdd       = MakeButton("Add Document",   AppTheme.Accent);
        var btnGoogleDoc = MakeButton("Add Google Doc", AppTheme.Accent);
        _btnDelete       = MakeButton("Delete",         AppTheme.ButtonDanger);
        var btnFolder    = MakeButton("Open Folder",    Color.FromArgb(100, 116, 139));

        btnAdd.Click       += OnAddDocument;
        btnGoogleDoc.Click += OnAddGoogleDoc;
        _btnDelete.Click   += OnDelete;
        btnFolder.Click    += (_, _) =>
        {
            DocumentService.EnsureFolder();
            System.Diagnostics.Process.Start("explorer.exe", DocumentService.DocumentsFolder);
        };

        int x = 0;
        foreach (var btn in new[] { btnAdd, btnGoogleDoc, _btnDelete, btnFolder })
        {
            btn.Location = new Point(x, 8);
            toolbar.Controls.Add(btn);
            x += btn.Width + 8;
        }

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 310,
            BackColor = AppTheme.Separator
        };

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
        _listView.Columns.Add("Title",   175);
        _listView.Columns.Add("Type",     52);
        _listView.Columns.Add("Date",     70);
        _listView.SelectedIndexChanged += OnSelectionChanged;
        split.Panel1.Controls.Add(_listView);

        _webView = new WebView2 { Dock = DockStyle.Fill };
        split.Panel2.Controls.Add(_webView);

        Controls.Add(split);
        Controls.Add(toolbar);
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
            if (_pendingNavUrl != null)
            {
                _webView.CoreWebView2.Navigate(_pendingNavUrl);
                _pendingNavUrl = null;
            }
        }
        catch { }
    }

    private void NavigateWebView(string url)
    {
        if (_webViewReady && _webView.CoreWebView2 != null)
        {
            _webView.CoreWebView2.Navigate(url);
        }
        else
        {
            _pendingNavUrl = url;
        }
    }

    private void LoadDocuments()
    {
        _listView.Items.Clear();
        try
        {
            using var db = new BocceDbContext();
            foreach (var doc in db.ClubDocuments.OrderBy(d => d.Title).ToList())
            {
                var item = new ListViewItem(doc.Title);
                item.SubItems.Add(doc.DocType == "googledocs" ? "GDOC" : doc.DocType.ToUpper());
                item.SubItems.Add(doc.UploadedAt.ToString("MM/dd/yy"));
                item.Tag = doc;
                _listView.Items.Add(item);
            }
        }
        catch { }
    }

    private void OnSelectionChanged(object? sender, EventArgs e)
    {
        if (_listView.SelectedItems.Count == 0) return;
        OpenDocument((ClubDocument)_listView.SelectedItems[0].Tag!);
    }

    private void OpenDocument(ClubDocument doc)
    {
        string url;
        if (doc.DocType == "googledocs")
        {
            url = doc.GoogleDocsUrl ?? "about:blank";
        }
        else if (doc.DocType == "pdf")
        {
            url = "file:///" + DocumentService.GetFilePath(doc).Replace('\\', '/');
        }
        else
        {
            url = ConvertDocxToTempHtml(doc);
        }
        NavigateWebView(url);
    }

    private static string ConvertDocxToTempHtml(ClubDocument doc)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"bocce_doc_{doc.Id}.html");
        try
        {
            var result = new Mammoth.DocumentConverter().ConvertToHtml(DocumentService.GetFilePath(doc));
            File.WriteAllText(tempPath,
                $"<html><head><meta charset='utf-8'>" +
                $"<style>body{{font-family:'Segoe UI',sans-serif;padding:28px 40px;max-width:900px;line-height:1.65}}</style></head>" +
                $"<body>{result.Value}</body></html>",
                System.Text.Encoding.UTF8);
        }
        catch (Exception ex)
        {
            var msg = System.Net.WebUtility.HtmlEncode(ex.Message);
            File.WriteAllText(tempPath,
                $"<html><body style='font-family:Segoe UI,sans-serif;padding:28px'>" +
                $"<p style='color:red'>Error rendering document: {msg}</p></body></html>");
        }
        return "file:///" + tempPath.Replace('\\', '/');
    }

    private void OnAddDocument(object? sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog
        {
            Title = "Select Document",
            Filter = "Documents (*.pdf;*.docx)|*.pdf;*.docx|PDF Files (*.pdf)|*.pdf|Word Documents (*.docx)|*.docx"
        };
        if (ofd.ShowDialog() != DialogResult.OK) return;

        var defaultTitle = Path.GetFileNameWithoutExtension(ofd.FileName);
        var title = PromptInput("Add Document", "Display name:", defaultTitle);
        if (title == null) return;

        try
        {
            using var db = new BocceDbContext();
            var doc = DocumentService.AddFile(db, ofd.FileName, title.Trim(), null, null);
            LoadDocuments();
            SelectItem(doc.Id);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to add document:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnAddGoogleDoc(object? sender, EventArgs e)
    {
        var url = PromptInput("Add Google Doc", "Google Doc share URL:", "https://docs.google.com/");
        if (url == null) return;
        if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("Please enter a valid URL starting with http.", "Invalid URL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var title = PromptInput("Add Google Doc", "Display name:", "Google Doc");
        if (title == null) return;

        try
        {
            using var db = new BocceDbContext();
            var doc = DocumentService.AddGoogleDoc(db, url.Trim(), title.Trim(), null, null);
            LoadDocuments();
            SelectItem(doc.Id);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to add link:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnDelete(object? sender, EventArgs e)
    {
        if (_listView.SelectedItems.Count == 0) return;
        var doc = (ClubDocument)_listView.SelectedItems[0].Tag!;

        if (MessageBox.Show($"Delete \"{doc.Title}\"?", "Confirm Delete",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        try
        {
            using var db = new BocceDbContext();
            var toDelete = db.ClubDocuments.Find(doc.Id);
            if (toDelete != null) DocumentService.Delete(db, toDelete);
            _webView.CoreWebView2?.Navigate("about:blank");
            LoadDocuments();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to delete:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SelectItem(int id)
    {
        foreach (ListViewItem item in _listView.Items)
        {
            if (((ClubDocument)item.Tag!).Id == id)
            {
                item.Selected = true;
                item.EnsureVisible();
                _listView.Focus();
                break;
            }
        }
    }

    private string? PromptInput(string caption, string label, string defaultValue = "")
    {
        using var form = new Form
        {
            Text = caption,
            Width = 440,
            Height = 148,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = AppTheme.ContentBackground
        };
        var lbl = new Label
        {
            Text = label,
            Left = 12, Top = 14, Width = 410, Height = 20,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.FontDefault
        };
        var txt = new TextBox
        {
            Text = defaultValue,
            Left = 12, Top = 38, Width = 408,
            Font = AppTheme.FontDefault
        };
        var btnOk = new Button
        {
            Text = "OK", DialogResult = DialogResult.OK,
            Left = 232, Top = 72, Width = 88, Height = 30,
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.Accent, ForeColor = Color.White,
            Font = AppTheme.FontButton
        };
        var btnCancel = new Button
        {
            Text = "Cancel", DialogResult = DialogResult.Cancel,
            Left = 328, Top = 72, Width = 88, Height = 30,
            FlatStyle = FlatStyle.Flat,
            Font = AppTheme.FontButton
        };
        form.Controls.AddRange([lbl, txt, btnOk, btnCancel]);
        form.AcceptButton = btnOk;
        form.CancelButton = btnCancel;
        form.ActiveControl = txt;
        txt.SelectAll();
        return form.ShowDialog(this) == DialogResult.OK ? txt.Text : null;
    }
}

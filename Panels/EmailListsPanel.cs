using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.Services;
using BocceManager.UI.Controls;
using BocceManager.UI.Theme;

namespace BocceManager.Panels;

// "Create groups from the userbase": custom named recipient groups (EmailList/EmailListMember)
// that Compose can target alongside the built-in division/team/all-members scopes.
public class EmailListsPanel : UserControl
{
    private sealed record ListItem(int Id, string Name, bool IsActive)
    {
        public override string ToString() => IsActive ? Name : $"{Name} (inactive)";
    }

    private sealed record PlayerItem(int Id, string Name, string SearchBlob)
    {
        public override string ToString() => Name;
    }

    private ListBox _lstLists = null!;
    private SearchBoxControl _txtSearch = null!;
    private CheckedListBox _clbPlayers = null!;
    private Button _btnAdd = null!, _btnRename = null!, _btnDelete = null!, _btnToggleActive = null!;

    private List<PlayerItem> _allPlayers = [];
    private HashSet<int> _memberIds = [];
    private bool _suppressCheckEvents;

    public EmailListsPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;
        BuildUI();
        LoadAll();
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
        // Panel1MinSize/Panel2MinSize/SplitterDistance can't be set to their real values
        // until the control has a real size — WinForms recomputes SplitterDistance
        // internally when the min sizes are set, and throws if Width is still 0.
        split.SizeChanged += (_, _) => SafeApplySplitDistance(split, 220, 260, 260);
        split.HandleCreated += (_, _) => BeginInvoke(new Action(() => SafeApplySplitDistance(split, 220, 260, 260)));

        // -- Left: lists --
        var leftLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = AppTheme.Surface };
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        var toolbar1 = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(6) };
        _btnAdd = MakeButton("Add", AppTheme.Accent);
        _btnAdd.Click += OnAdd;
        toolbar1.Controls.Add(_btnAdd);
        leftLayout.Controls.Add(toolbar1, 0, 0);

        _lstLists = new ListBox
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.FontDefault,
            BorderStyle = BorderStyle.None,
            ItemHeight = 26
        };
        _lstLists.SelectedIndexChanged += (_, _) => OnListSelected();
        leftLayout.Controls.Add(_lstLists, 0, 1);

        var toolbar2 = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(6) };
        _btnRename = MakeButton("Rename", Color.FromArgb(100, 116, 139));
        _btnToggleActive = MakeButton("Toggle Active", Color.FromArgb(100, 116, 139));
        _btnDelete = MakeButton("Delete", AppTheme.ButtonDanger);
        _btnRename.Click += OnRename;
        _btnToggleActive.Click += OnToggleActive;
        _btnDelete.Click += OnDelete;
        toolbar2.Controls.AddRange([_btnRename, _btnToggleActive, _btnDelete]);
        leftLayout.Controls.Add(toolbar2, 0, 2);

        split.Panel1.Controls.Add(leftLayout);

        // -- Right: members --
        var rightLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = AppTheme.Surface };
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var searchPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
        _txtSearch = new SearchBoxControl("Search name, email, phone, lot") { Dock = DockStyle.Fill };
        _txtSearch.SearchTextChanged += (_, _) => RefreshPlayerChecklist();
        searchPanel.Controls.Add(_txtSearch);
        rightLayout.Controls.Add(searchPanel, 0, 0);

        _clbPlayers = new CheckedListBox
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.FontDefault,
            BorderStyle = BorderStyle.None,
            CheckOnClick = true
        };
        _clbPlayers.ItemCheck += OnItemCheck;
        rightLayout.Controls.Add(_clbPlayers, 0, 1);

        split.Panel2.Controls.Add(rightLayout);

        Controls.Add(split);
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

    private void LoadAll()
    {
        try
        {
            using var db = new BocceDbContext();
            var leagueId = AppParameterService.GetDefaultLeagueId(db);

            var lists = leagueId.HasValue
                ? db.EmailLists.Where(l => l.LeagueId == leagueId.Value).OrderBy(l => l.Name).ToList()
                : [];

            _allPlayers = db.Players
                .Where(p => p.IsActive)
                .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
                .Select(p => new PlayerItem(p.Id, $"{p.LastName}, {p.FirstName}",
                    $"{p.LastName} {p.FirstName} {p.Email} {p.Phone} {p.LotNumber}"))
                .ToList();

            _lstLists.Items.Clear();
            foreach (var l in lists)
                _lstLists.Items.Add(new ListItem(l.Id, l.Name, l.IsActive));

            if (_lstLists.Items.Count > 0)
                _lstLists.SelectedIndex = 0;
            else
                RefreshPlayerChecklist();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading email lists:\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnListSelected()
    {
        if (_lstLists.SelectedItem is not ListItem item)
        {
            _memberIds = [];
            RefreshPlayerChecklist();
            return;
        }

        try
        {
            using var db = new BocceDbContext();
            _memberIds = db.EmailListMembers
                .Where(m => m.EmailListId == item.Id && m.IsActive)
                .Select(m => m.PlayerId)
                .ToHashSet();
        }
        catch (Exception ex)
        {
            AppLogger.Error(ex, "Failed to load email list members");
            _memberIds = [];
        }

        RefreshPlayerChecklist();
    }

    private void RefreshPlayerChecklist()
    {
        var filter = _txtSearch.SearchText.Trim();
        var visible = filter.Length == 0
            ? _allPlayers
            : _allPlayers.Where(p => SearchQueryService.MatchesAnyTerm(p.SearchBlob, filter)).ToList();

        _suppressCheckEvents = true;
        _clbPlayers.Items.Clear();
        foreach (var p in visible)
            _clbPlayers.Items.Add(p, _memberIds.Contains(p.Id));
        _suppressCheckEvents = false;

        _clbPlayers.Enabled = _lstLists.SelectedItem is ListItem;
    }

    private void OnItemCheck(object? sender, ItemCheckEventArgs e)
    {
        if (_suppressCheckEvents) return;
        if (_lstLists.SelectedItem is not ListItem list) return;
        if (_clbPlayers.Items[e.Index] is not PlayerItem player) return;

        bool willBeChecked = e.NewValue == CheckState.Checked;
        BeginInvoke(new Action(() => ToggleMember(list.Id, player.Id, willBeChecked)));
    }

    private void ToggleMember(int listId, int playerId, bool add)
    {
        try
        {
            using var db = new BocceDbContext();
            var existing = db.EmailListMembers.FirstOrDefault(m => m.EmailListId == listId && m.PlayerId == playerId);
            if (add)
            {
                if (existing == null)
                    db.EmailListMembers.Add(new EmailListMember { EmailListId = listId, PlayerId = playerId, IsActive = true });
                else
                    existing.IsActive = true;

                _memberIds.Add(playerId);
            }
            else
            {
                if (existing != null) existing.IsActive = false;
                _memberIds.Remove(playerId);
            }
            db.SaveChanges();
        }
        catch (Exception ex)
        {
            AppLogger.Error(ex, "Failed to update email list member");
        }
    }

    private void OnAdd(object? sender, EventArgs e)
    {
        var name = PromptForText("New Email List", "List Name:", "");
        if (name == null) return;

        try
        {
            using var db = new BocceDbContext();
            var leagueId = AppParameterService.GetDefaultLeagueId(db);
            if (!leagueId.HasValue)
            {
                MessageBox.Show("Select a default league before creating email lists.", "Email Lists", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            db.EmailLists.Add(new EmailList { LeagueId = leagueId.Value, Name = name, IsActive = true });
            db.SaveChanges();
            LoadAll();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error creating email list:\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnRename(object? sender, EventArgs e)
    {
        if (_lstLists.SelectedItem is not ListItem item) return;
        var name = PromptForText("Rename Email List", "List Name:", item.Name);
        if (name == null) return;

        try
        {
            using var db = new BocceDbContext();
            var list = db.EmailLists.Find(item.Id);
            if (list != null)
            {
                list.Name = name;
                db.SaveChanges();
                LoadAll();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error renaming email list:\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnToggleActive(object? sender, EventArgs e)
    {
        if (_lstLists.SelectedItem is not ListItem item) return;

        try
        {
            using var db = new BocceDbContext();
            var list = db.EmailLists.Find(item.Id);
            if (list != null)
            {
                list.IsActive = !list.IsActive;
                db.SaveChanges();
                LoadAll();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error updating email list:\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnDelete(object? sender, EventArgs e)
    {
        if (_lstLists.SelectedItem is not ListItem item) return;

        var confirm = MessageBox.Show($"Delete the email list \"{item.Name}\"? This cannot be undone.",
            "Delete Email List", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes) return;

        try
        {
            using var db = new BocceDbContext();
            var list = db.EmailLists.Find(item.Id);
            if (list != null)
            {
                db.EmailLists.Remove(list);
                db.SaveChanges();
                LoadAll();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error deleting email list:\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private string? PromptForText(string title, string label, string initial)
    {
        using var dialog = new Form
        {
            Text = title,
            Width = 360,
            Height = 160,
            StartPosition = FormStartPosition.CenterParent,
            BackColor = AppTheme.ContentBackground,
            Font = AppTheme.FontDefault,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var lbl = new Label { Text = label, Location = new Point(10, 15), Size = new Size(200, 20), ForeColor = AppTheme.TextPrimary };
        var txt = new TextBox
        {
            Location = new Point(10, 38),
            Size = new Size(320, 28),
            Text = initial,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle
        };
        var btnOk = new Button { Text = "OK", Location = new Point(174, 78), Size = new Size(75, 28), DialogResult = DialogResult.OK };
        var btnCancel = new Button { Text = "Cancel", Location = new Point(255, 78), Size = new Size(75, 28), DialogResult = DialogResult.Cancel };

        dialog.Controls.AddRange([lbl, txt, btnOk, btnCancel]);
        dialog.AcceptButton = btnOk;
        dialog.CancelButton = btnCancel;

        if (dialog.ShowDialog(this) != DialogResult.OK) return null;

        var value = txt.Text.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            MessageBox.Show("Name cannot be blank.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }
        return value;
    }
}

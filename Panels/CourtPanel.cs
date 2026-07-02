using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.Services;
using BocceManager.UI.Theme;

namespace BocceManager.Panels;

public class CourtPanel : UserControl
{
    private bool _isLoadingData = false;
    private int? _selectedCourtId;
    private List<(int Id, string Display, int SortOrder, bool IsActive, string Notes)> _allCourts = [];

    private ListBox _lstCourts = null!;
    private Button _btnNew = null!;
    private Button _btnUp = null!;
    private Button _btnDown = null!;
    private Button _btnDelete = null!;
    private int _draggedIndex = -1;
    private Point? _mouseDownPoint = null;

    public CourtPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;
        BuildUI();
        LoadCourts();
        AppParameterService.DefaultsChanged += (_, _) => LoadCourts();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            AppParameterService.DefaultsChanged -= (_, _) => LoadCourts();
        base.Dispose(disposing);
    }

    private void BuildUI()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = Padding.Empty,
            Margin = Padding.Empty,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // Courts listbox
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));  // Toolbar

        // Content area: listbox left, help panel right
        var contentLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = Padding.Empty,
            Margin = Padding.Empty,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        };
        contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // Left: Courts listbox with header
        var listPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = Padding.Empty,
            Margin = Padding.Empty,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
            BackColor = AppTheme.Surface
        };
        listPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        listPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var lblListHeader = new Label
        {
            Text = "Courts in Priority Order",
            Dock = DockStyle.Fill,
            Font = AppTheme.FontDefaultBold,
            ForeColor = AppTheme.TextSecondary,
            BackColor = AppTheme.Surface,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0)
        };
        listPanel.Controls.Add(lblListHeader, 0, 0);
        _lstCourts = new ListBox
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            Font = new Font(AppTheme.FontDefault.FontFamily, 14f, FontStyle.Regular),
            BorderStyle = BorderStyle.None,
            AllowDrop = true,
            ItemHeight = 30
        };
        _lstCourts.SelectedIndexChanged += OnCourtSelected;
        _lstCourts.DoubleClick += OnCourtDoubleClick;
        _lstCourts.DragEnter += OnDragEnter;
        _lstCourts.DragDrop += OnDragDrop;
        _lstCourts.DragOver += OnDragOver;
        _lstCourts.MouseDown += OnMouseDown;
        _lstCourts.MouseMove += OnMouseMove;
        _lstCourts.MouseUp += OnMouseUp;
        listPanel.Controls.Add(_lstCourts, 0, 1);
        contentLayout.Controls.Add(listPanel, 0, 0);

        // Right: Help panel
        var helpPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.ContentBackground,
            Padding = new Padding(16, 14, 16, 14)
        };
        var helpBox = BuildHelpBox();
        helpPanel.Controls.Add(helpBox);
        contentLayout.Controls.Add(helpPanel, 1, 0);

        layout.Controls.Add(contentLayout, 0, 0);

        // Bottom: Toolbar
        var toolbar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Separator,
            Padding = new Padding(8)
        };

        _btnNew = MakeButton("* New", AppTheme.Accent);
        _btnNew.Click += OnNewCourt;
        _btnNew.Location = new Point(8, 8);
        toolbar.Controls.Add(_btnNew);

        _btnUp = new Button
        {
            Text = "↑",
            Size = new Size(32, 32),
            Location = new Point(_btnNew.Right + 8, 8),
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.FontButton,
            Cursor = Cursors.Hand,
            Enabled = false,
            FlatAppearance = { BorderSize = 1, BorderColor = AppTheme.Separator }
        };
        _btnUp.Click += OnMoveUp;
        toolbar.Controls.Add(_btnUp);

        _btnDown = new Button
        {
            Text = "↓",
            Size = new Size(32, 32),
            Location = new Point(_btnUp.Right + 4, 8),
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.FontButton,
            Cursor = Cursors.Hand,
            Enabled = false,
            FlatAppearance = { BorderSize = 1, BorderColor = AppTheme.Separator }
        };
        _btnDown.Click += OnMoveDown;
        toolbar.Controls.Add(_btnDown);

        _btnDelete = MakeButton("Delete", AppTheme.ButtonDanger);
        _btnDelete.Click += OnDeleteCourt;
        _btnDelete.Location = new Point(_btnDown.Right + 8, 8);
        _btnDelete.Enabled = false;
        toolbar.Controls.Add(_btnDelete);

        layout.Controls.Add(toolbar, 0, 1);
        Controls.Add(layout);
    }

    private void LoadCourts()
    {
        if (_isLoadingData) return;
        _isLoadingData = true;

        try
        {
            using var db = new BocceDbContext();

            _allCourts = db.Courts
                .OrderBy(c => c.SortOrder)
                .AsEnumerable()
                .Select(c => (
                    c.Id,
                    Display: FormatCourtDisplay(c),
                    c.SortOrder,
                    c.IsActive,
                    c.Notes ?? ""))
                .ToList();

            _lstCourts.Items.Clear();
            foreach (var (id, display, _, _, _) in _allCourts)
                _lstCourts.Items.Add(new CourtListItem(id, display));

            _selectedCourtId = null;
            _btnUp.Enabled = false;
            _btnDown.Enabled = false;
            _btnDelete.Enabled = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading courts:\n\n{ex.Message}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _isLoadingData = false;
        }
    }

    // Naming style (number vs. letter) is a season-level setting (Season screen) that
    // controls how courts are labelled in schedules/brackets. This admin list always
    // shows both so it's unambiguous regardless of any season's display choice.
    private static string FormatCourtDisplay(Court court)
    {
        string baseDisplay = court.CourtLetter != ""
            ? $"Court {court.CourtNumber} ({court.CourtLetter})"
            : $"Court {court.CourtNumber}";

        if (!court.IsActive)
            baseDisplay += " (inactive)";

        if (!string.IsNullOrWhiteSpace(court.Notes))
            baseDisplay += " (* notes)";

        return baseDisplay;
    }

    private void OnCourtSelected(object? sender, EventArgs e)
    {
        if (_lstCourts.SelectedIndex < 0)
        {
            _selectedCourtId = null;
            _btnUp.Enabled = false;
            _btnDown.Enabled = false;
            _btnDelete.Enabled = false;
            return;
        }

        var item = _lstCourts.SelectedItem as CourtListItem;
        if (item != null)
        {
            _selectedCourtId = item.Id;
            _btnUp.Enabled = true;
            _btnDown.Enabled = true;
            _btnDelete.Enabled = !IsCourtInUse(item.Id);
        }
        else
        {
            _selectedCourtId = null;
            _btnUp.Enabled = false;
            _btnDown.Enabled = false;
            _btnDelete.Enabled = false;
        }
    }

    private void OnCourtDoubleClick(object? sender, EventArgs e)
    {
        try
        {
            if (_lstCourts.SelectedIndex < 0) return;

            var item = _lstCourts.SelectedItem as CourtListItem;
            if (item == null) return;

            var court = _allCourts.FirstOrDefault(c => c.Id == item.Id);
            if (court.Id == 0) return;

            var dialog = new Form
            {
                Text = "Court Properties",
                Width = 400,
                Height = 200,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = AppTheme.ContentBackground,
                Font = AppTheme.FontDefault
            };

            var lblNotes = new Label
            {
                Text = "Notes:",
                Location = new Point(10, 10),
                Size = new Size(100, 20),
                ForeColor = AppTheme.TextPrimary
            };
            var txtNotes = new TextBox
            {
                Location = new Point(10, 30),
                Size = new Size(370, 80),
                Multiline = true,
                Text = court.Notes,
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };
            var chkActive = new CheckBox
            {
                Text = "Active",
                Location = new Point(10, 115),
                Size = new Size(200, 20),
                Checked = court.IsActive,
                ForeColor = AppTheme.TextPrimary,
                BackColor = AppTheme.ContentBackground
            };

            var btnOk = new Button { Text = "OK", Location = new Point(240, 125), Size = new Size(70, 25), DialogResult = DialogResult.OK };
            var btnCancel = new Button { Text = "Cancel", Location = new Point(320, 125), Size = new Size(70, 25), DialogResult = DialogResult.Cancel };

            dialog.Controls.AddRange(new Control[] { lblNotes, txtNotes, chkActive, btnOk, btnCancel });

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                using var db = new BocceDbContext();
                var dbCourt = db.Courts.Find(court.Id);
                if (dbCourt != null)
                {
                    dbCourt.Notes = string.IsNullOrWhiteSpace(txtNotes.Text) ? null : txtNotes.Text.Trim();
                    dbCourt.IsActive = chkActive.Checked;
                    db.SaveChanges();
                    LoadCourts();
                }
            }

            dialog.Dispose();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error in court properties:\n\n{ex.Message}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnNewCourt(object? sender, EventArgs e)
    {
        try
        {
            using var db = new BocceDbContext();
            int nextNumber = db.Courts.Any() ? db.Courts.Max(c => c.CourtNumber) + 1 : 1;
            string nextLetter = GetLetterForNumber(nextNumber);

            var newCourt = new Court
            {
                CourtNumber = nextNumber,
                CourtLetter = nextLetter,
                SortOrder = nextNumber,
                IsActive = true,
                Notes = null
            };

            db.Courts.Add(newCourt);
            db.SaveChanges();
            LoadCourts();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error creating court:\n\n{ex.Message}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnMoveUp(object? sender, EventArgs e)
    {
        // Ensure we have a selection
        if (_lstCourts.SelectedIndex < 0) return;

        var item = _lstCourts.SelectedItem as CourtListItem;
        if (item == null) return;

        _selectedCourtId = item.Id;
        SwapCourtSortOrder(-1);
    }

    private void OnMoveDown(object? sender, EventArgs e)
    {
        // Ensure we have a selection
        if (_lstCourts.SelectedIndex < 0) return;

        var item = _lstCourts.SelectedItem as CourtListItem;
        if (item == null) return;

        _selectedCourtId = item.Id;
        SwapCourtSortOrder(1);
    }

    private void SwapCourtSortOrder(int direction)
    {
        if (!_selectedCourtId.HasValue) return;
        try
        {
            using var db = new BocceDbContext();
            var selectedCourt = db.Courts.Find(_selectedCourtId.Value);
            if (selectedCourt == null) return;

            var allCourts = db.Courts.OrderBy(c => c.SortOrder).ToList();
            int currentIndex = allCourts.FindIndex(c => c.Id == selectedCourt.Id);
            int newIndex = currentIndex + direction;

            if (newIndex < 0 || newIndex >= allCourts.Count) return;

            var otherCourt = allCourts[newIndex];
            var tempSort = selectedCourt.SortOrder;
            selectedCourt.SortOrder = otherCourt.SortOrder;
            otherCourt.SortOrder = tempSort;

            db.SaveChanges();

            int selectedId = _selectedCourtId.Value;
            LoadCourts();

            // Restore selection after reload
            for (int i = 0; i < _lstCourts.Items.Count; i++)
            {
                if (_lstCourts.Items[i] is CourtListItem li && li.Id == selectedId)
                {
                    _lstCourts.SelectedIndex = i;
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error reordering courts:\n\n{ex.Message}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnDeleteCourt(object? sender, EventArgs e)
    {
        if (_selectedCourtId == null) return;

        var result = MessageBox.Show(
            "Are you sure you want to delete this court?",
            "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

        if (result != DialogResult.Yes) return;

        try
        {
            using var db = new BocceDbContext();
            var court = db.Courts.Find(_selectedCourtId.Value);
            if (court == null) return;

            if (db.ScheduleTemplateMatches.Any(m => m.CourtId == court.Id))
            {
                MessageBox.Show(
                    "This court is used in schedule templates and cannot be deleted.",
                    "Cannot Delete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (db.PlayoffMatches.Any(pm => pm.CourtId == court.Id))
            {
                MessageBox.Show(
                    "This court is used in playoff matches and cannot be deleted.",
                    "Cannot Delete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int deletedSortOrder = court.SortOrder;
            int deletedNumber = court.CourtNumber;
            db.Courts.Remove(court);
            db.SaveChanges();

            var courtsToResequence = db.Courts
                .Where(c => c.SortOrder > deletedSortOrder)
                .OrderBy(c => c.SortOrder)
                .ToList();

            foreach (var c in courtsToResequence)
                c.SortOrder--;

            var courtsToRenumber = db.Courts
                .Where(c => c.CourtNumber > deletedNumber)
                .OrderBy(c => c.CourtNumber)
                .ToList();

            foreach (var c in courtsToRenumber)
            {
                c.CourtNumber--;
                c.CourtLetter = GetLetterForNumber(c.CourtNumber);
            }

            db.SaveChanges();
            _selectedCourtId = null;
            LoadCourts();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error deleting court:\n\n{ex.Message}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            int idx = _lstCourts.IndexFromPoint(e.Location);
            if (idx >= 0)
            {
                _draggedIndex = idx;
                _mouseDownPoint = e.Location;
            }
            else
            {
                _draggedIndex = -1;
                _mouseDownPoint = null;
            }
        }
    }

    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || _draggedIndex < 0 || _mouseDownPoint == null)
            return;

        var dragSize = SystemInformation.DragSize;
        var pt = _mouseDownPoint.Value;
        if (Math.Abs(e.X - pt.X) > dragSize.Width / 2 ||
            Math.Abs(e.Y - pt.Y) > dragSize.Height / 2)
        {
            _mouseDownPoint = null;
            _lstCourts.DoDragDrop(_draggedIndex, DragDropEffects.Move);
            _draggedIndex = -1;
        }
    }

    private void OnMouseUp(object? sender, MouseEventArgs e)
    {
        _draggedIndex = -1;
        _mouseDownPoint = null;
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        e.Effect = DragDropEffects.Move;
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.Effect = DragDropEffects.Move;
    }

    private void OnDragDrop(object? sender, DragEventArgs e)
    {
        if (_draggedIndex < 0) return;

        var point = _lstCourts.PointToClient(new Point(e.X, e.Y));
        int targetIndex = _lstCourts.IndexFromPoint(point);
        if (targetIndex < 0) targetIndex = _lstCourts.Items.Count - 1;

        if (_draggedIndex == targetIndex)
        {
            _draggedIndex = -1;
            return;
        }

        try
        {
            using var db = new BocceDbContext();
            var courts = db.Courts.OrderBy(c => c.SortOrder).ToList();

            if (_draggedIndex >= courts.Count || targetIndex >= courts.Count) return;

            var sourceCourt = courts[_draggedIndex];
            var targetCourt = courts[targetIndex];

            int tempSort = sourceCourt.SortOrder;
            sourceCourt.SortOrder = targetCourt.SortOrder;
            targetCourt.SortOrder = tempSort;

            db.SaveChanges();

            // Reload courts data
            LoadCourts();

            // Refresh and deselect
            _lstCourts.SelectedIndex = -1;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error reordering courts:\n\n{ex.Message}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _draggedIndex = -1;
        }
    }

    private Button MakeButton(string text, Color backColor) => new Button
    {
        Text = text,
        Size = new Size(96, 32),
        FlatStyle = FlatStyle.Flat,
        BackColor = backColor,
        ForeColor = Color.White,
        Font = AppTheme.FontButton,
        Cursor = Cursors.Hand,
        FlatAppearance = { BorderSize = 0 }
    };

    private string GetLetterForNumber(int number) =>
        ((char)('A' + (number - 1) % 26)).ToString();

    private bool IsCourtInUse(int courtId)
    {
        try
        {
            using var db = new BocceDbContext();
            return db.ScheduleTemplateMatches.Any(m => m.CourtId == courtId)
                || db.PlayoffMatches.Any(pm => pm.CourtId == courtId);
        }
        catch { return false; }
    }

    private RichTextBox BuildHelpBox()
    {
        var rtb = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = AppTheme.ContentBackground,
            ForeColor = AppTheme.TextPrimary,
            BorderStyle = BorderStyle.None,
            TabStop = false,
            ScrollBars = RichTextBoxScrollBars.None,
            WordWrap = true
        };

        var headFont = new Font(AppTheme.FontDefault.FontFamily, 10f, FontStyle.Bold);
        var bodyFont = new Font(AppTheme.FontDefault.FontFamily, 9.5f, FontStyle.Regular);
        var bodyColor = AppTheme.TextSecondary;

        void Section(string heading, string body)
        {
            rtb.SelectionFont = headFont;
            rtb.SelectionColor = AppTheme.TextPrimary;
            rtb.AppendText(heading + "\n");
            rtb.SelectionFont = bodyFont;
            rtb.SelectionColor = bodyColor;
            rtb.AppendText(body + "\n\n");
        }

        Section("REORDERING",
            "Drag courts up or down the list to set their scheduling priority, " +
            "or use the ↑ ↓ buttons to move one step at a time. " +
            "Courts are assigned to matches from the top down — the highest-priority courts are used first.");

        Section("DOUBLE-CLICK TO EDIT",
            "Double-click any court to open its properties. " +
            "You can leave notes about the court and mark it Active or Inactive. " +
            "Inactive courts are shown with (inactive) in the list and are skipped during schedule generation.");

        Section("ADDING A COURT",
            "Click * New to add the next court in sequence. " +
            "Court numbers (and letters) are assigned automatically — no gaps, no duplicates.");

        Section("DELETING A COURT",
            "Select a court and click Delete to remove it. " +
            "Courts that are already assigned to a league schedule or playoff match cannot be deleted — " +
            "the Delete button will remain disabled for those courts.");

        return rtb;
    }

    private sealed record CourtListItem(int Id, string Display) { public override string ToString() => Display; }
}

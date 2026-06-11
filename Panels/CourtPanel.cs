using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.Services;
using BocceManager.UI.Theme;

namespace BocceManager.Panels;

public class CourtPanel : UserControl
{
    private enum CourtMode { View, Edit, Create }

    private CourtMode _courtMode = CourtMode.View;
    private bool _isLoadingData = false;
    private int? _selectedCourtId;
    private List<(int Id, string Display)> _allCourts = [];

    private ListBox _lstCourts = null!;
    private Label _lblCourtNumber = null!;
    private Label _lblCourtLetter = null!;
    private CheckBox _chkActive = null!;
    private TextBox _txtNotes = null!;
    private Button _btnAdd = null!;
    private Button _btnEdit = null!;
    private Button _btnDelete = null!;
    private Button _btnSave = null!;
    private Button _btnCancel = null!;
    private Panel _editorPanel = null!;

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
            ColumnCount = 2,
            RowCount = 1,
            Padding = Padding.Empty,
            Margin = Padding.Empty,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // Left panel: courts list
        var leftPanel = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface };
        _lstCourts = new ListBox
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.FontDefault,
            BorderStyle = BorderStyle.None
        };
        _lstCourts.SelectedIndexChanged += OnCourtSelected;
        leftPanel.Controls.Add(_lstCourts);

        var leftToolbar = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            BackColor = AppTheme.Separator,
            Padding = new Padding(8)
        };
        _btnAdd = MakeButton("+ Add", AppTheme.Accent);
        _btnAdd.Click += OnAddCourt;
        _btnAdd.Location = new Point(8, 8);
        leftToolbar.Controls.Add(_btnAdd);

        leftPanel.Controls.Add(leftToolbar);
        layout.Controls.Add(leftPanel, 0, 0);

        // Right panel: editor
        _editorPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.ContentBackground,
            Padding = new Padding(16)
        };

        int y = 0;
        const int fieldHeight = 28;
        const int spacing = 12;
        const int labelWidth = 80;
        const int controlWidth = 300;

        Action<string, int> AddField = (label, py) =>
        {
            var lbl = new Label
            {
                Text = label,
                Location = new Point(0, py),
                Size = new Size(labelWidth, fieldHeight),
                Font = AppTheme.FontDefault,
                ForeColor = AppTheme.TextPrimary,
                TextAlign = ContentAlignment.MiddleLeft
            };
            _editorPanel.Controls.Add(lbl);
        };

        AddField("Number:", y);
        _lblCourtNumber = new Label
        {
            Location = new Point(labelWidth + 8, y),
            Size = new Size(controlWidth, fieldHeight),
            BackColor = AppTheme.ContentBackground,
            ForeColor = AppTheme.TextMuted,
            Font = AppTheme.FontDefault,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _editorPanel.Controls.Add(_lblCourtNumber);
        y += fieldHeight + spacing;

        AddField("Letter:", y);
        _lblCourtLetter = new Label
        {
            Location = new Point(labelWidth + 8, y),
            Size = new Size(controlWidth, fieldHeight),
            BackColor = AppTheme.ContentBackground,
            ForeColor = AppTheme.TextMuted,
            Font = AppTheme.FontDefault,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _editorPanel.Controls.Add(_lblCourtLetter);
        y += fieldHeight + spacing;

        AddField("Active:", y);
        _chkActive = new CheckBox
        {
            Location = new Point(labelWidth + 8, y),
            Size = new Size(20, fieldHeight),
            BackColor = AppTheme.ContentBackground,
            ForeColor = AppTheme.TextPrimary
        };
        _editorPanel.Controls.Add(_chkActive);
        y += fieldHeight + spacing;

        AddField("Notes:", y);
        _txtNotes = new TextBox
        {
            Location = new Point(labelWidth + 8, y),
            Size = new Size(controlWidth, 80),
            Multiline = true,
            WordWrap = true,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.FontDefault,
            BorderStyle = BorderStyle.FixedSingle
        };
        _editorPanel.Controls.Add(_txtNotes);
        y += 80 + spacing;

        // Buttons
        var buttonPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            BackColor = AppTheme.Separator,
            Padding = new Padding(8)
        };

        _btnEdit = MakeButton("Edit", AppTheme.Accent);
        _btnEdit.Click += OnEditCourt;
        _btnEdit.Location = new Point(8, 8);

        _btnDelete = MakeButton("Delete", AppTheme.ButtonDanger);
        _btnDelete.Click += OnDeleteCourt;
        _btnDelete.Location = new Point(_btnEdit.Right + 8, 8);

        _btnSave = MakeButton("Save", AppTheme.Accent);
        _btnSave.Click += OnSaveCourt;
        _btnSave.Location = new Point(8, 8);
        _btnSave.Visible = false;

        _btnCancel = new Button
        {
            Text = "Cancel",
            Size = new Size(96, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.FontButton,
            Cursor = Cursors.Hand,
            Location = new Point(_btnSave.Right + 8, 8),
            Visible = false,
            FlatAppearance = { BorderSize = 0 }
        };
        _btnCancel.Click += OnCancelEdit;

        buttonPanel.Controls.AddRange([_btnEdit, _btnDelete, _btnSave, _btnCancel]);
        _editorPanel.Controls.Add(buttonPanel);

        layout.Controls.Add(_editorPanel, 1, 0);
        Controls.Add(layout);

        ClearEditor();
        UpdateEditorUI();
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

    private void LoadCourts()
    {
        if (_isLoadingData) return;
        _isLoadingData = true;

        try
        {
            using var db = new BocceDbContext();
            _allCourts = db.Courts
                .OrderBy(c => c.CourtNumber)
                .Select(c => new { c.Id, c.CourtNumber, c.CourtLetter, c.IsActive })
                .AsEnumerable()
                .Select(c => (c.Id, Display: $"{c.CourtNumber} ({c.CourtLetter})" + (c.IsActive ? "" : " - INACTIVE")))
                .ToList();

            _lstCourts.Items.Clear();
            foreach (var (id, display) in _allCourts)
                _lstCourts.Items.Add(new CourtListItem(id, display));

            _selectedCourtId = null;
            ClearEditor();
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

    private void OnCourtSelected(object? sender, EventArgs e)
    {
        if (_isLoadingData) return;

        if (_lstCourts.SelectedItem is not CourtListItem item)
        {
            ClearEditor();
            return;
        }

        _selectedCourtId = item.Id;
        LoadCourtEditor();
    }

    private void LoadCourtEditor()
    {
        if (_selectedCourtId == null) return;

        try
        {
            using var db = new BocceDbContext();
            var court = db.Courts.Find(_selectedCourtId.Value);
            if (court == null) return;

            _lblCourtNumber.Text = court.CourtNumber.ToString();
            _lblCourtLetter.Text = court.CourtLetter;
            _chkActive.Checked = court.IsActive;
            _txtNotes.Text = court.Notes ?? "";

            _courtMode = CourtMode.View;
            UpdateEditorUI();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading court:\n\n{ex.Message}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnAddCourt(object? sender, EventArgs e)
    {
        _selectedCourtId = null;
        _courtMode = CourtMode.Create;
        ClearEditor();
        UpdateEditorUI();
    }

    private void OnEditCourt(object? sender, EventArgs e)
    {
        if (_selectedCourtId == null) return;
        _courtMode = CourtMode.Edit;
        UpdateEditorUI();
    }

    private void OnSaveCourt(object? sender, EventArgs e)
    {
        try
        {
            using var db = new BocceDbContext();

            if (_courtMode == CourtMode.Create)
            {
                int nextNumber = db.Courts.Any() ? db.Courts.Max(c => c.CourtNumber) + 1 : 1;
                string nextLetter = GetLetterForNumber(nextNumber);

                var newCourt = new Court
                {
                    CourtNumber = nextNumber,
                    CourtLetter = nextLetter,
                    IsActive = true,
                    Notes = string.IsNullOrWhiteSpace(_txtNotes.Text) ? null : _txtNotes.Text.Trim()
                };

                db.Courts.Add(newCourt);
                db.SaveChanges();
            }
            else if (_courtMode == CourtMode.Edit)
            {
                var court = db.Courts.Find(_selectedCourtId);
                if (court == null) return;

                court.IsActive = _chkActive.Checked;
                court.Notes = string.IsNullOrWhiteSpace(_txtNotes.Text) ? null : _txtNotes.Text.Trim();

                db.SaveChanges();
            }

            _courtMode = CourtMode.View;
            LoadCourts();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving court:\n\n{ex.Message}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnDeleteCourt(object? sender, EventArgs e)
    {
        if (_selectedCourtId == null) return;

        var result = MessageBox.Show(
            "Are you sure you want to delete this court?\n\nThis will permanently remove it from the system.",
            "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

        if (result != DialogResult.Yes) return;

        try
        {
            using var db = new BocceDbContext();
            var court = db.Courts.Find(_selectedCourtId.Value);
            if (court == null) return;

            // Check if court is used in any schedules
            bool usedInSchedule = db.Matches.Any(m => m.CourtId == court.Id);
            if (usedInSchedule)
            {
                MessageBox.Show(
                    "This court cannot be deleted because it is used in scheduled matches.",
                    "Cannot Delete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Also check playoff matches
            usedInSchedule = db.PlayoffMatches.Any(pm => pm.CourtId == court.Id);
            if (usedInSchedule)
            {
                MessageBox.Show(
                    "This court cannot be deleted because it is used in playoff matches.",
                    "Cannot Delete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int deletedNumber = court.CourtNumber;
            db.Courts.Remove(court);
            db.SaveChanges();

            // Resequence remaining courts
            ResequenceCourts(db, deletedNumber);

            _selectedCourtId = null;
            ClearEditor();
            LoadCourts();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error deleting court:\n\n{ex.Message}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ResequenceCourts(BocceDbContext db, int deletedNumber)
    {
        var courtsToUpdate = db.Courts
            .Where(c => c.CourtNumber > deletedNumber)
            .OrderBy(c => c.CourtNumber)
            .ToList();

        foreach (var court in courtsToUpdate)
        {
            court.CourtNumber--;
            court.CourtLetter = GetLetterForNumber(court.CourtNumber);
        }

        db.SaveChanges();
    }

    private void OnCancelEdit(object? sender, EventArgs e)
    {
        if (_selectedCourtId == null)
        {
            ClearEditor();
        }
        else
        {
            LoadCourtEditor();
        }
    }

    private void ClearEditor()
    {
        _lblCourtNumber.Text = "";
        _lblCourtLetter.Text = "";
        _chkActive.Checked = true;
        _txtNotes.Text = "";
    }

    private void UpdateEditorUI()
    {
        bool hasSelection = _selectedCourtId.HasValue;

        _editorPanel.BackColor = _courtMode switch
        {
            CourtMode.Create => AppTheme.CreateModeBackground,
            CourtMode.Edit => AppTheme.EditModeBackground,
            _ => AppTheme.ContentBackground
        };

        _chkActive.Enabled = _courtMode != CourtMode.View;
        _txtNotes.ReadOnly = _courtMode == CourtMode.View;

        _btnEdit.Visible = _courtMode == CourtMode.View && hasSelection;
        _btnDelete.Visible = _courtMode == CourtMode.View && hasSelection;
        _btnSave.Visible = _courtMode != CourtMode.View;
        _btnCancel.Visible = _courtMode != CourtMode.View;
    }

    private string GetLetterForNumber(int number)
    {
        return ((char)('A' + (number - 1) % 26)).ToString();
    }

    private sealed record CourtListItem(int Id, string Display) { public override string ToString() => Display; }
}

using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.Services;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Panels;

public class SchedulePanel : UserControl
{
    // ── State ──────────────────────────────────────────────────────────────────
    private int? _selectedSeasonId;
    private int? _selectedTemplateId;

    // ── Left panel ─────────────────────────────────────────────────────────────
    private ListBox _lstTemplates = null!;

    // ── Toolbar ────────────────────────────────────────────────────────────────
    private Button _btnGenerate = null!;
    private Button _btnDelete   = null!;

    // ── Right panel ────────────────────────────────────────────────────────────
    private Label         _lblTemplateTitle = null!;
    private Label         _lblStatus        = null!;
    private DataGridView  _grid             = null!;
    private DataGridViewTextBoxColumn  _colWeek  = null!;
    private DataGridViewTextBoxColumn  _colSlot1 = null!;
    private DataGridViewTextBoxColumn  _colVs    = null!;
    private DataGridViewTextBoxColumn  _colSlot2 = null!;
    private DataGridViewComboBoxColumn _colCourt = null!;

    private List<(int Id, string Display)> _seasonCourts = [];

    public SchedulePanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;
        BuildUI();
        AppParameterService.DefaultsChanged += OnDefaultsChanged;
        LoadContext();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            AppParameterService.DefaultsChanged -= OnDefaultsChanged;
        base.Dispose(disposing);
    }

    private void OnDefaultsChanged(object? sender, DefaultsChangedEventArgs e) => LoadContext();

    // ── UI construction ────────────────────────────────────────────────────────

    private void BuildUI()
    {
        // Outer: 2 columns — left list (220px) | right detail (fill)
        var outer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = Padding.Empty,
            Margin  = Padding.Empty,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        };
        outer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(outer);

        // ── Left panel: 3 rows (header | list | toolbar) ──────────────────────
        var leftLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = AppTheme.Surface,
            Padding = Padding.Empty,
            Margin  = Padding.Empty,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        };
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));   // header
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // list
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));   // toolbar
        outer.Controls.Add(leftLayout, 0, 0);

        var lblHeader = new Label
        {
            Text = "Templates",
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.Surface,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0),
            Font = new Font("Segoe UI", 11f, FontStyle.Bold)
        };
        leftLayout.Controls.Add(lblHeader, 0, 0);

        _lstTemplates = new ListBox
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            Font = new Font("Segoe UI", 10f),
            BorderStyle = BorderStyle.None,
            IntegralHeight = false
        };
        _lstTemplates.SelectedIndexChanged += OnTemplateSelected;
        leftLayout.Controls.Add(_lstTemplates, 0, 1);

        var leftToolbar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Separator,
            Padding = new Padding(8)
        };
        _btnGenerate = MakeButton("Generate", AppTheme.Accent);
        _btnGenerate.Click += OnGenerate;
        _btnGenerate.Location = new Point(8, 8);
        leftToolbar.Controls.Add(_btnGenerate);

        _btnDelete = MakeButton("Delete", AppTheme.ButtonDanger);
        _btnDelete.Click += OnDelete;
        _btnDelete.Enabled = false;
        _btnDelete.Location = new Point(_btnGenerate.Right + 8, 8);
        leftToolbar.Controls.Add(_btnDelete);
        leftLayout.Controls.Add(leftToolbar, 0, 2);

        // ── Right panel: 3 rows (title | status | grid) ───────────────────────
        var rightLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = AppTheme.ContentBackground,
            Padding = Padding.Empty,
            Margin  = Padding.Empty,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        };
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));  // title
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));  // status
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // grid
        outer.Controls.Add(rightLayout, 1, 0);

        _lblTemplateTitle = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.Surface,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 0, 0),
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            Text = ""
        };
        rightLayout.Controls.Add(_lblTemplateTitle, 0, 0);

        _lblStatus = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextSecondary,
            BackColor = AppTheme.ContentBackground,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 0, 0),
            Font = new Font("Segoe UI", 9.5f),
            Text = "Select a template on the left, or click Generate."
        };
        rightLayout.Controls.Add(_lblStatus, 0, 1);

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = AppTheme.ContentBackground,
            GridColor = AppTheme.Separator,
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            Font = new Font("Segoe UI", 10f),
            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.TextPrimary,
                SelectionBackColor = AppTheme.NavSelected,
                SelectionForeColor = AppTheme.NavText
            },
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppTheme.NavBackground,
                ForeColor = AppTheme.NavText,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            },
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeight = 30
        };
        _grid.DataError += (_, e) => e.Cancel = true;

        _colWeek  = new DataGridViewTextBoxColumn { Name = "Week",  HeaderText = "Week",       ReadOnly = true, FillWeight = 8  };
        _colSlot1 = new DataGridViewTextBoxColumn { Name = "Slot1", HeaderText = "Team 1 Slot", ReadOnly = true, FillWeight = 18 };
        _colVs    = new DataGridViewTextBoxColumn { Name = "Vs",    HeaderText = "",            ReadOnly = true, FillWeight = 6  };
        _colSlot2 = new DataGridViewTextBoxColumn { Name = "Slot2", HeaderText = "Team 2 Slot", ReadOnly = true, FillWeight = 18 };
        _colCourt = new DataGridViewComboBoxColumn
        {
            Name = "Court",
            HeaderText = "Court",
            FillWeight = 30,
            FlatStyle = FlatStyle.Flat,
            DisplayStyleForCurrentCellOnly = true
        };

        _grid.Columns.AddRange(_colWeek, _colSlot1, _colVs, _colSlot2, _colCourt);
        _grid.CellValueChanged += OnCourtCellChanged;
        _grid.CurrentCellDirtyStateChanged += OnCellDirty;
        rightLayout.Controls.Add(_grid, 0, 2);
    }

    // ── Data loading ───────────────────────────────────────────────────────────

    private void LoadContext()
    {
        try
        {
            using var db = new BocceDbContext();
            _selectedSeasonId = AppParameterService.GetDefaultSeasonId(db);

            _seasonCourts.Clear();
            if (_selectedSeasonId.HasValue)
            {
                _seasonCourts = db.SeasonCourts
                    .Where(sc => sc.SeasonId == _selectedSeasonId.Value)
                    .Include(sc => sc.Court)
                    .OrderBy(sc => sc.Court.CourtNumber)
                    .AsEnumerable()
                    .Select(sc => (
                        sc.CourtId,
                        Display: sc.Court.CourtLetter != ""
                            ? $"Court {sc.Court.CourtNumber} ({sc.Court.CourtLetter})"
                            : $"Court {sc.Court.CourtNumber}"))
                    .ToList();
            }
        }
        catch { }

        PopulateCourtCombo();
        LoadTemplateList();
    }

    private void PopulateCourtCombo()
    {
        _colCourt.Items.Clear();
        foreach (var (_, display) in _seasonCourts)
            _colCourt.Items.Add(display);
    }

    private void LoadTemplateList()
    {
        _lstTemplates.SelectedIndexChanged -= OnTemplateSelected;
        _lstTemplates.Items.Clear();

        if (_selectedSeasonId.HasValue)
        {
            try
            {
                using var db = new BocceDbContext();
                var templates = ScheduleTemplateService.GetTemplatesForSeason(db, _selectedSeasonId.Value);
                foreach (var t in templates)
                    _lstTemplates.Items.Add(new TemplateItem(t.Id, t.TeamCount, t.WeekCount, t.GeneratedAt));
            }
            catch { }
        }

        _lstTemplates.SelectedIndexChanged += OnTemplateSelected;
        _lstTemplates.Refresh();

        if (_lstTemplates.Items.Count == 0)
        {
            _selectedTemplateId = null;
            _btnDelete.Enabled = false;
            _grid.Rows.Clear();
            _lblTemplateTitle.Text = "";
            _lblStatus.Text = _selectedSeasonId.HasValue
                ? "No templates yet. Click Generate to build templates from season divisions."
                : "No season selected. Use the top bar to pick a league and season.";
        }
        else
        {
            _lstTemplates.SelectedIndex = 0;
        }
    }

    private void LoadTemplateDetail(int templateId)
    {
        _grid.Rows.Clear();

        try
        {
            using var db = new BocceDbContext();
            var rows = ScheduleTemplateService.GetTemplateRows(db, templateId);

            // Ensure court combo has items for all courts that appear in this template
            var templateCourtIds = rows.Select(r => r.CourtId).Distinct().ToHashSet();
            foreach (int cid in templateCourtIds)
            {
                if (_seasonCourts.All(c => c.Id != cid))
                {
                    // Court from template is no longer in season courts — add placeholder
                    string placeholder = $"Court #{cid}";
                    if (!_colCourt.Items.Contains(placeholder))
                        _colCourt.Items.Add(placeholder);
                }
            }

            foreach (var r in rows)
            {
                var courtEntry = _seasonCourts.FirstOrDefault(c => c.Id == r.CourtId);
                string courtDisplay = courtEntry != default
                    ? courtEntry.Display
                    : $"Court #{r.CourtId}";

                int rowIdx = _grid.Rows.Add(r.WeekNumber, r.Slot1, "vs", r.Slot2, courtDisplay);
                _grid.Rows[rowIdx].Tag = r;
            }

            if (_grid.Rows.Count > 0)
                _grid.FirstDisplayedScrollingRowIndex = 0;

            int weekCount = rows.Select(r => r.WeekNumber).Distinct().Count();
            _lblStatus.Text = $"{rows.Count} matches across {weekCount} weeks. Edit the Court column to reassign courts.";
        }
        catch (Exception ex)
        {
            _lblStatus.Text = $"Error loading template: {ex.Message}";
        }
    }

    // ── Event handlers ─────────────────────────────────────────────────────────

    private void OnTemplateSelected(object? sender, EventArgs e)
    {
        if (_lstTemplates.SelectedItem is not TemplateItem item)
        {
            _selectedTemplateId = null;
            _btnDelete.Enabled = false;
            _lblTemplateTitle.Text = "";
            _grid.Rows.Clear();
            return;
        }

        _selectedTemplateId = item.Id;
        _btnDelete.Enabled = true;
        _lblTemplateTitle.Text = $"{item.TeamCount}-Team Template  —  {item.WeekCount} weeks  (generated {item.GeneratedAt:yyyy-MM-dd HH:mm})";
        LoadTemplateDetail(item.Id);
    }

    private void OnGenerate(object? sender, EventArgs e)
    {
        if (!_selectedSeasonId.HasValue)
        {
            MessageBox.Show("No season selected. Use the top bar to choose a league and season.",
                "Generate Templates", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_seasonCourts.Count == 0)
        {
            MessageBox.Show(
                "This season has no courts configured.\n\n" +
                "Go to Administration → Courts to add courts, then assign them to this season in Season settings.",
                "Generate Templates", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            using var db = new BocceDbContext();

            var divCounts = db.Divisions
                .Where(d => d.SeasonId == _selectedSeasonId.Value && d.TeamsInDivision > 0)
                .Select(d => d.TeamsInDivision)
                .Distinct()
                .ToList();

            var season = db.Seasons.Find(_selectedSeasonId.Value);
            if (season?.MaxTeamsInDivision > 0 && !divCounts.Contains(season.MaxTeamsInDivision))
                divCounts.Add(season.MaxTeamsInDivision);

            var invalid = divCounts.Where(n => !ScheduleTemplateService.ValidTeamCounts.Contains(n)).ToList();
            if (invalid.Any())
            {
                MessageBox.Show(
                    $"Some divisions have invalid team counts: {string.Join(", ", invalid)}.\n\nOnly 4, 6, and 8 are supported.",
                    "Generate Templates", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (divCounts.Count == 0)
            {
                MessageBox.Show(
                    "No divisions with a teams-in-division value found for this season.\n\n" +
                    "Set Teams in Division on each division (or the season default) before generating.",
                    "Generate Templates", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int weekCount = (season?.WeeksInSeason > 0 ? season.WeeksInSeason : season?.GamesPerSeason) ?? 0;
            if (weekCount <= 0)
            {
                MessageBox.Show("Season must have Weeks in Season or Games Per Season set before generating.",
                    "Generate Templates", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var existing = db.ScheduleTemplates
                .Where(t => t.SeasonId == _selectedSeasonId.Value)
                .Select(t => t.TeamCount).ToList();
            var toReplace = divCounts.Intersect(existing).ToList();
            if (toReplace.Any())
            {
                string msg = "Templates already exist for:\n\n" +
                             string.Join("\n", toReplace.Select(n => $"  • {n}-team")) +
                             "\n\nThey will be replaced. Continue?";
                if (MessageBox.Show(msg, "Generate Templates", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;
            }

            var generated = new List<string>();
            var errors    = new List<string>();
            foreach (int tc in divCounts.OrderBy(n => n))
            {
                try
                {
                    ScheduleTemplateService.Generate(db, _selectedSeasonId.Value, tc);
                    generated.Add($"{tc}-team ({weekCount} weeks)");
                }
                catch (Exception ex)
                {
                    errors.Add($"{tc}-team: {ex.Message}");
                }
            }

            string body = (generated.Any() ? "Generated:\n" + string.Join("\n", generated.Select(s => $"  • {s}")) : "")
                        + (errors.Any() ? "\n\nErrors:\n" + string.Join("\n", errors.Select(s => $"  • {s}")) : "");
            MessageBox.Show(body.Trim(), "Generate Templates", MessageBoxButtons.OK,
                errors.Any() ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Generation failed:\n\n{ex.Message}", "Generate Templates",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        LoadTemplateList();
    }

    private void OnDelete(object? sender, EventArgs e)
    {
        if (_lstTemplates.SelectedItem is not TemplateItem item) return;

        if (MessageBox.Show(
                $"Delete the {item.TeamCount}-team template ({item.WeekCount} weeks)?\n\nThis cannot be undone.",
                "Delete Template", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        try
        {
            using var db = new BocceDbContext();
            ScheduleTemplateService.DeleteTemplate(db, item.Id);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Delete failed:\n\n{ex.Message}", "Delete Template",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        LoadTemplateList();
    }

    private void OnCellDirty(object? sender, EventArgs e)
    {
        if (_grid.CurrentCell?.OwningColumn == _colCourt && _grid.IsCurrentCellDirty)
            _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
    }

    private void OnCourtCellChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex != _grid.Columns.IndexOf(_colCourt)) return;
        if (_grid.Rows[e.RowIndex].Tag is not ScheduleTemplateService.TemplateRow row) return;

        string? selectedDisplay = _grid.Rows[e.RowIndex].Cells[_colCourt.Name].Value?.ToString();
        if (selectedDisplay == null) return;

        var courtEntry = _seasonCourts.FirstOrDefault(c => c.Display == selectedDisplay);
        if (courtEntry == default) return;

        try
        {
            using var db = new BocceDbContext();
            ScheduleTemplateService.UpdateMatchCourt(db, row.MatchId, courtEntry.Id);
            _lblStatus.Text = $"Court updated — Week {row.WeekNumber}, {row.Slot1} vs {row.Slot2}.";
            _grid.Rows[e.RowIndex].Tag = row with { CourtId = courtEntry.Id };
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not save court change:\n\n{ex.Message}", "Save Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

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

    private sealed class TemplateItem(int id, int teamCount, int weekCount, DateTime generatedAt)
    {
        public int Id           { get; } = id;
        public int TeamCount    { get; } = teamCount;
        public int WeekCount    { get; } = weekCount;
        public DateTime GeneratedAt { get; } = generatedAt;
        public override string ToString() => $"{TeamCount}-Team  ({WeekCount} wks)";
    }
}

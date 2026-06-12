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
    private bool _isEditingCourt = false;

    // ── Left panel ─────────────────────────────────────────────────────────────
    private ListBox _lstTemplates = null!;

    // ── Toolbar ────────────────────────────────────────────────────────────────
    private Button _btnGenerate = null!;
    private Button _btnDelete   = null!;
    private Label  _lblStatus   = null!;

    // ── Right panel ────────────────────────────────────────────────────────────
    private Label         _lblTemplateTitle = null!;
    private DataGridView  _grid             = null!;
    private DataGridViewTextBoxColumn  _colWeek   = null!;
    private DataGridViewTextBoxColumn  _colSlot1  = null!;
    private DataGridViewTextBoxColumn  _colVs     = null!;
    private DataGridViewTextBoxColumn  _colSlot2  = null!;
    private DataGridViewComboBoxColumn _colCourt  = null!;

    // court lookup for the combo column
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
        // Outer table: left list (220px) + right detail (fill)
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

        // ── Left panel ──────────────────────────────────────────────────────────
        var left = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface };
        outer.Controls.Add(left, 0, 0);

        var lblHeader = new Label
        {
            Text = "Templates",
            Dock = DockStyle.Top,
            Height = 36,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.Surface,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0),
            Font = new Font("Segoe UI", 11f, FontStyle.Bold)
        };
        left.Controls.Add(lblHeader);

        _lstTemplates = new ListBox
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            Font = new Font("Segoe UI", 10f),
            BorderStyle = BorderStyle.None,
            ItemHeight = 28
        };
        _lstTemplates.SelectedIndexChanged += OnTemplateSelected;
        left.Controls.Add(_lstTemplates);

        // toolbar at bottom of left panel
        var leftToolbar = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
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

        left.Controls.Add(leftToolbar);

        // ── Right panel ─────────────────────────────────────────────────────────
        var right = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.ContentBackground };
        outer.Controls.Add(right, 1, 0);

        // Status bar at top
        var topBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 40,
            BackColor = AppTheme.Surface,
            Padding = new Padding(10, 0, 10, 0)
        };
        _lblStatus = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextSecondary,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9.5f),
            Text = "Select a template on the left, or click Generate."
        };
        topBar.Controls.Add(_lblStatus);
        right.Controls.Add(topBar);

        _lblTemplateTitle = new Label
        {
            Dock = DockStyle.Top,
            Height = 32,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.ContentBackground,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 0, 0),
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            Text = ""
        };
        right.Controls.Add(_lblTemplateTitle);

        // Grid
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
                SelectionForeColor = AppTheme.TextPrimary
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

        _colWeek  = new DataGridViewTextBoxColumn { Name = "Week",  HeaderText = "Week",  ReadOnly = true, FillWeight = 10 };
        _colSlot1 = new DataGridViewTextBoxColumn { Name = "Slot1", HeaderText = "Team 1 Slot", ReadOnly = true, FillWeight = 20 };
        _colVs    = new DataGridViewTextBoxColumn { Name = "Vs",    HeaderText = "vs",    ReadOnly = true, FillWeight = 8 };
        _colSlot2 = new DataGridViewTextBoxColumn { Name = "Slot2", HeaderText = "Team 2 Slot", ReadOnly = true, FillWeight = 20 };
        _colCourt = new DataGridViewComboBoxColumn
        {
            Name = "Court",
            HeaderText = "Court",
            FillWeight = 25,
            FlatStyle = FlatStyle.Flat,
            DisplayStyleForCurrentCellOnly = true
        };

        _grid.Columns.AddRange(_colWeek, _colSlot1, _colVs, _colSlot2, _colCourt);
        _grid.CellValueChanged    += OnCourtCellChanged;
        _grid.CurrentCellDirtyStateChanged += OnCellDirty;
        right.Controls.Add(_grid);
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
                    .Select(sc => new { sc.CourtId, Display = sc.Court.CourtLetter != ""
                        ? $"Court {sc.Court.CourtNumber} ({sc.Court.CourtLetter})"
                        : $"Court {sc.Court.CourtNumber}" })
                    .AsEnumerable()
                    .Select(x => (x.CourtId, x.Display))
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

            foreach (var r in rows)
            {
                int courtDisplayIdx = _seasonCourts.FindIndex(c => c.Id == r.CourtId);
                string courtDisplay = courtDisplayIdx >= 0 ? _seasonCourts[courtDisplayIdx].Display : $"Court #{r.CourtId}";

                int rowIdx = _grid.Rows.Add(r.WeekNumber, r.Slot1, "vs", r.Slot2, courtDisplay);
                _grid.Rows[rowIdx].Tag = r;
            }

            _lblStatus.Text = $"{rows.Count} matches across {rows.Select(r => r.WeekNumber).Distinct().Count()} weeks. Court column is editable.";
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
            MessageBox.Show("This season has no courts configured.\n\nGo to Administration → Courts to add courts, then use Season settings to assign them to this season.",
                "Generate Templates", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            using var db = new BocceDbContext();

            // Find distinct team counts across all divisions in this season
            var teamCounts = db.Divisions
                .Where(d => d.SeasonId == _selectedSeasonId.Value && d.TeamsInDivision > 0)
                .Select(d => d.TeamsInDivision)
                .Distinct()
                .OrderBy(n => n)
                .ToList();

            // Also check season default
            var season = db.Seasons.Find(_selectedSeasonId.Value);
            if (season?.MaxTeamsInDivision > 0 && !teamCounts.Contains(season.MaxTeamsInDivision))
                teamCounts.Add(season.MaxTeamsInDivision);

            var invalidCounts = teamCounts.Where(n => !ScheduleTemplateService.ValidTeamCounts.Contains(n)).ToList();
            if (invalidCounts.Any())
            {
                MessageBox.Show(
                    $"Some divisions have invalid team counts: {string.Join(", ", invalidCounts)}.\n\n" +
                    "Only 4, 6, and 8 are supported. Fix the divisions first.",
                    "Generate Templates", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (teamCounts.Count == 0)
            {
                MessageBox.Show("No divisions with a teams-in-division value found for this season.\n\n" +
                    "Set the Teams in Division on each division (or the season default) before generating templates.",
                    "Generate Templates", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Check season has week count
            int weekCount = (season?.WeeksInSeason > 0 ? season.WeeksInSeason : season?.GamesPerSeason) ?? 0;
            if (weekCount <= 0)
            {
                MessageBox.Show("Season must have Weeks in Season or Games Per Season set before generating templates.",
                    "Generate Templates", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Confirm replacement if templates already exist
            var existingCounts = db.ScheduleTemplates
                .Where(t => t.SeasonId == _selectedSeasonId.Value)
                .Select(t => t.TeamCount)
                .ToList();
            var toReplace = teamCounts.Intersect(existingCounts).ToList();

            if (toReplace.Any())
            {
                string msg = $"Templates for the following team counts already exist and will be replaced:\n\n" +
                             $"{string.Join(", ", toReplace.Select(n => $"{n}-team"))}\n\n" +
                             $"Continue?";
                if (MessageBox.Show(msg, "Generate Templates", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;
            }

            var generated = new List<string>();
            var errors    = new List<string>();

            foreach (int tc in teamCounts)
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

            string summary = generated.Any()
                ? $"Generated:\n  • {string.Join("\n  • ", generated)}"
                : "";
            string errSummary = errors.Any()
                ? $"\n\nErrors:\n  • {string.Join("\n  • ", errors)}"
                : "";

            MessageBox.Show(summary + errSummary,
                "Generate Templates", MessageBoxButtons.OK,
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
        if (!_selectedTemplateId.HasValue) return;

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
            _lblStatus.Text = $"Court updated for Week {row.WeekNumber}, {row.Slot1} vs {row.Slot2}.";

            // Update the row tag with the new court id
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
        public int Id          { get; } = id;
        public int TeamCount   { get; } = teamCount;
        public int WeekCount   { get; } = weekCount;
        public DateTime GeneratedAt { get; } = generatedAt;
        public override string ToString() => $"{TeamCount}-Team  ({WeekCount} wks)";
    }
}

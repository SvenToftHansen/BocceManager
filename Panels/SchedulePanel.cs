using System.Drawing.Printing;
using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.Services;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Panels;

public class SchedulePanel : UserControl
{
    // ── State ──────────────────────────────────────────────────────────────────
    private int?      _selectedSeasonId;
    private int?      _selectedTemplateId;
    private DateOnly? _seasonStartDate;
    private string    _courtDisplay = "number";
    private bool      _locked;
    private bool      _loading;

    // ── Left panel ─────────────────────────────────────────────────────────────
    private ListBox _lstTemplates = null!;

    // ── Toolbar ────────────────────────────────────────────────────────────────
    private Button _btnGenerate = null!;
    private Button _btnDelete   = null!;
    private Button _btnPrint    = null!;

    // ── Right panel ────────────────────────────────────────────────────────────
    private Label        _lblTemplateTitle = null!;
    private Label        _lblStatus        = null!;
    private DataGridView _grid             = null!;
    private DataGridViewTextBoxColumn _colWeek = null!;
    private Button _btnLock = null!;

    private List<(int Id, string Display)> _seasonCourts = [];

    public SchedulePanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;
        BuildUI();
        AppParameterService.DefaultsChanged += OnDefaultsChanged;
        HandleCreated += (_, _) => BeginInvoke(new Action(LoadContext));
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
        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.FontDefault,
            Padding = new Point(12, 6)
        };
        Controls.Add(tabs);

        var templatesTab = new TabPage("  Templates  ");
        tabs.TabPages.Add(templatesTab);

        var divSchedulesTab = new TabPage("  Division Schedules  ");
        BuildDivisionSchedulesTab(divSchedulesTab);
        tabs.TabPages.Add(divSchedulesTab);

        // Outer: left list (220px) | right detail (fill)
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
        templatesTab.Controls.Add(outer);

        // ── Left panel ────────────────────────────────────────────────────────
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
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        outer.Controls.Add(leftLayout, 0, 0);

        leftLayout.Controls.Add(new Label
        {
            Text = "Templates",
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.Surface,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0),
            Font = new Font("Segoe UI", 11f, FontStyle.Bold)
        }, 0, 0);

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

        // ── Right panel ───────────────────────────────────────────────────────
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
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        outer.Controls.Add(rightLayout, 1, 0);

        // Title row: label | Lock | Print
        var titleRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = AppTheme.Surface,
            Padding = Padding.Empty,
            Margin = Padding.Empty,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        };
        titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 106));

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
        titleRow.Controls.Add(_lblTemplateTitle, 0, 0);

        _btnLock = new Button
        {
            Dock = DockStyle.Fill,
            Text = "🔓 Unlocked",
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.FontButton,
            Cursor = Cursors.Hand,
            Margin = new Padding(4),
            Enabled = false
        };
        _btnLock.FlatAppearance.BorderSize = 1;
        _btnLock.FlatAppearance.BorderColor = AppTheme.Separator;
        _btnLock.Click += OnLockToggle;
        titleRow.Controls.Add(_btnLock, 1, 0);

        _btnPrint = new Button
        {
            Dock = DockStyle.Fill,
            Text = "Print...",
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.FontButton,
            Cursor = Cursors.Hand,
            Margin = new Padding(4, 4, 8, 4),
            Enabled = false
        };
        _btnPrint.FlatAppearance.BorderSize = 1;
        _btnPrint.FlatAppearance.BorderColor = AppTheme.Separator;
        _btnPrint.Click += OnPrint;
        titleRow.Controls.Add(_btnPrint, 2, 0);

        rightLayout.Controls.Add(titleRow, 0, 0);

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
            SelectionMode = DataGridViewSelectionMode.CellSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ReadOnly = true,
            Font = new Font("Segoe UI", 10f),
            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.TextPrimary,
                SelectionBackColor = AppTheme.NavSelected,
                SelectionForeColor = AppTheme.NavText,
                Alignment = DataGridViewContentAlignment.MiddleCenter
            },
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppTheme.NavBackground,
                ForeColor = AppTheme.NavText,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            },
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeight = 30
        };

        _colWeek = new DataGridViewTextBoxColumn
        {
            Name = "Week",
            HeaderText = "Week Of",
            ReadOnly = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            Width = 145,
            MinimumWidth = 145,
            Resizable = DataGridViewTriState.False,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                BackColor = AppTheme.ContentBackground,
                ForeColor = AppTheme.TextSecondary,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            }
        };

        _grid.DataError += (_, de) => de.ThrowException = false;
        _grid.CellClick += OnCellClick;
        _grid.CellMouseEnter += (_, e) =>
        {
            if (!_locked && e.ColumnIndex > 0 && e.RowIndex >= 0)
                _grid.Cursor = Cursors.Hand;
        };
        _grid.CellMouseLeave += (_, _) => _grid.Cursor = Cursors.Default;
        _grid.Columns.Add(_colWeek);

        rightLayout.Controls.Add(_grid, 0, 2);
    }

    // ── Data loading ───────────────────────────────────────────────────────────

    private void LoadContext()
    {
        try
        {
            using var db = new BocceDbContext();
            _selectedSeasonId = AppParameterService.GetDefaultSeasonId(db);
            _seasonStartDate  = null;
            _courtDisplay     = "number";

            if (_selectedSeasonId.HasValue)
            {
                var season = db.Seasons.Find(_selectedSeasonId.Value);
                _seasonStartDate = season?.StartDate;
                _courtDisplay = AppParameterService.GetAppParameter(db, "CourtDisplay") ?? "number";

                _seasonCourts = db.Courts
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.CourtNumber)
                    .AsEnumerable()
                    .Select(c => (c.Id, Display: CourtLabel(c, _courtDisplay)))
                    .ToList();
            }
            else
            {
                _seasonCourts.Clear();
            }
        }
        catch { }

        LoadTemplateList();
        LoadDivisionsList();
    }

    private static string CourtLabel(Court court, string display) =>
        display == "letter" && court.CourtLetter != ""
            ? $"Court {court.CourtLetter}"
            : $"Court {court.CourtNumber}";

    private string WeekLabel(int weekNumber)
    {
        if (!_seasonStartDate.HasValue)
            return $"Week {weekNumber}";
        DateOnly start = _seasonStartDate.Value.AddDays((weekNumber - 1) * 7);
        DateOnly end   = start.AddDays(6);
        return $"{start:MMM d} – {end:MMM d}";
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
                    _lstTemplates.Items.Add(new TemplateItem(t.Id, t.TeamCount, t.WeekCount, t.GeneratedAt, t.IsLocked));
            }
            catch { }
        }

        _lstTemplates.SelectedIndexChanged += OnTemplateSelected;
        _lstTemplates.Refresh();

        if (_lstTemplates.Items.Count == 0)
        {
            _selectedTemplateId = null;
            _btnDelete.Enabled  = false;
            _btnPrint.Enabled   = false;
            ClearGrid();
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

    private void ClearGrid()
    {
        _grid.Rows.Clear();
        while (_grid.Columns.Count > 1)
            _grid.Columns.RemoveAt(1);
    }

    private void RebuildCourtColumns(List<(int Id, string Display)> courts)
    {
        while (_grid.Columns.Count > 1)
            _grid.Columns.RemoveAt(1);

        foreach (var (id, display) in courts)
        {
            var col = new DataGridViewTextBoxColumn
            {
                Name = $"Court_{id}",
                HeaderText = display,
                ReadOnly = true,
                Tag = id,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            _grid.Columns.Add(col);
        }
    }

    private void LoadTemplateDetail(int templateId)
    {
        _loading = true;
        _grid.Rows.Clear();

        try
        {
            using var db = new BocceDbContext();
            var rows = ScheduleTemplateService.GetTemplateRows(db, templateId);

            if (rows.Count == 0)
            {
                RebuildCourtColumns([]);
                _lblStatus.Text = "Template has no matches.";
                return;
            }

            // Courts used in this template, natural order by CourtNumber
            var courtIds = rows.Select(r => r.CourtId).Distinct().ToList();
            var courts = db.Courts
                .Where(c => courtIds.Contains(c.Id))
                .OrderBy(c => c.CourtNumber)
                .AsEnumerable()
                .Select(c => (c.Id, Display: CourtLabel(c, _courtDisplay)))
                .ToList();

            RebuildCourtColumns(courts);

            // One row per week — pivot matches into court columns
            var byWeek = rows.GroupBy(r => r.WeekNumber).OrderBy(g => g.Key).ToList();

            foreach (var weekGroup in byWeek)
            {
                var matchesByCourt = weekGroup.ToDictionary(
                    r => r.CourtId,
                    r => new MatchEntry(r.MatchId, r.Slot1, r.Slot2));

                var values = new object[1 + courts.Count];
                values[0] = WeekLabel(weekGroup.Key);

                for (int i = 0; i < courts.Count; i++)
                {
                    if (matchesByCourt.TryGetValue(courts[i].Id, out var m))
                    {
                        // Always display in alpha order
                        string s1 = string.Compare(m.Slot1, m.Slot2, StringComparison.Ordinal) <= 0 ? m.Slot1 : m.Slot2;
                        string s2 = string.Compare(m.Slot1, m.Slot2, StringComparison.Ordinal) <= 0 ? m.Slot2 : m.Slot1;
                        values[i + 1] = $"{s1} vs {s2}";
                    }
                    else
                    {
                        values[i + 1] = "";
                    }
                }

                int rowIdx = _grid.Rows.Add(values);
                _grid.Rows[rowIdx].Tag = new WeekDisplayRow(weekGroup.Key, matchesByCourt);
            }

            if (_grid.Rows.Count > 0)
                _grid.FirstDisplayedScrollingRowIndex = 0;

            _lblStatus.Text = $"{rows.Count} matches across {byWeek.Count} weeks.  Click a court cell to swap teams.";
        }
        catch (Exception ex)
        {
            _lblStatus.Text = $"Error loading template: {ex.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    // ── Event handlers ─────────────────────────────────────────────────────────

    private void OnTemplateSelected(object? sender, EventArgs e)
    {
        if (_lstTemplates.SelectedItem is not TemplateItem item)
        {
            _selectedTemplateId    = null;
            _btnDelete.Enabled     = false;
            _btnPrint.Enabled      = false;
            _btnLock.Enabled       = false;
            _lblTemplateTitle.Text = "";
            ClearGrid();
            _locked = false;
            ApplyLockState();
            return;
        }

        _selectedTemplateId    = item.Id;
        _btnDelete.Enabled     = true;
        _btnPrint.Enabled      = true;
        _lblTemplateTitle.Text = $"{item.TeamCount}-Team Template  —  {item.WeekCount} weeks  (generated {item.GeneratedAt:yyyy-MM-dd HH:mm})";

        _btnLock.Enabled = true;
        _locked          = item.IsLocked;
        ApplyLockState();

        ClearGrid();
        LoadTemplateDetail(item.Id);
    }

    private void OnGenerate(object? sender, EventArgs e)
    {
        if (_locked)
        {
            MessageBox.Show("Schedule is locked. Click 🔒 Locked to unlock before regenerating.",
                "Locked", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!_selectedSeasonId.HasValue)
        {
            MessageBox.Show("No season selected. Use the top bar to choose a league and season.",
                "Generate Templates", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_seasonCourts.Count == 0)
        {
            MessageBox.Show(
                "No active courts found.\n\nGo to Administration → Courts to add courts.",
                "Generate Templates", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            using var db = new BocceDbContext();

            // Collect team counts from divisions (round odd counts down to even)
            var divCounts = db.Divisions
                .Where(d => d.SeasonId == _selectedSeasonId.Value && d.TeamsInDivision > 0)
                .Select(d => d.TeamsInDivision % 2 == 1 ? d.TeamsInDivision - 1 : d.TeamsInDivision)
                .Where(d => d > 0)
                .Distinct()
                .ToList();

            var season = db.Seasons.Find(_selectedSeasonId.Value);
            if (season?.MaxTeamsInDivision > 0)
            {
                int maxEven = season.MaxTeamsInDivision % 2 == 1 ? season.MaxTeamsInDivision - 1 : season.MaxTeamsInDivision;
                if (maxEven > 0 && !divCounts.Contains(maxEven))
                    divCounts.Add(maxEven);
            }

            if (divCounts.Count == 0)
            {
                MessageBox.Show(
                    "No divisions with valid team counts found for this season.\n\n" +
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

            // Check which templates are locked vs unlocked
            var existing = db.ScheduleTemplates
                .Where(t => t.SeasonId == _selectedSeasonId.Value)
                .ToDictionary(t => t.TeamCount, t => t.IsLocked);

            var locked   = divCounts.Where(tc => existing.ContainsKey(tc) && existing[tc]).ToList();
            var unlocked = divCounts.Where(tc => !existing.ContainsKey(tc) || !existing[tc]).ToList();

            var statusMsg = "Teams in divisions (rounded down if odd):\n\n";
            if (unlocked.Any())
                statusMsg += "Will generate:\n" + string.Join("\n", unlocked.OrderBy(n => n).Select(n => $"  • {n}-team"));
            if (locked.Any())
                statusMsg += (unlocked.Any() ? "\n\n" : "") + "Locked (will skip):\n" + string.Join("\n", locked.OrderBy(n => n).Select(n => $"  • {n}-team"));

            statusMsg += "\n\nContinue?";

            if (MessageBox.Show(statusMsg, "Generate Templates", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            var generated = new List<string>();
            var errors    = new List<string>();
            var skipped   = new List<string>();

            foreach (int tc in divCounts.OrderBy(n => n))
            {
                if (existing.ContainsKey(tc) && existing[tc])
                {
                    skipped.Add($"{tc}-team (locked)");
                    continue;
                }

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

            string body = "";
            if (generated.Any())
                body += "Generated:\n" + string.Join("\n", generated.Select(s => $"  • {s}"));
            if (skipped.Any())
                body += (generated.Any() ? "\n\n" : "") + "Skipped:\n" + string.Join("\n", skipped.Select(s => $"  • {s}"));
            if (errors.Any())
                body += (generated.Any() || skipped.Any() ? "\n\n" : "") + "Errors:\n" + string.Join("\n", errors.Select(s => $"  • {s}"));

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

        try
        {
            using var db = new BocceDbContext();
            var divisionScheduleCount = db.ScheduleDivisions.Count(s => s.TemplateId == item.Id);

            var message = $"Delete the {item.TeamCount}-team template ({item.WeekCount} weeks)?";
            if (divisionScheduleCount > 0)
                message += $"\n\n⚠️ This template has {divisionScheduleCount} division schedule(s) attached.\nThey will be permanently removed.";
            message += "\n\nThis cannot be undone.";

            if (MessageBox.Show(message, "Delete Template", MessageBoxButtons.YesNo,
                divisionScheduleCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            ScheduleTemplateService.DeleteTemplate(db, item.Id);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Delete failed:\n\n{ex.Message}", "Delete Template",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        LoadTemplateList();
        LoadDivisionsList();
    }

    // ── Print ──────────────────────────────────────────────────────────────────

    private void OnPrint(object? sender, EventArgs e)
    {
        if (!_selectedSeasonId.HasValue) return;
        var templates = _lstTemplates.Items.Cast<TemplateItem>().ToList();
        if (templates.Count == 0) return;

        try
        {
            var sections = SchedulePrintService.BuildSections(_selectedSeasonId.Value);
            var doc      = SchedulePrintService.BuildDocument(sections);
            doc.DocumentName = templates.Count == 1
                ? $"{templates[0].TeamCount}-Team Template"
                : "Schedule Templates";
            SchedulePrintService.ShowPrintPreview(this, doc);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not build print preview:\n\n{ex.Message}", "Print",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ── Lock / unlock ──────────────────────────────────────────────────────────

    private void OnLockToggle(object? sender, EventArgs e)
    {
        if (_selectedTemplateId == null) return;
        _locked = !_locked;

        try
        {
            using var db = new BocceDbContext();
            var tmpl = db.ScheduleTemplates.Find(_selectedTemplateId.Value);
            if (tmpl != null)
            {
                tmpl.IsLocked = _locked;
                db.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            _locked = !_locked;  // revert on failure
            MessageBox.Show($"Could not save lock state:\n{ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        // Keep the in-memory TemplateItem in sync so switching templates restores correctly
        if (_lstTemplates.SelectedItem is TemplateItem currentItem)
            currentItem.IsLocked = _locked;

        ApplyLockState();
    }

    private void ApplyLockState()
    {
        if (_locked)
        {
            _btnLock.Text = "🔒 Locked";
            _btnLock.BackColor = Color.FromArgb(180, 100, 0);
            _btnLock.ForeColor = Color.White;
            _btnLock.FlatAppearance.BorderColor = Color.FromArgb(180, 100, 0);
        }
        else
        {
            _btnLock.Text = "🔓 Unlocked";
            _btnLock.BackColor = AppTheme.Surface;
            _btnLock.ForeColor = AppTheme.TextPrimary;
            _btnLock.FlatAppearance.BorderColor = AppTheme.Separator;
        }
    }

    // ── Cell click → team swap popup ──────────────────────────────────────────

    private void OnCellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (_locked || _loading || e.RowIndex < 0 || e.ColumnIndex <= 0) return;
        if (_grid.Rows[e.RowIndex].Tag is not WeekDisplayRow weekRow) return;
        if (_grid.Columns[e.ColumnIndex].Tag is not int courtId) return;
        if (!weekRow.Matches.TryGetValue(courtId, out var match)) return;

        // All valid team letters derived from this week's matches
        var allSlots = weekRow.Matches.Values
            .SelectMany(m => new[] { m.Slot1, m.Slot2 })
            .Distinct()
            .OrderBy(s => s)
            .ToList();

        // Current assignment in alpha order for display
        string cur1 = string.Compare(match.Slot1, match.Slot2, StringComparison.Ordinal) <= 0 ? match.Slot1 : match.Slot2;
        string cur2 = string.Compare(match.Slot1, match.Slot2, StringComparison.Ordinal) <= 0 ? match.Slot2 : match.Slot1;

        var (newSlot1, newSlot2, ok) = ShowTeamSwapDialog(cur1, cur2, allSlots);
        if (!ok) return;

        // Normalize to alpha order
        if (string.Compare(newSlot1, newSlot2, StringComparison.Ordinal) > 0)
            (newSlot1, newSlot2) = (newSlot2, newSlot1);

        if (newSlot1 == cur1 && newSlot2 == cur2) return;

        ApplyCascadeSwap(e.RowIndex, courtId, newSlot1, newSlot2, weekRow);
    }

    private (string Slot1, string Slot2, bool Ok) ShowTeamSwapDialog(
        string cur1, string cur2, List<string> allSlots)
    {
        using var dlg = new Form
        {
            Text = "Change Court Teams",
            Width = 300,
            Height = 165,
            StartPosition = FormStartPosition.CenterParent,
            BackColor = AppTheme.ContentBackground,
            Font = AppTheme.FontDefault,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var lblPrompt = new Label
        {
            Text = "Assign teams to this court:",
            Location = new Point(16, 12),
            Size = new Size(260, 18),
            ForeColor = AppTheme.TextSecondary,
            Font = new Font("Segoe UI", 9f)
        };

        var cmbA = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(16, 36),
            Size = new Size(80, 28),
            Font = AppTheme.FontDefault
        };
        foreach (var s in allSlots) cmbA.Items.Add(s);
        cmbA.SelectedItem = cur1;

        var lblVs = new Label
        {
            Text = "vs",
            Location = new Point(106, 40),
            Size = new Size(28, 20),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = AppTheme.TextSecondary
        };

        var cmbB = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(144, 36),
            Size = new Size(80, 28),
            Font = AppTheme.FontDefault
        };
        foreach (var s in allSlots) cmbB.Items.Add(s);
        cmbB.SelectedItem = cur2;

        var btnOk = new Button
        {
            Text = "OK",
            Location = new Point(112, 96),
            Size = new Size(70, 28),
            DialogResult = DialogResult.OK,
            BackColor = AppTheme.Accent,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnOk.FlatAppearance.BorderSize = 0;

        var btnCancel = new Button
        {
            Text = "Cancel",
            Location = new Point(192, 96),
            Size = new Size(70, 28),
            DialogResult = DialogResult.Cancel,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            FlatStyle = FlatStyle.Flat
        };
        btnCancel.FlatAppearance.BorderColor = AppTheme.Separator;

        dlg.Controls.AddRange(new Control[] { lblPrompt, cmbA, lblVs, cmbB, btnOk, btnCancel });
        dlg.AcceptButton = btnOk;
        dlg.CancelButton = btnCancel;

        if (dlg.ShowDialog(this) != DialogResult.OK)
            return ("", "", false);

        string s1 = cmbA.SelectedItem?.ToString() ?? "";
        string s2 = cmbB.SelectedItem?.ToString() ?? "";

        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2) || s1 == s2)
        {
            MessageBox.Show("Please select two different teams.", "Invalid Selection",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return ("", "", false);
        }

        return (s1, s2, true);
    }

    private void ApplyCascadeSwap(
        int rowIndex, int targetCourtId, string newSlot1, string newSlot2, WeekDisplayRow weekRow)
    {
        var targetMatch = weekRow.Matches[targetCourtId];
        var oldSlots = new HashSet<string>(StringComparer.Ordinal) { targetMatch.Slot1, targetMatch.Slot2 };
        var newSlots = new HashSet<string>(StringComparer.Ordinal) { newSlot1, newSlot2 };

        // Teams moving into the target court (alphabetical for positional consistency)
        var arriving = newSlots.Except(oldSlots).OrderBy(s => s).ToList();
        // Teams displaced from the target court (same positional order)
        var leaving  = oldSlots.Except(newSlots).OrderBy(s => s).ToList();

        // Find the source court for each arriving team
        var sourceCourts = new List<(int CourtId, MatchEntry Match)>();
        foreach (var team in arriving)
        {
            foreach (var (cid, m) in weekRow.Matches)
            {
                if (cid == targetCourtId) continue;
                if (m.Slot1 == team || m.Slot2 == team)
                {
                    sourceCourts.Add((cid, m));
                    break;
                }
            }
        }

        try
        {
            using var db = new BocceDbContext();

            // Update target match
            var dbTarget = db.ScheduleTemplateMatches.Find(targetMatch.MatchId)!;
            dbTarget.Slot1 = newSlot1;
            dbTarget.Slot2 = newSlot2;

            // Positionally swap the displaced teams into each source court
            for (int i = 0; i < arriving.Count; i++)
            {
                if (i >= sourceCourts.Count) break;
                var (_, srcMatch) = sourceCourts[i];
                var arrivingTeam  = arriving[i];
                var leavingTeam   = i < leaving.Count ? leaving[i] : leaving[0];

                var dbSrc = db.ScheduleTemplateMatches.Find(srcMatch.MatchId)!;
                dbSrc.Slot1 = dbSrc.Slot1 == arrivingTeam ? leavingTeam : dbSrc.Slot1;
                dbSrc.Slot2 = dbSrc.Slot2 == arrivingTeam ? leavingTeam : dbSrc.Slot2;
                if (string.Compare(dbSrc.Slot1, dbSrc.Slot2, StringComparison.Ordinal) > 0)
                    (dbSrc.Slot1, dbSrc.Slot2) = (dbSrc.Slot2, dbSrc.Slot1);
            }

            db.SaveChanges();

            // Rebuild the updated match dictionary for this row's tag
            var updated = new Dictionary<int, MatchEntry>(weekRow.Matches)
            {
                [targetCourtId] = new MatchEntry(targetMatch.MatchId, newSlot1, newSlot2)
            };

            for (int i = 0; i < arriving.Count; i++)
            {
                if (i >= sourceCourts.Count) break;
                var (srcCourtId, srcMatch) = sourceCourts[i];
                var arrivingTeam = arriving[i];
                var leavingTeam  = i < leaving.Count ? leaving[i] : leaving[0];

                string s1 = srcMatch.Slot1 == arrivingTeam ? leavingTeam : srcMatch.Slot1;
                string s2 = srcMatch.Slot2 == arrivingTeam ? leavingTeam : srcMatch.Slot2;
                if (string.Compare(s1, s2, StringComparison.Ordinal) > 0) (s1, s2) = (s2, s1);
                updated[srcCourtId] = new MatchEntry(srcMatch.MatchId, s1, s2);
            }

            // Update the row in the grid
            var gridRow = _grid.Rows[rowIndex];
            gridRow.Tag = weekRow with { Matches = updated };

            for (int ci = 1; ci < _grid.Columns.Count; ci++)
            {
                if (_grid.Columns[ci].Tag is int cid && updated.TryGetValue(cid, out var um))
                {
                    string s1 = string.Compare(um.Slot1, um.Slot2, StringComparison.Ordinal) <= 0 ? um.Slot1 : um.Slot2;
                    string s2 = string.Compare(um.Slot1, um.Slot2, StringComparison.Ordinal) <= 0 ? um.Slot2 : um.Slot1;
                    gridRow.Cells[ci].Value = $"{s1} vs {s2}";
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Swap failed:\n\n{ex.Message}", "Edit Schedule",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ── Division Schedules Tab ────────────────────────────────────────────────

    private ListBox _lstDivisions = null!;
    private DataGridView _gridDivisionSchedules = null!;
    private Button _btnGenerateCurrent = null!;
    private Button _btnGenerateAll = null!;
    private Button _btnDeleteDivSchedule = null!;
    private Button _btnPrintDivSchedule = null!;
    private Label _lblDivStatus = null!;
    private Label _lblDayHeader = null!;
    private List<(int Id, string Name)> _divisionsForSchedule = [];

    private void BuildDivisionSchedulesTab(TabPage tab)
    {
        tab.BackColor = AppTheme.ContentBackground;

        var outer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = Padding.Empty,
            Margin = Padding.Empty,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        };
        outer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // Left panel
        var leftLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = AppTheme.Surface,
            Padding = Padding.Empty,
            Margin = Padding.Empty,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        };
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));

        leftLayout.Controls.Add(new Label
        {
            Text = "Divisions",
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.Surface,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0),
            Font = new Font("Segoe UI", 11f, FontStyle.Bold)
        }, 0, 0);

        _lstDivisions = new ListBox
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            Font = new Font("Segoe UI", 10f),
            BorderStyle = BorderStyle.None,
            IntegralHeight = false
        };
        _lstDivisions.SelectedIndexChanged += (_, _) => OnDivisionSelected();
        leftLayout.Controls.Add(_lstDivisions, 0, 1);

        var leftToolbar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Separator,
            Padding = new Padding(8)
        };

        // Row 1: Generate buttons
        _btnGenerateCurrent = MakeButton("Generate", AppTheme.Accent);
        _btnGenerateCurrent.Click += (_, _) => OnGenerateDivisionSchedule(false);
        _btnGenerateCurrent.Location = new Point(8, 8);
        _btnGenerateCurrent.Size = new Size(85, 28);
        leftToolbar.Controls.Add(_btnGenerateCurrent);

        _btnGenerateAll = MakeButton("Gen All", AppTheme.Accent);
        _btnGenerateAll.Click += (_, _) => OnGenerateDivisionSchedule(true);
        _btnGenerateAll.Location = new Point(101, 8);
        _btnGenerateAll.Size = new Size(85, 28);
        leftToolbar.Controls.Add(_btnGenerateAll);

        // Row 2: Delete buttons
        _btnDeleteDivSchedule = MakeButton("Delete", AppTheme.ButtonDanger);
        _btnDeleteDivSchedule.Click += (_, _) => OnDeleteDivisionSchedule(false);
        _btnDeleteDivSchedule.Enabled = false;
        _btnDeleteDivSchedule.Location = new Point(8, 40);
        _btnDeleteDivSchedule.Size = new Size(85, 28);
        leftToolbar.Controls.Add(_btnDeleteDivSchedule);

        var btnDeleteAll = MakeButton("Del All", AppTheme.ButtonDanger);
        btnDeleteAll.Click += (_, _) => OnDeleteDivisionSchedule(true);
        btnDeleteAll.Location = new Point(101, 40);
        btnDeleteAll.Size = new Size(85, 28);
        leftToolbar.Controls.Add(btnDeleteAll);

        // Row 3: Print buttons
        _btnPrintDivSchedule = MakeButton("Print", AppTheme.Accent);
        _btnPrintDivSchedule.Click += (_, _) => OnPrintDivisionSchedule(false);
        _btnPrintDivSchedule.Enabled = false;
        _btnPrintDivSchedule.Location = new Point(8, 72);
        _btnPrintDivSchedule.Size = new Size(85, 28);
        leftToolbar.Controls.Add(_btnPrintDivSchedule);

        var btnPrintAll = MakeButton("Print All", AppTheme.Accent);
        btnPrintAll.Click += (_, _) => OnPrintDivisionSchedule(true);
        btnPrintAll.Location = new Point(101, 72);
        btnPrintAll.Size = new Size(85, 28);
        leftToolbar.Controls.Add(btnPrintAll);

        leftLayout.RowStyles[2] = new RowStyle(SizeType.Absolute, 108);
        leftLayout.Controls.Add(leftToolbar, 0, 2);
        outer.Controls.Add(leftLayout, 0, 0);

        // Right panel
        var rightLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = AppTheme.ContentBackground,
            Padding = Padding.Empty,
            Margin = Padding.Empty,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        };
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var titleRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = AppTheme.Surface,
            Padding = Padding.Empty,
            Margin = Padding.Empty,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        };
        titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 106));

        var lblTitle = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Division Schedule",
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.Surface,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 0, 0),
            Font = new Font("Segoe UI", 11f, FontStyle.Bold)
        };
        titleRow.Controls.Add(lblTitle, 0, 0);

        _btnPrintDivSchedule = new Button
        {
            Dock = DockStyle.Fill,
            Text = "Print...",
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.FontButton,
            Cursor = Cursors.Hand,
            Margin = new Padding(4, 4, 8, 4),
            Enabled = false
        };
        _btnPrintDivSchedule.FlatAppearance.BorderSize = 1;
        _btnPrintDivSchedule.FlatAppearance.BorderColor = AppTheme.Separator;
        _btnPrintDivSchedule.Click += (_, _) => OnPrintDivisionSchedule(false);
        titleRow.Controls.Add(_btnPrintDivSchedule, 1, 0);
        rightLayout.Controls.Add(titleRow, 0, 0);

        _lblDayHeader = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.Surface,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 0, 0),
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Text = ""
        };
        rightLayout.Controls.Add(_lblDayHeader, 0, 1);

        _lblDivStatus = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextSecondary,
            BackColor = AppTheme.ContentBackground,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 0, 0),
            Font = new Font("Segoe UI", 9.5f),
            Text = "Select divisions and click Generate."
        };
        rightLayout.Controls.Add(_lblDivStatus, 0, 2);

        _gridDivisionSchedules = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = AppTheme.ContentBackground,
            GridColor = AppTheme.Separator,
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            SelectionMode = DataGridViewSelectionMode.CellSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ReadOnly = true,
            Font = new Font("Segoe UI", 9.5f),
            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.TextPrimary,
                SelectionBackColor = AppTheme.NavSelected,
                SelectionForeColor = AppTheme.NavText,
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                WrapMode = DataGridViewTriState.True
            },
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppTheme.NavBackground,
                ForeColor = AppTheme.NavText,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            },
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeight = 28,
            RowTemplate = { Height = 45 }
        };
        _gridDivisionSchedules.DataError += (_, de) => de.ThrowException = false;
        rightLayout.Controls.Add(_gridDivisionSchedules, 0, 3);
        outer.Controls.Add(rightLayout, 1, 0);

        tab.Controls.Add(outer);
    }

    private void LoadDivisionsList()
    {
        using var db = new BocceDbContext();
        var currentLeague = AppParameterService.GetDefaultLeagueId(db);
        var currentSeason = AppParameterService.GetDefaultSeasonId(db);

        _lstDivisions.Items.Clear();
        _divisionsForSchedule.Clear();

        if (!currentLeague.HasValue || !currentSeason.HasValue) return;

        var divisions = db.Divisions
            .Where(d => d.Season.LeagueId == currentLeague && d.SeasonId == currentSeason)
            .Where(d => d.Teams.Any())
            .OrderBy(d => d.SortName)
            .Select(d => new { d.Id, d.Name })
            .ToList();

        foreach (var div in divisions)
        {
            _divisionsForSchedule.Add((div.Id, div.Name));
            _lstDivisions.Items.Add(div.Name);
        }
    }

    private void OnDivisionSelected()
    {
        if (_lstDivisions.SelectedIndex < 0)
        {
            _gridDivisionSchedules.Rows.Clear();
            _gridDivisionSchedules.Columns.Clear();
            _lblDayHeader.Text = "";
            _lblDivStatus.Text = "Select a division to view its schedule.";
            _btnDeleteDivSchedule.Enabled = false;
            _btnPrintDivSchedule.Enabled = false;
            return;
        }

        var divisionId = _divisionsForSchedule[_lstDivisions.SelectedIndex].Id;
        LoadDivisionScheduleGrid(divisionId);
    }

    private void LoadDivisionScheduleGrid(int divisionId)
    {
        try
        {
            using var db = new BocceDbContext();
            var division = db.Divisions.Include(d => d.DaySlot).Include(d => d.TimeSlot)
                .FirstOrDefault(d => d.Id == divisionId);

            var schedules = db.ScheduleDivisions
                .Include(s => s.Team1)
                .Include(s => s.Team2)
                .Include(s => s.Court)
                .Where(s => s.DivisionId == divisionId)
                .OrderBy(s => s.MatchDate)
                .ThenBy(s => s.Court!.CourtNumber)
                .ToList();

            _gridDivisionSchedules.Columns.Clear();
            _gridDivisionSchedules.Rows.Clear();

            if (!schedules.Any())
            {
                _lblDivStatus.Text = "No schedule generated for this division.";
                _lblDayHeader.Text = "";
                _btnDeleteDivSchedule.Enabled = false;
                _btnPrintDivSchedule.Enabled = false;
                return;
            }

            // Set day header
            var dayName = schedules.First().MatchDate.ToString("dddd");
            var timeStr = division?.TimeSlot?.Timeslot12h ?? "TBD";
            _lblDayHeader.Text = $"{dayName} {timeStr}";

            // Get all courts from the season
            var seasonId = division?.SeasonId;
            var allCourts = seasonId.HasValue
                ? db.Courts.Where(c => c.IsActive).OrderBy(c => c.CourtNumber).ToList()
                : [];

            // Add date column
            _gridDivisionSchedules.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Date",
                HeaderText = "Date of",
                ReadOnly = true,
                Width = 75,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleLeft,
                    Padding = new Padding(4, 0, 0, 0)
                }
            });

            // Add court columns
            foreach (var court in allCourts)
            {
                var courtLabel = court != null
                    ? _courtDisplay == "letter" && court.CourtLetter != ""
                        ? $"Court {court.CourtLetter}"
                        : $"Court {court.CourtNumber}"
                    : "No Court";

                _gridDivisionSchedules.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = $"Court_{court?.Id}",
                    HeaderText = courtLabel,
                    ReadOnly = true,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Alignment = DataGridViewContentAlignment.MiddleCenter,
                        WrapMode = DataGridViewTriState.True
                    }
                });
            }

            // Group schedules by date and build rows
            var schedulesByDate = schedules.GroupBy(s => s.MatchDate).OrderBy(g => g.Key);

            foreach (var dateGroup in schedulesByDate)
            {
                var dateStr = dateGroup.Key.ToString("MMM d");
                var row = new DataGridViewRow();
                row.Cells.Add(new DataGridViewTextBoxCell { Value = dateStr });

                // Add cells for each court
                foreach (var court in allCourts)
                {
                    var matchesForCourt = dateGroup.Where(s => s.CourtId == court?.Id).ToList();
                    var matchText = "";

                    if (matchesForCourt.Any())
                    {
                        foreach (var match in matchesForCourt)
                        {
                            var team1Name = match.Team1?.DisplayName ?? "Unknown";
                            var team2Name = match.Team2?.DisplayName ?? "Unknown";

                            // Strip first 2 characters (e.g., "A-" from "A-Hansen")
                            var team1 = team1Name.Length > 2 ? team1Name.Substring(2) : team1Name;
                            var team2 = team2Name.Length > 2 ? team2Name.Substring(2) : team2Name;

                            if (matchText != "") matchText += "\n";
                            matchText += $"{team1} vs {team2}";
                        }
                    }
                    else
                    {
                        matchText = "-";
                    }

                    row.Cells.Add(new DataGridViewTextBoxCell { Value = matchText });
                }

                _gridDivisionSchedules.Rows.Add(row);
            }

            _lblDivStatus.Text = $"Showing {schedules.Count} match(es)";
            _btnDeleteDivSchedule.Enabled = true;
            _btnPrintDivSchedule.Enabled = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading schedule: {ex.Message}");
            _lblDivStatus.Text = "Error loading schedule.";
        }
    }

    private void OnGenerateDivisionSchedule(bool generateAll)
    {
        try
        {
            using var db = new BocceDbContext();
            var currentLeague = AppParameterService.GetDefaultLeagueId(db);
            var currentSeason = AppParameterService.GetDefaultSeasonId(db);

            if (!currentLeague.HasValue || !currentSeason.HasValue)
            {
                MessageBox.Show("Please select a league and season.");
                return;
            }

            var season = db.Seasons.Find(currentSeason);
            if (season == null || !season.StartDate.HasValue)
            {
                MessageBox.Show("Season must have a start date.");
                return;
            }

            List<int> divisionIds = generateAll
                ? _divisionsForSchedule.Select(d => d.Id).ToList()
                : _lstDivisions.SelectedIndex >= 0
                    ? [_divisionsForSchedule[_lstDivisions.SelectedIndex].Id]
                    : [];

            if (!divisionIds.Any())
            {
                MessageBox.Show(generateAll ? "No divisions to generate." : "Please select a division.");
                return;
            }

            // Check if any divisions already have schedules
            var existingCount = db.ScheduleDivisions
                .Where(s => divisionIds.Contains(s.DivisionId))
                .Count();

            if (existingCount > 0)
            {
                var msg = generateAll
                    ? $"This will overwrite {existingCount} existing division schedule(s). Continue?"
                    : $"This division already has {existingCount} schedule record(s). Overwrite them?";

                if (MessageBox.Show(
                    msg,
                    "Overwrite Existing Schedules",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;
            }

            int generatedCount = 0;
            foreach (var divId in divisionIds)
            {
                if (GenerateDivisionScheduleForOne(db, divId, season))
                    generatedCount++;
            }

            db.SaveChanges();
            MessageBox.Show($"Generated schedules for {generatedCount} division(s).");

            // Reload divisions and display the first generated schedule
            LoadDivisionsList();
            if (divisionIds.Any())
            {
                var firstGenId = divisionIds.First();
                var firstIdx = _divisionsForSchedule.FindIndex(d => d.Id == firstGenId);
                if (firstIdx >= 0)
                {
                    _lstDivisions.SelectedIndex = firstIdx;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error generating schedules: {ex.Message}");
        }
    }

    private bool GenerateDivisionScheduleForOne(BocceDbContext db, int divisionId, Season season)
    {
        var division = db.Divisions.Include(d => d.Teams)
            .Include(d => d.DaySlot)
            .Include(d => d.TimeSlot)
            .FirstOrDefault(d => d.Id == divisionId);

        if (division == null || !division.Teams.Any()) return false;

        if (!division.DaySlotId.HasValue || !division.TimeSlotId.HasValue) return false;

        var daySlot = division.DaySlot;
        if (daySlot == null) return false;

        var teamCount = division.Teams.Count;
        var template = db.ScheduleTemplates
            .Include(t => t.Weeks).ThenInclude(w => w.Matches)
            .FirstOrDefault(t => t.SeasonId == season.Id && t.TeamCount == teamCount);

        if (template == null) return false;

        // Delete existing division schedules for this template
        var existing = db.ScheduleDivisions
            .Where(s => s.DivisionId == divisionId && s.TemplateId == template.Id)
            .ToList();
        db.ScheduleDivisions.RemoveRange(existing);

        var startDate = season.StartDate!.Value;
        var targetDayOfWeek = (DayOfWeek)daySlot.DayNbr;

        // Find first occurrence of target day
        var daysUntilTarget = ((int)targetDayOfWeek - (int)startDate.DayOfWeek + 7) % 7;
        var firstMatchDate = startDate.AddDays(daysUntilTarget);

        var teams = division.Teams.OrderBy(t => t.DisplayName).ToList();
        var teamMap = new Dictionary<string, int>();

        for (int i = 0; i < teams.Count; i++)
        {
            var letterIndex = i;
            var letter = char.ConvertFromUtf32(65 + letterIndex);
            teamMap[letter] = teams[i].Id;
        }

        foreach (var week in template.Weeks)
        {
            var matchDate = firstMatchDate.AddDays(week.WeekNumber * 7);

            foreach (var match in week.Matches)
            {
                if (!teamMap.ContainsKey(match.Slot1) || !teamMap.ContainsKey(match.Slot2))
                    continue;

                var divSchedule = new ScheduleDivision
                {
                    DivisionId = divisionId,
                    TemplateId = template.Id,
                    TemplateWeekNumber = week.WeekNumber,
                    MatchDate = matchDate,
                    Team1Id = teamMap[match.Slot1],
                    Team2Id = teamMap[match.Slot2],
                    CourtId = match.CourtId
                };

                db.ScheduleDivisions.Add(divSchedule);
            }
        }

        return true;
    }

    private void OnDeleteDivisionSchedule(bool deleteAll)
    {
        if (!deleteAll && _lstDivisions.SelectedIndex < 0) return;

        try
        {
            using var db = new BocceDbContext();

            if (deleteAll)
            {
                var allSchedules = db.ScheduleDivisions.ToList();
                if (!allSchedules.Any())
                {
                    MessageBox.Show("No schedules to delete.");
                    return;
                }

                if (MessageBox.Show(
                    $"Delete ALL division schedules ({allSchedules.Count} records)?\n\nThis cannot be undone.",
                    "Delete All Schedules",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;

                db.ScheduleDivisions.RemoveRange(allSchedules);
                db.SaveChanges();

                MessageBox.Show($"Deleted {allSchedules.Count} schedule record(s).");
                _gridDivisionSchedules.Rows.Clear();
                _gridDivisionSchedules.Columns.Clear();
                _lblDayHeader.Text = "";
                _lblDivStatus.Text = "All schedules deleted.";
            }
            else
            {
                var divisionName = _divisionsForSchedule[_lstDivisions.SelectedIndex].Name;
                var divisionId = _divisionsForSchedule[_lstDivisions.SelectedIndex].Id;

                var schedules = db.ScheduleDivisions.Where(s => s.DivisionId == divisionId).ToList();
                if (!schedules.Any())
                {
                    MessageBox.Show("No schedules to delete for this division.");
                    return;
                }

                if (MessageBox.Show(
                    $"Delete all schedules for {divisionName}?\n\nThis cannot be undone.",
                    "Delete Division Schedule",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;

                var count = schedules.Count;
                db.ScheduleDivisions.RemoveRange(schedules);
                db.SaveChanges();

                MessageBox.Show($"Deleted {count} schedule record(s).");
                LoadDivisionScheduleGrid(divisionId);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error deleting schedule: {ex.Message}");
        }
    }

    private void OnPrintDivisionSchedule(bool printAll)
    {
        if (printAll)
            MessageBox.Show("Print all schedules functionality coming soon.");
        else
            MessageBox.Show("Print schedule functionality coming soon.");
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

    private sealed class TemplateItem(int id, int teamCount, int weekCount, DateTime generatedAt, bool isLocked)
    {
        public int      Id          { get; } = id;
        public int      TeamCount   { get; } = teamCount;
        public int      WeekCount   { get; } = weekCount;
        public DateTime GeneratedAt { get; } = generatedAt;
        public bool     IsLocked    { get; set; } = isLocked;
        public override string ToString() => $"{TeamCount}-Team  ({WeekCount} wks)";
    }

    private record WeekDisplayRow(int WeekNumber, Dictionary<int, MatchEntry> Matches);
    private record MatchEntry(int MatchId, string Slot1, string Slot2);
}

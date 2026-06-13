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

    // ── Left panel ─────────────────────────────────────────────────────────────
    private ListBox _lstTemplates = null!;

    // ── Toolbar ────────────────────────────────────────────────────────────────
    private Button _btnGenerate = null!;
    private Button _btnDelete   = null!;
    private Button _btnPrint    = null!;

    // ── Right panel ────────────────────────────────────────────────────────────
    private Label         _lblTemplateTitle = null!;
    private Label         _lblStatus        = null!;
    private DataGridView  _grid             = null!;
    private DataGridViewTextBoxColumn    _colWeek  = null!;
    private DataGridViewComboBoxColumn   _colSlot1 = null!;
    private DataGridViewTextBoxColumn    _colVs    = null!;
    private DataGridViewComboBoxColumn   _colSlot2 = null!;
    private DataGridViewComboBoxColumn   _colCourt = null!;
    private Button _btnLock = null!;
    private bool   _inSwap;
    private bool   _locked;
    private bool   _loading;

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

        // ── Tab 1: Templates ──────────────────────────────────────────────────
        var templatesTab = new TabPage("  Templates  ");
        tabs.TabPages.Add(templatesTab);

        // ── Tab 2: Division Schedules (placeholder) ───────────────────────────
        var divSchedulesTab = new TabPage("  Division Schedules  ");
        divSchedulesTab.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "Division Schedules — coming soon.",
            Font = AppTheme.FontDefault,
            ForeColor = AppTheme.TextSecondary,
            TextAlign = ContentAlignment.MiddleCenter
        });
        tabs.TabPages.Add(divSchedulesTab);

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
        templatesTab.Controls.Add(outer);

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

        // Title row: template label (fill) + Lock button + Print button
        var titleRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1,
            BackColor = AppTheme.Surface,
            Padding = Padding.Empty, Margin = Padding.Empty,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        };
        titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
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
            Text = "🔒 Lock",
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.FontButton,
            Cursor = Cursors.Hand,
            Margin = new Padding(4, 4, 4, 4),
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
        _colWeek  = new DataGridViewTextBoxColumn  { Name = "Week",  HeaderText = "Week Of", ReadOnly = true, FillWeight = 16 };
        _colSlot1 = new DataGridViewComboBoxColumn { Name = "Slot1", HeaderText = "Team 1",  FillWeight = 14, DisplayStyleForCurrentCellOnly = true };
        _colVs    = new DataGridViewTextBoxColumn  { Name = "Vs",    HeaderText = "",         ReadOnly = true, FillWeight = 5  };
        _colSlot2 = new DataGridViewComboBoxColumn { Name = "Slot2", HeaderText = "Team 2",  FillWeight = 14, DisplayStyleForCurrentCellOnly = true };
        _colCourt = new DataGridViewComboBoxColumn { Name = "Court", HeaderText = "Court",   FillWeight = 30, DisplayStyleForCurrentCellOnly = true };

        _grid.DataError                    += (_, de) => de.ThrowException = false;
        _grid.CellClick                    += OnCellClick;
        _grid.CellValueChanged             += OnCellEdit;
        _grid.CurrentCellDirtyStateChanged += OnCurrentCellDirty;
        _grid.Columns.AddRange(_colWeek, _colSlot1, _colVs, _colSlot2, _colCourt);
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

                if (season != null)
                    _courtDisplay = AppParameterService.GetCourtDisplay(db, season.LeagueId);

                _seasonCourts = db.SeasonCourts
                    .Where(sc => sc.SeasonId == _selectedSeasonId.Value)
                    .Include(sc => sc.Court)
                    .OrderBy(sc => sc.Court.CourtNumber)
                    .AsEnumerable()
                    .Select(sc => (
                        sc.CourtId,
                        Display: CourtLabel(sc.Court, _courtDisplay)))
                    .ToList();
            }
            else
            {
                _seasonCourts.Clear();
            }
        }
        catch { }

        LoadTemplateList();
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
        return $"{start:MMM d} - {end:MMM d}";
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
            _btnDelete.Enabled  = false;
            _btnPrint.Enabled   = false;
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
        _loading = true;
        _grid.Rows.Clear();
        _colCourt.Items.Clear();
        foreach (var court in _seasonCourts)
            _colCourt.Items.Add(court.Display);

        try
        {
            using var db = new BocceDbContext();
            var rows = ScheduleTemplateService.GetTemplateRows(db, templateId);

            foreach (var r in rows)
            {
                var courtEntry = _seasonCourts.FirstOrDefault(c => c.Id == r.CourtId);
                string courtDisplay = courtEntry != default
                    ? courtEntry.Display
                    : $"Court #{r.CourtId}";

                string weekLabel = WeekLabel(r.WeekNumber);
                int rowIdx = _grid.Rows.Add(weekLabel, r.Slot1, "vs", r.Slot2, courtDisplay);
                _grid.Rows[rowIdx].Tag = r;
            }

            if (_grid.Rows.Count > 0)
                _grid.FirstDisplayedScrollingRowIndex = 0;

            int weekCount = rows.Select(r => r.WeekNumber).Distinct().Count();
            _lblStatus.Text = $"{rows.Count} matches across {weekCount} weeks.";
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
            _grid.Rows.Clear();
            _locked = false;
            ApplyLockState();
            return;
        }

        _selectedTemplateId    = item.Id;
        _btnDelete.Enabled     = true;
        _btnPrint.Enabled      = true;
        _lblTemplateTitle.Text = $"{item.TeamCount}-Team Template  —  {item.WeekCount} weeks  (generated {item.GeneratedAt:yyyy-MM-dd HH:mm})";

        // Unlock whenever a different template is loaded
        _btnLock.Enabled = true;
        _locked          = false;
        ApplyLockState();

        // Repopulate slot comboboxes before loading rows (values must match items)
        _grid.Rows.Clear();
        _colSlot1.Items.Clear();
        _colSlot2.Items.Clear();
        for (int i = 0; i < item.TeamCount; i++)
        {
            string letter = ((char)('A' + i)).ToString();
            _colSlot1.Items.Add(letter);
            _colSlot2.Items.Add(letter);
        }
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
        _locked = !_locked;
        ApplyLockState();
    }

    private void ApplyLockState()
    {
        _colSlot1.ReadOnly = _locked;
        _colSlot2.ReadOnly = _locked;
        _colCourt.ReadOnly = _locked;
        if (_locked)
        {
            _btnLock.Text      = "🔒 Locked";
            _btnLock.BackColor = Color.FromArgb(180, 100, 0);
            _btnLock.ForeColor = Color.White;
            _btnLock.FlatAppearance.BorderColor = Color.FromArgb(180, 100, 0);
        }
        else
        {
            _btnLock.Text      = "🔓 Unlocked";
            _btnLock.BackColor = AppTheme.Surface;
            _btnLock.ForeColor = AppTheme.TextPrimary;
            _btnLock.FlatAppearance.BorderColor = AppTheme.Separator;
        }
    }

    // ── Cell editing: single-click to open ComboBox ───────────────────────────

    private void OnCellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (_locked || e.RowIndex < 0) return;
        if (e.ColumnIndex == _colSlot1.Index || e.ColumnIndex == _colSlot2.Index || e.ColumnIndex == _colCourt.Index)
            _grid.BeginEdit(true);
    }

    // Fire swap immediately when dropdown selection changes (not on focus-leave)
    private void OnCurrentCellDirty(object? sender, EventArgs e)
    {
        if (_locked || !_grid.IsCurrentCellDirty) return;
        var col = _grid.CurrentCell?.ColumnIndex;
        if (col == _colSlot1.Index || col == _colSlot2.Index || col == _colCourt.Index)
            _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
    }

    // ── Cell editing: swap on commit ──────────────────────────────────────────

    private void OnCellEdit(object? sender, DataGridViewCellEventArgs e)
    {
        if (_inSwap || _loading) return;
        var row = _grid.Rows[e.RowIndex];
        if (row.Tag is not ScheduleTemplateService.TemplateRow orig) return;

        if (e.ColumnIndex == _colSlot1.Index || e.ColumnIndex == _colSlot2.Index)
        {
            string newVal = row.Cells[e.ColumnIndex].Value?.ToString() ?? "";
            string oldVal = e.ColumnIndex == _colSlot1.Index ? orig.Slot1 : orig.Slot2;
            if (newVal == oldVal) return;

            // Find the partner in the same week that currently holds newVal
            DataGridViewRow? partner    = null;
            bool             inSlot1    = false;
            foreach (DataGridViewRow other in _grid.Rows)
            {
                if (other.Index == e.RowIndex || other.Tag is not ScheduleTemplateService.TemplateRow otherOrig) continue;
                if (otherOrig.WeekNumber != orig.WeekNumber) continue;
                if (otherOrig.Slot1 == newVal) { partner = other; inSlot1 = true;  break; }
                if (otherOrig.Slot2 == newVal) { partner = other; inSlot1 = false; break; }
            }

            if (partner == null)
            {
                // No partner — revert (shouldn't happen in a valid template)
                _inSwap = true;
                row.Cells[e.ColumnIndex].Value = oldVal;
                _inSwap = false;
                return;
            }

            var    pOrig      = (ScheduleTemplateService.TemplateRow)partner.Tag!;
            string pNewSlot1  = inSlot1 ? oldVal : pOrig.Slot1;
            string pNewSlot2  = inSlot1 ? pOrig.Slot2 : oldVal;
            string newSlot1   = e.ColumnIndex == _colSlot1.Index ? newVal : orig.Slot1;
            string newSlot2   = e.ColumnIndex == _colSlot2.Index ? newVal : orig.Slot2;

            try
            {
                using var db = new BocceDbContext();
                ScheduleTemplateService.UpdateMatchSlots(db, pOrig.MatchId,  pNewSlot1, pNewSlot2);
                ScheduleTemplateService.UpdateMatchSlots(db, orig.MatchId,   newSlot1,  newSlot2);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Swap failed:\n\n{ex.Message}", "Edit Schedule",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                _inSwap = true;
                row.Cells[e.ColumnIndex].Value = oldVal;
                _inSwap = false;
                return;
            }

            _inSwap = true;
            partner.Cells[_colSlot1.Index].Value = pNewSlot1;
            partner.Cells[_colSlot2.Index].Value = pNewSlot2;
            partner.Tag = pOrig with { Slot1 = pNewSlot1, Slot2 = pNewSlot2 };
            row.Tag     = orig with { Slot1 = newSlot1,   Slot2 = newSlot2 };
            _inSwap = false;
        }
        else if (e.ColumnIndex == _colCourt.Index)
        {
            string newDisplay    = row.Cells[e.ColumnIndex].Value?.ToString() ?? "";
            var    newCourtEntry = _seasonCourts.FirstOrDefault(c => c.Display == newDisplay);
            if (newCourtEntry == default || newCourtEntry.Id == orig.CourtId) return;

            // Find the partner in the same week currently on the target court
            DataGridViewRow? partner = null;
            foreach (DataGridViewRow other in _grid.Rows)
            {
                if (other.Index == e.RowIndex || other.Tag is not ScheduleTemplateService.TemplateRow otherOrig) continue;
                if (otherOrig.WeekNumber != orig.WeekNumber) continue;
                if (otherOrig.CourtId == newCourtEntry.Id) { partner = other; break; }
            }

            if (partner == null) return;

            var pOrig         = (ScheduleTemplateService.TemplateRow)partner.Tag!;
            var oldCourtEntry = _seasonCourts.FirstOrDefault(c => c.Id == orig.CourtId);
            if (oldCourtEntry == default) return;

            try
            {
                using var db = new BocceDbContext();
                ScheduleTemplateService.UpdateMatchCourt(db, pOrig.MatchId, orig.CourtId);
                ScheduleTemplateService.UpdateMatchCourt(db, orig.MatchId,  newCourtEntry.Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Court swap failed:\n\n{ex.Message}", "Edit Schedule",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                _inSwap = true;
                row.Cells[e.ColumnIndex].Value = oldCourtEntry.Display;
                _inSwap = false;
                return;
            }

            _inSwap = true;
            partner.Cells[_colCourt.Index].Value = oldCourtEntry.Display;
            partner.Tag = pOrig with { CourtId = orig.CourtId };
            row.Tag     = orig with { CourtId = newCourtEntry.Id };
            _inSwap = false;
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

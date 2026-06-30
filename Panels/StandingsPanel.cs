using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.Services;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Panels;

public class StandingsPanel : UserControl
{
    private int?   _seasonId;
    private Label  _lblStatus = null!;
    private Button _btnFilter = null!;
    private TabControl _tabs  = null!;
    private TabPage?   _allDivTabPage = null;
    private Panel?     _allDivPanel   = null;

    // ── Stored data for filter-driven rebuilds ────────────────────────────────

    private record DivData(Division Div, List<StandingView> Rows);

    private List<(int Id, string Label, int Sort)> _timeCols   = [];
    private Dictionary<int, List<DivData>>          _timeColDivs = []; // timeId → ordered list (by day)
    private int[]  _teamColWidths = [];
    private bool   _isGamesMode;
    private bool   _h2hUsed;

    // ── Filter state ──────────────────────────────────────────────────────────

    private List<(int DivId, string Label)> _divFilterItems = [];
    private HashSet<int>? _filteredDivIds = null;   // null = all visible
    private Dictionary<int, Panel> _divCellPanels  = [];

    // ── Construction ──────────────────────────────────────────────────────────

    public StandingsPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;
        BuildUi();
        AppParameterService.DefaultsChanged += OnDefaultsChanged;
        LoadData();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            AppParameterService.DefaultsChanged -= OnDefaultsChanged;
        base.Dispose(disposing);
    }

    private void OnDefaultsChanged(object? sender, DefaultsChangedEventArgs e) => LoadData();

    // ── UI skeleton ───────────────────────────────────────────────────────────

    private void BuildUi()
    {
        var toolbar = new Panel
        {
            Dock = DockStyle.Top, Height = 48,
            BackColor = AppTheme.Surface, Padding = new Padding(8, 0, 8, 0)
        };
        toolbar.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.Separator);
            e.Graphics.DrawLine(pen, 0, toolbar.Height - 1, toolbar.Width, toolbar.Height - 1);
        };

        var btnPrint = MakeBtn("Print", Color.FromArgb(60, 100, 160), 8, 80);
        btnPrint.Click += (_, _) => PrintCurrent();
        toolbar.Controls.Add(btnPrint);

        _btnFilter = MakeBtn("All Divisions ▼", Color.FromArgb(80, 120, 170), 96, 150);
        _btnFilter.Click += (_, _) => ShowFilterDropdown();
        toolbar.Controls.Add(_btnFilter);

        _lblStatus = new Label
        {
            AutoSize = true, Font = AppTheme.FontDefault,
            ForeColor = AppTheme.TextSecondary, Location = new Point(258, 15)
        };
        toolbar.Controls.Add(_lblStatus);

        _tabs = new TabControl
        {
            Dock = DockStyle.Fill, Font = AppTheme.FontDefault, Padding = new Point(12, 4)
        };
        _tabs.SelectedIndexChanged += (_, _) => _btnFilter.Visible = _tabs.SelectedIndex == 0;

        Controls.Add(_tabs);
        Controls.Add(toolbar);
    }

    private static Button MakeBtn(string text, Color bg, int x, int w) => new()
    {
        Text = text, Font = AppTheme.FontDefault,
        Height = 30, Width = w, Location = new Point(x, 9),
        BackColor = bg, ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
        FlatAppearance = { BorderSize = 0 }
    };

    // ── Data loading ──────────────────────────────────────────────────────────

    private void LoadData()
    {
        _tabs.TabPages.Clear();
        _allDivTabPage = null;
        _allDivPanel   = null;
        _timeCols      = [];
        _timeColDivs   = [];
        _teamColWidths = [];
        _divFilterItems.Clear();
        _filteredDivIds = null;
        _lblStatus.Text = "";
        _btnFilter.Text = "All Divisions ▼";
        _btnFilter.Visible = true;

        try
        {
            using var db = new BocceDbContext();
            _seasonId = AppParameterService.GetDefaultSeasonId(db);
            if (!_seasonId.HasValue) { _lblStatus.Text = "No season selected."; return; }

            var season = db.Seasons.Find(_seasonId.Value);
            if (season == null) { _lblStatus.Text = "Season not found."; return; }

            var allRows = db.Standings
                .Where(s => s.SeasonId == _seasonId.Value)
                .OrderBy(s => s.DivisionId).ThenBy(s => s.DivisionRank).ThenBy(s => s.DivisionSeed)
                .ToList();

            if (allRows.Count == 0) { _lblStatus.Text = "No scores entered yet."; return; }

            _isGamesMode = season.ScoringMode == "games_mode";
            _h2hUsed     = allRows.Any(r => r.H2HPlusMinus != 0 || r.H2HWins != 0);

            var divIds = allRows.Select(r => r.DivisionId).Distinct().ToList();
            var divisions = db.Divisions
                .Include(d => d.DaySlot)
                .Include(d => d.TimeSlot)
                .Where(d => divIds.Contains(d.Id))
                .ToList();

            // Timeslot columns, ordered by SortOrder
            _timeCols = divisions
                .Where(d => d.TimeSlotId.HasValue && d.TimeSlot != null)
                .Select(d => (d.TimeSlotId!.Value, d.TimeSlot!.Timeslot12h, d.TimeSlot!.SortOrder ?? 999))
                .Distinct().OrderBy(t => t.Item3)
                .Select(t => (Id: t.Item1, Label: t.Item2, Sort: t.Item3)).ToList();

            // Per-timeslot column: divisions in day order (packed, no gaps)
            _timeColDivs = _timeCols.ToDictionary(
                tc => tc.Id,
                tc => divisions
                    .Where(d => d.TimeSlotId == tc.Id && d.DaySlotId.HasValue && d.DaySlot != null)
                    .OrderBy(d => d.DaySlot!.DayNbr)
                    .Select(d => new DivData(d, allRows.Where(r => r.DivisionId == d.Id).ToList()))
                    .ToList());

            // Measure team column widths per timeslot column
            using (var bmp = new Bitmap(1, 1))
            using (var g   = Graphics.FromImage(bmp))
            {
                int MeasureW(string s) => (int)Math.Ceiling(g.MeasureString(s, AppTheme.FontDefault).Width) + 10;
                _teamColWidths = _timeCols.Select((tc, _) =>
                {
                    var divs = _timeColDivs.TryGetValue(tc.Id, out var list) ? list : [];
                    int maxNameW = divs
                        .SelectMany(d => d.Rows.Select(r => MeasureW(r.TeamName)))
                        .DefaultIfEmpty(0).Max();
                    return Math.Max(MeasureW("Team"), maxNameW);
                }).ToArray();
            }

            // Build filter items: per timeslot column, in day order
            foreach (var (tc, _) in _timeCols.Select((tc, i) => (tc, i)))
            {
                if (!_timeColDivs.TryGetValue(tc.Id, out var divs)) continue;
                foreach (var d in divs)
                    _divFilterItems.Add((d.Div.Id, $"{TitleAbbr(d.Div.DaySlot?.DayAbbr)} {tc.Label}"));
            }

            // ── All Divisions tab ─────────────────────────────────────────────
            _allDivTabPage = MakePage("All Divisions");
            RebuildAllDivisionsPanel();
            _tabs.TabPages.Add(_allDivTabPage);

            // ── Season Seed tab ───────────────────────────────────────────────
            var divNames = divisions.ToDictionary(d => d.Id, d => d.Name);
            var seedRows = allRows.OrderBy(r => r.SeasonSeed).ToList();
            var seedPage = MakePage("Season Seed");
            var seedPanel = BuildSeasonSeedSplitPanel(seedRows, divNames, season.TeamsInPlayoffs, _isGamesMode);
            seedPanel.Dock = DockStyle.Fill;
            seedPage.Controls.Add(seedPanel);
            _tabs.TabPages.Add(seedPage);
        }
        catch (Exception ex)
        {
            _lblStatus.Text = $"Error: {ex.Message}";
        }
    }

    // ── All-Divisions scrollable panel (rebuilt on filter change) ─────────────

    private const int CellHdrH = 26;
    private const int DgvHdrH  = 28;
    private const int DgvRowH  = 26;
    private const int SlotHdrH = 32;
    private const int ColGap   = 8;
    private const int CellGap  = 6;

    private static int[] GetStatWidths(bool isGamesMode, bool h2hUsed)
    {
        var ws = new List<int> { 50, 44, 44 };  // Seed, GP/MP, W
        if (!isGamesMode) ws.Add(44);            // T
        ws.AddRange([44, 44, 50, 54]);           // L, F, Pts, +/-
        if (h2hUsed) ws.AddRange([64, 54]);      // H2H+/-, H2HW
        return ws.ToArray();
    }

    private void RebuildAllDivisionsPanel()
    {
        if (_allDivTabPage == null) return;

        _allDivPanel?.Dispose();
        _allDivCellPanels_Clear();

        if (_timeCols.Count == 0) { _allDivPanel = null; return; }

        int[] statWs    = GetStatWidths(_isGamesMode, _h2hUsed);
        int   statTotal = statWs.Sum();
        int[] colWs     = _teamColWidths.Select(tw => tw + statTotal).ToArray();

        // Visible divisions per column (filter applied)
        var visibleByCol = _timeCols.Select((tc, _) =>
        {
            var divs = _timeColDivs.TryGetValue(tc.Id, out var list) ? list : [];
            return divs
                .Where(d => _filteredDivIds == null || _filteredDivIds.Contains(d.Div.Id))
                .ToList();
        }).ToList();

        // Per-column heights (sum of div table heights + gaps)
        int[] colHs = visibleByCol.Select(divs =>
            divs.Sum(d => CellHdrH + DgvHdrH + d.Rows.Count * DgvRowH + CellGap)
        ).ToArray();

        int[] colXs = new int[_timeCols.Count];
        for (int ci = 0, x = 0; ci < _timeCols.Count; ci++) { colXs[ci] = x; x += colWs[ci] + ColGap; }

        int totalW = colWs.Sum() + (_timeCols.Count - 1) * ColGap;
        int totalH = SlotHdrH + (colHs.DefaultIfEmpty(0).Max());

        var outer = new Panel
        {
            Dock = DockStyle.Fill, AutoScroll = true,
            BackColor = AppTheme.ContentBackground,
            AutoScrollMinSize = new Size(totalW, totalH)
        };

        // Timeslot column headers
        for (int ci = 0; ci < _timeCols.Count; ci++)
            outer.Controls.Add(new Label
            {
                Text = _timeCols[ci].Label,
                Font = AppTheme.FontDefaultBold,
                ForeColor = Color.White,
                BackColor = AppTheme.GridHeaderBackground,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(colXs[ci], 0),
                Size = new Size(colWs[ci], SlotHdrH),
                AutoSize = false
            });

        // Division cells — packed top-to-bottom per column, no gaps for missing days
        for (int ci = 0; ci < _timeCols.Count; ci++)
        {
            int y    = SlotHdrH;
            int teamW = _teamColWidths[ci];
            foreach (var d in visibleByCol[ci])
            {
                int rowsH  = DgvHdrH + d.Rows.Count * DgvRowH + 2;
                int cellH  = CellHdrH + rowsH;
                var cellPanel = BuildDivCell(d.Div, d.Rows, teamW, statWs, colWs[ci], cellH);
                cellPanel.Location = new Point(colXs[ci], y);
                outer.Controls.Add(cellPanel);
                _divCellPanels[d.Div.Id] = cellPanel;
                y += cellH + CellGap;
            }
        }

        _allDivPanel = outer;
        _allDivTabPage.Controls.Clear();
        _allDivTabPage.Controls.Add(_allDivPanel);
    }

    private void _allDivCellPanels_Clear() => _divCellPanels.Clear();

    private Panel BuildDivCell(Division div, List<StandingView> rows,
        int teamW, int[] statWs, int colW, int cellH)
    {
        var cell = new Panel { Size = new Size(colW, cellH), BackColor = AppTheme.ContentBackground };

        // Division name only (division name already contains day info)
        cell.Controls.Add(new Label
        {
            Text = div.Name,
            Font = AppTheme.FontDefaultBold,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(100, 130, 170),
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(0, 0),
            Size = new Size(colW, CellHdrH),
            AutoSize = false,
            Padding = new Padding(6, 0, 0, 0)
        });

        var grid = BuildDivGrid(rows, teamW, statWs);
        grid.Location = new Point(0, CellHdrH);
        grid.Size = new Size(colW, DgvHdrH + rows.Count * DgvRowH + 2);
        cell.Controls.Add(grid);
        return cell;
    }

    private DataGridView BuildDivGrid(List<StandingView> rows, int teamW, int[] statWs)
    {
        var grid = MakeGrid();
        grid.ScrollBars = ScrollBars.None;
        grid.Columns.Add(Col("Team", "Team", teamW));

        var defs = GetStatDefs(_isGamesMode, _h2hUsed);
        for (int i = 0; i < defs.Count; i++)
        {
            var (name, hdr, mid, tip) = defs[i];
            grid.Columns.Add(Col(name, hdr, statWs[i], mid: mid, tip: tip));
        }

        foreach (var r in rows)
        {
            var vals = new List<object?> { r.TeamName, r.SeasonSeed, _isGamesMode ? r.GamesPlayed : r.MatchesPlayed, r.Wins };
            if (!_isGamesMode) vals.Add(r.Ties);
            vals.Add(r.Losses); vals.Add(r.Forfeits);
            vals.Add(r.StandingsPoints); vals.Add(PmStr(r.PlusMinus));
            if (_h2hUsed) { vals.Add(PmStr(r.H2HPlusMinus)); vals.Add(r.H2HWins); }

            int idx = grid.Rows.Add(vals.Cast<object>().ToArray());
            ApplyRowStyle(grid.Rows[idx], idx);
            if (r.DivisionRank == 1) grid.Rows[idx].DefaultCellStyle.Font = AppTheme.FontDefaultBold;
        }
        return grid;
    }

    private static List<(string Name, string Hdr, bool Mid, string Tip)> GetStatDefs(bool isGamesMode, bool h2hUsed)
    {
        var d = new List<(string, string, bool, string)>
        {
            ("Seed", "Seed", true, "Overall season seed / playoff position"),
            (isGamesMode ? "GP" : "MP", isGamesMode ? "GP" : "MP", true,
             isGamesMode ? "Games Played" : "Matches Played"),
            ("W", "W", true, "Wins")
        };
        if (!isGamesMode) d.Add(("T", "T", true, "Ties"));
        d.Add(("L",   "L",   true, "Non-forfeit losses"));
        d.Add(("F",   "F",   true, "Forfeit losses"));
        d.Add(("Pts", "Pts", true, "Standings points"));
        d.Add(("PM",  "+/-", true, "Plus/Minus"));
        if (h2hUsed) { d.Add(("H2HPM", "H2H+/-", true, "H2H PM")); d.Add(("H2HW", "H2HW", true, "H2H Wins")); }
        return d;
    }

    // ── Season Seed two-column panel ──────────────────────────────────────────

    private static SplitContainer BuildSeasonSeedSplitPanel(
        List<StandingView> rows, Dictionary<int, string> divNames,
        int teamsInPlayoffs, bool isGamesMode)
    {
        int total = rows.Count;
        int leftN = (total + 1) / 2;

        // Measure Team column width from ALL rows so both halves stay consistent
        int teamW;
        using (var bmp = new Bitmap(1, 1))
        using (var g   = Graphics.FromImage(bmp))
        {
            int M(string s) => (int)Math.Ceiling(g.MeasureString(s, AppTheme.FontDefault).Width) + 10;
            teamW = Math.Max(M("Team"), rows.Select(r => M(r.TeamName)).DefaultIfEmpty(0).Max());
        }

        var sc = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 6,
            BackColor = AppTheme.Separator,
            Panel1MinSize = 100,
            Panel2MinSize = 100
        };

        void Apply()
        {
            if (!sc.IsHandleCreated || sc.Width <= sc.SplitterWidth) return;
            int half = (sc.Width - sc.SplitterWidth) / 2;
            sc.SplitterDistance = Math.Clamp(half, sc.Panel1MinSize,
                sc.Width - sc.SplitterWidth - sc.Panel2MinSize);
        }
        sc.HandleCreated += (_, _) => Apply();
        sc.SizeChanged   += (_, _) => Apply();

        var leftGrid  = BuildSeasonSeedGrid(rows.Take(leftN).ToList(),  divNames, teamsInPlayoffs, isGamesMode, teamW);
        var rightGrid = BuildSeasonSeedGrid(rows.Skip(leftN).ToList(),  divNames, teamsInPlayoffs, isGamesMode, teamW);
        leftGrid.Dock  = DockStyle.Fill;
        rightGrid.Dock = DockStyle.Fill;

        sc.Panel1.BackColor = AppTheme.ContentBackground;
        sc.Panel2.BackColor = AppTheme.ContentBackground;
        sc.Panel1.Controls.Add(leftGrid);
        sc.Panel2.Controls.Add(rightGrid);
        return sc;
    }

    // ── Season Seed grid ──────────────────────────────────────────────────────

    private static DataGridView BuildSeasonSeedGrid(
        List<StandingView> rows, Dictionary<int, string> divNames,
        int teamsInPlayoffs, bool isGamesMode, int teamW = 155)
    {
        var grid = MakeGrid();

        grid.Columns.AddRange(
            Col("Seed",   "Seed",      62,  mid: true, tip: "Season seed — playoff order"),
            Col("Team",   "Team",      teamW),
            Col("Div",    "Division",  185, tip: "Division"),
            Col("DivR",   "Div. Seed", 92,  mid: true, tip: "Division seed (finish position in division)"),
            Col("Pts",    "Pts",       50,  mid: true, tip: "Standings points"),
            Col("PM",     "+/-",       54,  mid: true, tip: "Plus/Minus"),
            Col("W",      "W",         44,  mid: true, tip: "Wins")
        );
        if (!isGamesMode)
            grid.Columns.Add(Col("T", "T", 44, mid: true, tip: "Ties"));
        grid.Columns.AddRange(
            Col("L", "L", 44, mid: true, tip: "Losses"),
            Col("F", "F", 44, mid: true, tip: "Forfeits")
        );

        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            var vals = new List<object?>
            {
                r.SeasonSeed, r.TeamName,
                divNames.TryGetValue(r.DivisionId, out var dn) ? dn : "?",
                r.DivisionRank, r.StandingsPoints, PmStr(r.PlusMinus), r.Wins
            };
            if (!isGamesMode) vals.Add(r.Ties);
            vals.Add(r.Losses); vals.Add(r.Forfeits);

            int idx = grid.Rows.Add(vals.Cast<object>().ToArray());
            ApplyRowStyle(grid.Rows[idx], i);

            bool qualifies = teamsInPlayoffs > 0 && r.SeasonSeed <= teamsInPlayoffs;
            if (qualifies) grid.Rows[idx].DefaultCellStyle.Font = AppTheme.FontDefaultBold;
            if (teamsInPlayoffs > 0 && r.SeasonSeed == teamsInPlayoffs) grid.Rows[idx].DividerHeight = 2;
        }
        return grid;
    }

    // ── Division filter dropdown ───────────────────────────────────────────────

    private void ShowFilterDropdown()
    {
        if (_divFilterItems.Count == 0) return;

        int formH = Math.Min(440, 40 + _divFilterItems.Count * 22 + 40);
        var form = new Form
        {
            FormBorderStyle = FormBorderStyle.FixedToolWindow,
            Text = "Filter Divisions",
            StartPosition = FormStartPosition.Manual,
            Size = new Size(226, formH),
            BackColor = AppTheme.Surface
        };

        var clb = new CheckedListBox
        {
            Dock = DockStyle.Fill, CheckOnClick = true,
            Font = AppTheme.FontDefault,
            BackColor = AppTheme.ContentBackground,
            ForeColor = AppTheme.TextPrimary
        };
        foreach (var (divId, label) in _divFilterItems)
            clb.Items.Add(label, _filteredDivIds == null || _filteredDivIds.Contains(divId));

        var btnBar  = new Panel { Dock = DockStyle.Bottom, Height = 36, BackColor = AppTheme.Surface };
        var btnAll  = MakeSmallBtn("All",   Color.FromArgb(60, 100, 160), 6);
        var btnNone = MakeSmallBtn("None",  Color.FromArgb(100, 100, 110), 72);
        var btnClose= MakeSmallBtn("Close", Color.FromArgb(80, 80, 80), 138);
        btnBar.Controls.AddRange([btnAll, btnNone, btnClose]);
        form.Controls.Add(clb);
        form.Controls.Add(btnBar);

        clb.ItemCheck += (_, _) => BeginInvoke(() => ApplyFilterFrom(clb));
        btnAll.Click  += (_, _) => { for (int i = 0; i < clb.Items.Count; i++) clb.SetItemChecked(i, true);  BeginInvoke(() => ApplyFilterFrom(clb)); };
        btnNone.Click += (_, _) => { for (int i = 0; i < clb.Items.Count; i++) clb.SetItemChecked(i, false); BeginInvoke(() => ApplyFilterFrom(clb)); };
        btnClose.Click += (_, _) => form.Close();

        var pt = _btnFilter.PointToScreen(new Point(0, _btnFilter.Height));
        form.Location = pt;
        form.Show(FindForm() ?? (IWin32Window?)this);
        form.Deactivate += (_, _) => { try { form.Close(); } catch { } };
    }

    private static Button MakeSmallBtn(string text, Color bg, int x) => new()
    {
        Text = text, Width = 60, Height = 28, Location = new Point(x, 4),
        Font = AppTheme.FontDefault, FlatStyle = FlatStyle.Flat,
        BackColor = bg, ForeColor = Color.White, FlatAppearance = { BorderSize = 0 }
    };

    private void ApplyFilterFrom(CheckedListBox clb)
    {
        var checkedIdx = clb.CheckedIndices.Cast<int>().ToHashSet();
        if (checkedIdx.Count == _divFilterItems.Count)
        {
            _filteredDivIds = null;
            _btnFilter.Text = "All Divisions ▼";
        }
        else
        {
            _filteredDivIds = checkedIdx.Select(i => _divFilterItems[i].DivId).ToHashSet();
            int n = checkedIdx.Count;
            _btnFilter.Text = n == 0 ? "No Divisions ▼" : $"{n} division{(n == 1 ? "" : "s")} ▼";
        }
        // Rebuild with compact layout — visible divisions always climb to top
        RebuildAllDivisionsPanel();
    }

    // ── Print ─────────────────────────────────────────────────────────────────

    private void PrintCurrent()
    {
        if (!_seasonId.HasValue) return;

        bool onSeedTab = _tabs.SelectedIndex > 0;
        if (onSeedTab)
        {
            StandingsPrintService.ShowPrintPreview(this, _seasonId.Value, StandingsPrintMode.SeasonSeed);
        }
        else
        {
            StandingsPrintService.ShowPrintPreview(this, _seasonId.Value, StandingsPrintMode.AllDivisions, _filteredDivIds);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static TabPage MakePage(string title) => new(title)
    {
        BackColor = AppTheme.ContentBackground, Padding = new Padding(0)
    };

    private static DataGridView MakeGrid() => new()
    {
        ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
        RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
        BackgroundColor = AppTheme.ContentBackground, GridColor = AppTheme.Separator,
        BorderStyle = BorderStyle.None, Font = AppTheme.FontDefault,
        ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = AppTheme.Surface, ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.FontDefaultBold, Padding = new Padding(4, 0, 4, 0)
        },
        DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = AppTheme.ContentBackground, ForeColor = AppTheme.TextPrimary,
            Padding = new Padding(4, 2, 4, 2),
            SelectionBackColor = AppTheme.NavSelected, SelectionForeColor = Color.White
        },
        RowTemplate = { Height = 26 },
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
        ColumnHeadersHeight = 28
    };

    private static DataGridViewTextBoxColumn Col(string name, string header, int width,
        bool mid = false, string? tip = null) => new()
    {
        Name = name, HeaderText = header, Width = width, ReadOnly = true,
        SortMode = DataGridViewColumnSortMode.NotSortable,
        DefaultCellStyle  = new DataGridViewCellStyle
            { Alignment = mid ? DataGridViewContentAlignment.MiddleCenter : DataGridViewContentAlignment.MiddleLeft },
        HeaderCell = { Style =
            { Alignment = mid ? DataGridViewContentAlignment.MiddleCenter : DataGridViewContentAlignment.MiddleLeft },
            ToolTipText = tip ?? header }
    };

    private static void ApplyRowStyle(DataGridViewRow row, int index)
    {
        if (index % 2 == 1) row.DefaultCellStyle.BackColor = Color.FromArgb(245, 247, 250);
    }

    private static string PmStr(int v) => v >= 0 ? $"+{v}" : $"{v}";

    private static string TitleAbbr(string? abbr) =>
        string.IsNullOrEmpty(abbr) ? "" :
        char.ToUpper(abbr[0]) + abbr.Substring(1).ToLower();
}

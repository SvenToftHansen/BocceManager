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

    // All-divisions panel filter state
    private List<(int DivId, string Label)> _divFilterItems = [];
    private HashSet<int>? _filteredDivIds = null;          // null = all visible
    private Dictionary<int, Panel> _divCellPanels = [];   // divId → cell panel in All Divisions tab

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
        btnPrint.Click += (_, _) => PrintStandings();
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
        _divCellPanels.Clear();
        _divFilterItems.Clear();
        _filteredDivIds = null;
        _lblStatus.Text = "";
        _btnFilter.Text = "All Divisions ▼";

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

            bool isGamesMode = season.ScoringMode == "games_mode";
            bool h2hUsed = allRows.Any(r => r.H2HPlusMinus != 0 || r.H2HWins != 0);

            var divIds = allRows.Select(r => r.DivisionId).Distinct().ToList();
            var divisions = db.Divisions
                .Include(d => d.DaySlot)
                .Include(d => d.TimeSlot)
                .Where(d => divIds.Contains(d.Id))
                .ToList();

            // Distinct timeslot columns ordered by SortOrder
            var timeCols = divisions
                .Where(d => d.TimeSlotId.HasValue && d.TimeSlot != null)
                .Select(d => (d.TimeSlotId!.Value, d.TimeSlot!.Timeslot12h, d.TimeSlot!.SortOrder ?? 999))
                .Distinct().OrderBy(t => t.Item3)
                .Select(t => (Id: t.Item1, Label: t.Item2, Sort: t.Item3)).ToList();

            // Distinct day rows ordered by DayNbr
            var dayRows = divisions
                .Where(d => d.DaySlotId.HasValue && d.DaySlot != null)
                .Select(d => (d.DaySlotId!.Value, d.DaySlot!.DayName, d.DaySlot!.DayNbr))
                .Distinct().OrderBy(d => d.Item3)
                .Select(d => (Id: d.Item1, Name: d.Item2, Nbr: d.Item3)).ToList();

            // Cell map: (dayId, timeId) → (division, rows)
            var cells = new Dictionary<(int, int), (Division Div, List<StandingView> Rows)>();
            foreach (var div in divisions.Where(d => d.DaySlotId.HasValue && d.TimeSlotId.HasValue))
            {
                var key = (div.DaySlotId!.Value, div.TimeSlotId!.Value);
                if (!cells.ContainsKey(key))
                    cells[key] = (div, allRows.Where(r => r.DivisionId == div.Id).ToList());
            }

            // Build filter item list: day × timeslot order
            foreach (var day in dayRows)
                foreach (var tc in timeCols)
                    if (cells.TryGetValue((day.Id, tc.Id), out var cell))
                        _divFilterItems.Add((cell.Div.Id, $"{TitleAbbr(cell.Div.DaySlot?.DayAbbr)} {tc.Label}"));

            // Measure team column widths per timeslot column (max of all team names in column)
            int[] teamColWidths;
            using (var bmp = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(bmp))
            {
                int MeasureW(string s) => (int)Math.Ceiling(g.MeasureString(s, AppTheme.FontDefault).Width) + 10;
                teamColWidths = timeCols.Select((tc, _) =>
                {
                    int maxNameW = dayRows
                        .Select(dr => cells.TryGetValue((dr.Id, tc.Id), out var c) ? c.Rows : null)
                        .Where(r => r != null)
                        .SelectMany(r => r!.Select(sv => MeasureW(sv.TeamName)))
                        .DefaultIfEmpty(0).Max();
                    return Math.Max(MeasureW("Team"), maxNameW);
                }).ToArray();
            }

            // ── All Divisions tab ─────────────────────────────────────────────
            var allDivPage = MakePage("All Divisions");
            var scrollPanel = BuildAllDivisionsPanel(timeCols, dayRows, cells, teamColWidths, isGamesMode, h2hUsed);
            allDivPage.Controls.Add(scrollPanel);
            _tabs.TabPages.Add(allDivPage);

            // ── Season Seed tab ───────────────────────────────────────────────
            var divNames = divisions.ToDictionary(d => d.Id, d => d.Name);
            var seedRows = allRows.OrderBy(r => r.SeasonSeed).ToList();
            var seedPage = MakePage("Season Seed");
            var seedGrid = BuildSeasonSeedGrid(seedRows, divNames, season.TeamsInPlayoffs, isGamesMode);
            seedGrid.Dock = DockStyle.Fill;
            seedPage.Controls.Add(seedGrid);
            _tabs.TabPages.Add(seedPage);
        }
        catch (Exception ex)
        {
            _lblStatus.Text = $"Error: {ex.Message}";
        }
    }

    // ── All-Divisions scrollable panel ────────────────────────────────────────

    private const int CellHdrH = 26;   // division header label height
    private const int DgvHdrH  = 28;   // DataGridView column header height
    private const int DgvRowH  = 26;   // DataGridView data row height
    private const int SlotHdrH = 32;   // timeslot column header label height
    private const int ColGap   = 8;    // gap between timeslot columns
    private const int RowGap   = 6;    // gap between day-row blocks

    private static int[] GetStatWidths(bool isGamesMode, bool h2hUsed)
    {
        // Ordered to match GetStatDefs: GP/MP, W, [T], L, F, Pts, +/-, [H2H+/-, H2HW]
        var ws = new List<int> { 44, 44 };      // GP/MP, W
        if (!isGamesMode) ws.Add(44);            // T (match mode only)
        ws.AddRange([44, 44, 50, 54]);           // L, F, Pts, +/-
        if (h2hUsed) ws.AddRange([64, 54]);      // H2H+/-, H2HW
        return ws.ToArray();
    }

    private Panel BuildAllDivisionsPanel(
        List<(int Id, string Label, int Sort)> timeCols,
        List<(int Id, string Name, int Nbr)>   dayRows,
        Dictionary<(int, int), (Division Div, List<StandingView> Rows)> cells,
        int[] teamColWidths,
        bool isGamesMode, bool h2hUsed)
    {
        int[] statWs = GetStatWidths(isGamesMode, h2hUsed);
        int   statTotal = statWs.Sum();
        int[] colWs = teamColWidths.Select(tw => tw + statTotal).ToArray();

        // Per-day-row block heights: CellHdr + DgvHdr + maxTeams*DgvRow
        int[] blockHs = dayRows.Select(dr =>
        {
            int maxN = timeCols
                .Select(tc => cells.TryGetValue((dr.Id, tc.Id), out var c) ? c.Rows.Count : 0)
                .DefaultIfEmpty(0).Max();
            return CellHdrH + DgvHdrH + maxN * DgvRowH;
        }).ToArray();

        int[] colXs = new int[timeCols.Count];
        for (int ci = 0, x = 0; ci < timeCols.Count; ci++) { colXs[ci] = x; x += colWs[ci] + ColGap; }

        int totalW = colWs.Sum() + (timeCols.Count - 1) * ColGap;
        int totalH = SlotHdrH + blockHs.Sum() + dayRows.Count * RowGap;

        var outer = new Panel
        {
            Dock = DockStyle.Fill, AutoScroll = true,
            BackColor = AppTheme.ContentBackground,
            AutoScrollMinSize = new Size(totalW, totalH)
        };

        // Timeslot column header labels (top row)
        for (int ci = 0; ci < timeCols.Count; ci++)
            outer.Controls.Add(new Label
            {
                Text = timeCols[ci].Label,
                Font = AppTheme.FontDefaultBold,
                ForeColor = Color.White,
                BackColor = AppTheme.GridHeaderBackground,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(colXs[ci], 0),
                Size = new Size(colWs[ci], SlotHdrH),
                AutoSize = false
            });

        // Division cell panels
        int yOff = SlotHdrH;
        for (int ri = 0; ri < dayRows.Count; ri++)
        {
            var day    = dayRows[ri];
            int blockH = blockHs[ri];

            for (int ci = 0; ci < timeCols.Count; ci++)
            {
                var tc = timeCols[ci];
                if (!cells.TryGetValue((day.Id, tc.Id), out var cell)) continue;

                var cellPanel = BuildDivCell(cell.Div, cell.Rows, teamColWidths[ci], statWs, colWs[ci], blockH, isGamesMode, h2hUsed);
                cellPanel.Location = new Point(colXs[ci], yOff);
                outer.Controls.Add(cellPanel);
                _divCellPanels[cell.Div.Id] = cellPanel;
            }

            yOff += blockH + RowGap;
        }

        return outer;
    }

    private static Panel BuildDivCell(Division div, List<StandingView> rows,
        int teamW, int[] statWs, int colW, int blockH,
        bool isGamesMode, bool h2hUsed)
    {
        var cell = new Panel { Size = new Size(colW, blockH), BackColor = AppTheme.ContentBackground };

        cell.Controls.Add(new Label
        {
            Text = $"{div.DaySlot?.DayName ?? ""}  ·  {div.Name}",
            Font = AppTheme.FontDefaultBold,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(100, 130, 170),
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(0, 0),
            Size = new Size(colW, CellHdrH),
            AutoSize = false,
            Padding = new Padding(6, 0, 0, 0)
        });

        var grid = BuildDivGrid(rows, teamW, statWs, isGamesMode, h2hUsed);
        grid.Location = new Point(0, CellHdrH);
        grid.Size = new Size(colW, DgvHdrH + rows.Count * DgvRowH + 2);
        cell.Controls.Add(grid);

        return cell;
    }

    private static DataGridView BuildDivGrid(List<StandingView> rows,
        int teamW, int[] statWs, bool isGamesMode, bool h2hUsed)
    {
        var grid = MakeGrid();
        grid.ScrollBars = ScrollBars.None;

        grid.Columns.Add(Col("Team", "Team", teamW));

        var defs = GetStatDefs(isGamesMode, h2hUsed);
        for (int i = 0; i < defs.Count; i++)
        {
            var (name, hdr, mid, tip) = defs[i];
            grid.Columns.Add(Col(name, hdr, statWs[i], mid: mid, tip: tip));
        }

        foreach (var r in rows)
        {
            var vals = new List<object?> { r.TeamName, isGamesMode ? r.GamesPlayed : r.MatchesPlayed, r.Wins };
            if (!isGamesMode) vals.Add(r.Ties);
            vals.Add(r.Losses); vals.Add(r.Forfeits);
            vals.Add(r.StandingsPoints); vals.Add(PmStr(r.PlusMinus));
            if (h2hUsed) { vals.Add(PmStr(r.H2HPlusMinus)); vals.Add(r.H2HWins); }

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
            (isGamesMode ? "GP" : "MP", isGamesMode ? "GP" : "MP", true,
             isGamesMode ? "Games Played" : "Matches Played"),
            ("W", "W", true, "Wins")
        };
        if (!isGamesMode) d.Add(("T", "T", true, "Ties"));
        d.Add(("L",   "L",   true, "Non-forfeit losses"));
        d.Add(("F",   "F",   true, "Forfeit losses"));
        d.Add(("Pts", "Pts", true, "Standings points"));
        d.Add(("PM",  "+/-", true, "Plus/Minus"));
        if (h2hUsed)
        {
            d.Add(("H2HPM", "H2H+/-", true, "H2H plus/minus"));
            d.Add(("H2HW",  "H2HW",   true, "H2H wins"));
        }
        return d;
    }

    // ── Season Seed grid ──────────────────────────────────────────────────────

    private static DataGridView BuildSeasonSeedGrid(
        List<StandingView> rows, Dictionary<int, string> divNames,
        int teamsInPlayoffs, bool isGamesMode)
    {
        var grid = MakeGrid();

        grid.Columns.AddRange(
            Col("Seed", "Seed",     62,  mid: true, tip: "Season seed — playoff order"),
            Col("Team", "Team",     155),
            Col("Div",  "Division", 185, tip: "Division"),
            Col("DivR", "Div #",    52,  mid: true, tip: "Division rank"),
            Col("Pts",  "Pts",      50,  mid: true, tip: "Standings points"),
            Col("PM",   "+/-",      54,  mid: true, tip: "Plus/Minus"),
            Col("W",    "W",        44,  mid: true, tip: "Wins")
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

        int formH = Math.Min(420, 40 + _divFilterItems.Count * 22 + 40);
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

        var btnBar = new Panel { Dock = DockStyle.Bottom, Height = 36, BackColor = AppTheme.Surface };
        var btnAll = new Button
        {
            Text = "All", Width = 60, Height = 28, Location = new Point(6, 4),
            Font = AppTheme.FontDefault, FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(60, 100, 160), ForeColor = Color.White,
            FlatAppearance = { BorderSize = 0 }
        };
        var btnNone = new Button
        {
            Text = "None", Width = 60, Height = 28, Location = new Point(72, 4),
            Font = AppTheme.FontDefault, FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(100, 100, 110), ForeColor = Color.White,
            FlatAppearance = { BorderSize = 0 }
        };
        var btnClose = new Button
        {
            Text = "Close", Width = 64, Height = 28, Location = new Point(138, 4),
            Font = AppTheme.FontDefault, FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(80, 80, 80), ForeColor = Color.White,
            FlatAppearance = { BorderSize = 0 }
        };
        btnBar.Controls.AddRange([btnAll, btnNone, btnClose]);

        form.Controls.Add(clb);
        form.Controls.Add(btnBar);

        clb.ItemCheck += (_, _) => BeginInvoke(() => ApplyFilterFrom(clb));
        btnAll.Click  += (_, _) =>
        {
            for (int i = 0; i < clb.Items.Count; i++) clb.SetItemChecked(i, true);
            BeginInvoke(() => ApplyFilterFrom(clb));
        };
        btnNone.Click += (_, _) =>
        {
            for (int i = 0; i < clb.Items.Count; i++) clb.SetItemChecked(i, false);
            BeginInvoke(() => ApplyFilterFrom(clb));
        };
        btnClose.Click += (_, _) => form.Close();

        var pt = _btnFilter.PointToScreen(new Point(0, _btnFilter.Height));
        form.Location = pt;
        form.Show(FindForm() ?? (IWin32Window?)this);
        form.Deactivate += (_, _) => { try { form.Close(); } catch { } };
    }

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
        foreach (var (divId, panel) in _divCellPanels)
            panel.Visible = _filteredDivIds == null || _filteredDivIds.Contains(divId);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static TabPage MakePage(string title) => new(title)
    {
        BackColor = AppTheme.ContentBackground,
        Padding   = new Padding(0)
    };

    private static DataGridView MakeGrid() => new()
    {
        ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
        RowHeadersVisible = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
        BackgroundColor = AppTheme.ContentBackground,
        GridColor = AppTheme.Separator,
        BorderStyle = BorderStyle.None,
        Font = AppTheme.FontDefault,
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

    private void PrintStandings()
    {
        if (!_seasonId.HasValue) return;
        StandingsPrintService.ShowPrintPreview(this, _seasonId.Value);
    }
}

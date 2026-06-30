using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.Services;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Panels;

public class StandingsPanel : UserControl
{
    // ── Session state ─────────────────────────────────────────────────────────

    private int?   _seasonId;
    private bool   _isGamesMode;
    private bool   _h2hUsed;
    private bool   _firstPlaceGuaranteed;
    private int    _teamsInPlayoffs;
    private int    _maxWeek;                        // last week that has any scores
    private bool   _suppressWeekRefresh;

    // Structural data — loaded once per season; stable across week-range changes
    private List<Division> _allDivisions = [];

    // Standings data — re-aggregated when week range changes
    private List<StandingView> _currentRows = [];

    // ── UI controls ───────────────────────────────────────────────────────────

    private Label            _lblStatus  = null!;
    private Button           _btnFilter  = null!;
    private NumericUpDown    _numFrom    = null!;
    private NumericUpDown    _numTo      = null!;
    private TabControl       _tabs       = null!;
    private TabPage?         _allDivPage = null;
    private TabPage?         _seedPage   = null;
    private Panel?           _allDivPanel = null;

    // ── Division grid data ────────────────────────────────────────────────────

    private record DivData(Division Div, List<StandingView> Rows);

    private List<(int Id, string Label, int Sort)> _timeCols    = [];
    private Dictionary<int, List<DivData>>          _timeColDivs = [];
    private int[] _teamColWidths = [];

    // ── Filter state ──────────────────────────────────────────────────────────

    private List<(int DivId, string Label)> _divFilterItems = [];
    private HashSet<int>? _filteredDivIds = null;
    private Dictionary<int, Panel> _divCellPanels = [];

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

        // Print & Filter buttons
        var btnPrint = MakeBtn("Print", Color.FromArgb(60, 100, 160), 8, 80);
        btnPrint.Click += (_, _) => PrintCurrent();
        toolbar.Controls.Add(btnPrint);

        _btnFilter = MakeBtn("All Divisions ▼", Color.FromArgb(80, 120, 170), 96, 150);
        _btnFilter.Click += (_, _) => ShowFilterDropdown();
        toolbar.Controls.Add(_btnFilter);

        // ── Week range controls ────────────────────────────────────────────────
        var lblWeek = new Label
        {
            Text = "Weeks:", AutoSize = true, Font = AppTheme.FontDefault,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(260, 15)
        };
        toolbar.Controls.Add(lblWeek);

        _numFrom = MakeWeekSpinner(308);
        toolbar.Controls.Add(_numFrom);

        toolbar.Controls.Add(new Label
        {
            Text = "–", AutoSize = true, Font = AppTheme.FontDefault,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(363, 15)
        });

        _numTo = MakeWeekSpinner(374);
        toolbar.Controls.Add(_numTo);

        var btnAllWeeks = MakeBtn("All", Color.FromArgb(90, 100, 110), 430, 42);
        btnAllWeeks.Click += (_, _) => ResetWeekRange();
        toolbar.Controls.Add(btnAllWeeks);

        _lblStatus = new Label
        {
            AutoSize = true, Font = AppTheme.FontSmall,
            ForeColor = AppTheme.TextMuted, Location = new Point(480, 17)
        };
        toolbar.Controls.Add(_lblStatus);

        // Week spinner events — refresh standings on change
        _numFrom.ValueChanged += (_, _) =>
        {
            if (_suppressWeekRefresh) return;
            if (_numFrom.Value > _numTo.Value) _numTo.Value = _numFrom.Value;
            RefreshStandings();
        };
        _numTo.ValueChanged += (_, _) =>
        {
            if (_suppressWeekRefresh) return;
            if (_numTo.Value < _numFrom.Value) _numFrom.Value = _numTo.Value;
            RefreshStandings();
        };

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

    private static NumericUpDown MakeWeekSpinner(int x) => new()
    {
        Location = new Point(x, 12), Size = new Size(52, 26),
        Minimum = 1, Maximum = 52, Value = 1,
        Font = AppTheme.FontDefault, TextAlign = HorizontalAlignment.Center,
        BorderStyle = BorderStyle.FixedSingle
    };

    // ── Data loading — structural (once per season) ───────────────────────────

    private void LoadData()
    {
        _tabs.TabPages.Clear();
        _allDivPage = null; _seedPage = null; _allDivPanel = null;
        _timeCols = []; _timeColDivs = []; _teamColWidths = [];
        _divFilterItems.Clear(); _filteredDivIds = null;
        _allDivisions = []; _currentRows = []; _maxWeek = 0;
        _lblStatus.Text = "";
        _btnFilter.Text = "All Divisions ▼";
        _btnFilter.Visible = true;

        _suppressWeekRefresh = true;
        try
        {
            using var db = new BocceDbContext();
            _seasonId = AppParameterService.GetDefaultSeasonId(db);
            if (!_seasonId.HasValue) { _lblStatus.Text = "No season selected."; return; }

            var season = db.Seasons.Find(_seasonId.Value);
            if (season == null) { _lblStatus.Text = "Season not found."; return; }

            _isGamesMode          = season.ScoringMode == "games_mode";
            _firstPlaceGuaranteed = season.FirstPlaceGuaranteed;
            _teamsInPlayoffs      = season.TeamsInPlayoffs;

            // Find the highest week number that has any scores in this season
            _maxWeek = db.Scoring
                .Where(s => s.SeasonId == _seasonId.Value)
                .Select(s => s.WeekId)
                .DefaultIfEmpty(0).Max();

            if (_maxWeek == 0) { _lblStatus.Text = "No scores entered yet."; return; }

            // Set spinner range and default (1 → last scored week)
            _numFrom.Maximum = _maxWeek;
            _numTo.Maximum   = _maxWeek;
            _numFrom.Value   = 1;
            _numTo.Value     = _maxWeek;

            // Load structural division data
            var allDivIds = db.Scoring
                .Where(s => s.SeasonId == _seasonId.Value)
                .Select(s => s.DivisionId).Distinct().ToList();

            _allDivisions = db.Divisions
                .Include(d => d.DaySlot)
                .Include(d => d.TimeSlot)
                .Where(d => allDivIds.Contains(d.Id))
                .ToList();

            // Timeslot columns (ordered by SortOrder)
            _timeCols = _allDivisions
                .Where(d => d.TimeSlotId.HasValue && d.TimeSlot != null)
                .Select(d => (d.TimeSlotId!.Value, d.TimeSlot!.Timeslot12h, d.TimeSlot!.SortOrder ?? 999))
                .Distinct().OrderBy(t => t.Item3)
                .Select(t => (Id: t.Item1, Label: t.Item2, Sort: t.Item3)).ToList();

            // Filter items list
            foreach (var tc in _timeCols)
            {
                var divsInCol = _allDivisions
                    .Where(d => d.TimeSlotId == tc.Id && d.DaySlotId.HasValue && d.DaySlot != null)
                    .OrderBy(d => d.DaySlot!.DayNbr);
                foreach (var d in divsInCol)
                    _divFilterItems.Add((d.Id, $"{TitleAbbr(d.DaySlot?.DayAbbr)} {tc.Label}"));
            }

            // Measure team column widths from all team names (stable across week ranges)
            using (var bmp = new Bitmap(1, 1))
            using (var g   = Graphics.FromImage(bmp))
            {
                int Mw(string s) => (int)Math.Ceiling(g.MeasureString(s, AppTheme.FontDefault).Width) + 10;
                _teamColWidths = _timeCols.Select((tc, _) =>
                {
                    var names = db.Scoring
                        .Where(s => s.SeasonId == _seasonId.Value && _allDivisions
                            .Where(d => d.TimeSlotId == tc.Id).Select(d => d.Id).Contains(s.DivisionId))
                        .Select(s => s.TeamName).Distinct().ToList();
                    int maxW = names.Select(n => Mw(n)).DefaultIfEmpty(0).Max();
                    return Math.Max(Mw("Team"), maxW);
                }).ToArray();
            }

            // Create tab stubs — content filled by RefreshStandings below
            _allDivPage = MakePage("All Divisions");
            _seedPage   = MakePage("Season Seed");
            _tabs.TabPages.Add(_allDivPage);
            _tabs.TabPages.Add(_seedPage);
        }
        finally
        {
            _suppressWeekRefresh = false;
        }

        RefreshStandings();
    }

    // ── Standings refresh — runs on week range change ─────────────────────────

    private void RefreshStandings()
    {
        if (!_seasonId.HasValue || _maxWeek == 0) return;

        int fromWeek = (int)(_numFrom?.Value ?? 1);
        int toWeek   = (int)(_numTo?.Value   ?? _maxWeek);

        try
        {
            using var db = new BocceDbContext();
            bool isAll = fromWeek == 1 && toWeek == _maxWeek;

            if (isAll)
            {
                // Use the pre-aggregated (and H2H-resolved) Standings view
                _currentRows = db.Standings
                    .Where(s => s.SeasonId == _seasonId.Value)
                    .OrderBy(s => s.DivisionId).ThenBy(s => s.DivisionRank).ThenBy(s => s.DivisionSeed)
                    .ToList();
            }
            else
            {
                // Aggregate Scoring rows filtered to the chosen week range
                _currentRows = AggregateForWeekRange(db, _seasonId.Value,
                    _isGamesMode, _firstPlaceGuaranteed, fromWeek, toWeek);
            }

            _h2hUsed = _currentRows.Any(r => r.H2HPlusMinus != 0 || r.H2HWins != 0);

            // Rebuild per-timeslot column data
            _timeColDivs = _timeCols.ToDictionary(
                tc => tc.Id,
                tc => _allDivisions
                    .Where(d => d.TimeSlotId == tc.Id && d.DaySlotId.HasValue && d.DaySlot != null)
                    .OrderBy(d => d.DaySlot!.DayNbr)
                    .Select(d => new DivData(d, _currentRows.Where(r => r.DivisionId == d.Id).ToList()))
                    .ToList());

            RebuildAllDivisionsPanel();
            RebuildSeasonSeedPanel();

            if (!isAll)
                _lblStatus.Text = $"Weeks {fromWeek}–{toWeek}";
            else
                _lblStatus.Text = "";
        }
        catch (Exception ex)
        {
            _lblStatus.Text = $"Error: {ex.Message}";
        }
    }

    private void ResetWeekRange()
    {
        _suppressWeekRefresh = true;
        _numFrom.Value = 1;
        _numTo.Value   = _maxWeek;
        _suppressWeekRefresh = false;
        RefreshStandings();
    }

    // ── Custom week-range aggregation (bypasses Standings view) ───────────────

    private static List<StandingView> AggregateForWeekRange(
        BocceDbContext db, int seasonId,
        bool isGamesMode, bool firstPlaceGuaranteed,
        int fromWeek, int toWeek)
    {
        var rows = db.Scoring
            .Where(s => s.SeasonId == seasonId && s.WeekId >= fromWeek && s.WeekId <= toWeek)
            .ToList();

        if (rows.Count == 0) return [];

        // Aggregate per (TeamId, DivisionId)
        var agg = rows
            .GroupBy(s => new { s.TeamId, s.DivisionId, s.TeamName, s.SeasonId, s.LeagueId })
            .Select(g => new StandingView
            {
                TeamId   = g.Key.TeamId,
                TeamName = g.Key.TeamName,
                DivisionId  = g.Key.DivisionId,
                SeasonId    = g.Key.SeasonId,
                LeagueId    = g.Key.LeagueId,
                GamesPlayed    = isGamesMode ? g.Count() : 0,
                MatchesPlayed  = isGamesMode ? 0 : g.Select(s => s.ScheduleDivisionsId).Distinct().Count(),
                Wins       = g.Count(s => s.IsWin),
                Ties       = g.Count(s => s.IsTie),
                Losses     = g.Count(s => s.IsLoss && !s.IsForfeit),
                Forfeits   = g.Count(s => s.IsForfeit),
                StandingsPoints = g.Sum(s => s.WinPTS + s.TiePTS + s.LossPTS + s.ForfeitPoints),
                PlusMinus  = g.Sum(s => s.PlusMinus),
                PointsFor  = g.Sum(s => s.PointsFor),
                PointsAgainst = g.Sum(s => s.PointsAgainst),
                H2HPlusMinus = 0,  // cross-div H2H not computed for date-range slices
                H2HWins      = 0
            })
            .ToList();

        // Division rank (DENSE_RANK per division)
        foreach (var dg in agg.GroupBy(r => r.DivisionId))
        {
            int seed = 1;
            int prevPts = int.MaxValue, prevPm = int.MaxValue, prevW = int.MaxValue;
            foreach (var r in dg.OrderByDescending(r => r.StandingsPoints)
                                 .ThenByDescending(r => r.PlusMinus)
                                 .ThenByDescending(r => r.Wins))
            {
                if (r.StandingsPoints != prevPts || r.PlusMinus != prevPm || r.Wins != prevW)
                {
                    prevPts = r.StandingsPoints; prevPm = r.PlusMinus; prevW = r.Wins;
                }
                // Assign the rank at the first position for this stats group
                r.DivisionRank = seed;
                r.DivisionSeed = seed;
                seed++;
            }
            // Correct DivisionRank to DENSE_RANK (tied teams share the same rank)
            var sorted2 = dg.OrderByDescending(r => r.StandingsPoints)
                            .ThenByDescending(r => r.PlusMinus)
                            .ThenByDescending(r => r.Wins).ToList();
            int rank = 1;
            for (int i = 0; i < sorted2.Count; i++)
            {
                if (i > 0 && (sorted2[i].StandingsPoints != sorted2[i-1].StandingsPoints
                           || sorted2[i].PlusMinus        != sorted2[i-1].PlusMinus
                           || sorted2[i].Wins             != sorted2[i-1].Wins))
                    rank = i + 1;
                sorted2[i].DivisionRank = rank;
            }
        }

        // Season seed (mirrors FixSeasonSeedOrdering logic)
        int sseed = 1;
        foreach (var r in agg
            .OrderBy(r => firstPlaceGuaranteed && r.DivisionRank == 1 ? 0 : 1)
            .ThenByDescending(r => r.StandingsPoints)
            .ThenByDescending(r => r.PlusMinus)
            .ThenByDescending(r => r.Wins)
            .ThenBy(r => r.DivisionRank))
        {
            r.SeasonSeed = sseed++;
        }

        return agg.OrderBy(r => r.DivisionId).ThenBy(r => r.DivisionRank).ToList();
    }

    // ── All-Divisions scrollable panel ────────────────────────────────────────

    private const int CellHdrH = 26;
    private const int DgvHdrH  = 28;
    private const int DgvRowH  = 26;
    private const int SlotHdrH = 32;
    private const int ColGap   = 8;
    private const int CellGap  = 6;

    private static int[] GetStatWidths(bool isGamesMode, bool h2hUsed)
    {
        var ws = new List<int> { 40, 44, 44 };  // #(Seed), GP/MP, W
        if (!isGamesMode) ws.Add(44);            // T
        ws.AddRange([44, 44, 50, 54]);           // L, F, Pts, +/-
        if (h2hUsed) ws.AddRange([64, 54]);      // H2H+/-, H2HW
        return ws.ToArray();
    }

    private void RebuildAllDivisionsPanel()
    {
        if (_allDivPage == null) return;

        _allDivPanel?.Dispose();
        _divCellPanels.Clear();

        if (_timeCols.Count == 0) { _allDivPanel = null; return; }

        int[] statWs    = GetStatWidths(_isGamesMode, _h2hUsed);
        int   statTotal = statWs.Sum();
        int[] colWs     = _teamColWidths.Select(tw => tw + statTotal).ToArray();

        var visibleByCol = _timeCols.Select((tc, _) =>
        {
            var divs = _timeColDivs.TryGetValue(tc.Id, out var list) ? list : [];
            return divs
                .Where(d => _filteredDivIds == null || _filteredDivIds.Contains(d.Div.Id))
                .ToList();
        }).ToList();

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

        for (int ci = 0; ci < _timeCols.Count; ci++)
            outer.Controls.Add(new Label
            {
                Text = _timeCols[ci].Label, Font = AppTheme.FontDefaultBold,
                ForeColor = Color.White, BackColor = AppTheme.GridHeaderBackground,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(colXs[ci], 0), Size = new Size(colWs[ci], SlotHdrH), AutoSize = false
            });

        for (int ci = 0; ci < _timeCols.Count; ci++)
        {
            int y = SlotHdrH;
            int teamW = _teamColWidths[ci];
            foreach (var d in visibleByCol[ci])
            {
                int rowsH = DgvHdrH + d.Rows.Count * DgvRowH + 2;
                int cellH = CellHdrH + rowsH;
                var cellPanel = BuildDivCell(d.Div, d.Rows, teamW, statWs, colWs[ci], cellH);
                cellPanel.Location = new Point(colXs[ci], y);
                outer.Controls.Add(cellPanel);
                _divCellPanels[d.Div.Id] = cellPanel;
                y += cellH + CellGap;
            }
        }

        _allDivPanel = outer;
        _allDivPage.Controls.Clear();
        _allDivPage.Controls.Add(_allDivPanel);
    }

    private Panel BuildDivCell(Division div, List<StandingView> rows,
        int teamW, int[] statWs, int colW, int cellH)
    {
        var cell = new Panel { Size = new Size(colW, cellH), BackColor = AppTheme.ContentBackground };
        cell.Controls.Add(new Label
        {
            Text = div.Name, Font = AppTheme.FontDefaultBold,
            ForeColor = Color.White, BackColor = Color.FromArgb(100, 130, 170),
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(0, 0), Size = new Size(colW, CellHdrH), AutoSize = false,
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
            ("Seed", "#", true, "# = Overall season seed (playoff position)"),
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

    private void RebuildSeasonSeedPanel()
    {
        if (_seedPage == null) return;
        _seedPage.Controls.Clear();

        var seedRows = _currentRows.OrderBy(r => r.SeasonSeed).ToList();
        var divNames = _allDivisions.ToDictionary(d => d.Id, d => d.Name);
        var panel    = BuildSeasonSeedSplitPanel(seedRows, divNames, _teamsInPlayoffs, _isGamesMode);
        panel.Dock = DockStyle.Fill;
        _seedPage.Controls.Add(panel);
    }

    private static SplitContainer BuildSeasonSeedSplitPanel(
        List<StandingView> rows, Dictionary<int, string> divNames,
        int teamsInPlayoffs, bool isGamesMode)
    {
        int leftN = (rows.Count + 1) / 2;

        int teamW;
        using (var bmp = new Bitmap(1, 1))
        using (var g   = Graphics.FromImage(bmp))
        {
            int M(string s) => (int)Math.Ceiling(g.MeasureString(s, AppTheme.FontDefault).Width) + 10;
            teamW = Math.Max(M("Team"), rows.Select(r => M(r.TeamName)).DefaultIfEmpty(0).Max());
        }

        var sc = new SplitContainer
        {
            Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterWidth = 6,
            BackColor = AppTheme.Separator, Panel1MinSize = 100, Panel2MinSize = 100
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
        leftGrid.Dock = DockStyle.Fill; rightGrid.Dock = DockStyle.Fill;
        sc.Panel1.BackColor = AppTheme.ContentBackground;
        sc.Panel2.BackColor = AppTheme.ContentBackground;
        sc.Panel1.Controls.Add(leftGrid);
        sc.Panel2.Controls.Add(rightGrid);
        return sc;
    }

    private static DataGridView BuildSeasonSeedGrid(
        List<StandingView> rows, Dictionary<int, string> divNames,
        int teamsInPlayoffs, bool isGamesMode, int teamW = 155)
    {
        var grid = MakeGrid();
        grid.Columns.AddRange(
            Col("Seed", "Seed",      62,  mid: true, tip: "Season seed — playoff order"),
            Col("Team", "Team",      teamW),
            Col("Div",  "Division",  185, tip: "Division"),
            Col("DivR", "Div. Seed", 92,  mid: true, tip: "Division seed (finish position in division)"),
            Col("Pts",  "Pts",       50,  mid: true, tip: "Standings points"),
            Col("PM",   "+/-",       54,  mid: true, tip: "Plus/Minus"),
            Col("W",    "W",         44,  mid: true, tip: "Wins")
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
        var form  = new Form
        {
            FormBorderStyle = FormBorderStyle.FixedToolWindow,
            Text = "Filter Divisions", StartPosition = FormStartPosition.Manual,
            Size = new Size(226, formH), BackColor = AppTheme.Surface
        };
        var clb = new CheckedListBox
        {
            Dock = DockStyle.Fill, CheckOnClick = true,
            Font = AppTheme.FontDefault,
            BackColor = AppTheme.ContentBackground, ForeColor = AppTheme.TextPrimary
        };
        foreach (var (divId, label) in _divFilterItems)
            clb.Items.Add(label, _filteredDivIds == null || _filteredDivIds.Contains(divId));

        var btnBar  = new Panel { Dock = DockStyle.Bottom, Height = 36, BackColor = AppTheme.Surface };
        var btnAll   = MakeSmallBtn("All",   Color.FromArgb(60, 100, 160), 6);
        var btnNone  = MakeSmallBtn("None",  Color.FromArgb(100, 100, 110), 72);
        var btnClose = MakeSmallBtn("Close", Color.FromArgb(80, 80, 80), 138);
        btnBar.Controls.AddRange([btnAll, btnNone, btnClose]);
        form.Controls.Add(clb); form.Controls.Add(btnBar);

        clb.ItemCheck  += (_, _) => BeginInvoke(() => ApplyFilterFrom(clb));
        btnAll.Click   += (_, _) => { for (int i = 0; i < clb.Items.Count; i++) clb.SetItemChecked(i, true);  BeginInvoke(() => ApplyFilterFrom(clb)); };
        btnNone.Click  += (_, _) => { for (int i = 0; i < clb.Items.Count; i++) clb.SetItemChecked(i, false); BeginInvoke(() => ApplyFilterFrom(clb)); };
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
        RebuildAllDivisionsPanel();
    }

    // ── Print ─────────────────────────────────────────────────────────────────

    private void PrintCurrent()
    {
        if (!_seasonId.HasValue) return;
        bool onSeedTab = _tabs.SelectedIndex > 0;
        if (onSeedTab)
            StandingsPrintService.ShowPrintPreview(this, _seasonId.Value, StandingsPrintMode.SeasonSeed);
        else
            StandingsPrintService.ShowPrintPreview(this, _seasonId.Value, StandingsPrintMode.AllDivisions, _filteredDivIds);
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

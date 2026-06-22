using System.Drawing.Printing;
using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.Services;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Panels;

public class ScoreEntryPanel : UserControl
{
    // ── State ──────────────────────────────────────────────────────────────────
    private int? _seasonId;
    private int  _selectedWeek = 1;

    // ── Controls ───────────────────────────────────────────────────────────────
    private ComboBox _cmbWeek        = null!;
    private Button   _btnSave        = null!;
    private Button   _btnClearAll    = null!;
    private Button   _btnPrint       = null!;
    private Button   _btnPrintSeason = null!;
    private Label    _lblStatus      = null!;
    private Panel    _headerPanel    = null!;
    private Panel    _scroll         = null!;

    // (sdId, game 1|2, team 1|2) → TextBox
    private readonly Dictionary<(int sdId, int game, int team), TextBox> _boxes = [];
    // ordered left→right, top→bottom for arrow/Enter navigation
    private readonly List<TextBox> _boxOrder = [];
    // number of score boxes per row (colCount × 4); used for Up/Down navigation
    private int _boxesPerRow = 0;
    // cached query result for the current week (used by Print Week)
    private WeekGridData? _weekData;

    // ── Grid data types ────────────────────────────────────────────────────────

    private record MatchInfo(
        ScheduleDivision SD,
        DateOnly Date, string DayName,
        string TimeStr, int TimeSort,
        int CourtId, string CourtLabel, int CourtSort,
        string T1Letter, string T2Letter);

    private record PrintHeader(string ClubName, string LeagueName, string SeasonName);

    private record WeekGridData(
        List<MatchInfo> Matches,
        List<(DateOnly Date, string DayName, string TimeStr, int TimeSort)> RowKeys,
        List<(int CourtId, string CourtLabel, int CourtSort)> ColKeys,
        Dictionary<(DateOnly, string, int), MatchInfo> Lookup,
        PrintHeader Header);

    // ── Static fonts ───────────────────────────────────────────────────────────
    private static readonly Font s_hdrFont  = new("Segoe UI",  9f, FontStyle.Bold);
    private static readonly Font s_lttrFont = new("Segoe UI",  8f, FontStyle.Bold);
    private static readonly Font s_numFont  = new("Segoe UI", 11f, FontStyle.Bold);
    private static readonly Font s_titleFnt = new("Segoe UI", 11f, FontStyle.Bold);
    private static readonly Font s_dayFont  = new("Segoe UI", 10f, FontStyle.Bold);

    // ── Colors ─────────────────────────────────────────────────────────────────
    private static readonly Color HdrBg     = Color.FromArgb(35, 55, 80);
    private static readonly Color LttrBg    = Color.FromArgb(210, 220, 235);
    private static readonly Color LttrFg    = Color.FromArgb(40, 60, 100);
    private static readonly Color ValidGn   = Color.FromArgb(198, 239, 206);
    private static readonly Color InvalidRd = Color.FromArgb(255, 199, 206);
    private static readonly Color HatchFg   = Color.FromArgb(160, 160, 160);
    private static readonly Color HatchBg   = Color.FromArgb(225, 225, 225);
    private static readonly Color RowEven   = Color.White;
    private static readonly Color RowOdd    = Color.FromArgb(246, 248, 252);

    // ── Layout constants ───────────────────────────────────────────────────────
    private const int Px     = 8;
    private const int DayW   = 88;
    private const int TimeW  = 72;
    private const int BoxW   = 44;
    private const int BoxH   = 24;
    private const int GameG  = 8;
    private const int CtG    = 12;
    private const int Hdr0H  = 26;
    private const int Hdr1H  = 22;
    private const int RowH   = 54;
    private const int LttrH  = 18;
    private const int LttrY  = 3;
    private const int BoxY   = LttrH + 5;         // = 23
    private const int CourtW = BoxW * 4 + GameG;  // = 184

    private int CrtX(int ci) => Px + DayW + TimeW + ci * (CourtW + CtG);
    private int BxX(int ci, int game, int team) =>
        CrtX(ci) + (game - 1) * (BoxW * 2 + GameG) + (team - 1) * BoxW;

    // ── Constructor ────────────────────────────────────────────────────────────

    public ScoreEntryPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;
        BuildUI();
        AppParameterService.DefaultsChanged += OnDefaultsChanged;
        HandleCreated += (_, _) => BeginInvoke(new Action(LoadContext));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) AppParameterService.DefaultsChanged -= OnDefaultsChanged;
        base.Dispose(disposing);
    }

    private void OnDefaultsChanged(object? sender, DefaultsChangedEventArgs e) => LoadContext();

    // ── UI Construction ────────────────────────────────────────────────────────

    private void BuildUI()
    {
        var outer = new TableLayoutPanel
        {
            Dock            = DockStyle.Fill,
            ColumnCount     = 1,
            RowCount        = 3,
            Padding         = Padding.Empty,
            Margin          = Padding.Empty,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        };
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, Hdr0H + Hdr1H + 4));
        outer.RowStyles.Add(new RowStyle(SizeType.Percent,  100));
        Controls.Add(outer);

        // ── Toolbar ──
        var toolbar = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface };
        outer.Controls.Add(toolbar, 0, 0);
        toolbar.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.Separator);
            e.Graphics.DrawLine(pen, 0, toolbar.Height - 1, toolbar.Width, toolbar.Height - 1);
        };

        toolbar.Controls.Add(new Label
        {
            Text = "Week:", Font = AppTheme.FontDefaultBold,
            ForeColor = AppTheme.TextPrimary, AutoSize = true,
            Location = new Point(12, 16)
        });

        _cmbWeek = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = AppTheme.FontDefault, Width = 80, Location = new Point(62, 13)
        };
        toolbar.Controls.Add(_cmbWeek);

        _btnSave = MakeBtn("Save Scores", AppTheme.ButtonSuccess, 155, 100);
        _btnSave.Click += OnSave;
        toolbar.Controls.Add(_btnSave);

        _btnClearAll = MakeBtn("Clear All", Color.FromArgb(180, 100, 30), 260, 85);
        _btnClearAll.Click += OnClearAll;
        toolbar.Controls.Add(_btnClearAll);

        _btnPrint = MakeBtn("Print Week", Color.FromArgb(60, 100, 160), 352, 95);
        _btnPrint.Click += OnPrint;
        toolbar.Controls.Add(_btnPrint);

        _btnPrintSeason = MakeBtn("Print Season", Color.FromArgb(60, 100, 160), 452, 110);
        _btnPrintSeason.Click += OnPrintSeason;
        toolbar.Controls.Add(_btnPrintSeason);

        _lblStatus = new Label
        {
            Text = "", Font = AppTheme.FontDefault,
            ForeColor = AppTheme.TextSecondary,
            AutoSize = true, Location = new Point(570, 16)
        };
        toolbar.Controls.Add(_lblStatus);

        // ── Sticky column headers ──
        _headerPanel = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.ContentBackground };
        outer.Controls.Add(_headerPanel, 0, 1);

        // ── Scrollable data rows ──
        _scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = AppTheme.ContentBackground };
        outer.Controls.Add(_scroll, 0, 2);
    }

    private static Button MakeBtn(string text, Color bg, int x, int w) => new()
    {
        Text = text, Font = AppTheme.FontDefault,
        BackColor = bg, ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Size = new Size(w, 28), Location = new Point(x, 11),
        FlatAppearance = { BorderSize = 0 }
    };

    // ── Context Load ───────────────────────────────────────────────────────────

    private void LoadContext()
    {
        _cmbWeek.SelectedIndexChanged -= OnWeekChanged;
        _cmbWeek.Items.Clear();
        ClearGrid();

        try
        {
            using var db = new BocceDbContext();
            _seasonId = AppParameterService.GetDefaultSeasonId(db);

            if (_seasonId.HasValue)
            {
                var season = db.Seasons.Find(_seasonId.Value);
                if (season != null)
                {
                    int maxW = Math.Max(1, season.WeeksInSeason);
                    for (int w = 1; w <= maxW; w++) _cmbWeek.Items.Add(w);
                }
                else SetStatus("Season not found.");
            }
            else SetStatus("No season selected.");
        }
        catch (Exception ex) { SetStatus($"Error: {ex.Message}"); }

        _cmbWeek.SelectedIndexChanged += OnWeekChanged;
        if (_cmbWeek.Items.Count > 0) _cmbWeek.SelectedIndex = 0;
    }

    private void OnWeekChanged(object? sender, EventArgs e)
    {
        if (_cmbWeek.SelectedItem is int w) { _selectedWeek = w; BuildGrid(); }
    }

    private void ClearGrid()
    {
        var oldHdr    = _headerPanel.Controls.Cast<Control>().ToArray();
        var oldScroll = _scroll.Controls.Cast<Control>().ToArray();
        _headerPanel.Controls.Clear();
        _scroll.Controls.Clear();
        _scroll.AutoScrollPosition = new Point(0, 0);
        foreach (var c in oldHdr.Concat(oldScroll)) c.Dispose();
        _boxes.Clear();
        _boxOrder.Clear();
        _boxesPerRow = 0;
        _weekData = null;
    }

    // ── Grid Building ──────────────────────────────────────────────────────────

    private void BuildGrid()
    {
        ClearGrid();
        SetStatus("Loading...");
        if (!_seasonId.HasValue) { SetStatus("No season selected."); return; }
        try { BuildGridInner(); }
        catch (Exception ex) { SetStatus($"Error ({ex.GetType().Name}): {ex.Message}"); }
    }

    private void BuildGridInner()
    {
        var data = QueryWeekData(_selectedWeek);
        if (data == null) { SetStatus($"No matches for Week {_selectedWeek}."); return; }
        _weekData = data;

        var rowKeys = data.RowKeys;
        var colKeys = data.ColKeys;
        var lookup  = data.Lookup;

        _boxesPerRow = colKeys.Count * 4;  // G1T1 G1T2 G2T1 G2T2 per court

        // ── Sticky column headers ──────────────────────────────────────────────
        var hdrs = new List<Control>();
        hdrs.Add(Lbl($"Week {_selectedWeek}", Px, 0,
            AppTheme.TextPrimary, s_titleFnt, DayW + TimeW - 4, Hdr0H + Hdr1H));

        for (int ci = 0; ci < colKeys.Count; ci++)
        {
            hdrs.Add(Hdr(colKeys[ci].CourtLabel, CrtX(ci),      0,     CourtW,    Hdr0H));
            hdrs.Add(Hdr("Game 1",               BxX(ci, 1, 1), Hdr0H, BoxW * 2, Hdr1H));
            hdrs.Add(Hdr("Game 2",               BxX(ci, 2, 1), Hdr0H, BoxW * 2, Hdr1H));
        }
        _headerPanel.Controls.AddRange([.. hdrs]);

        // ── Group rows by day for outline rendering ────────────────────────────
        var dayGroups = new List<List<(DateOnly Date, string DayName, string TimeStr, int TimeSort)>>();
        string? curDay = null;
        foreach (var rk in rowKeys)
        {
            if (rk.DayName != curDay) { dayGroups.Add([rk]); curDay = rk.DayName; }
            else dayGroups[^1].Add(rk);
        }

        // ── Data rows ─────────────────────────────────────────────────────────
        int rowW = CrtX(colKeys.Count - 1) + CourtW + Px;
        int overallY = 4, rowIdx = 0, matchedCells = 0, hatchCells = 0;
        var dayWrappers = new List<Panel>();

        foreach (var dayGroup in dayGroups)
        {
            int dayRowCount = dayGroup.Count;
            int dayH = dayRowCount * RowH + 4;

            var dayWrapper = new Panel
            {
                Location  = new Point(0, overallY),
                Size      = new Size(rowW, dayH),
                BackColor = AppTheme.ContentBackground
            };
            dayWrapper.Paint += (s, e) =>
            {
                var p = (Panel)s!;
                using var pen = new Pen(Color.Black, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
            };

            for (int i = 0; i < dayGroup.Count; i++, rowIdx++)
            {
                var (date, dayName, timeStr, _) = dayGroup[i];
                Color rowBg = rowIdx % 2 == 0 ? RowEven : RowOdd;

                var rowPanel = new Panel
                {
                    Location  = new Point(1, 2 + i * RowH),
                    Size      = new Size(rowW - 2, RowH - 2),
                    BackColor = rowBg
                };

                if (i == 0)
                    rowPanel.Controls.Add(Lbl(dayName, Px, (RowH - 20) / 2,
                        AppTheme.TextPrimary, s_dayFont, DayW, 20));

                rowPanel.Controls.Add(Lbl(timeStr.Length > 0 ? timeStr : "—",
                    Px + DayW, (RowH - 20) / 2,
                    AppTheme.TextSecondary, AppTheme.FontDefault, TimeW, 20,
                    ContentAlignment.MiddleCenter));

                for (int ci = 0; ci < colKeys.Count; ci++)
                {
                    var (courtId, _, _) = colKeys[ci];
                    int cx = CrtX(ci);

                    if (!lookup.TryGetValue((date, timeStr, courtId), out var m))
                    {
                        hatchCells++;
                        var hatch = new Panel { Location = new Point(cx, 0), Size = new Size(CourtW, RowH - 2) };
                        hatch.Paint += (s, e) =>
                        {
                            using var br = new System.Drawing.Drawing2D.HatchBrush(
                                System.Drawing.Drawing2D.HatchStyle.ForwardDiagonal, HatchFg, HatchBg);
                            e.Graphics.FillRectangle(br,
                                new Rectangle(0, 0, ((Panel)s!).Width, ((Panel)s!).Height));
                        };
                        rowPanel.Controls.Add(hatch);
                        continue;
                    }

                    matchedCells++;
                    var sd = m.SD;

                    var cell = new Panel
                    {
                        Location  = new Point(cx, 0),
                        Size      = new Size(CourtW, RowH - 2),
                        BackColor = rowBg
                    };

                    for (int g = 1; g <= 2; g++)
                    {
                        int lx1 = BxX(ci, g, 1) - cx;
                        int lx2 = BxX(ci, g, 2) - cx;
                        cell.Controls.Add(TLetter(m.T1Letter, lx1, LttrY, BoxW));
                        cell.Controls.Add(TLetter(m.T2Letter, lx2, LttrY, BoxW));
                    }

                    for (int g = 1; g <= 2; g++)
                    {
                        int v1  = g == 1 ? sd.Team1Score1 : sd.Team1Score2;
                        int v2  = g == 1 ? sd.Team2Score1 : sd.Team2Score2;
                        int bx1 = BxX(ci, g, 1) - cx;
                        int bx2 = BxX(ci, g, 2) - cx;

                        var b1 = SBox(v1, bx1, BoxY, BoxW, BoxH);
                        var b2 = SBox(v2, bx2, BoxY, BoxW, BoxH);

                        _boxes[(sd.Id, g, 1)] = b1;
                        _boxes[(sd.Id, g, 2)] = b2;
                        _boxOrder.Add(b1);
                        _boxOrder.Add(b2);

                        WirePair(b1, b2);
                        ColorPair(b1, b2);

                        cell.Controls.Add(b1);
                        cell.Controls.Add(b2);
                    }

                    rowPanel.Controls.Add(cell);
                }

                dayWrapper.Controls.Add(rowPanel);
            }

            overallY += dayH + 6;
            dayWrappers.Add(dayWrapper);
        }

        _scroll.Controls.AddRange([.. dayWrappers]);
        SetStatus($"Week {_selectedWeek}: {data.Matches.Count} records | {rowKeys.Count} rows | {colKeys.Count} courts | {matchedCells} cells | {hatchCells} hatched");
    }

    // ── Data Query (shared by grid build and printing) ─────────────────────────

    private WeekGridData? QueryWeekData(int week)
    {
        if (!_seasonId.HasValue) return null;

        using var db = new BocceDbContext();

        var season   = db.Seasons.Include(s => s.League).FirstOrDefault(s => s.Id == _seasonId.Value);
        var clubName = db.AppParameters.FirstOrDefault(p => p.Key == "ClubName")?.Value ?? "";
        var header   = new PrintHeader(
            ClubName:   clubName,
            LeagueName: season?.League.Name ?? "",
            SeasonName: season?.Name ?? "");

        var sds = db.ScheduleDivisions
            .Include(sd => sd.Division).ThenInclude(d => d.DaySlot)
            .Include(sd => sd.Division).ThenInclude(d => d.TimeSlot)
            .Include(sd => sd.Team1)
            .Include(sd => sd.Team2)
            .Include(sd => sd.Court)
            .Where(sd => sd.Division.SeasonId == _seasonId.Value
                      && sd.TemplateWeekNumber == week)
            .ToList();

        if (sds.Count == 0) return null;

        var matches = sds.Select(sd => new MatchInfo(
            SD:         sd,
            Date:       sd.MatchDate,
            DayName:    sd.Division.DaySlot?.DayName      ?? sd.MatchDate.DayOfWeek.ToString(),
            TimeStr:    sd.Division.TimeSlot?.Timeslot12h  ?? "",
            TimeSort:   sd.Division.TimeSlot?.SortOrder    ?? 9999,
            CourtId:    sd.CourtId ?? 0,
            CourtLabel: sd.Court != null ? $"Court {sd.Court.CourtNumber}" : "Court ?",
            CourtSort:  sd.Court?.SortOrder ?? 9999,
            T1Letter:   sd.Team1?.TeamLetter ?? "?",
            T2Letter:   sd.Team2?.TeamLetter ?? "?")).ToList();

        var rowKeys = matches
            .Select(m => (m.Date, m.DayName, m.TimeStr, m.TimeSort))
            .Distinct()
            .OrderBy(k => k.Date).ThenBy(k => k.TimeSort)
            .ToList();

        var colKeys = matches
            .Where(m => m.CourtId != 0)
            .Select(m => (m.CourtId, m.CourtLabel, m.CourtSort))
            .Distinct()
            .OrderBy(c => c.CourtSort)
            .ToList();

        if (colKeys.Count == 0) return null;

        var lookup = matches
            .Where(m => m.CourtId != 0)
            .GroupBy(m => (m.Date, m.TimeStr, m.CourtId))
            .ToDictionary(g => g.Key, g => g.First());

        return new WeekGridData(matches, rowKeys, colKeys, lookup, header);
    }

    // ── Validation & Wiring ────────────────────────────────────────────────────

    private void WirePair(TextBox b1, TextBox b2)
    {
        // Immediate colour feedback on every keystroke
        b1.TextChanged += (_, _) => ColorPair(b1, b2);
        b2.TextChanged += (_, _) => ColorPair(b1, b2);

        // Select-all on focus — makes replacing the current value easy
        b1.GotFocus += (_, _) => b1.SelectAll();
        b2.GotFocus += (_, _) => b2.SelectAll();

        // Numeric-only input; range enforced at the character level
        b1.KeyPress += FilterNumeric;
        b2.KeyPress += FilterNumeric;

        // Arrow / Enter navigation
        b1.KeyDown += (_, e) => NavigateBox(b1, e);
        b2.KeyDown += (_, e) => NavigateBox(b2, e);

        // Auto-fill rules on leaving b1 (first box of the pair)
        b1.Leave += (_, _) =>
        {
            if (!int.TryParse(b1.Text.Trim(), out int v)) return;
            if (v is >= 0 and <= 11)
                b2.Text = "12";  // normal: auto-set partner to 12
            else if (v == -1 && int.TryParse(b2.Text.Trim(), out int bv) && bv > 0)
                b2.Text = "0";   // forfeit: partner must be 0 or -1
        };

        // Forfeit correction on leaving b2 (second box)
        b2.Leave += (_, _) =>
        {
            if (int.TryParse(b2.Text.Trim(), out int v) && v == -1
                && int.TryParse(b1.Text.Trim(), out int av) && av > 0)
                b1.Text = "0";
        };
    }

    // Only allow characters that could form a valid integer in [-1 .. 12]
    private static void FilterNumeric(object? sender, KeyPressEventArgs e)
    {
        if (char.IsControl(e.KeyChar)) return;
        if (!char.IsDigit(e.KeyChar) && e.KeyChar != '-') { e.Handled = true; return; }

        var tb = (TextBox)sender!;
        var proposed = tb.Text
            .Remove(tb.SelectionStart, tb.SelectionLength)
            .Insert(tb.SelectionStart, e.KeyChar.ToString());

        if (proposed == "-") return;  // leading minus is fine (partial -1)

        if (!int.TryParse(proposed, out int v) || v < -1 || v > 12)
            e.Handled = true;
    }

    private void NavigateBox(TextBox tb, KeyEventArgs e)
    {
        int idx = _boxOrder.IndexOf(tb);
        if (idx < 0) return;

        int next = e.KeyCode switch
        {
            Keys.Enter or Keys.Right => idx + 1,
            Keys.Left                => idx - 1,
            Keys.Down when _boxesPerRow > 0 => idx + _boxesPerRow,
            Keys.Up   when _boxesPerRow > 0 => idx - _boxesPerRow,
            _ => -1
        };

        if (next < 0 || next >= _boxOrder.Count) return;
        e.SuppressKeyPress = true;
        _boxOrder[next].Focus();
    }

    private static void ColorPair(TextBox b1, TextBox b2)
    {
        bool v1 = int.TryParse(b1.Text.Trim(), out int a);
        bool v2 = int.TryParse(b2.Text.Trim(), out int b);

        Color c;
        if (!v1 || !v2)
            c = Color.White;       // one or both blank — neutral
        else if (a == -1 || b == -1)
        {
            // Forfeit: the other score must be 0 or -1
            bool valid = (a == -1 && b is 0 or -1) || (b == -1 && a is 0 or -1);
            c = valid ? ValidGn : InvalidRd;
        }
        else if (a == 12 && b == 12)
            c = InvalidRd;         // INVALID: both 12
        else if (a == 12 || b == 12)
            c = ValidGn;           // valid game result
        else
            c = InvalidRd;         // no winner yet

        b1.BackColor = c;
        b2.BackColor = c;
    }

    // ── Save ──────────────────────────────────────────────────────────────────

    private void OnSave(object? sender, EventArgs e)
    {
        if (!_seasonId.HasValue) return;
        try
        {
            using var db = new BocceDbContext();
            var ids   = _boxes.Keys.Select(k => k.sdId).Distinct().ToList();
            var sdMap = db.ScheduleDivisions
                          .Where(sd => ids.Contains(sd.Id))
                          .ToDictionary(sd => sd.Id);

            foreach (var id in ids)
            {
                if (!sdMap.TryGetValue(id, out var sd)) continue;
                if (_boxes.TryGetValue((id, 1, 1), out var bx)) sd.Team1Score1 = ParseScore(bx);
                if (_boxes.TryGetValue((id, 1, 2), out bx))     sd.Team2Score1 = ParseScore(bx);
                if (_boxes.TryGetValue((id, 2, 1), out bx))     sd.Team1Score2 = ParseScore(bx);
                if (_boxes.TryGetValue((id, 2, 2), out bx))     sd.Team2Score2 = ParseScore(bx);
            }

            db.SaveChanges();
            SetStatus($"Saved {ids.Count} match{(ids.Count == 1 ? "" : "es")}.");
        }
        catch (Exception ex) { SetStatus($"Save error: {ex.Message}"); }
    }

    private static int ParseScore(TextBox tb) =>
        int.TryParse(tb.Text.Trim(), out int v) ? v : 0;

    // ── Clear All ─────────────────────────────────────────────────────────────

    private void OnClearAll(object? sender, EventArgs e)
    {
        if (_boxes.Count == 0) { SetStatus("No scores to clear."); return; }
        if (MessageBox.Show(
                "Clear all entered scores for this week?",
                "Clear Scores", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            != DialogResult.Yes) return;

        foreach (var tb in _boxes.Values) { tb.Text = ""; tb.BackColor = Color.White; }
        SetStatus("All scores cleared.");
    }

    // ── Print Week ────────────────────────────────────────────────────────────

    private void OnPrint(object? sender, EventArgs e)
    {
        if (!_seasonId.HasValue) { SetStatus("No season selected."); return; }
        if (_weekData == null)   { SetStatus($"No data for Week {_selectedWeek}."); return; }

        bool hasScores  = _boxes.Values.Any(tb => !string.IsNullOrWhiteSpace(tb.Text));
        bool showScores = false;

        if (hasScores)
        {
            var ans = MessageBox.Show(
                "Scores are entered for this week.\n\nInclude scores in the printout?",
                "Print Week Score Sheet",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            showScores = ans == DialogResult.Yes;
        }

        Func<int, int, int, string> getScore = showScores
            ? ((sdId, game, team) =>
                _boxes.TryGetValue((sdId, game, team), out var tb) ? tb.Text.Trim() : "")
            : ((_, _, _) => "");

        ShowPrintPreview($"Week {_selectedWeek}", _weekData, getScore);
    }

    // ── Print Season ──────────────────────────────────────────────────────────

    private void OnPrintSeason(object? sender, EventArgs e)
    {
        if (!_seasonId.HasValue) { SetStatus("No season selected."); return; }

        int totalWeeks;
        using (var db = new BocceDbContext())
        {
            var season = db.Seasons.Find(_seasonId.Value);
            if (season == null) { SetStatus("Season not found."); return; }
            totalWeeks = season.WeeksInSeason;
        }

        SetStatus("Building season data…");
        Application.DoEvents();

        var pages = new List<(int week, WeekGridData data)>();
        for (int w = 1; w <= totalWeeks; w++)
        {
            var d = QueryWeekData(w);
            if (d != null) pages.Add((w, d));
        }

        if (pages.Count == 0) { SetStatus("No schedule data found."); return; }

        int pageIdx = 0;
        var pd = new PrintDocument();
        pd.DefaultPageSettings.Landscape = true;
        pd.DefaultPageSettings.Margins = new Margins(150, 50, 50, 50);  // 1.5" left (clipboard), 0.5" others
        pd.PrintPage += (_, pe) =>
        {
            var (wk, dat) = pages[pageIdx++];
            DrawWeekGrid(pe.Graphics!, pe.MarginBounds, wk, dat, (_, _, _) => "");
            pe.HasMorePages = pageIdx < pages.Count;
        };

        using var preview = new PrintPreviewDialog { Document = pd, WindowState = FormWindowState.Maximized };
        preview.ShowDialog(this);
        SetStatus($"Season preview: {pages.Count} weeks.");
    }

    private void ShowPrintPreview(string _, WeekGridData data, Func<int, int, int, string> getScore)
    {
        var pd = new PrintDocument();
        pd.DefaultPageSettings.Landscape = true;
        pd.DefaultPageSettings.Margins = new Margins(150, 50, 50, 50);  // 1.5" left (clipboard), 0.5" others
        pd.PrintPage += (__, pe) =>
        {
            DrawWeekGrid(pe.Graphics!, pe.MarginBounds, _selectedWeek, data, getScore);
            pe.HasMorePages = false;
        };

        using var preview = new PrintPreviewDialog { Document = pd, WindowState = FormWindowState.Maximized };
        preview.ShowDialog(this);
    }

    // ── Print Drawing ─────────────────────────────────────────────────────────

    private void DrawWeekGrid(Graphics g, RectangleF bounds, int week,
        WeekGridData data, Func<int, int, int, string> getScore)
    {
        int colCount = data.ColKeys.Count;
        if (colCount == 0) return;

        // ── Print-only layout constants ────────────────────────────────────────
        // Wider day column so "Wednesday" fits on one line (screen uses DayW=88)
        const float pDayW  = 110f;
        const float pTimeW = 72f;

        // Local helpers replacing CrtX/BxX for the print context
        int PrtX(int ci) => (int)(Px + pDayW + pTimeW) + ci * (CourtW + CtG);
        int PrtBxX(int ci, int gm, int tm) =>
            PrtX(ci) + (gm - 1) * (BoxW * 2 + GameG) + (tm - 1) * BoxW;

        float pGridW = PrtX(colCount - 1) + CourtW + Px;

        // ── Determine which rows show team letters ─────────────────────────────
        // Only the first row of each consecutive group with the same matchup pattern shows letters.
        const float SlimRowH = 34f;
        var showLtrs = new bool[data.RowKeys.Count];
        string? prevKey = null;
        for (int ri = 0; ri < data.RowKeys.Count; ri++)
        {
            var (d, _, ts, _) = data.RowKeys[ri];
            var key = string.Join("|", data.ColKeys.Select(ck =>
                data.Lookup.TryGetValue((d, ts, ck.CourtId), out var mm)
                    ? $"{mm.T1Letter},{mm.T2Letter}" : "X"));
            showLtrs[ri] = key != prevKey;
            prevKey = key;
        }

        // ── Compute row heights: expand to fill available vertical space ───────
        // Scale horizontally to fill page width, then stretch rows to fill the remainder.
        const float TitleH = 26f;    // title + subtitle share one line
        float hdrH    = Hdr0H + Hdr1H + 2f;
        float fixedH  = TitleH + hdrH + 4f;
        float dataH   = showLtrs.Sum(sl => sl ? (float)RowH : SlimRowH);
        float scW     = bounds.Width / pGridW;
        float availDataH = bounds.Height / scW - fixedH;

        float rh_letter, rh_slim;
        if (availDataH > dataH && dataH > 0)
        {
            // Distribute all remaining height proportionally, keeping slim:letter ratio constant
            float ratio = SlimRowH / RowH;
            int   nLtr  = showLtrs.Count(b => b);
            int   nSlim = showLtrs.Length - nLtr;
            rh_letter = availDataH / (nLtr + nSlim * ratio);
            rh_slim   = rh_letter * ratio;
        }
        else
        {
            rh_letter = RowH;
            rh_slim   = SlimRowH;
        }

        // Internal row measurements scaled to new row height
        float rh_mult   = rh_letter / RowH;
        float act_LttrH = LttrH * rh_mult;
        float act_LttrY = LttrY * rh_mult;
        float act_BoxY  = BoxY  * rh_mult;
        float act_BoxH  = BoxH  * rh_mult;

        float actualGridH = fixedH + showLtrs.Sum(sl => sl ? rh_letter : rh_slim);
        float sc = Math.Min(bounds.Width / pGridW, bounds.Height / actualGridH);

        var state = g.Save();
        g.TranslateTransform(bounds.Left, bounds.Top);
        g.ScaleTransform(sc, sc);

        using var titleFont = new Font("Segoe UI", 12f,   FontStyle.Bold);
        using var hdrFont   = new Font("Segoe UI",  7.5f, FontStyle.Bold);
        using var subFnt    = new Font("Segoe UI",  7.5f, FontStyle.Regular);
        using var dayFnt    = new Font("Segoe UI", 10.5f, FontStyle.Bold);
        using var timeFnt   = new Font("Segoe UI", 10.5f, FontStyle.Bold);
        using var lttrFnt   = new Font("Segoe UI",  7f,   FontStyle.Bold);
        using var scoreFnt  = new Font("Segoe UI", 10f,   FontStyle.Bold);

        using var blackBr  = new SolidBrush(Color.Black);
        using var dimBr    = new SolidBrush(Color.FromArgb(80, 80, 80));
        using var whiteBr  = new SolidBrush(Color.White);
        using var hdrBgBr  = new SolidBrush(HdrBg);
        using var lttrBgBr = new SolidBrush(LttrBg);
        using var lttrFgBr = new SolidBrush(LttrFg);
        using var evenBr   = new SolidBrush(RowEven);
        using var oddBr    = new SolidBrush(RowOdd);
        using var thinPen  = new Pen(Color.Black, 0.5f);
        using var sfCtr    = new StringFormat
            { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        using var sfLft    = new StringFormat
            { Alignment = StringAlignment.Near,   LineAlignment = StringAlignment.Center };
        using var sfRgt    = new StringFormat
            { Alignment = StringAlignment.Far,    LineAlignment = StringAlignment.Center };

        float y = 0f;

        // Title + subtitle on the same line: title left, Club | League | Season right
        g.DrawString($"Week {week}  —  Score Sheet", titleFont, blackBr,
            new RectangleF(Px, y, pGridW / 2f, TitleH), sfLft);
        var subParts = new[] { data.Header.ClubName, data.Header.LeagueName, data.Header.SeasonName }
            .Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
        if (subParts.Length > 0)
            g.DrawString(string.Join("  |  ", subParts), subFnt, dimBr,
                new RectangleF(0, y, pGridW - Px, TitleH), sfRgt);
        y += TitleH;

        // Court / game column headers
        g.FillRectangle(hdrBgBr, Px, y, pDayW + pTimeW - 4, hdrH);
        g.DrawString("Day / Time", hdrFont, whiteBr,
            new RectangleF(Px, y, pDayW + pTimeW - 4, hdrH), sfCtr);

        for (int ci = 0; ci < colCount; ci++)
        {
            int cx  = PrtX(ci);
            int g1x = PrtBxX(ci, 1, 1);
            int g2x = PrtBxX(ci, 2, 1);

            g.FillRectangle(hdrBgBr, cx, y, CourtW, Hdr0H);
            g.DrawString(data.ColKeys[ci].CourtLabel, hdrFont, whiteBr,
                new RectangleF(cx, y, CourtW, Hdr0H), sfCtr);

            g.FillRectangle(hdrBgBr, g1x, y + Hdr0H, BoxW * 2, Hdr1H);
            g.DrawString("Game 1", hdrFont, whiteBr,
                new RectangleF(g1x, y + Hdr0H, BoxW * 2, Hdr1H), sfCtr);

            g.FillRectangle(hdrBgBr, g2x, y + Hdr0H, BoxW * 2, Hdr1H);
            g.DrawString("Game 2", hdrFont, whiteBr,
                new RectangleF(g2x, y + Hdr0H, BoxW * 2, Hdr1H), sfCtr);
        }
        y += hdrH;

        // Data rows — variable height, expanded to fill page
        string? lastDay = null;
        float   cumY    = y;

        for (int ri = 0; ri < data.RowKeys.Count; ri++)
        {
            var (date, dayName, timeStr, _) = data.RowKeys[ri];
            bool showLtr = showLtrs[ri];
            float rh     = showLtr ? rh_letter : rh_slim;
            float ry     = cumY;
            cumY += rh;

            bool newDay = dayName != lastDay;
            lastDay = dayName;

            g.FillRectangle(ri % 2 == 0 ? evenBr : oddBr, 0, ry, pGridW, rh - 2);

            if (newDay)
                g.DrawString(dayName, dayFnt, blackBr,
                    new RectangleF(Px + 2, ry, pDayW - 2, rh - 2), sfLft);

            g.DrawString(timeStr.Length > 0 ? timeStr : "—", timeFnt, dimBr,
                new RectangleF(Px + pDayW, ry, pTimeW, rh - 2), sfCtr);

            for (int ci = 0; ci < colCount; ci++)
            {
                var (courtId, _, _) = data.ColKeys[ci];
                int cx = PrtX(ci);

                if (!data.Lookup.TryGetValue((date, timeStr, courtId), out var m))
                {
                    using var hatchBr = new System.Drawing.Drawing2D.HatchBrush(
                        System.Drawing.Drawing2D.HatchStyle.ForwardDiagonal, HatchFg, HatchBg);
                    g.FillRectangle(hatchBr, cx, ry, CourtW, rh - 2);
                    continue;
                }

                var sd  = m.SD;
                // Score box y: below letters when shown, else centred in slim row
                float bry = ry + (showLtr ? act_BoxY : (rh_slim - act_BoxH) / 2f);

                for (int gm = 1; gm <= 2; gm++)
                {
                    int lx1 = PrtBxX(ci, gm, 1);
                    int lx2 = PrtBxX(ci, gm, 2);

                    if (showLtr)
                    {
                        float lry = ry + act_LttrY;
                        g.FillRectangle(lttrBgBr, lx1, lry, BoxW, act_LttrH);
                        g.DrawString(m.T1Letter, lttrFnt, lttrFgBr,
                            new RectangleF(lx1, lry, BoxW, act_LttrH), sfCtr);
                        g.FillRectangle(lttrBgBr, lx2, lry, BoxW, act_LttrH);
                        g.DrawString(m.T2Letter, lttrFnt, lttrFgBr,
                            new RectangleF(lx2, lry, BoxW, act_LttrH), sfCtr);
                    }

                    string txt1 = getScore(sd.Id, gm, 1);
                    string txt2 = getScore(sd.Id, gm, 2);

                    g.DrawRectangle(thinPen, (float)lx1, bry, (float)(BoxW - 1), act_BoxH - 1);
                    if (txt1.Length > 0)
                        g.DrawString(txt1, scoreFnt, blackBr,
                            new RectangleF(lx1, bry, BoxW, act_BoxH), sfCtr);

                    g.DrawRectangle(thinPen, (float)lx2, bry, (float)(BoxW - 1), act_BoxH - 1);
                    if (txt2.Length > 0)
                        g.DrawString(txt2, scoreFnt, blackBr,
                            new RectangleF(lx2, bry, BoxW, act_BoxH), sfCtr);
                }
            }
        }

        g.Restore(state);
    }

    // ── Control Factories ─────────────────────────────────────────────────────

    private static TextBox SBox(int value, int x, int y, int w, int h) => new()
    {
        Text        = value == 0 ? "" : value.ToString(),
        TextAlign   = HorizontalAlignment.Center,
        Font        = s_numFont,
        Location    = new Point(x, y),
        Size        = new Size(w, h),
        BorderStyle = BorderStyle.FixedSingle,
        MaxLength   = 3,
        BackColor   = Color.White
    };

    private static Label Hdr(string text, int x, int y, int w, int h) => new()
    {
        Text = text, Font = s_hdrFont,
        ForeColor = Color.White, BackColor = HdrBg,
        TextAlign = ContentAlignment.MiddleCenter,
        Location = new Point(x, y), Size = new Size(w, h)
    };

    private static Label TLetter(string letter, int x, int y, int w) => new()
    {
        Text = letter, Font = s_lttrFont,
        ForeColor = LttrFg, BackColor = LttrBg,
        TextAlign = ContentAlignment.MiddleCenter,
        Location = new Point(x, y), Size = new Size(w, LttrH)
    };

    private static Label Lbl(string text, int x, int y,
        Color fore, Font font, int w, int h,
        ContentAlignment align = ContentAlignment.MiddleLeft) => new()
    {
        Text = text, Font = font,
        ForeColor = fore, BackColor = Color.Transparent,
        TextAlign = align,
        Location = new Point(x, y), Size = new Size(w, h)
    };

    private void SetStatus(string msg) => _lblStatus.Text = msg;
}

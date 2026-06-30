using System.Drawing.Printing;
using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Services;

public static class StandingsPrintService
{
    // ── Layout constants (hundredths-of-inch / GDI units) ─────────────────────

    private const float Margin    = 25f;
    private const float DocHdrH   = 36f;
    private const float SlotHdrH  = 22f;
    private const float DivHdrH   = 20f;
    private const float ColHdrH   = 20f;
    private const float RowH      = 18f;
    private const float CellGapH  = 6f;    // vertical gap between division tables within a column
    private const float ColGapW   = 10f;   // horizontal gap between timeslot columns
    private const float FooterH   = 16f;
    private const float PadX      = 4f;

    // ── Data model ────────────────────────────────────────────────────────────

    private record DivBlock(string DivName, List<StandingView> Rows);

    private record Layout(
        string DocHeader,
        List<(int Id, string Label, int Sort)>  TimeCols,
        // Per timeslot column: ordered list of divisions (day order, packed — no gaps)
        List<List<DivBlock>> ColumnDivs,
        // Season seed
        List<(StandingView Row, string ShortDivName)> SeedRows,
        int  TeamsInPlayoffs,
        bool IsGamesMode,
        bool H2hUsed,
        string[] StatHeaders,
        float[]  StatWidths,
        float[]  TimeColTeamW);   // per timeslot column

    // ── Entry point ───────────────────────────────────────────────────────────

    public static void ShowPrintPreview(Control parent, int seasonId,
        StandingsPrintMode mode = StandingsPrintMode.AllDivisions,
        IEnumerable<int>? divisionFilter = null)
    {
        var doc = BuildDocument(seasonId, mode, divisionFilter);
        PrintPreviewService.ShowPrintPreview(parent, doc);
    }

    // ── Data loading ──────────────────────────────────────────────────────────

    private static PrintDocument BuildDocument(int seasonId,
        StandingsPrintMode mode, IEnumerable<int>? divisionFilter)
    {
        using var db = new BocceDbContext();
        var season = db.Seasons.Include(s => s.League).First(s => s.Id == seasonId);

        var allRows = db.Standings
            .Where(s => s.SeasonId == seasonId)
            .OrderBy(s => s.DivisionId).ThenBy(s => s.DivisionRank).ThenBy(s => s.DivisionSeed)
            .ToList();

        bool isGamesMode = season.ScoringMode == "games_mode";
        bool h2hUsed     = allRows.Any(r => r.H2HPlusMinus != 0 || r.H2HWins != 0);

        var clubName  = AppParameterService.GetAppParameter(db, "ClubName") ?? "Bocce League";
        string docHdr = $"{clubName}  —  {season.League.Name}  —  {season.Name}  —  Standings";

        var divIds = allRows.Select(r => r.DivisionId).Distinct().ToList();
        var divisions = db.Divisions
            .Include(d => d.DaySlot)
            .Include(d => d.TimeSlot)
            .Where(d => divIds.Contains(d.Id))
            .ToList();

        var timeCols = divisions
            .Where(d => d.TimeSlotId.HasValue && d.TimeSlot != null)
            .Select(d => (d.TimeSlotId!.Value, d.TimeSlot!.Timeslot12h, d.TimeSlot!.SortOrder ?? 999))
            .Distinct().OrderBy(t => t.Item3)
            .Select(t => (Id: t.Item1, Label: t.Item2, Sort: t.Item3)).ToList();

        var filterSet = divisionFilter?.ToHashSet();

        // Per-timeslot column: packed ordered list of visible divisions
        var columnDivs = timeCols.Select(tc =>
            divisions
                .Where(d => d.TimeSlotId == tc.Id && d.DaySlotId.HasValue && d.DaySlot != null)
                .Where(d => filterSet == null || filterSet.Contains(d.Id))
                .OrderBy(d => d.DaySlot!.DayNbr)
                .Select(d => new DivBlock(d.Name, allRows.Where(r => r.DivisionId == d.Id).ToList()))
                .ToList()
        ).ToList();

        // Short division name "Mon 9:00 AM" for seed page (no comma)
        var shortNames = divisions.ToDictionary(d => d.Id,
            d => d.DaySlot != null && d.TimeSlot != null
                ? $"{ToTitle(d.DaySlot.DayAbbr)} {d.TimeSlot.Timeslot12h}"
                : d.Name);

        var seedRows = allRows.OrderBy(r => r.SeasonSeed)
            .Select(r => (Row: r, Short: shortNames.TryGetValue(r.DivisionId, out var sn) ? sn : "?"))
            .ToList();

        string[] statHdrs = BuildStatHeaders(isGamesMode, h2hUsed);

        float[] statWidths, tcTeamW;
        using (var bmp = new Bitmap(1, 1))
        using (var g   = Graphics.FromImage(bmp))
        {
            var hf = new Font("Consolas", 8.5f, FontStyle.Bold);
            var df = new Font("Consolas", 8.5f);
            try
            {
                float W(string s, Font f) => g.MeasureString(s, f).Width + PadX * 2;

                statWidths = statHdrs.Select(h =>
                {
                    float hw = W(h, hf);
                    float dw = h switch
                    {
                        "Seed" => W("99", df),
                        _ when h.Contains('+') || h.Contains('-') => W("+999", df),
                        _ => W("999", df)
                    };
                    return MathF.Max(hw, dw);
                }).ToArray();

                tcTeamW = timeCols.Select((tc, ci) =>
                {
                    var divs = ci < columnDivs.Count ? columnDivs[ci] : [];
                    float max = divs
                        .SelectMany(d => d.Rows.Select(r => W(r.TeamName, df)))
                        .DefaultIfEmpty(0f).Max();
                    return MathF.Max(W("Team", hf), max);
                }).ToArray();
            }
            finally { hf.Dispose(); df.Dispose(); }
        }

        var lay = new Layout(docHdr, timeCols, columnDivs, seedRows,
            season.TeamsInPlayoffs, isGamesMode, h2hUsed, statHdrs, statWidths, tcTeamW);

        return CreatePrintDoc(lay, mode);
    }

    // "Seed" (SeasonSeed) appears first so readers see overall playoff position at a glance
    private static string[] BuildStatHeaders(bool isGamesMode, bool h2hUsed)
    {
        var h = new List<string> { "Seed", isGamesMode ? "GP" : "MP", "W" };
        if (!isGamesMode) h.Add("T");
        h.AddRange(["L", "F", "Pts", "+/-"]);
        if (h2hUsed) h.AddRange(["H2H+/-", "H2HW"]);
        return h.ToArray();
    }

    // ── Print document ────────────────────────────────────────────────────────

    private static PrintDocument CreatePrintDoc(Layout lay, StandingsPrintMode mode)
    {
        int  pageNum  = 0;
        // Each timeslot column independently tracks how far through its division list it is
        int  slotIdx  = 0;   // shared "slot" index — all columns advance together
        int  maxSlots = lay.ColumnDivs.Select(c => c.Count).DefaultIfEmpty(0).Max();
        bool seedDone = false;

        var doc = new PrintDocument { DocumentName = "Standings" };
        doc.QueryPageSettings += (_, qe) =>
        {
            qe.PageSettings.Landscape = true;
            qe.PageSettings.Margins   = new Margins((int)Margin, (int)Margin, (int)Margin, (int)Margin);
        };
        doc.BeginPrint += (_, _) =>
        {
            pageNum  = 0;
            slotIdx  = 0;
            seedDone = false;
        };

        doc.PrintPage += (_, pe) =>
        {
            if (pe.Graphics == null) return;
            pageNum++;
            var g    = pe.Graphics;
            var b    = pe.MarginBounds;
            float yMax = b.Bottom - FooterH;

            using var hf     = new Font("Consolas", 8.5f, FontStyle.Bold);
            using var df     = new Font("Consolas", 8.5f);
            using var bigF   = new Font("Consolas", 11f, FontStyle.Bold);
            using var darkBr = new SolidBrush(AppTheme.NavHeader);
            using var hdrBr  = new SolidBrush(AppTheme.GridHeaderBackground);
            using var dimBr  = new SolidBrush(Color.FromArgb(235, 240, 248));
            using var linePen= new Pen(AppTheme.GridLines);
            using var divPen = new Pen(Color.FromArgb(160, 160, 160));
            using var csf    = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using var lsf    = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };

            bool printDivs = mode != StandingsPrintMode.SeasonSeed;
            bool printSeed = mode != StandingsPrintMode.AllDivisions;

            if (printDivs && slotIdx < maxSlots)
            {
                // ── Division grid pages (packed, no day-aligned gaps) ──────────
                float y = b.Top;

                if (pageNum == 1)
                {
                    g.FillRectangle(darkBr, b.Left, y, b.Width, DocHdrH);
                    g.DrawString(lay.DocHeader, bigF, Brushes.White,
                        new RectangleF(b.Left, y, b.Width, DocHdrH), csf);
                    y += DocHdrH;
                }

                var (colXs, colWs) = ComputeColPositions(lay, b.Left, b.Width);
                DrawTimeslotHeaders(g, colXs, colWs, y, lay, hf, hdrBr, linePen, csf);
                y += SlotHdrH;

                pe.HasMorePages = false;
                while (slotIdx < maxSlots)
                {
                    // Height of this slot = max div table height across all columns at this slot index
                    float slotH = lay.ColumnDivs
                        .Select(col => slotIdx < col.Count ? DivHdrH + ColHdrH + col[slotIdx].Rows.Count * RowH : 0f)
                        .DefaultIfEmpty(0f).Max();

                    if (y + slotH > yMax) { pe.HasMorePages = true; goto PageDone; }

                    DrawSlot(g, colXs, colWs, y, lay, slotIdx, hf, df, hdrBr, dimBr, linePen, divPen, lsf, csf);
                    y += slotH + CellGapH;
                    slotIdx++;
                }

                if (printSeed && !seedDone) pe.HasMorePages = true;
            }
            else if (printSeed && !seedDone)
            {
                // ── Season seed page ──────────────────────────────────────────
                DrawSeedPage(g, b, lay, hf, df, bigF, darkBr, hdrBr, dimBr, linePen, csf, lsf);
                seedDone = true;
                pe.HasMorePages = false;
            }

            PageDone:
            var fs = g.MeasureString($"Page {pageNum}", df);
            g.DrawString($"Page {pageNum}", df, Brushes.Gray, b.Right - fs.Width, b.Bottom - fs.Height);
        };

        return doc;
    }

    // ── Column-position helpers ───────────────────────────────────────────────

    private static (float[] xs, float[] ws) ComputeColPositions(Layout lay, float left, float pageW)
    {
        int     n      = lay.TimeCols.Count;
        float   statsW = lay.StatWidths.Sum();
        float[] ws     = lay.TimeColTeamW.Select(tw => tw + statsW).ToArray();
        float   total  = ws.Sum() + (n - 1) * ColGapW;

        if (total > pageW)
        {
            float scale = pageW / total;
            ws = ws.Select(w => w * scale).ToArray();
        }

        float[] xs = new float[n];
        float   x  = left;
        for (int i = 0; i < n; i++) { xs[i] = x; x += ws[i] + ColGapW; }
        return (xs, ws);
    }

    // ── Division grid drawing ─────────────────────────────────────────────────

    private static void DrawTimeslotHeaders(Graphics g, float[] xs, float[] ws, float y,
        Layout lay, Font hf, SolidBrush hdrBr, Pen linePen, StringFormat csf)
    {
        for (int i = 0; i < lay.TimeCols.Count; i++)
        {
            g.FillRectangle(hdrBr, xs[i], y, ws[i], SlotHdrH);
            g.DrawString(lay.TimeCols[i].Label, hf, Brushes.White,
                new RectangleF(xs[i], y, ws[i], SlotHdrH), csf);
            g.DrawRectangle(linePen, xs[i], y, ws[i] - 1, SlotHdrH - 1);
        }
    }

    // Draw one "slot" — the next division from each timeslot column, side by side
    private static void DrawSlot(Graphics g, float[] xs, float[] ws, float y, Layout lay,
        int slotIdx, Font hf, Font df, SolidBrush hdrBr, SolidBrush dimBr,
        Pen linePen, Pen divPen, StringFormat lsf, StringFormat csf)
    {
        for (int ci = 0; ci < lay.TimeCols.Count; ci++)
        {
            if (slotIdx >= lay.ColumnDivs[ci].Count) continue;
            DrawDivTable(g, xs[ci], y, ws[ci], lay.TimeColTeamW[ci],
                lay.ColumnDivs[ci][slotIdx], lay, hf, df, hdrBr, dimBr, linePen, lsf, csf);
        }
    }

    private static void DrawDivTable(Graphics g, float x, float y, float colW, float teamW,
        DivBlock blk, Layout lay, Font hf, Font df,
        SolidBrush hdrBr, SolidBrush dimBr, Pen linePen,
        StringFormat lsf, StringFormat csf)
    {
        float yStart = y;
        float[] sw   = lay.StatWidths;

        using var divHdrBr = new SolidBrush(Color.FromArgb(100, 130, 170));
        g.FillRectangle(divHdrBr, x, y, colW, DivHdrH);
        // Division name only — it already contains day info
        g.DrawString(blk.DivName, hf, Brushes.White,
            new RectangleF(x + PadX, y, colW - PadX * 2, DivHdrH), lsf);
        y += DivHdrH;

        g.FillRectangle(hdrBr, x, y, colW, ColHdrH);
        g.DrawString("Team", hf, Brushes.White, new RectangleF(x + PadX, y, teamW - PadX * 2, ColHdrH), lsf);
        float sx = x + teamW;
        for (int i = 0; i < lay.StatHeaders.Length; i++)
        {
            g.DrawString(lay.StatHeaders[i], hf, Brushes.White, new RectangleF(sx, y, sw[i], ColHdrH), csf);
            sx += sw[i];
        }
        y += ColHdrH;

        for (int ri = 0; ri < blk.Rows.Count; ri++)
        {
            var r = blk.Rows[ri];
            if (ri % 2 == 1) g.FillRectangle(dimBr, x, y, colW, RowH);

            using var rowF = new Font("Consolas", 8.5f, r.DivisionRank == 1 ? FontStyle.Bold : FontStyle.Regular);
            g.DrawString(r.TeamName, rowF, Brushes.Black,
                new RectangleF(x + PadX, y, teamW - PadX * 2, RowH), lsf);

            var vals = GetStatValues(r, lay.IsGamesMode, lay.H2hUsed);
            sx = x + teamW;
            for (int i = 0; i < vals.Length; i++)
            {
                g.DrawString(vals[i], rowF, Brushes.Black, new RectangleF(sx, y, sw[i], RowH), csf);
                sx += sw[i];
            }
            y += RowH;
        }

        g.DrawRectangle(linePen, x, yStart, colW - 1, y - yStart - 1);
    }

    private static string[] GetStatValues(StandingView r, bool isGamesMode, bool h2hUsed)
    {
        var v = new List<string>
        {
            r.SeasonSeed.ToString(),
            (isGamesMode ? r.GamesPlayed : r.MatchesPlayed).ToString(),
            r.Wins.ToString()
        };
        if (!isGamesMode) v.Add(r.Ties.ToString());
        v.Add(r.Losses.ToString()); v.Add(r.Forfeits.ToString());
        v.Add(r.StandingsPoints.ToString()); v.Add(PmStr(r.PlusMinus));
        if (h2hUsed) { v.Add(PmStr(r.H2HPlusMinus)); v.Add(r.H2HWins.ToString()); }
        return v.ToArray();
    }

    // ── Season seed page ──────────────────────────────────────────────────────

    private static void DrawSeedPage(Graphics g, Rectangle b, Layout lay,
        Font hf, Font df, Font bigF,
        SolidBrush darkBr, SolidBrush hdrBr, SolidBrush dimBr, Pen linePen,
        StringFormat csf, StringFormat lsf)
    {
        float y = b.Top;
        g.FillRectangle(darkBr, b.Left, y, b.Width, DocHdrH);
        g.DrawString("Season Standings — Playoff Seeding Order", bigF, Brushes.White,
            new RectangleF(b.Left, y, b.Width, DocHdrH), csf);
        y += DocHdrH;

        float halfW  = (b.Width - ColGapW) / 2f;
        float leftX  = b.Left;
        float rightX = b.Left + halfW + ColGapW;

        int total = lay.SeedRows.Count;
        int leftN = (total + 1) / 2;

        string[] hdrs = BuildSeedHeaders(lay.IsGamesMode);

        // Measure Team and Division widths using the actual print Graphics — avoids DPI mismatch
        float W(string s, Font f) => g.MeasureString(s, f).Width + PadX * 2;
        float seedTeamW = MathF.Max(W("Team", hf),
            lay.SeedRows.Select(sr => W(sr.Row.TeamName, df)).DefaultIfEmpty(0f).Max());
        float seedDivW  = MathF.Max(W("Division", hf),
            lay.SeedRows.Select(sr => W(sr.ShortDivName, df)).DefaultIfEmpty(0f).Max());

        float[] colWs = MeasureSeedCols(hdrs, seedTeamW, seedDivW, halfW, g, hf, df);

        DrawSeedHalf(g, leftX,  halfW, y, hdrs, colWs, lay, lay.SeedRows.Take(leftN).ToList(), hf, df, hdrBr, dimBr, linePen, csf, lsf);
        DrawSeedHalf(g, rightX, halfW, y, hdrs, colWs, lay, lay.SeedRows.Skip(leftN).ToList(), hf, df, hdrBr, dimBr, linePen, csf, lsf);
    }

    private static string[] BuildSeedHeaders(bool isGamesMode)
    {
        var h = new List<string> { "#", "Team", "Division", "Pts", "+/-", "W" };
        if (!isGamesMode) h.Add("T");
        h.AddRange(["L", "F"]);
        return h.ToArray();
    }

    private static float[] MeasureSeedCols(string[] hdrs, float teamW, float divW,
        float halfW, Graphics g, Font hf, Font df)
    {
        float W(string s, Font f) => g.MeasureString(s, f).Width + PadX * 2;

        float[] ws = hdrs.Select(h => h switch
        {
            "Team"     => teamW,
            "Division" => divW,
            _ => MathF.Max(W(h, hf), W(h.Contains('+') || h.Contains('-') ? "+999" : "999", df))
        }).ToArray();

        float total = ws.Sum();
        if (total > halfW)
        {
            float fixedW = total - teamW - divW;
            float avail  = halfW - fixedW;
            if (avail > 0)
            {
                float ratio = avail / (teamW + divW);
                for (int i = 0; i < hdrs.Length; i++)
                    if (hdrs[i] is "Team" or "Division") ws[i] *= ratio;
            }
        }
        return ws;
    }

    private static void DrawSeedHalf(Graphics g, float x, float w, float y,
        string[] hdrs, float[] colWs, Layout lay,
        List<(StandingView Row, string ShortDivName)> rows,
        Font hf, Font df, SolidBrush hdrBr, SolidBrush dimBr, Pen linePen,
        StringFormat csf, StringFormat lsf)
    {
        float yStart = y;

        g.FillRectangle(hdrBr, x, y, w, ColHdrH);
        float cx = x;
        for (int i = 0; i < hdrs.Length; i++)
        {
            var sf = hdrs[i] is "Team" or "Division" ? lsf : csf;
            g.DrawString(hdrs[i], hf, Brushes.White,
                new RectangleF(cx + PadX, y, colWs[i] - PadX * 2, ColHdrH), sf);
            cx += colWs[i];
        }
        y += ColHdrH;

        for (int ri = 0; ri < rows.Count; ri++)
        {
            var (r, shortDiv) = rows[ri];
            if (ri % 2 == 1) g.FillRectangle(dimBr, x, y, w, RowH);

            bool qualifies = lay.TeamsInPlayoffs > 0 && r.SeasonSeed <= lay.TeamsInPlayoffs;
            using var rowF = new Font("Consolas", 8.5f, qualifies ? FontStyle.Bold : FontStyle.Regular);

            cx = x;
            var vals = GetSeedValues(r, shortDiv, lay.IsGamesMode);
            for (int i = 0; i < hdrs.Length; i++)
            {
                var sf = hdrs[i] is "Team" or "Division" ? lsf : csf;
                g.DrawString(vals[i], rowF, Brushes.Black,
                    new RectangleF(cx + PadX, y, colWs[i] - PadX * 2, RowH), sf);
                cx += colWs[i];
            }

            if (lay.TeamsInPlayoffs > 0 && r.SeasonSeed == lay.TeamsInPlayoffs)
                using (var cut = new Pen(Color.Gray, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                    g.DrawLine(cut, x, y + RowH, x + w, y + RowH);

            y += RowH;
        }

        g.DrawRectangle(linePen, x, yStart, w - 1, y - yStart - 1);
    }

    private static string[] GetSeedValues(StandingView r, string shortDivName, bool isGamesMode)
    {
        var v = new List<string>
            { r.SeasonSeed.ToString(), r.TeamName, shortDivName,
              r.StandingsPoints.ToString(), PmStr(r.PlusMinus), r.Wins.ToString() };
        if (!isGamesMode) v.Add(r.Ties.ToString());
        v.Add(r.Losses.ToString()); v.Add(r.Forfeits.ToString());
        return v.ToArray();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string PmStr(int v) => v >= 0 ? $"+{v}" : $"{v}";

    private static string ToTitle(string? s) =>
        string.IsNullOrEmpty(s) ? "" : char.ToUpper(s[0]) + s.Substring(1).ToLower();
}

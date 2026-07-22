using System.Drawing;
using System.Drawing.Printing;
using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.UI.Theme;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Services;

public static class TeamsPrintService
{
    // ── Data structures ───────────────────────────────────────────────────────

    public enum ListingMode { Compact, FullRoster }

    public record PrintSection(string DocHeader, string TimeSlotLabel, List<DaySection> DaySections);
    public record DaySection(string DayLabel, string TimeSlot, List<TeamRow> Teams);
    public record TeamRow(string TeamIdentifier, string CaptainName, string AllOtherPlayers, string Phone, IReadOnlyList<string> OtherPlayersList);

    // ── Layout constants ──────────────────────────────────────────────────────

    private const float ColTeamW      = 100f;  // Wider for team names
    private const float ColCaptainW   = 120f;
    private const float ColPhoneW     = 138f;
    private const float ColPlayerMinW = 70f;   // Minimum width per player column in Full Roster mode
    private const float DayGapH       = 12f;   // 1/8 inch gap between days


    private const float DocHdrH     = 48f;  // Increased to prevent cutoff
    private const float TimeSlotH   = 28f;
    private const float ColHdrH     = 24f;
    private const float DataRowH    = 18f;
    private const float DayHdrH     = 20f;
    private const float FooterH     = 18f;

    // ── Day order ──────────────────────────────────────────────────────────────

    private static readonly string[] DayOrder = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];

    private static int DayIndex(string day) => Array.IndexOf(DayOrder, day);

    // ── Build sections from DB ────────────────────────────────────────────────

    public static List<PrintSection> BuildSections(int seasonId)
    {
        using var db = new BocceDbContext();

        var season = db.Seasons
            .Include(s => s.League)
            .FirstOrDefault(s => s.Id == seasonId);

        if (season == null) throw new InvalidOperationException($"Season {seasonId} not found.");
        if (season.League == null) throw new InvalidOperationException($"Season {seasonId} has no league.");

        var clubName = AppParameterService.GetAppParameter(db, "ClubName") ?? "Bocce League";
        string docHeader = $"{clubName}  –  {season.League.Name}  –  {season.Name} - Team Listing";

        // Load divisions separately to avoid cartesian product duplication
        var divisions = db.Divisions
            .Where(d => d.SeasonId == seasonId)
            .Include(d => d.TimeSlot)
            .Include(d => d.DaySlot)
            .AsNoTracking()
            .ToList();

        if (divisions.Count == 0)
            return [];

        // Load teams separately
        var divisionIds = divisions.Select(d => d.Id).ToList();
        var teams = db.Teams
            .Where(t => divisionIds.Contains(t.DivisionId))
            .AsNoTracking()
            .ToList();

        if (teams.Count == 0)
            return [];

        // Load team players separately
        var teamIds = teams.Select(t => t.Id).ToList();
        var teamPlayers = db.TeamPlayers
            .Where(tp => teamIds.Contains(tp.TeamId))
            .Include(tp => tp.Player)
            .AsNoTracking()
            .ToList();

        // Rebuild the object graph manually to avoid duplication
        var divisionsWithTeams = divisions
            .Where(d => d.TimeSlot != null)
            .Select(d => new
            {
                Division = d,
                Teams = teams
                    .Where(t => t.DivisionId == d.Id)
                    .Select(t => new
                    {
                        Team = t,
                        Players = teamPlayers.Where(tp => tp.TeamId == t.Id).ToList()
                    })
                    .ToList()
            })
            .Where(x => x.Teams.Count > 0)
            .ToList();

        if (divisionsWithTeams.Count == 0)
            return [];

        // Group by TimeSlot
        var divisionsByTimeSlot = divisionsWithTeams
            .GroupBy(x => new { x.Division.TimeSlot!.Timeslot12h, TimeSlotOrder = x.Division.TimeSlot.Timeslot24h })
            .OrderBy(g => g.Key.TimeSlotOrder)
            .ToList();

        var sections = new List<PrintSection>();

        foreach (var timeSlotGroup in divisionsByTimeSlot)
        {
            string timeSlotLabel = $"{GetTimeSlotLabel(timeSlotGroup.Key.Timeslot12h)} - {timeSlotGroup.Key.Timeslot12h}";

            // Group divisions by day within this time slot, ordered by day
            var divisionsByDay = timeSlotGroup
                .OrderBy(x => DayIndex(x.Division.DaySlot?.DayName ?? ""))
                .GroupBy(x => x.Division.DaySlot!.DayName)
                .OrderBy(g => DayIndex(g.Key))
                .ToList();

            var daySections = new List<DaySection>();

            foreach (var dayGroup in divisionsByDay)
            {
                var teamRows = new List<TeamRow>();

                foreach (var divData in dayGroup.Where(x => x.Division.DaySlot != null))
                {
                    foreach (var team in divData.Teams.OrderBy(t => t.Team.TeamLetter))
                    {
                        var playersForTeam = team.Players
                            .OrderBy(tp => tp.Player.LastName)
                            .ThenBy(tp => tp.Player.FirstName)
                            .ToList();

                        int? captainId = team.Team.CaptainPlayerId;
                        var captainTp  = captainId.HasValue
                            ? playersForTeam.FirstOrDefault(tp => tp.PlayerId == captainId)
                            : playersForTeam.FirstOrDefault(tp => tp.Role == "captain");

                        string captain = captainTp != null ? $"{captainTp.Player.FirstName} {captainTp.Player.LastName}".Trim() : "";
                        string phone = captainTp?.Player.Phone ?? "";

                        var otherPlayers = playersForTeam
                            .Where(tp => tp != captainTp)
                            .OrderBy(tp => tp.Player.LastName)
                            .ThenBy(tp => tp.Player.FirstName)
                            .Select(tp => $"{tp.Player.FirstName} {tp.Player.LastName}".Trim())
                            .ToList();

                        string players = string.Join(" | ", otherPlayers);
                        // Use DisplayName if available, otherwise fall back to SystemName
                        string teamName = !string.IsNullOrEmpty(team.Team.DisplayName)
                            ? team.Team.DisplayName
                            : team.Team.SystemName;
                        if (!team.Team.IsActive) teamName += " *";

                        teamRows.Add(new TeamRow(teamName, captain, players, phone, otherPlayers));
                    }
                }

                string dayLabel = dayGroup.Key;
                string timeSlot = timeSlotGroup.Key.Timeslot12h;
                daySections.Add(new DaySection(dayLabel, timeSlot, teamRows));
            }

            sections.Add(new PrintSection(docHeader, timeSlotLabel, daySections));
        }

        return sections;
    }

    private static string GetTimeSlotLabel(string slot12h)
    {
        // Parse "9:00 am" → hour 9 → "MORNINGS"
        // Parse "1:00 pm" or "3:30 pm" → "AFTERNOONS"
        if (string.IsNullOrEmpty(slot12h)) return "";
        if (slot12h.Contains("9:00")) return "MORNINGS";
        if (slot12h.Contains("1:00") || slot12h.Contains("3:30")) return "AFTERNOONS";
        return "";
    }


    // ── Build PrintDocument ───────────────────────────────────────────────────

    private record ColumnDef(string Header, float Width, Func<TeamRow, string> GetText, bool Wrap);

    private static int MaxOtherPlayers(List<PrintSection> sections) =>
        sections.SelectMany(s => s.DaySections).SelectMany(d => d.Teams)
            .Select(t => t.OtherPlayersList.Count).DefaultIfEmpty(0).Max();

    public static PrintDocument BuildDocument(List<PrintSection> sections, ListingMode mode = ListingMode.Compact)
    {
        int secIdx  = 0;
        int dayIdx  = 0;
        int rowIdx  = 0;
        int pageNum = 0;
        int maxOtherPlayers = mode == ListingMode.FullRoster ? MaxOtherPlayers(sections) : 0;

        var doc = new PrintDocument();
        doc.QueryPageSettings += (_, qe) =>
        {
            qe.PageSettings.Landscape = mode == ListingMode.FullRoster;  // Extra columns need the width
            qe.PageSettings.Margins   = new Margins(10, 15, 10, 15);  // Extra space top/bottom
        };
        doc.BeginPrint += (_, _) => { secIdx = 0; dayIdx = 0; rowIdx = 0; pageNum = 0; };

        doc.PrintPage += (_, pe) =>
        {
            if (pe.Graphics == null) return;
            pageNum++;
            var ps       = doc.PrinterSettings;
            int fromPage = ps.PrintRange == PrintRange.SomePages ? ps.FromPage : 1;
            int toPage   = ps.PrintRange == PrintRange.SomePages ? ps.ToPage   : int.MaxValue;
            bool draw    = pageNum >= fromPage && pageNum <= toPage;
            var g      = pe.Graphics;
            var b      = pe.MarginBounds;
            float y    = b.Top;
            float yMax = b.Bottom - FooterH;

            var docHdrFont   = AppTheme.FontNavHeader;  // Even smaller header font
            var timeSlotFont = AppTheme.FontNavHeader;
            var dayHdrFont   = AppTheme.FontNavHeader;
            var colHdrFont   = AppTheme.FontGridHeader;
            var dataFont     = AppTheme.FontDefault;
            var footerFont   = AppTheme.FontSmall;
            using var headerBrush  = new SolidBrush(AppTheme.NavHeader);
            using var lightBrush   = new SolidBrush(AppTheme.NavHover);
            using var textBrush    = new SolidBrush(Color.Black);  // Always black for readability
            using var hdrFill      = new SolidBrush(AppTheme.GridHeaderBackground);
            using var altFill      = new SolidBrush(AppTheme.GridAlternateRow);
            using var sepPen       = new Pen(AppTheme.GridLines);
            using var leftAlign    = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near };
            using var centerAlign  = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

            float lx = b.Left;
            float tableWidth = b.Width;

            // ── Column layout (per-mode) ────────────────────────────────────
            List<ColumnDef> columns;
            if (mode == ListingMode.Compact)
            {
                float colPlayersW = tableWidth - ColTeamW - ColCaptainW - ColPhoneW;
                columns =
                [
                    new("Team",      ColTeamW,      r => r.TeamIdentifier, false),
                    new("Captain",   ColCaptainW,    r => r.CaptainName,    false),
                    new("Players",   colPlayersW,    r => r.AllOtherPlayers, true),
                    new("Contact #", ColPhoneW,      r => r.Phone,          false),
                ];
            }
            else
            {
                float fixedW      = ColTeamW + ColCaptainW + ColPhoneW;
                float playerColW  = Math.Max(ColPlayerMinW, (tableWidth - fixedW) / Math.Max(1, maxOtherPlayers));
                columns =
                [
                    new("Team",    ColTeamW,    r => r.TeamIdentifier, false),
                    new("Captain", ColCaptainW, r => r.CaptainName,    false),
                ];
                for (int i = 0; i < maxOtherPlayers; i++)
                {
                    int idx = i;
                    columns.Add(new($"Player {i + 2}", playerColW,
                        r => idx < r.OtherPlayersList.Count ? r.OtherPlayersList[idx] : "", false));
                }
                columns.Add(new("Contact #", ColPhoneW, r => r.Phone, false));
            }

            // ── Draw helpers ──────────────────────────────────────────────────
            void DrawTimeSlotLabel(string text)
            {
                if (!draw) return;
                g.FillRectangle(headerBrush, b.Left, y, b.Width, TimeSlotH);
                var timeSlotRect = new RectangleF(b.Left, y, b.Width, TimeSlotH);
                g.DrawString(text, timeSlotFont, new SolidBrush(Color.White), timeSlotRect, centerAlign);
            }
            void DrawColHeader()
            {
                if (!draw) return;
                g.FillRectangle(hdrFill, lx, y, tableWidth, ColHdrH);
                float cx = lx;
                foreach (var col in columns)
                {
                    DrawColText(col.Header, cx, col.Width);
                    cx += col.Width;
                }
                g.DrawRectangle(Pens.Gray, lx, y, tableWidth - 1, ColHdrH - 1);

                void DrawColText(string text, float x, float w)
                {
                    g.DrawString(text, colHdrFont, new SolidBrush(Color.White), new RectangleF(x + 2, y + 2, w - 4, ColHdrH - 4), leftAlign);
                    g.DrawLine(sepPen, x + w, y, x + w, y + ColHdrH);
                }
            }
            void DrawDayLabel(string dayText, string timeSlot)
            {
                if (!draw) return;
                g.FillRectangle(lightBrush, b.Left, y, b.Width, DayHdrH);
                var dayRect = new RectangleF(b.Left, y, b.Width, DayHdrH);
                string fullLabel = $"{dayText} - {timeSlot}";
                g.DrawString(fullLabel, AppTheme.FontDefaultBold, new SolidBrush(Color.White), dayRect, centerAlign);
            }
            // Greedily pack names onto lines, breaking at name boundaries only.
            // Returns the wrapped text (lines joined with \n) and the required row height.
            (string text, float rowH) WrapPlayers(string players, float colWidth)
            {
                if (string.IsNullOrEmpty(players)) return ("", DataRowH);
                var names   = players.Split(" | ");
                var lines   = new List<string>();
                var current = new List<string>();
                foreach (var name in names)
                {
                    current.Add(name);
                    float w = g.MeasureString(string.Join(" | ", current), dataFont,
                                              PointF.Empty, StringFormat.GenericTypographic).Width;
                    if (w > colWidth - 4 && current.Count > 1)
                    {
                        current.RemoveAt(current.Count - 1);
                        lines.Add(string.Join(" | ", current));
                        current.Clear();
                        current.Add(name);
                    }
                }
                if (current.Count > 0) lines.Add(string.Join(" | ", current));
                string wrapped = string.Join("\n", lines);
                float  totalH  = Math.Max(DataRowH, lines.Count * dataFont.GetHeight(g) + 4);
                return (wrapped, totalH);
            }
            // Computes wrapped text (for wrapping columns) and the resulting row height.
            (Dictionary<int, string> wrapped, float rowH) ComputeRow(TeamRow r)
            {
                float rowH = DataRowH;
                var wrapped = new Dictionary<int, string>();
                for (int i = 0; i < columns.Count; i++)
                {
                    if (!columns[i].Wrap) continue;
                    var (text, h) = WrapPlayers(columns[i].GetText(r), columns[i].Width);
                    wrapped[i] = text;
                    rowH = Math.Max(rowH, h);
                }
                return (wrapped, rowH);
            }
            void DrawDataRow(TeamRow r, float rowH, Dictionary<int, string> wrapped)
            {
                if (!draw) return;
                // All white background, no alternating colors
                float cx = lx;
                for (int i = 0; i < columns.Count; i++)
                {
                    var col  = columns[i];
                    var text = wrapped.TryGetValue(i, out var w) ? w : col.GetText(r);
                    DrawCell(text, cx, col.Width);
                    cx += col.Width;
                }
                g.DrawLine(sepPen, lx, y + rowH, lx + tableWidth, y + rowH);

                void DrawCell(string text, float x, float w)
                {
                    if (!string.IsNullOrEmpty(text))
                        g.DrawString(text, dataFont, textBrush, new RectangleF(x + 2, y + 1, w - 4, rowH - 2), leftAlign);
                    g.DrawLine(sepPen, x + w, y, x + w, y + rowH);
                }
            }
            // ──────────────────────────────────────────────────────────────────

            // Document header — every page
            if (sections.Count > 0)
            {
                if (draw)
                {
                    g.FillRectangle(headerBrush, b.Left, y, b.Width, DocHdrH);
                    var hdrRect = new RectangleF(b.Left, y, b.Width, DocHdrH);
                    g.DrawString(sections[0].DocHeader, docHdrFont, new SolidBrush(Color.White), hdrRect, centerAlign);
                }
                y += DocHdrH;
            }

            pe.HasMorePages = false;

            if (secIdx < sections.Count)
            {
                var sec = sections[secIdx];

                // Start of section (dayIdx=0 && rowIdx=0): draw TimeSlot label + col header.
                // Require enough room for section header + col header + day header + 1 row
                // so we never draw empty headers at the bottom of a page.
                if (dayIdx == 0 && rowIdx == 0)
                {
                    float minH = TimeSlotH + ColHdrH + DayHdrH + DataRowH;
                    if (y + minH > yMax) { pe.HasMorePages = true; goto PageDone; }
                    DrawTimeSlotLabel(sec.TimeSlotLabel);
                    y += TimeSlotH;
                    DrawColHeader();
                    y += ColHdrH;
                }
                else
                {
                    // Continuation page within this section — redraw column header only
                    if (y + ColHdrH + DataRowH > yMax) { pe.HasMorePages = true; goto PageDone; }
                    DrawColHeader();
                    y += ColHdrH;
                }

                for (; dayIdx < sec.DaySections.Count; dayIdx++)
                {
                    var day = sec.DaySections[dayIdx];

                    if (rowIdx == 0)
                    {
                        // Add gap above day header
                        if (dayIdx > 0)
                            y += DayGapH;
                        // Orphan guard: require day header + at least 1 row
                        if (y + DayHdrH + DataRowH > yMax) { pe.HasMorePages = true; goto PageDone; }
                        DrawDayLabel(day.DayLabel, day.TimeSlot);
                        y += DayHdrH;
                    }

                    for (; rowIdx < day.Teams.Count; rowIdx++)
                    {
                        var (wrapped, rowH) = ComputeRow(day.Teams[rowIdx]);
                        if (y + rowH > yMax) { pe.HasMorePages = true; goto PageDone; }
                        DrawDataRow(day.Teams[rowIdx], rowH, wrapped);
                        y += rowH;
                    }

                    rowIdx = 0;
                }

                // Section fully rendered — advance and force a new page for the next section
                dayIdx = 0;
                secIdx++;

                if (secIdx < sections.Count)
                    pe.HasMorePages = true;
            }

            PageDone:
            if (ps.PrintRange == PrintRange.SomePages && pageNum >= toPage)
                pe.HasMorePages = false;
            if (draw)
            {
                string footer   = $"Page {pageNum}";
                var    footerSz = g.MeasureString(footer, footerFont);
                g.DrawString(footer, footerFont, Brushes.Gray,
                    b.Right - footerSz.Width, b.Bottom - footerSz.Height);
            }
        };

        return doc;
    }

    // ── Flat export data (CSV) ────────────────────────────────────────────────

    public static (string[] Headers, List<string[]> Rows) BuildFlatExportData(List<PrintSection> sections, ListingMode mode)
    {
        int maxOtherPlayers = mode == ListingMode.FullRoster ? MaxOtherPlayers(sections) : 0;

        string[] headers = mode == ListingMode.Compact
            ? ["Time Slot", "Day", "Team", "Captain", "Players", "Phone"]
            : ["Time Slot", "Day", "Team", "Captain", .. Enumerable.Range(2, maxOtherPlayers).Select(n => $"Player {n}"), "Phone"];

        var rows = new List<string[]>();
        foreach (var section in sections)
        {
            foreach (var day in section.DaySections)
            {
                foreach (var team in day.Teams)
                {
                    var row = new List<string> { section.TimeSlotLabel, day.DayLabel, team.TeamIdentifier, team.CaptainName };
                    if (mode == ListingMode.Compact)
                    {
                        row.Add(team.AllOtherPlayers);
                    }
                    else
                    {
                        for (int i = 0; i < maxOtherPlayers; i++)
                            row.Add(i < team.OtherPlayersList.Count ? team.OtherPlayersList[i] : "");
                    }
                    row.Add(team.Phone);
                    rows.Add(row.ToArray());
                }
            }
        }

        return (headers, rows);
    }

    // ── Excel export (mirrors the printed layout exactly) ────────────────────

    public static void ExportToExcel(Control parent, string filename, List<PrintSection> sections, ListingMode mode)
    {
        using var saveDlg = new SaveFileDialog
        {
            Title = "Export to Excel",
            Filter = "Excel files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
            FileName = $"{filename}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        if (saveDlg.ShowDialog(parent) != DialogResult.OK) return;

        try
        {
            int maxOtherPlayers = mode == ListingMode.FullRoster ? MaxOtherPlayers(sections) : 0;

            string[] columnHeaders = mode == ListingMode.Compact
                ? ["Team", "Captain", "Players", "Contact #"]
                : ["Team", "Captain", .. Enumerable.Range(2, maxOtherPlayers).Select(n => $"Player {n}"), "Contact #"];

            int colCount = columnHeaders.Length;

            var headerFill = XLColor.FromColor(AppTheme.NavHeader);
            var timeSlotFill = XLColor.FromColor(AppTheme.NavHeader);
            var dayFill = XLColor.FromColor(AppTheme.NavHover);
            var colHdrFill = XLColor.FromColor(AppTheme.GridHeaderBackground);

            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Team Listing");
            int r = 1;

            if (sections.Count > 0)
            {
                ws.Range(r, 1, r, colCount).Merge();
                var cell = ws.Cell(r, 1);
                cell.Value = sections[0].DocHeader;
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = headerFill;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Row(r).Height = 22;
                r += 2;  // blank spacer row
            }

            foreach (var section in sections)
            {
                ws.Range(r, 1, r, colCount).Merge();
                var tsCell = ws.Cell(r, 1);
                tsCell.Value = section.TimeSlotLabel;
                tsCell.Style.Font.Bold = true;
                tsCell.Style.Font.FontColor = XLColor.White;
                tsCell.Style.Fill.BackgroundColor = timeSlotFill;
                tsCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Row(r).Height = 20;
                r++;

                for (int c = 0; c < colCount; c++)
                {
                    var hc = ws.Cell(r, c + 1);
                    hc.Value = columnHeaders[c];
                    hc.Style.Font.Bold = true;
                    hc.Style.Font.FontColor = XLColor.White;
                    hc.Style.Fill.BackgroundColor = colHdrFill;
                }
                r++;

                foreach (var day in section.DaySections)
                {
                    ws.Range(r, 1, r, colCount).Merge();
                    var dCell = ws.Cell(r, 1);
                    dCell.Value = $"{day.DayLabel} - {day.TimeSlot}";
                    dCell.Style.Font.Bold = true;
                    dCell.Style.Font.FontColor = XLColor.White;
                    dCell.Style.Fill.BackgroundColor = dayFill;
                    dCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    r++;

                    foreach (var team in day.Teams)
                    {
                        int c = 1;
                        ws.Cell(r, c++).Value = team.TeamIdentifier;
                        ws.Cell(r, c++).Value = team.CaptainName;
                        if (mode == ListingMode.Compact)
                        {
                            ws.Cell(r, c++).Value = team.AllOtherPlayers;
                        }
                        else
                        {
                            for (int i = 0; i < maxOtherPlayers; i++)
                                ws.Cell(r, c++).Value = i < team.OtherPlayersList.Count ? team.OtherPlayersList[i] : "";
                        }
                        ws.Cell(r, c++).Value = team.Phone;
                        r++;
                    }
                }

                r++;  // spacer between sections
            }

            ws.Columns().AdjustToContents();

            wb.SaveAs(saveDlg.FileName);

            AppLogger.Info("Excel export saved: {Path}", saveDlg.FileName);
            MessageBox.Show($"Exported to {saveDlg.FileName}", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            AppLogger.Error(ex, "Excel export failed for {Filename}", filename);
            MessageBox.Show($"Error exporting to Excel: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ── Preview / print UI ────────────────────────────────────────────────────

    public static void ShowPrintPreview(Control parent, PrintDocument doc, string exportBaseName, List<PrintSection> sections, ListingMode mode)
    {
        var (headers, rows) = BuildFlatExportData(sections, mode);
        PrintPreviewService.ShowPrintPreview(parent, doc, headers, rows,
            p => ExportToExcel(p, exportBaseName, sections, mode));
    }
}

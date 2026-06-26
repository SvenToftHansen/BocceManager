using System.Drawing;
using System.Drawing.Printing;
using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Services;

public static class TeamsPrintService
{
    // ── Data structures ───────────────────────────────────────────────────────

    public record PrintSection(string DocHeader, string TimeSlotLabel, List<DaySection> DaySections);
    public record DaySection(string DayLabel, string TimeSlot, List<TeamRow> Teams);
    public record TeamRow(string TeamIdentifier, string CaptainName, string AllOtherPlayers, string Phone);

    // ── Layout constants ──────────────────────────────────────────────────────

    private const float ColTeamW    = 100f;  // Wider for team names
    private const float ColCaptainW = 120f;
    private const float ColPhoneW   = 138f;
    private const float DayGapH     = 12f;   // 1/8 inch gap between days


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

                        teamRows.Add(new TeamRow(teamName, captain, players, phone));
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

    public static PrintDocument BuildDocument(List<PrintSection> sections)
    {
        int secIdx  = 0;
        int dayIdx  = 0;
        int rowIdx  = 0;
        int pageNum = 0;

        var doc = new PrintDocument();
        doc.QueryPageSettings += (_, qe) =>
        {
            qe.PageSettings.Landscape = false;
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
            float ColPlayersW = tableWidth - ColTeamW - ColCaptainW - ColPhoneW;  // Fill remaining space

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
                DrawColText("Team",       cx, ColTeamW);    cx += ColTeamW;
                DrawColText("Captain",    cx, ColCaptainW); cx += ColCaptainW;
                DrawColText("Players",    cx, ColPlayersW); cx += ColPlayersW;
                DrawColText("Contact #",  cx, ColPhoneW);
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
            (string text, float rowH) WrapPlayers(string players)
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
                    if (w > ColPlayersW - 4 && current.Count > 1)
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
            void DrawDataRow(TeamRow r, bool alt, float rowH, string wrappedPlayers)
            {
                if (!draw) return;
                // All white background, no alternating colors
                float cx = lx;
                DrawCell(r.TeamIdentifier, cx, ColTeamW);  cx += ColTeamW;
                DrawCell(r.CaptainName,    cx, ColCaptainW); cx += ColCaptainW;
                DrawCell(wrappedPlayers,   cx, ColPlayersW); cx += ColPlayersW;
                DrawCell(r.Phone,          cx, ColPhoneW);
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
                        var (wrappedPlayers, rowH) = WrapPlayers(day.Teams[rowIdx].AllOtherPlayers);
                        if (y + rowH > yMax) { pe.HasMorePages = true; goto PageDone; }
                        DrawDataRow(day.Teams[rowIdx], rowIdx % 2 == 1, rowH, wrappedPlayers);
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

    // ── Preview / print UI ────────────────────────────────────────────────────

    public static void ShowPrintPreview(Control parent, PrintDocument doc, string? exportFilename = null, string[]? exportHeaders = null, List<string[]>? exportRows = null)
    {
        PrintPreviewService.ShowPrintPreview(parent, doc, exportHeaders, exportRows);
    }
}

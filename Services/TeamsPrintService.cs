using System.Drawing;
using System.Drawing.Printing;
using BocceManager.Data;
using BocceManager.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Services;

public static class TeamsPrintService
{
    // ── Data structures ───────────────────────────────────────────────────────

    public record PrintSection(string DocHeader, string TimeSlotLabel, List<DaySection> DaySections);
    public record DaySection(string DayLabel, List<TeamRow> Teams);
    public record TeamRow(string TeamIdentifier, string FormattedPlayers, string Phone);

    // ── Layout constants ──────────────────────────────────────────────────────

    private const float ColTeamW    = 90f;
    private const float ColPlayersW = 400f;  // All players with captain marked
    private const float ColPhoneW   = 120f;
    private const float TableW      = ColTeamW + ColPlayersW + ColPhoneW;

    private const string PrintFont = "Consolas";

    private const float DocHdrH     = 40f;
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

        string docHeader = $"Golden Vista Bocce Ball  –  {season.League.Name}  –  {season.Name}";

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

                        string phone = captainTp?.Player.Phone ?? "";
                        string formattedPlayers = FormatPlayersWithGrouping(playersForTeam, captainTp);
                        string teamName = team.Team.DisplayName ?? $"{team.Team.TeamLetter}-{captainTp?.Player.LastName ?? ""}".TrimEnd('-', ' ');
                        string teamIdentifier = teamName;

                        teamRows.Add(new TeamRow(teamIdentifier, formattedPlayers, phone));
                    }
                }

                string dayLabel = dayGroup.Key;
                daySections.Add(new DaySection(dayLabel, teamRows));
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

    private static string FormatPlayersWithGrouping(List<TeamPlayer> players, TeamPlayer? captain)
    {
        if (players.Count == 0) return "";

        var groups = new List<string>();
        var playersByLastName = players
            .GroupBy(tp => tp.Player.LastName)
            .OrderBy(g => players.IndexOf(players.FirstOrDefault(tp => tp.Player.LastName == g.Key) ?? players[0]))
            .ToList();

        foreach (var group in playersByLastName)
        {
            var groupPlayers = group.OrderBy(tp => tp.Player.FirstName).ToList();
            var captainInGroup = groupPlayers.FirstOrDefault(tp => tp == captain);

            if (groupPlayers.Count == 1)
            {
                var p = groupPlayers[0];
                string prefix = p == captain ? "*" : "";
                groups.Add($"{prefix}{p.Player.FirstName} {p.Player.LastName}".Trim());
            }
            else if (groupPlayers.Count == 2)
            {
                var nonCaptain = groupPlayers.FirstOrDefault(tp => tp != captain);
                if (captainInGroup != null)
                {
                    groups.Add($"{nonCaptain?.Player.FirstName} & *{captainInGroup.Player.FirstName} {captainInGroup.Player.LastName}".Trim());
                }
                else
                {
                    var firstNames = groupPlayers.Select(tp => tp.Player.FirstName).ToList();
                    groups.Add($"{string.Join(" & ", firstNames)} {groupPlayers[0].Player.LastName}".Trim());
                }
            }
            else
            {
                var nonCaptains = groupPlayers.Where(tp => tp != captain).Select(tp => tp.Player.FirstName).ToList();
                if (captainInGroup != null)
                {
                    groups.Add($"{string.Join(", ", nonCaptains)} & *{captainInGroup.Player.FirstName} {captainInGroup.Player.LastName}".Trim());
                }
                else
                {
                    var firstNames = groupPlayers.Select(tp => tp.Player.FirstName).ToList();
                    groups.Add($"{string.Join(", ", firstNames.SkipLast(1))} & {firstNames.Last()} {groupPlayers[0].Player.LastName}".Trim());
                }
            }
        }

        return string.Join(", ", groups);
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
            qe.PageSettings.Margins   = new Margins(10, 10, 10, 10);
        };
        doc.BeginPrint += (_, _) => { secIdx = 0; dayIdx = 0; rowIdx = 0; pageNum = 0; };

        doc.PrintPage += (_, pe) =>
        {
            if (pe.Graphics == null) return;
            pageNum++;
            var g      = pe.Graphics;
            var b      = pe.MarginBounds;
            float y    = b.Top;
            float yMax = b.Bottom - FooterH;

            using var docHdrFont   = new Font(PrintFont, 16f, FontStyle.Bold);
            using var timeSlotFont = new Font(PrintFont, 12f, FontStyle.Bold);
            using var dayHdrFont   = new Font(PrintFont, 10f, FontStyle.Bold);
            using var colHdrFont   = new Font(PrintFont,  9f, FontStyle.Bold);
            using var dataFont     = new Font(PrintFont,  9f);
            using var footerFont   = new Font(PrintFont,  8f);
            using var darkGreenBrush = new SolidBrush(Color.FromArgb(0, 100, 0));
            using var lightGreenBrush = new SolidBrush(Color.FromArgb(144, 238, 144));
            using var whiteBrush   = new SolidBrush(Color.White);
            using var hdrFill      = new SolidBrush(Color.FromArgb(210, 210, 210));
            using var altFill      = new SolidBrush(Color.FromArgb(246, 246, 246));
            using var sepPen       = new Pen(Color.FromArgb(190, 190, 190));
            using var leftAlign    = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near };
            using var centerAlign  = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

            float lx = b.Left;
            float tableWidth = b.Width;

            // ── Draw helpers ──────────────────────────────────────────────────
            void DrawTimeSlotLabel(string text)
            {
                g.FillRectangle(darkGreenBrush, b.Left, y, b.Width, TimeSlotH);
                var timeSlotRect = new RectangleF(b.Left, y, b.Width, TimeSlotH);
                g.DrawString(text, timeSlotFont, whiteBrush, timeSlotRect, centerAlign);
            }
            void DrawColHeader()
            {
                g.FillRectangle(hdrFill, lx, y, tableWidth, ColHdrH);
                float cx = lx;
                DrawColText("Team",     cx, ColTeamW);    cx += ColTeamW;
                DrawColText("Players",  cx, ColPlayersW); cx += ColPlayersW;
                DrawColText("Contact #", cx, ColPhoneW);
                g.DrawRectangle(Pens.Gray, lx, y, tableWidth - 1, ColHdrH - 1);

                void DrawColText(string text, float x, float w)
                {
                    g.DrawString(text, colHdrFont, Brushes.Black, new RectangleF(x + 2, y + 2, w - 4, ColHdrH - 4), leftAlign);
                    g.DrawLine(sepPen, x + w, y, x + w, y + ColHdrH);
                }
            }
            void DrawDayLabel(string text)
            {
                g.FillRectangle(lightGreenBrush, b.Left, y, b.Width, DayHdrH);
                var dayRect = new RectangleF(b.Left, y, b.Width, DayHdrH);
                g.DrawString(text, dayHdrFont, Brushes.Black, dayRect, centerAlign);
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
                if (alt) g.FillRectangle(altFill, lx, y, tableWidth, rowH);
                float cx = lx;
                DrawCell(r.TeamIdentifier, cx, ColTeamW);  cx += ColTeamW;
                DrawCell(wrappedPlayers,   cx, ColPlayersW); cx += ColPlayersW;
                DrawCell(r.Phone,          cx, ColPhoneW);
                g.DrawLine(sepPen, lx, y + rowH, lx + tableWidth, y + rowH);

                void DrawCell(string text, float x, float w)
                {
                    if (!string.IsNullOrEmpty(text))
                        g.DrawString(text, dataFont, Brushes.Black, new RectangleF(x + 2, y + 1, w - 4, rowH - 2), leftAlign);
                    g.DrawLine(sepPen, x + w, y, x + w, y + rowH);
                }
            }
            // ──────────────────────────────────────────────────────────────────

            // Document header — every page
            if (sections.Count > 0)
            {
                g.FillRectangle(darkGreenBrush, b.Left, y, b.Width, DocHdrH);
                var hdrRect = new RectangleF(b.Left, y, b.Width, DocHdrH);
                g.DrawString(sections[0].DocHeader, docHdrFont, whiteBrush, hdrRect, centerAlign);
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
                        // Orphan guard: require day header + at least 1 row
                        if (y + DayHdrH + DataRowH > yMax) { pe.HasMorePages = true; goto PageDone; }
                        DrawDayLabel(day.DayLabel);
                        y += DayHdrH;
                    }

                    for (; rowIdx < day.Teams.Count; rowIdx++)
                    {
                        var (wrappedPlayers, rowH) = WrapPlayers(day.Teams[rowIdx].FormattedPlayers);
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
            string footer   = $"Page {pageNum}";
            var    footerSz = g.MeasureString(footer, footerFont);
            g.DrawString(footer, footerFont, Brushes.Gray,
                b.Right - footerSz.Width, b.Bottom - footerSz.Height);
        };

        return doc;
    }

    // ── Preview / print UI ────────────────────────────────────────────────────

    public static void ShowPrintPreview(Control parent, PrintDocument doc)
    {
        // PrintPreviewControl.ComputePreview() calls PrinterSettings.GetHdevmode() —
        // it needs a valid printer name even for preview. Prefer a virtual PDF printer
        // so no physical printer is required to open the preview.
        var printerList = PrinterSettings.InstalledPrinters.Cast<string>().ToList();
        if (printerList.Count > 0 &&
            !printerList.Any(p => p.Equals(doc.PrinterSettings.PrinterName, StringComparison.OrdinalIgnoreCase)))
        {
            doc.PrinterSettings.PrinterName =
                printerList.FirstOrDefault(p => p.Contains("PDF", StringComparison.OrdinalIgnoreCase))
                ?? printerList[0];
        }
        bool hasPrinter = printerList.Count > 0;

        // Pre-render to get page count before showing the form
        var countCtrl = new PreviewPrintController();
        doc.PrintController = countCtrl;
        doc.Print();
        int totalPages = Math.Max(1, countCtrl.GetPreviewPageInfo().Length);

        using var form = new Form
        {
            Text          = $"Print Preview  —  {doc.DocumentName}",
            WindowState   = FormWindowState.Maximized,
            StartPosition = FormStartPosition.CenterParent,
            MinimumSize   = new Size(700, 500),
            BackColor     = Color.FromArgb(240, 240, 240)
        };

        // Declare preview early so Prev/Next handlers can close over it
        var preview = new PrintPreviewControl
        {
            Dock      = DockStyle.Fill,
            Document  = doc,
            AutoZoom  = true,
            BackColor = Color.FromArgb(240, 240, 240)
        };

        var toolbar = new ToolStrip
        {
            Dock      = DockStyle.Top,
            BackColor = Color.FromArgb(50, 50, 50),
            GripStyle = ToolStripGripStyle.Hidden,
            Padding   = new Padding(6, 3, 6, 3)
        };

        ToolStripButton Btn(string text, Color back) => new ToolStripButton(text)
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            BackColor    = back,
            ForeColor    = Color.White,
            Font         = new Font("Segoe UI", 9f, FontStyle.Bold),
            Margin       = new Padding(4, 0, 4, 0),
            AutoSize     = true,
            AutoToolTip  = false
        };

        var btnPrinter = Btn("🖨  Print to Printer", Color.FromArgb(0, 120, 215));
        var btnPdf     = Btn("📄  Save as PDF...",    Color.FromArgb(180, 50, 50));
        var btnWeb     = Btn("🌐  Website",           Color.FromArgb(120, 120, 120));
        var btnPrev    = Btn("◀  Prev",               Color.FromArgb(70, 70, 80));
        var btnNext    = Btn("Next  ▶",               Color.FromArgb(70, 70, 80));
        var btnClose   = Btn("✕  Close",              Color.FromArgb(80, 80, 80));
        var lblPage    = new ToolStripLabel($"Page 1 of {totalPages}")
        {
            ForeColor = Color.LightGray,
            Margin    = new Padding(6, 0, 6, 0)
        };

        void UpdateNav()
        {
            int p           = preview.StartPage;
            lblPage.Text    = $"Page {p + 1} of {totalPages}";
            btnPrev.Enabled = p > 0;
            btnNext.Enabled = p < totalPages - 1;
        }

        btnPrinter.Enabled = hasPrinter;
        btnWeb.Enabled     = false;
        btnPrev.Enabled    = false;
        btnNext.Enabled    = totalPages > 1;
        btnClose.Alignment = ToolStripItemAlignment.Right;

        toolbar.Items.AddRange([
            btnPrinter,
            new ToolStripSeparator(),
            btnPdf,
            new ToolStripSeparator(),
            btnWeb,
            new ToolStripSeparator(),
            btnPrev,
            lblPage,
            btnNext,
            btnClose
        ]);
        form.Controls.Add(toolbar);
        form.Controls.Add(preview);

        btnPrinter.Click += (_, _) => SendToPrinter(parent, doc);
        btnPdf.Click     += (_, _) => SendToPdf(parent, doc, doc.DocumentName);
        btnClose.Click   += (_, _) => form.Close();
        btnPrev.Click    += (_, _) => { preview.StartPage = Math.Max(0, preview.StartPage - 1); UpdateNav(); };
        btnNext.Click    += (_, _) => { preview.StartPage = Math.Min(totalPages - 1, preview.StartPage + 1); UpdateNav(); };

        form.ShowDialog(parent);
    }

    public static void SendToPrinter(Control parent, PrintDocument doc)
    {
        using var dlg = new PrintDialog { Document = doc, UseEXDialog = true };
        if (dlg.ShowDialog(parent) == DialogResult.OK)
        {
            doc.PrintController = new StandardPrintController();
            doc.Print();
        }
    }

    public static void SendToPdf(Control parent, PrintDocument doc, string suggestedName)
    {
        using var saveDlg = new SaveFileDialog
        {
            Title      = "Save as PDF",
            Filter     = "PDF files (*.pdf)|*.pdf",
            FileName   = suggestedName + ".pdf",
            DefaultExt = "pdf"
        };
        if (saveDlg.ShowDialog(parent) != DialogResult.OK) return;

        doc.PrinterSettings.PrinterName   = "Microsoft Print to PDF";
        doc.PrinterSettings.PrintToFile   = true;
        doc.PrinterSettings.PrintFileName = saveDlg.FileName;

        try
        {
            doc.PrintController = new StandardPrintController();
            doc.Print();
            MessageBox.Show("PDF saved successfully.", "Save as PDF",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"PDF export failed:\n\n{ex.Message}\n\n" +
                "Ensure 'Microsoft Print to PDF' is installed on this computer.",
                "Save as PDF", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

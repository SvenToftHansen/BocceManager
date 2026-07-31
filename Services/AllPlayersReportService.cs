using System.Drawing;
using System.Drawing.Printing;
using BocceManager.Data;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Services;

public static class AllPlayersReportService
{
    public record PlayerFeeRow(
        string Name, string Phone, string Email, string LotNumber,
        bool? InitiationPaid, bool? SeasonPaid, bool OnTeam, bool OnSpareList);

    public record ReportFilter(
        bool Paid, bool Unpaid,
        bool MissingPhone, bool MissingEmail, bool MissingLotNumber,
        bool OnTeam, bool NotOnTeam,
        bool OnSpareList, bool NotOnSpareList)
    {
        public string Describe()
        {
            var parts = new List<string>();

            var fee = new List<string>();
            if (Paid) fee.Add("Paid");
            if (Unpaid) fee.Add("Unpaid");
            if (fee.Count > 0) parts.Add("Fee: " + string.Join(" or ", fee));

            var missing = new List<string>();
            if (MissingPhone) missing.Add("Phone");
            if (MissingEmail) missing.Add("Email");
            if (MissingLotNumber) missing.Add("Lot Number");
            if (missing.Count > 0) parts.Add("Missing " + string.Join(" or ", missing));

            var team = new List<string>();
            if (OnTeam) team.Add("On a Team");
            if (NotOnTeam) team.Add("Not on a Team");
            if (team.Count > 0) parts.Add(string.Join(" or ", team));

            var spare = new List<string>();
            if (OnSpareList) spare.Add("On Spare List");
            if (NotOnSpareList) spare.Add("Not on Spare List");
            if (spare.Count > 0) parts.Add(string.Join(" or ", spare));

            return parts.Count > 0 ? "Filter: " + string.Join("   •   ", parts) : "Filter: All Players";
        }
    }

    // ── Layout constants ──────────────────────────────────────────────────────

    private const float ColNameW  = 150f;
    private const float ColPhoneW = 130f;
    private const float ColLotW   = 60f;
    private const float ColCheckW = 72f;

    private const float DocHdrH    = 32f;
    private const float FilterHdrH = 18f;
    private const float ColHdrH    = 22f;
    private const float DataRowH   = 18f;
    private const float FooterH    = 18f;

    // ── Build data from DB ─────────────────────────────────────────────────────

    public static List<PlayerFeeRow> BuildData(int leagueId, int? seasonId, ReportFilter filter)
    {
        using var db = new BocceDbContext();

        var players = db.Players
            .Where(p => p.IsActive)
            .AsNoTracking()
            .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
            .Select(p => new { p.Id, p.FirstName, p.LastName, p.Email, p.Phone, p.LotNumber })
            .ToList();

        var initiationPaidByPlayer = db.InitiationFees
            .AsNoTracking()
            .ToList()
            .ToDictionary(f => f.PlayerId, f => f.AmountPaid >= f.AmountOwing);

        var seasonPaidByPlayer = new Dictionary<int, bool>();
        if (seasonId.HasValue)
        {
            seasonPaidByPlayer = db.SeasonFees
                .Where(f => f.SeasonId == seasonId.Value)
                .AsNoTracking()
                .ToList()
                .ToDictionary(f => f.PlayerId, f => f.AmountPaid >= f.AmountOwing);
        }

        var onTeamPlayerIds = seasonId.HasValue
            ? db.TeamPlayers
                .Where(tp => tp.IsActive && tp.Team.Division.SeasonId == seasonId.Value)
                .Select(tp => tp.PlayerId)
                .ToHashSet()
            : [];

        var onSpareListPlayerIds = db.SpareLists
            .Where(s => s.LeagueId == leagueId && s.IsActive)
            .Select(s => s.PlayerId)
            .ToHashSet();

        var rows = new List<PlayerFeeRow>();
        foreach (var p in players)
        {
            bool? initiationPaid = initiationPaidByPlayer.TryGetValue(p.Id, out var ip) ? ip : null;
            bool? seasonPaid     = seasonPaidByPlayer.TryGetValue(p.Id, out var sp) ? sp : null;
            bool overallPaid     = initiationPaid != false && seasonPaid != false;
            bool onTeam          = onTeamPlayerIds.Contains(p.Id);
            bool onSpareList     = onSpareListPlayerIds.Contains(p.Id);

            if (filter.Paid || filter.Unpaid)
            {
                bool matchesFee = (filter.Paid && overallPaid) || (filter.Unpaid && !overallPaid);
                if (!matchesFee) continue;
            }

            if (filter.MissingPhone || filter.MissingEmail || filter.MissingLotNumber)
            {
                bool matchesMissing =
                    (filter.MissingPhone && string.IsNullOrWhiteSpace(p.Phone)) ||
                    (filter.MissingEmail && string.IsNullOrWhiteSpace(p.Email)) ||
                    (filter.MissingLotNumber && string.IsNullOrWhiteSpace(p.LotNumber));
                if (!matchesMissing) continue;
            }

            if (filter.OnTeam || filter.NotOnTeam)
            {
                bool matchesTeam = (filter.OnTeam && onTeam) || (filter.NotOnTeam && !onTeam);
                if (!matchesTeam) continue;
            }

            if (filter.OnSpareList || filter.NotOnSpareList)
            {
                bool matchesSpare = (filter.OnSpareList && onSpareList) || (filter.NotOnSpareList && !onSpareList);
                if (!matchesSpare) continue;
            }

            var name = string.IsNullOrWhiteSpace(p.LastName)
                ? p.FirstName.Trim()
                : string.IsNullOrWhiteSpace(p.FirstName)
                    ? p.LastName.Trim()
                    : $"{p.LastName}, {p.FirstName}".Trim();

            rows.Add(new PlayerFeeRow(
                name, p.Phone ?? "", p.Email ?? "", p.LotNumber ?? "",
                initiationPaid, seasonPaid, onTeam, onSpareList));
        }

        return rows;
    }

    // ── Build PrintDocument ───────────────────────────────────────────────────

    public static PrintDocument BuildDocument(string docHeader, string filterText, List<PlayerFeeRow> data)
    {
        var state = new RenderState { CurrentDataIndex = 0 };
        int pageNum = 0;

        var doc = new PrintDocument();
        doc.QueryPageSettings += (_, qe) =>
        {
            qe.PageSettings.Landscape = true;
            qe.PageSettings.Margins   = new Margins(30, 30, 20, 20);
        };
        doc.BeginPrint += (_, _) => { state.CurrentDataIndex = 0; pageNum = 0; };

        doc.PrintPage += (_, e) =>
        {
            pageNum++;
            int currentDataIndex = state.CurrentDataIndex;

            var g = e.Graphics;
            var b = e.MarginBounds;
            float y = b.Top;
            float tableWidth = b.Width;
            float colEmailW = tableWidth - ColNameW - ColPhoneW - ColLotW - ColCheckW * 4;

            var docHdrFont = AppTheme.FontNavHeader;
            var filterFont = AppTheme.FontSmall;
            var colHdrFont = AppTheme.FontGridHeader;
            var dataFont   = AppTheme.FontDefault;
            var footerFont = AppTheme.FontSmall;

            using var headerBrush = new SolidBrush(AppTheme.NavHeader);
            using var colHdrBrush = new SolidBrush(AppTheme.GridHeaderBackground);
            using var textBrush   = new SolidBrush(Color.Black);
            using var whiteBrush  = new SolidBrush(Color.White);
            using var altFill     = new SolidBrush(AppTheme.GridAlternateRow);
            using var sepPen      = new Pen(AppTheme.GridLines);
            using var boxPen      = new Pen(Color.Black, 1f);
            using var checkPen    = new Pen(Color.Black, 1.6f);
            using var leftAlign   = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
            using var centerAlign = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };

            // Document header
            g.FillRectangle(headerBrush, b.Left, y, b.Width, DocHdrH);
            g.DrawString(docHeader, docHdrFont, whiteBrush, new RectangleF(b.Left, y, b.Width, DocHdrH), centerAlign);
            y += DocHdrH;

            // Filter description line
            g.DrawString(filterText, filterFont, textBrush, new RectangleF(b.Left, y, b.Width, FilterHdrH), centerAlign);
            y += FilterHdrH;

            float lx = b.Left;

            void DrawColHeader()
            {
                g.FillRectangle(colHdrBrush, lx, y, tableWidth, ColHdrH);
                float cx = lx;
                DrawColText("Name",  cx, ColNameW);  cx += ColNameW;
                DrawColText("Phone", cx, ColPhoneW); cx += ColPhoneW;
                DrawColText("Email", cx, colEmailW); cx += colEmailW;
                DrawColText("Lot #", cx, ColLotW);   cx += ColLotW;
                DrawColText("Init Fee",   cx, ColCheckW, centerAlign); cx += ColCheckW;
                DrawColText("Season Fee", cx, ColCheckW, centerAlign); cx += ColCheckW;
                DrawColText("On Team",    cx, ColCheckW, centerAlign); cx += ColCheckW;
                DrawColText("On Spare",   cx, ColCheckW, centerAlign);
                g.DrawRectangle(Pens.Gray, lx, y, tableWidth - 1, ColHdrH - 1);

                void DrawColText(string text, float x, float w, StringFormat? sf = null)
                {
                    g.DrawString(text, colHdrFont, whiteBrush, new RectangleF(x + 2, y, w - 4, ColHdrH), sf ?? leftAlign);
                    g.DrawLine(sepPen, x + w, y, x + w, y + ColHdrH);
                }
            }

            void DrawCheckCell(bool? isChecked, float x, float w)
            {
                if (isChecked.HasValue)
                {
                    float boxSize = Math.Min(DataRowH - 6, w - 6);
                    float boxX = x + (w - boxSize) / 2f;
                    float boxY = y + (DataRowH - boxSize) / 2f;
                    g.DrawRectangle(boxPen, boxX, boxY, boxSize, boxSize);
                    if (isChecked.Value)
                    {
                        g.DrawLine(checkPen, boxX + boxSize * 0.15f, boxY + boxSize * 0.55f, boxX + boxSize * 0.4f, boxY + boxSize * 0.8f);
                        g.DrawLine(checkPen, boxX + boxSize * 0.4f, boxY + boxSize * 0.8f, boxX + boxSize * 0.85f, boxY + boxSize * 0.2f);
                    }
                }
                else
                {
                    g.DrawString("—", dataFont, textBrush, new RectangleF(x, y, w, DataRowH), centerAlign);
                }
                g.DrawLine(sepPen, x + w, y, x + w, y + DataRowH);
            }

            void DrawDataRow(PlayerFeeRow row, bool alt)
            {
                if (alt) g.FillRectangle(altFill, lx, y, tableWidth, DataRowH);

                float cx = lx;
                DrawCell(row.Name,  cx, ColNameW);  cx += ColNameW;
                DrawCell(row.Phone, cx, ColPhoneW); cx += ColPhoneW;
                DrawCell(row.Email, cx, colEmailW); cx += colEmailW;
                DrawCell(row.LotNumber, cx, ColLotW); cx += ColLotW;
                DrawCheckCell(row.InitiationPaid, cx, ColCheckW); cx += ColCheckW;
                DrawCheckCell(row.SeasonPaid, cx, ColCheckW); cx += ColCheckW;
                DrawCheckCell(row.OnTeam, cx, ColCheckW); cx += ColCheckW;
                DrawCheckCell(row.OnSpareList, cx, ColCheckW);

                g.DrawLine(sepPen, lx, y + DataRowH, lx + tableWidth, y + DataRowH);

                void DrawCell(string text, float x, float w)
                {
                    if (!string.IsNullOrEmpty(text))
                        g.DrawString(text, dataFont, textBrush, new RectangleF(x + 2, y, w - 4, DataRowH), leftAlign);
                    g.DrawLine(sepPen, x + w, y, x + w, y + DataRowH);
                }
            }

            DrawColHeader();
            y += ColHdrH;

            float yMax = b.Bottom - FooterH;
            while (currentDataIndex < data.Count && y + DataRowH <= yMax)
            {
                DrawDataRow(data[currentDataIndex], currentDataIndex % 2 == 1);
                y += DataRowH;
                currentDataIndex++;
            }

            state.CurrentDataIndex = currentDataIndex;
            e.HasMorePages = currentDataIndex < data.Count;

            string footer = $"Page {pageNum}";
            var footerSz = g.MeasureString(footer, footerFont);
            g.DrawString(footer, footerFont, Brushes.Gray, b.Right - footerSz.Width, b.Bottom - footerSz.Height);
        };

        return doc;
    }

    // ── Flat export data (Excel/CSV) ─────────────────────────────────────────

    public static (string[] Headers, List<string[]> Rows) BuildFlatExportData(List<PlayerFeeRow> data)
    {
        string[] headers = ["Name", "Phone", "Email", "Lot #", "Initiation Fee Paid", "Season Fee Paid", "On a Team", "On Spare List"];
        var rows = data.Select(r => new[]
        {
            r.Name, r.Phone, r.Email, r.LotNumber,
            DescribePaid(r.InitiationPaid),
            DescribePaid(r.SeasonPaid),
            r.OnTeam ? "Yes" : "No",
            r.OnSpareList ? "Yes" : "No"
        }).ToList();

        return (headers, rows);

        static string DescribePaid(bool? paid) => paid switch
        {
            true  => "Yes",
            false => "No",
            null  => "N/A"
        };
    }

    // ── Preview / print UI ────────────────────────────────────────────────────

    public static void ShowPrintPreview(Control parent, PrintDocument doc, List<PlayerFeeRow> data)
    {
        var (headers, rows) = BuildFlatExportData(data);
        PrintPreviewService.ShowPrintPreview(parent, doc, headers, rows);
    }

    private class RenderState
    {
        public int CurrentDataIndex { get; set; }
    }
}

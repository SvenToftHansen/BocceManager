using System.Drawing;
using System.Drawing.Printing;
using BocceManager.Data;
using BocceManager.UI.Theme;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Services;

public static class UnpaidFeesReportService
{
    // ── Data structures ───────────────────────────────────────────────────────

    public record FeeRow(string Name, string Email, string Phone, string LotNumber);
    public record FeeSection(string Title, List<FeeRow> Rows);

    // ── Layout constants ──────────────────────────────────────────────────────

    private const float ColNameW  = 170f;
    private const float ColPhoneW = 110f;
    private const float ColLotW   = 60f;
    private const float ColCheckW = 55f;

    private const float DocHdrH     = 40f;
    private const float SectionHdrH = 24f;
    private const float ColHdrH     = 20f;
    private const float DataRowH    = 18f;
    private const float FooterH     = 18f;
    private const float SectionGapH = 14f;

    // ── Build sections from DB ────────────────────────────────────────────────

    public static List<FeeSection> BuildSections(int? seasonId)
    {
        using var db = new BocceDbContext();

        var initRows = db.InitiationFees
            .Include(f => f.Player)
            .Where(f => f.AmountPaid == 0 || f.AmountPaid < f.AmountOwing)
            .AsNoTracking()
            .OrderBy(f => f.Player.LastName).ThenBy(f => f.Player.FirstName)
            .Select(f => new FeeRow(
                $"{f.Player.LastName}, {f.Player.FirstName}",
                f.Player.Email ?? "",
                f.Player.Phone ?? "",
                f.Player.LotNumber ?? ""))
            .ToList();

        var seasonRows = new List<FeeRow>();
        if (seasonId.HasValue)
        {
            seasonRows = db.SeasonFees
                .Include(f => f.Player)
                .Where(f => f.SeasonId == seasonId.Value && (f.AmountPaid == 0 || f.AmountPaid < f.AmountOwing))
                .AsNoTracking()
                .OrderBy(f => f.Player.LastName).ThenBy(f => f.Player.FirstName)
                .Select(f => new FeeRow(
                    $"{f.Player.LastName}, {f.Player.FirstName}",
                    f.Player.Email ?? "",
                    f.Player.Phone ?? "",
                    f.Player.LotNumber ?? ""))
                .ToList();
        }

        return
        [
            new FeeSection("INITIATION FEES — UNPAID", initRows),
            new FeeSection("YEARLY FEES — UNPAID", seasonRows)
        ];
    }

    // ── Build PrintDocument ───────────────────────────────────────────────────

    public static PrintDocument BuildDocument(string docHeader, List<FeeSection> sections)
    {
        int secIdx  = 0;
        int rowIdx  = 0;
        int pageNum = 0;

        var doc = new PrintDocument();
        doc.QueryPageSettings += (_, qe) =>
        {
            qe.PageSettings.Landscape = false;
            qe.PageSettings.Margins   = new Margins(30, 30, 20, 20);
        };
        doc.BeginPrint += (_, _) => { secIdx = 0; rowIdx = 0; pageNum = 0; };

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

            var docHdrFont     = AppTheme.FontNavHeader;
            var sectionHdrFont = AppTheme.FontDefaultBold;
            var colHdrFont     = AppTheme.FontGridHeader;
            var dataFont       = AppTheme.FontDefault;
            var footerFont     = AppTheme.FontSmall;
            using var headerBrush  = new SolidBrush(AppTheme.NavHeader);
            using var sectionBrush = new SolidBrush(AppTheme.GridHeaderBackground);
            using var textBrush    = new SolidBrush(Color.Black);
            using var whiteBrush   = new SolidBrush(Color.White);
            using var altFill      = new SolidBrush(AppTheme.GridAlternateRow);
            using var sepPen       = new Pen(AppTheme.GridLines);
            using var boxPen       = new Pen(Color.Black, 1f);
            using var leftAlign    = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
            using var centerAlign  = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

            float lx = b.Left;
            float tableWidth = b.Width;
            float colEmailW  = tableWidth - ColNameW - ColPhoneW - ColLotW - ColCheckW;

            // ── Draw helpers ──────────────────────────────────────────────────
            void DrawDocHeader()
            {
                if (!draw) return;
                g.FillRectangle(headerBrush, b.Left, y, b.Width, DocHdrH);
                var rect = new RectangleF(b.Left, y, b.Width, DocHdrH);
                g.DrawString(docHeader, docHdrFont, whiteBrush, rect, centerAlign);
            }
            void DrawSectionHeader(string title)
            {
                if (!draw) return;
                g.FillRectangle(sectionBrush, b.Left, y, b.Width, SectionHdrH);
                var rect = new RectangleF(b.Left, y, b.Width, SectionHdrH);
                g.DrawString(title, sectionHdrFont, whiteBrush, rect, centerAlign);
            }
            void DrawColHeader()
            {
                if (!draw) return;
                g.FillRectangle(sectionBrush, lx, y, tableWidth, ColHdrH);
                float cx = lx;
                DrawColText("Name",  cx, ColNameW);  cx += ColNameW;
                DrawColText("Email", cx, colEmailW); cx += colEmailW;
                DrawColText("Phone", cx, ColPhoneW); cx += ColPhoneW;
                DrawColText("Lot #", cx, ColLotW);   cx += ColLotW;
                DrawColText("Paid",  cx, ColCheckW, centerAlign);
                g.DrawRectangle(Pens.Gray, lx, y, tableWidth - 1, ColHdrH - 1);

                void DrawColText(string text, float x, float w, StringFormat? sf = null)
                {
                    g.DrawString(text, colHdrFont, whiteBrush, new RectangleF(x + 2, y, w - 4, ColHdrH), sf ?? leftAlign);
                    g.DrawLine(sepPen, x + w, y, x + w, y + ColHdrH);
                }
            }
            void DrawDataRow(FeeRow r, bool alt)
            {
                if (!draw) return;
                if (alt) g.FillRectangle(altFill, lx, y, tableWidth, DataRowH);

                float cx = lx;
                DrawCell(r.Name,      cx, ColNameW);  cx += ColNameW;
                DrawCell(r.Email,     cx, colEmailW); cx += colEmailW;
                DrawCell(r.Phone,     cx, ColPhoneW); cx += ColPhoneW;
                DrawCell(r.LotNumber, cx, ColLotW);   cx += ColLotW;

                // Checkbox — empty square to hand-tick
                float boxSize = Math.Min(DataRowH - 6, ColCheckW - 6);
                float boxX = cx + (ColCheckW - boxSize) / 2f;
                float boxY = y + (DataRowH - boxSize) / 2f;
                g.DrawRectangle(boxPen, boxX, boxY, boxSize, boxSize);
                g.DrawLine(sepPen, cx, y, cx, y + DataRowH);

                g.DrawLine(sepPen, lx, y + DataRowH, lx + tableWidth, y + DataRowH);

                void DrawCell(string text, float x, float w)
                {
                    if (!string.IsNullOrEmpty(text))
                        g.DrawString(text, dataFont, textBrush, new RectangleF(x + 2, y, w - 4, DataRowH), leftAlign);
                    g.DrawLine(sepPen, x + w, y, x + w, y + DataRowH);
                }
            }
            // ──────────────────────────────────────────────────────────────────

            DrawDocHeader();
            y += DocHdrH;

            pe.HasMorePages = false;

            for (; secIdx < sections.Count; secIdx++)
            {
                var sec = sections[secIdx];
                if (sec.Rows.Count == 0)
                {
                    rowIdx = 0;
                    continue;
                }

                if (rowIdx == 0)
                {
                    float minH = SectionHdrH + ColHdrH + DataRowH;
                    if (y + minH > yMax) { pe.HasMorePages = true; goto PageDone; }
                    DrawSectionHeader(sec.Title);
                    y += SectionHdrH;
                    DrawColHeader();
                    y += ColHdrH;
                }
                else
                {
                    if (y + ColHdrH + DataRowH > yMax) { pe.HasMorePages = true; goto PageDone; }
                    DrawColHeader();
                    y += ColHdrH;
                }

                for (; rowIdx < sec.Rows.Count; rowIdx++)
                {
                    if (y + DataRowH > yMax) { pe.HasMorePages = true; goto PageDone; }
                    DrawDataRow(sec.Rows[rowIdx], rowIdx % 2 == 1);
                    y += DataRowH;
                }

                rowIdx = 0;
                y += SectionGapH;
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

    public static (string[] Headers, List<string[]> Rows) BuildFlatExportData(List<FeeSection> sections)
    {
        string[] headers = ["Fee Type", "Name", "Email", "Phone", "Lot #", "Paid"];
        var rows = new List<string[]>();

        foreach (var section in sections)
            foreach (var r in section.Rows)
                rows.Add([section.Title, r.Name, r.Email, r.Phone, r.LotNumber, ""]);

        return (headers, rows);
    }

    // ── Excel export (mirrors the printed layout exactly) ────────────────────

    public static void ExportToExcel(Control parent, string filename, string docHeader, List<FeeSection> sections)
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
            string[] columnHeaders = ["Name", "Email", "Phone", "Lot #", "Paid"];
            int colCount = columnHeaders.Length;

            var docFill     = XLColor.FromColor(AppTheme.NavHeader);
            var sectionFill = XLColor.FromColor(AppTheme.GridHeaderBackground);

            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Unpaid Fees");
            int r = 1;

            ws.Range(r, 1, r, colCount).Merge();
            var docCell = ws.Cell(r, 1);
            docCell.Value = docHeader;
            docCell.Style.Font.Bold = true;
            docCell.Style.Font.FontColor = XLColor.White;
            docCell.Style.Fill.BackgroundColor = docFill;
            docCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Row(r).Height = 20;
            r += 2;

            foreach (var section in sections)
            {
                if (section.Rows.Count == 0) continue;

                ws.Range(r, 1, r, colCount).Merge();
                var secCell = ws.Cell(r, 1);
                secCell.Value = section.Title;
                secCell.Style.Font.Bold = true;
                secCell.Style.Font.FontColor = XLColor.White;
                secCell.Style.Fill.BackgroundColor = sectionFill;
                secCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Row(r).Height = 18;
                r++;

                for (int c = 0; c < colCount; c++)
                {
                    var hc = ws.Cell(r, c + 1);
                    hc.Value = columnHeaders[c];
                    hc.Style.Font.Bold = true;
                    hc.Style.Font.FontColor = XLColor.White;
                    hc.Style.Fill.BackgroundColor = sectionFill;
                }
                r++;

                foreach (var row in section.Rows)
                {
                    ws.Cell(r, 1).Value = row.Name;
                    ws.Cell(r, 2).Value = row.Email;
                    ws.Cell(r, 3).Value = row.Phone;
                    ws.Cell(r, 4).Value = row.LotNumber;

                    var checkCell = ws.Cell(r, 5);
                    checkCell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    checkCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    r++;
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

    public static void ShowPrintPreview(Control parent, PrintDocument doc, string exportBaseName, string docHeader, List<FeeSection> sections)
    {
        var (headers, rows) = BuildFlatExportData(sections);
        PrintPreviewService.ShowPrintPreview(parent, doc, headers, rows,
            p => ExportToExcel(p, exportBaseName, docHeader, sections));
    }
}

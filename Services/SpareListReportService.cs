using System.Drawing;
using System.Drawing.Printing;
using BocceManager.Data;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;
using BocceManager.Data.Entities;

namespace BocceManager.Services;

public static class SpareListReportService
{
    public record SpareListRow(string Name, string Phone, string Email, string LotNumber, string Notes);

    private const float ColNameW = 150f;
    private const float ColPhoneW = 120f;
    private const float ColEmailW = 180f;
    private const float ColLotW = 80f;
    private const float ColNotesW = 140f;

    public static List<SpareListRow> BuildSpareListData(int leagueId)
    {
        using var db = new BocceDbContext();

        var spareList = db.SpareLists
            .Where(s => s.LeagueId == leagueId && s.IsActive)
            .Include(s => s.Player)
            .AsNoTracking()
            .OrderBy(s => s.Player.LastName)
            .ThenBy(s => s.Player.FirstName)
            .Select(s => new SpareListRow(
                $"{s.Player.LastName}, {s.Player.FirstName}",
                s.Player.Phone ?? "",
                s.Player.Email ?? "",
                s.Player.LotNumber ?? "",
                s.Notes ?? ""
            ))
            .ToList();

        return spareList;
    }

    public static PrintDocument BuildDocument(string clubName, string leagueName, string seasonName, List<SpareListRow> data)
    {
        var doc = new PrintDocument();
        var state = new RenderState { CurrentDataIndex = 0 };

        doc.QueryPageSettings += (_, qe) =>
        {
            qe.PageSettings.Margins = new Margins(30, 30, 30, 30);
        };

        doc.BeginPrint += (_, _) => state.CurrentDataIndex = 0;

        doc.PrintPage += (_, e) =>
        {
            int currentDataIndex = state.CurrentDataIndex;

            var g = e.Graphics;
            float y = e.MarginBounds.Top;
            float pageWidth = e.MarginBounds.Width;

            // Fonts and brushes
            using var fontTitle = new Font("Segoe UI", 11f, FontStyle.Bold);
            using var fontColHeader = new Font("Segoe UI", 9f, FontStyle.Bold);
            using var fontData = new Font("Segoe UI", 9f);
            using var headerBrush = new SolidBrush(Color.White);
            using var textBrush = new SolidBrush(Color.Black);
            using var hdrBgBrush = new SolidBrush(AppTheme.NavHeader);
            using var penGrid = new Pen(Color.FromArgb(200, 200, 200));

            // Full-width header with AppTheme background (matching Team Listing height)
            float headerHeight = 28f;
            g.FillRectangle(hdrBgBrush, e.MarginBounds.Left, y, pageWidth, headerHeight);

            var headerText = $"{clubName} – {leagueName} – {seasonName} - SPARE LIST";
            var sfHeader = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(headerText, fontTitle, headerBrush, new RectangleF(e.MarginBounds.Left, y, pageWidth, headerHeight), sfHeader);
            sfHeader.Dispose();

            y += headerHeight + 4;

            // Column headers with dark background
            float colHdrHeight = 18f;
            g.FillRectangle(hdrBgBrush, e.MarginBounds.Left, y, pageWidth, colHdrHeight);

            var sfLeft = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
            g.DrawString("Name", fontColHeader, headerBrush, new RectangleF(e.MarginBounds.Left + 2, y, ColNameW - 4, colHdrHeight), sfLeft);
            g.DrawString("Phone", fontColHeader, headerBrush, new RectangleF(e.MarginBounds.Left + ColNameW + 2, y, ColPhoneW - 4, colHdrHeight), sfLeft);
            g.DrawString("Email", fontColHeader, headerBrush, new RectangleF(e.MarginBounds.Left + ColNameW + ColPhoneW + 2, y, ColEmailW - 4, colHdrHeight), sfLeft);
            g.DrawString("Lot #", fontColHeader, headerBrush, new RectangleF(e.MarginBounds.Left + ColNameW + ColPhoneW + ColEmailW + 2, y, ColLotW - 4, colHdrHeight), sfLeft);
            g.DrawString("Notes", fontColHeader, headerBrush, new RectangleF(e.MarginBounds.Left + ColNameW + ColPhoneW + ColEmailW + ColLotW + 2, y, ColNotesW - 4, colHdrHeight), sfLeft);

            g.DrawRectangle(penGrid, e.MarginBounds.Left, y, pageWidth - 1, colHdrHeight - 1);
            y += colHdrHeight;

            // Data rows
            var stringFormat = new StringFormat
            {
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            };
            float dataRowHeight = 16f;

            using var altBrush = new SolidBrush(Color.FromArgb(245, 245, 245));

            while (currentDataIndex < data.Count && y < e.MarginBounds.Bottom - 20)
            {
                var row = data[currentDataIndex];

                // Draw alternating row background
                if (currentDataIndex % 2 == 1)
                {
                    g.FillRectangle(altBrush, e.MarginBounds.Left, y, pageWidth, dataRowHeight);
                }

                // Clip text to column widths
                g.DrawString(row.Name, fontData, textBrush,
                    new RectangleF(e.MarginBounds.Left + 2, y, ColNameW - 5, dataRowHeight), stringFormat);
                g.DrawString(row.Phone, fontData, textBrush,
                    new RectangleF(e.MarginBounds.Left + ColNameW + 2, y, ColPhoneW - 5, dataRowHeight), stringFormat);
                g.DrawString(row.Email, fontData, textBrush,
                    new RectangleF(e.MarginBounds.Left + ColNameW + ColPhoneW + 2, y, ColEmailW - 5, dataRowHeight), stringFormat);
                g.DrawString(row.LotNumber, fontData, textBrush,
                    new RectangleF(e.MarginBounds.Left + ColNameW + ColPhoneW + ColEmailW + 2, y, ColLotW - 5, dataRowHeight), stringFormat);
                g.DrawString(row.Notes, fontData, textBrush,
                    new RectangleF(e.MarginBounds.Left + ColNameW + ColPhoneW + ColEmailW + ColLotW + 2, y, ColNotesW - 5, dataRowHeight), stringFormat);

                // Draw row separator
                g.DrawLine(penGrid, e.MarginBounds.Left, y + dataRowHeight, e.MarginBounds.Right, y + dataRowHeight);

                y += dataRowHeight;
                currentDataIndex++;
            }

            state.CurrentDataIndex = currentDataIndex;
            e.HasMorePages = currentDataIndex < data.Count;
        };

        return doc;
    }

    public static void ShowPrintPreview(Control parent, PrintDocument doc, string leagueName, List<SpareListRow> data)
    {
        var exportHeaders = new[] { "Name", "Phone", "Email", "Lot Number", "Notes" };
        var exportRows = data.Select(r => new[] { r.Name, r.Phone, r.Email, r.LotNumber, r.Notes }).ToList();
        PrintPreviewService.ShowPrintPreview(parent, doc, exportHeaders, exportRows);
    }

    private class RenderState
    {
        public int CurrentDataIndex { get; set; }
    }
}

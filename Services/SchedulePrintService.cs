using System.Drawing;
using System.Drawing.Printing;
using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Services;

public static class SchedulePrintService
{
    // ── Data structures ───────────────────────────────────────────────────────

    public record PrintSection(string DocHeader, string TmplTitle, string[] CourtHeaders, List<PDataRow> Rows);
    public record PDataRow(string Week, (string S1, string S2)?[] Slots, bool Alt);

    // ── Layout constants ──────────────────────────────────────────────────────

    private const float PrintWeekW  = 150f;
    private const float PrintCourtW = 162.5f;  // 4 × 162.5 + 150 week = 800 = full margin width
    private const int   PrintCourts = 4;
    private const float DocHdrH     = 48f;  // Increased to prevent cutoff
    private const float TmplTitleH  = 28f;
    private const float ColHdrH     = 24f;
    private const float DataRowH    = 22f;
    private const float SepH        = 12f;
    private const float FooterH     = 18f;

    // Consolas: monospaced — all capital slot letters (A–H) have identical width
    private const string PrintFont = "Consolas";

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string CourtLabel(Court court, string display) =>
        display == "letter" && !string.IsNullOrEmpty(court.CourtLetter)
            ? $"Court {court.CourtLetter}"
            : $"Court {court.CourtNumber}";

    private static string WeekLabel(DateOnly? start, int weekNum)
    {
        if (!start.HasValue) return $"Week {weekNum}";
        DateOnly s = start.Value.AddDays((weekNum - 1) * 7);
        DateOnly e = s.AddDays(6);
        return $"{s:MMM d} - {e:MMM d}";
    }

    // ── Build sections from DB ────────────────────────────────────────────────

    public static List<PrintSection> BuildSections(int seasonId)
    {
        using var db = new BocceDbContext();

        var season = db.Seasons
            .Include(s => s.League)
            .First(s => s.Id == seasonId);

        string docHeader    = $"Golden Vista Bocce Ball  –  {season.League.Name}  –  {season.Name}";
        string courtDisplay = AppParameterService.GetCourtDisplay(db, season.LeagueId);

        var seasonCourts = db.SeasonCourts
            .Where(sc => sc.SeasonId == seasonId)
            .Include(sc => sc.Court)
            .OrderBy(sc => sc.Court.CourtNumber)
            .AsEnumerable()
            .Select(sc => (Id: sc.CourtId, Display: CourtLabel(sc.Court, courtDisplay)))
            .ToList();

        var courtOrder = seasonCourts.Take(PrintCourts).ToList();
        string[] courtHeaders = Enumerable.Range(0, PrintCourts)
            .Select(i => i < courtOrder.Count ? courtOrder[i].Display : $"Court {i + 1}")
            .ToArray();

        DateOnly? startDate = season.StartDate;
        var templates = ScheduleTemplateService.GetTemplatesForSeason(db, seasonId);
        var sections  = new List<PrintSection>();

        foreach (var tmpl in templates)
        {
            string title = $"{tmpl.TeamCount}-Team Template  —  {tmpl.WeekCount} weeks";
            if (startDate.HasValue)
            {
                var last = startDate.Value.AddDays((tmpl.WeekCount - 1) * 7 + 6);
                title += $"  ({startDate.Value:MMM d} – {last:MMM d, yyyy})";
            }

            var rows  = ScheduleTemplateService.GetTemplateRows(db, tmpl.Id);
            var weeks = rows
                .GroupBy(r => r.WeekNumber)
                .OrderBy(g => g.Key)
                .Select(g => (WeekNumber: g.Key, Matches: g.OrderBy(r => r.Slot1).ToList()))
                .ToList();

            int alt = 0;
            var dataRows = new List<PDataRow>();
            foreach (var (weekNum, matches) in weeks)
            {
                var slots = new (string S1, string S2)?[PrintCourts];
                foreach (var m in matches)
                {
                    int pos = courtOrder.FindIndex(c => c.Id == m.CourtId);
                    if (pos >= 0) slots[pos] = (m.Slot1, m.Slot2);
                }
                dataRows.Add(new PDataRow(WeekLabel(startDate, weekNum), slots, alt % 2 == 1));
                alt++;
            }

            sections.Add(new PrintSection(docHeader, title, courtHeaders, dataRows));
        }

        return sections;
    }

    // ── Build PrintDocument ───────────────────────────────────────────────────

    public static PrintDocument BuildDocument(List<PrintSection> sections)
    {
        int   secIdx  = 0;
        int   rowIdx  = 0;
        int   pageNum = 0;
        float tableW  = PrintWeekW + PrintCourts * PrintCourtW;

        var doc = new PrintDocument();
        doc.QueryPageSettings += (_, qe) =>
        {
            qe.PageSettings.Landscape = false;
            qe.PageSettings.Margins   = new Margins(10, 10, 10, 10);
        };
        doc.BeginPrint += (_, _) => { secIdx = 0; rowIdx = 0; pageNum = 0; };

        doc.PrintPage += (_, pe) =>
        {
            if (pe.Graphics == null) return;
            pageNum++;
            var g      = pe.Graphics;
            var b      = pe.MarginBounds;
            float y    = b.Top;
            float yMax = b.Bottom - FooterH;
            float pageH = yMax - b.Top;

            var docHdrFont = AppTheme.FontNavHeader;
            var titleFont  = AppTheme.FontSectionHeading;
            var hdrFont    = AppTheme.FontGridHeader;
            var dataFont   = AppTheme.FontDefault;
            var teamFont   = AppTheme.FontDefaultBold;
            var vsFont     = AppTheme.FontSmall;
            var footerFont = AppTheme.FontSmall;
            using var headerBrush = new SolidBrush(AppTheme.NavHeader);
            using var hdrFill     = new SolidBrush(AppTheme.GridHeaderBackground);
            using var sepPen      = new Pen(AppTheme.GridLines);
            using var centerSf   = new StringFormat
            {
                Alignment     = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            float lx = b.Left;
            float tableW_adjusted = b.Width;

            // ── Draw helpers ──────────────────────────────────────────────────
            void DrawTmplTitle(string text)
            {
                g.DrawString(text, titleFont, Brushes.Black, lx, y + 3);
            }
            void DrawColHeader(string[] headers)
            {
                g.FillRectangle(hdrFill, lx, y, tableW_adjusted, ColHdrH);
                g.DrawString("Week Of", hdrFont, new SolidBrush(Color.White), lx + 3, y + 4);
                for (int i = 0; i < PrintCourts; i++)
                {
                    float cx = lx + PrintWeekW + i * PrintCourtW;
                    g.DrawLine(sepPen, cx, y, cx, y + ColHdrH);
                    g.DrawString(headers[i], hdrFont, new SolidBrush(Color.White),
                        new RectangleF(cx, y, PrintCourtW, ColHdrH), centerSf);
                }
                g.DrawRectangle(sepPen, lx, y, tableW_adjusted - 1, ColHdrH - 1);
            }
            void DrawDataRow(PDataRow r)
            {
                // All white background, no alternating colors
                g.DrawString(r.Week, dataFont, new SolidBrush(Color.Black), lx + 3, y + 3);
                for (int i = 0; i < PrintCourts; i++)
                {
                    float cx = lx + PrintWeekW + i * PrintCourtW;
                    g.DrawLine(sepPen, cx, y, cx, y + DataRowH);
                    if (r.Slots[i] is { } slot)
                    {
                        // 3-part centered: SLOT1 (bold) + explicit gap + vs (italic) + gap + SLOT2 (bold)
                        const float vsGap = 6f;
                        var sf    = StringFormat.GenericTypographic;
                        float w1  = g.MeasureString(slot.S1, teamFont, PointF.Empty, sf).Width;
                        float wv  = g.MeasureString("vs",    vsFont,   PointF.Empty, sf).Width;
                        float w2  = g.MeasureString(slot.S2, teamFont, PointF.Empty, sf).Width;
                        float tx  = cx + (PrintCourtW - (w1 + vsGap + wv + vsGap + w2)) / 2f;
                        float ry  = y + (DataRowH - teamFont.GetHeight(g)) / 2f;
                        g.DrawString(slot.S1, teamFont, new SolidBrush(Color.Black), tx,                           ry, sf);
                        g.DrawString("vs",    vsFont,   new SolidBrush(Color.Black), tx + w1 + vsGap,              ry, sf);
                        g.DrawString(slot.S2, teamFont, new SolidBrush(Color.Black), tx + w1 + vsGap + wv + vsGap, ry, sf);
                    }
                }
                g.DrawLine(sepPen, lx, y + DataRowH, lx + tableW_adjusted, y + DataRowH);
            }
            // ─────────────────────────────────────────────────────────────────

            // Document header — page 1 only, with dark background and white text
            if (pageNum == 1 && sections.Count > 0)
            {
                g.FillRectangle(headerBrush, b.Left, y, b.Width, DocHdrH);
                var centerSf_hdr = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                var hdrRect = new RectangleF(b.Left, y, b.Width, DocHdrH);
                g.DrawString(sections[0].DocHeader, docHdrFont, new SolidBrush(Color.White), hdrRect, centerSf_hdr);
                y += DocHdrH;
            }

            bool firstOnPage = true;
            pe.HasMorePages  = false;

            for (; secIdx < sections.Count; secIdx++)
            {
                var sec     = sections[secIdx];
                bool isCont = rowIdx > 0;
                float hdrH  = TmplTitleH + ColHdrH;

                if (!isCont)
                {
                    float secH = hdrH + sec.Rows.Count * DataRowH;

                    // Section fits on a blank page but not remaining space → defer
                    if (!firstOnPage && y + secH > yMax && secH <= pageH)
                    {
                        pe.HasMorePages = true;
                        break;
                    }

                    // Separator between sections
                    if (!firstOnPage)
                    {
                        if (y + SepH > yMax) { pe.HasMorePages = true; break; }
                        g.DrawLine(sepPen, lx, y + SepH / 2f, lx + tableW_adjusted, y + SepH / 2f);
                        y += SepH;
                    }
                }

                // Title + column header always drawn, even on continuation pages
                if (y + hdrH > yMax) { pe.HasMorePages = true; break; }
                DrawTmplTitle(sec.TmplTitle); y += TmplTitleH;
                DrawColHeader(sec.CourtHeaders); y += ColHdrH;

                for (; rowIdx < sec.Rows.Count; rowIdx++)
                {
                    if (y + DataRowH > yMax) { pe.HasMorePages = true; goto PageDone; }
                    DrawDataRow(sec.Rows[rowIdx]); y += DataRowH;
                }

                rowIdx      = 0;
                firstOnPage = false;
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
        btnWeb.Enabled  = false;
        btnPrev.Enabled = false;
        btnNext.Enabled = totalPages > 1;
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

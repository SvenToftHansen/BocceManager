using System.Drawing;
using System.Drawing.Printing;
using BocceManager.Data;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Services;

public static class SpareListReportService
{
    public record SpareListRow(string Name, string Phone, string Email, string LotNumber, string Notes);

    private const float ColNameW = 200f;
    private const float ColPhoneW = 130f;
    private const float ColEmailW = 200f;
    private const float ColLotW = 100f;
    private const float ColNotesW = 150f;

    private const float DocHdrH = 28f;
    private const float ColHdrH = 20f;
    private const float DataRowH = 16f;
    private const float FooterH = 16f;

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

    public static PrintDocument BuildDocument(string leagueName, List<SpareListRow> data)
    {
        var doc = new PrintDocument();
        int currentDataIndex = 0;

        doc.PrintPage += (_, e) =>
        {
            var g = e.Graphics;
            float y = e.MarginBounds.Top;
            float pageWidth = e.MarginBounds.Width;

            // Title
            using var fontTitle = new Font("Consolas", 14, FontStyle.Bold);
            g.DrawString($"{leagueName} – Spare List", fontTitle, Brushes.Black, e.MarginBounds.Left, y);
            y += DocHdrH;

            // Column headers
            using var fontColHeader = new Font("Consolas", 9, FontStyle.Bold);
            using var penSeparator = new Pen(Color.Black, 1);

            g.DrawString("Name", fontColHeader, Brushes.Black, e.MarginBounds.Left, y);
            g.DrawString("Phone", fontColHeader, Brushes.Black, e.MarginBounds.Left + ColNameW, y);
            g.DrawString("Email", fontColHeader, Brushes.Black, e.MarginBounds.Left + ColNameW + ColPhoneW, y);
            g.DrawString("Lot #", fontColHeader, Brushes.Black, e.MarginBounds.Left + ColNameW + ColPhoneW + ColEmailW, y);
            g.DrawString("Notes", fontColHeader, Brushes.Black, e.MarginBounds.Left + ColNameW + ColPhoneW + ColEmailW + ColLotW, y);

            y += ColHdrH;
            g.DrawLine(penSeparator, e.MarginBounds.Left, y, e.MarginBounds.Right, y);
            y += 4;

            // Data rows
            using var fontData = new Font("Consolas", 8);
            var stringFormat = new StringFormat
            {
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            };

            while (currentDataIndex < data.Count && y < e.MarginBounds.Bottom - FooterH)
            {
                var row = data[currentDataIndex];

                // Clip text to column widths
                g.DrawString(row.Name, fontData, Brushes.Black,
                    new RectangleF(e.MarginBounds.Left, y, ColNameW - 5, DataRowH), stringFormat);
                g.DrawString(row.Phone, fontData, Brushes.Black,
                    new RectangleF(e.MarginBounds.Left + ColNameW, y, ColPhoneW - 5, DataRowH), stringFormat);
                g.DrawString(row.Email, fontData, Brushes.Black,
                    new RectangleF(e.MarginBounds.Left + ColNameW + ColPhoneW, y, ColEmailW - 5, DataRowH), stringFormat);
                g.DrawString(row.LotNumber, fontData, Brushes.Black,
                    new RectangleF(e.MarginBounds.Left + ColNameW + ColPhoneW + ColEmailW, y, ColLotW - 5, DataRowH), stringFormat);
                g.DrawString(row.Notes, fontData, Brushes.Black,
                    new RectangleF(e.MarginBounds.Left + ColNameW + ColPhoneW + ColEmailW + ColLotW, y, ColNotesW - 5, DataRowH), stringFormat);

                y += DataRowH;
                currentDataIndex++;
            }

            e.HasMorePages = currentDataIndex < data.Count;
        };

        return doc;
    }

    public static void ShowPrintPreview(Control parent, PrintDocument doc, string leagueName, List<SpareListRow> data)
    {
        var printerList = PrinterSettings.InstalledPrinters.Cast<string>().ToList();
        string preferredPrinter = printerList.FirstOrDefault(p => p.Contains("PDF") || p.Contains("pdf"))
                               ?? printerList.FirstOrDefault()
                               ?? "Microsoft Print to PDF";

        if (PrinterSettings.InstalledPrinters.Contains(preferredPrinter))
            doc.PrinterSettings.PrinterName = preferredPrinter;

        var form = new Form
        {
            Text = "Print Preview - Spare List",
            Width = 1000,
            Height = 700,
            StartPosition = FormStartPosition.CenterParent,
            BackColor = AppTheme.ContentBackground
        };

        var preview = new PrintPreviewControl
        {
            Dock = DockStyle.Fill,
            Document = doc,
            AutoZoom = false,
            Zoom = 1.0,
            BackColor = Color.DarkGray
        };

        var toolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = AppTheme.Surface,
            Padding = new Padding(10)
        };

        var btnPrint = new Button
        {
            Text = "🖨 Print",
            Location = new Point(10, 10),
            Size = new Size(100, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.Accent,
            ForeColor = Color.White,
            Font = AppTheme.FontButton,
            Cursor = Cursors.Hand
        };
        btnPrint.FlatAppearance.BorderSize = 0;
        btnPrint.Click += (_, _) =>
        {
            SendToPrinter(parent, doc);
            form.Close();
        };

        var btnPdf = new Button
        {
            Text = "📄 PDF",
            Location = new Point(120, 10),
            Size = new Size(100, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.Accent,
            ForeColor = Color.White,
            Font = AppTheme.FontButton,
            Cursor = Cursors.Hand
        };
        btnPdf.FlatAppearance.BorderSize = 0;
        btnPdf.Click += (_, _) =>
        {
            SendToPdf(parent, doc, $"{leagueName}_SpareList");
            form.Close();
        };

        var btnExcel = new Button
        {
            Text = "📊 Excel",
            Location = new Point(230, 10),
            Size = new Size(100, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.Accent,
            ForeColor = Color.White,
            Font = AppTheme.FontButton,
            Cursor = Cursors.Hand
        };
        btnExcel.FlatAppearance.BorderSize = 0;
        btnExcel.Click += (_, _) =>
        {
            ExportToExcel(parent, leagueName, data);
            form.Close();
        };

        var btnCsv = new Button
        {
            Text = "📋 CSV",
            Location = new Point(340, 10),
            Size = new Size(100, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.Accent,
            ForeColor = Color.White,
            Font = AppTheme.FontButton,
            Cursor = Cursors.Hand
        };
        btnCsv.FlatAppearance.BorderSize = 0;
        btnCsv.Click += (_, _) =>
        {
            ExportToCsv(parent, leagueName, data);
            form.Close();
        };

        var btnClose = new Button
        {
            Text = "Close",
            Location = new Point(450, 10),
            Size = new Size(100, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.FontButton,
            Cursor = Cursors.Hand
        };
        btnClose.FlatAppearance.BorderSize = 1;
        btnClose.FlatAppearance.BorderColor = AppTheme.Separator;
        btnClose.Click += (_, _) => form.Close();

        toolbar.Controls.AddRange([btnPrint, btnPdf, btnExcel, btnCsv, btnClose]);
        form.Controls.Add(preview);
        form.Controls.Add(toolbar);

        form.ShowDialog(parent);
    }

    public static void SendToPrinter(Control parent, PrintDocument doc)
    {
        doc.PrinterSettings.MinimumPage = 1;
        doc.PrinterSettings.MaximumPage = 999;
        doc.PrinterSettings.FromPage = 1;
        doc.PrinterSettings.ToPage = 999;

        using var printDlg = new PrintDialog { Document = doc };
        if (printDlg.ShowDialog(parent) == DialogResult.OK)
        {
            doc.PrintController = new StandardPrintController();
            doc.Print();
        }
    }

    public static void SendToPdf(Control parent, PrintDocument doc, string suggestedName)
    {
        using var saveDlg = new SaveFileDialog
        {
            Title = "Save as PDF",
            Filter = "PDF files (*.pdf)|*.pdf",
            FileName = $"{suggestedName}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        if (saveDlg.ShowDialog(parent) != DialogResult.OK) return;

        try
        {
            doc.PrinterSettings.PrinterName = "Microsoft Print to PDF";
            doc.PrinterSettings.PrintToFile = true;
            doc.PrinterSettings.PrintFileName = saveDlg.FileName;
            doc.PrintController = new StandardPrintController();
            doc.Print();
            MessageBox.Show($"Saved to {saveDlg.FileName}", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving PDF: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public static void ExportToExcel(Control parent, string leagueName, List<SpareListRow> data)
    {
        using var saveDlg = new SaveFileDialog
        {
            Title = "Export to Excel",
            Filter = "Excel files (*.csv)|*.csv|All Files (*.*)|*.*",
            FileName = $"{leagueName}_SpareList_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        if (saveDlg.ShowDialog(parent) != DialogResult.OK) return;

        try
        {
            using var writer = new StreamWriter(saveDlg.FileName);
            writer.WriteLine("Name,Phone,Email,Lot Number,Notes");

            foreach (var row in data)
            {
                writer.WriteLine($"\"{row.Name}\",\"{row.Phone}\",\"{row.Email}\",\"{row.LotNumber}\",\"{row.Notes}\"");
            }

            MessageBox.Show($"Exported to {saveDlg.FileName}", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error exporting to Excel: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public static void ExportToCsv(Control parent, string leagueName, List<SpareListRow> data)
    {
        using var saveDlg = new SaveFileDialog
        {
            Title = "Export to CSV",
            Filter = "CSV files (*.csv)|*.csv",
            FileName = $"{leagueName}_SpareList_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        if (saveDlg.ShowDialog(parent) != DialogResult.OK) return;

        try
        {
            using var writer = new StreamWriter(saveDlg.FileName);
            writer.WriteLine("Name,Phone,Email,Lot Number,Notes");

            foreach (var row in data)
            {
                writer.WriteLine($"\"{row.Name}\",\"{row.Phone}\",\"{row.Email}\",\"{row.LotNumber}\",\"{row.Notes}\"");
            }

            MessageBox.Show($"Exported to {saveDlg.FileName}", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error exporting to CSV: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

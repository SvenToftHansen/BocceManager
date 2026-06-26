using System.Drawing.Printing;

namespace BocceManager.Services;

/// <summary>
/// Unified print preview dialog used by all reports and print operations.
/// Provides consistent UI, export options, and print handling across the application.
/// </summary>
public static class PrintPreviewService
{
    /// <summary>
    /// Shows a print preview dialog with toolbar for printing, PDF export, and data export (Excel/CSV).
    /// </summary>
    /// <param name="parent">Parent control for dialog centering</param>
    /// <param name="doc">PrintDocument to preview</param>
    /// <param name="exportHeaders">Optional headers for Excel/CSV export</param>
    /// <param name="exportRows">Optional data rows for Excel/CSV export</param>
    public static void ShowPrintPreview(Control parent, PrintDocument doc, string[]? exportHeaders = null, List<string[]>? exportRows = null)
    {
        var printerList = PrinterSettings.InstalledPrinters.Cast<string>().ToList();
        if (printerList.Count > 0 &&
            !printerList.Any(p => p.Equals(doc.PrinterSettings.PrinterName, StringComparison.OrdinalIgnoreCase)))
        {
            doc.PrinterSettings.PrinterName =
                printerList.FirstOrDefault(p => p.Contains("PDF", StringComparison.OrdinalIgnoreCase))
                ?? printerList[0];
        }
        bool hasPrinter = printerList.Count > 0;

        var countCtrl = new PreviewPrintController();
        doc.PrintController = countCtrl;
        doc.Print();
        int totalPages = Math.Max(1, countCtrl.GetPreviewPageInfo().Length);

        using var form = new Form
        {
            Text = $"Print Preview  —  {doc.DocumentName}",
            WindowState = FormWindowState.Maximized,
            StartPosition = FormStartPosition.CenterParent,
            MinimumSize = new Size(700, 500),
            BackColor = Color.FromArgb(240, 240, 240)
        };

        var preview = new PrintPreviewControl
        {
            Dock = DockStyle.Fill,
            Document = doc,
            AutoZoom = true,
            BackColor = Color.FromArgb(240, 240, 240)
        };

        var toolbar = new ToolStrip
        {
            Dock = DockStyle.Top,
            BackColor = Color.FromArgb(50, 50, 50),
            GripStyle = ToolStripGripStyle.Hidden,
            Padding = new Padding(6, 3, 6, 3)
        };

        ToolStripButton Btn(string text, Color back) => new ToolStripButton(text)
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            BackColor = back,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Margin = new Padding(4, 0, 4, 0),
            AutoSize = true,
            AutoToolTip = false
        };

        var btnPrinter = Btn("🖨  Print to Printer", Color.FromArgb(0, 120, 215));
        var btnPdf = Btn("📄  Save as PDF...", Color.FromArgb(180, 50, 50));
        var btnExcel = Btn("📊  Export to Excel", Color.FromArgb(80, 150, 50));
        var btnCsv = Btn("📋  Export to CSV", Color.FromArgb(100, 140, 180));
        var btnPrev = Btn("◀  Prev", Color.FromArgb(70, 70, 80));
        var btnNext = Btn("Next  ▶", Color.FromArgb(70, 70, 80));
        var btnClose = Btn("✕  Close", Color.FromArgb(80, 80, 80));
        var lblPage = new ToolStripLabel($"Page 1 of {totalPages}")
        {
            ForeColor = Color.LightGray,
            Margin = new Padding(6, 0, 6, 0)
        };

        void UpdateNav()
        {
            int p = preview.StartPage;
            lblPage.Text = $"Page {p + 1} of {totalPages}";
            btnPrev.Enabled = p > 0;
            btnNext.Enabled = p < totalPages - 1;
        }

        btnPrinter.Enabled = hasPrinter;
        btnExcel.Enabled = exportHeaders != null && exportRows != null;
        btnCsv.Enabled = exportHeaders != null && exportRows != null;
        btnPrev.Enabled = false;
        btnNext.Enabled = totalPages > 1;
        btnClose.Alignment = ToolStripItemAlignment.Right;

        toolbar.Items.AddRange([
            btnPrinter,
            new ToolStripSeparator(),
            btnPdf,
            new ToolStripSeparator(),
            btnExcel,
            btnCsv,
            new ToolStripSeparator(),
            btnPrev,
            lblPage,
            btnNext,
            btnClose
        ]);
        form.Controls.Add(preview);
        form.Controls.Add(toolbar);

        btnPrinter.Click += (_, _) => SendToPrinter(parent, doc);
        btnPdf.Click += (_, _) => SendToPdf(parent, doc, doc.DocumentName);
        btnExcel.Click += (_, _) =>
        {
            if (exportHeaders != null && exportRows != null)
            {
                ReportExportService.ExportToExcel(parent, doc.DocumentName, exportHeaders, exportRows);
            }
        };
        btnCsv.Click += (_, _) =>
        {
            if (exportHeaders != null && exportRows != null)
            {
                ReportExportService.ExportToCsv(parent, doc.DocumentName, exportHeaders, exportRows);
            }
        };
        btnClose.Click += (_, _) => form.Close();
        btnPrev.Click += (_, _) => { preview.StartPage = Math.Max(0, preview.StartPage - 1); UpdateNav(); };
        btnNext.Click += (_, _) => { preview.StartPage = Math.Min(totalPages - 1, preview.StartPage + 1); UpdateNav(); };

        form.ShowDialog(parent);
    }

    private static void SendToPrinter(Control parent, PrintDocument doc)
    {
        doc.PrinterSettings.MinimumPage = 1;
        doc.PrinterSettings.MaximumPage = 999;
        doc.PrinterSettings.FromPage = 1;
        doc.PrinterSettings.ToPage = 999;
        using var dlg = new PrintDialog { Document = doc, UseEXDialog = true, AllowSomePages = true, AllowCurrentPage = true };
        if (dlg.ShowDialog(parent) == DialogResult.OK)
        {
            doc.PrintController = new StandardPrintController();
            doc.Print();
        }
    }

    private static void SendToPdf(Control parent, PrintDocument doc, string suggestedName)
    {
        using var saveDlg = new SaveFileDialog
        {
            Title = "Save as PDF",
            Filter = "PDF files (*.pdf)|*.pdf",
            FileName = suggestedName + ".pdf",
            DefaultExt = "pdf"
        };
        if (saveDlg.ShowDialog(parent) != DialogResult.OK) return;

        doc.PrinterSettings.PrinterName = "Microsoft Print to PDF";
        doc.PrinterSettings.PrintToFile = true;
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

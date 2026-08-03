using System.Drawing.Printing;

namespace BocceManager.Services;

// Headless variant of the "Microsoft Print to PDF" trick PrintPreviewService's interactive
// "Save as PDF" button uses — renders a PrintDocument straight to a file, no dialogs.
public static class PdfRenderService
{
    public static bool TryRenderToPdf(PrintDocument doc, string outputPath, out string? error)
    {
        var printerList = PrinterSettings.InstalledPrinters.Cast<string>().ToList();
        var pdfPrinter = printerList.FirstOrDefault(p => p.Equals("Microsoft Print to PDF", StringComparison.OrdinalIgnoreCase))
            ?? printerList.FirstOrDefault(p => p.Contains("PDF", StringComparison.OrdinalIgnoreCase));

        if (pdfPrinter == null)
        {
            error = "No PDF-capable printer (e.g. 'Microsoft Print to PDF') is installed on this computer.";
            return false;
        }

        doc.PrinterSettings.PrinterName = pdfPrinter;
        doc.PrinterSettings.PrintToFile = true;
        doc.PrinterSettings.PrintFileName = outputPath;

        try
        {
            doc.PrintController = new StandardPrintController();
            doc.Print();
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}

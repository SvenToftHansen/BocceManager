using ClosedXML.Excel;

namespace BocceManager.Services;

public static class ReportExportService
{
    public static void ExportToExcel(Control parent, string filename, string[] headers, List<string[]> rows)
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
            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Data");

            for (int col = 0; col < headers.Length; col++)
            {
                var cell = ws.Cell(1, col + 1);
                cell.Value = headers[col];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
                cell.Style.Font.FontColor = XLColor.White;
            }

            for (int row = 0; row < rows.Count; row++)
            {
                var rowData = rows[row];
                for (int col = 0; col < rowData.Length; col++)
                    ws.Cell(row + 2, col + 1).Value = rowData[col];
            }

            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(1);

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

    public static void ExportToCsv(Control parent, string filename, string[] headers, List<string[]> rows)
    {
        using var saveDlg = new SaveFileDialog
        {
            Title = "Export to CSV",
            Filter = "CSV files (*.csv)|*.csv",
            FileName = $"{filename}_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        if (saveDlg.ShowDialog(parent) != DialogResult.OK) return;

        try
        {
            using var writer = new StreamWriter(saveDlg.FileName);
            writer.WriteLine(string.Join(",", headers.Select(h => $"\"{h}\"")));

            foreach (var row in rows)
            {
                writer.WriteLine(string.Join(",", row.Select(v => $"\"{v}\"")));
            }

            MessageBox.Show($"Exported to {saveDlg.FileName}", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error exporting to CSV: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

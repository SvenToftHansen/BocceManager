namespace BocceManager.Services;

public static class ReportExportService
{
    public static void ExportToExcel(Control parent, string filename, string[] headers, List<string[]> rows)
    {
        using var saveDlg = new SaveFileDialog
        {
            Title = "Export to Excel",
            Filter = "Excel files (*.csv)|*.csv|All Files (*.*)|*.*",
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

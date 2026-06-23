using BocceManager.Data;
using BocceManager.Services;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Printing;

namespace BocceManager.Panels;

public class ReportSpareListPanel : UserControl
{
    private DataGridView _dgvSpareList = null!;
    private Label _lblMessage = null!;
    private Button _btnPrint = null!;
    private Button _btnExportCsv = null!;
    private Panel _toolbarPanel = null!;

    public ReportSpareListPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;
        BuildUi();
        LoadData();
    }

    private void BuildUi()
    {
        _toolbarPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 44,
            BackColor = AppTheme.Surface,
            Padding = new Padding(12, 8, 12, 8)
        };

        _btnPrint = new Button
        {
            Text = "🖨 Print",
            Location = new Point(12, 8),
            Size = new Size(100, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.Accent,
            ForeColor = Color.White,
            Font = AppTheme.FontButton,
            Cursor = Cursors.Hand
        };
        _btnPrint.FlatAppearance.BorderSize = 0;
        _btnPrint.Click += (_, _) => PrintReport();

        _btnExportCsv = new Button
        {
            Text = "💾 Export CSV",
            Location = new Point(120, 8),
            Size = new Size(110, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.Accent,
            ForeColor = Color.White,
            Font = AppTheme.FontButton,
            Cursor = Cursors.Hand
        };
        _btnExportCsv.FlatAppearance.BorderSize = 0;
        _btnExportCsv.Click += (_, _) => ExportToCsv();

        _toolbarPanel.Controls.AddRange([_btnPrint, _btnExportCsv]);
        Controls.Add(_toolbarPanel);

        _dgvSpareList = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToOrderColumns = false,
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
            BackgroundColor = AppTheme.ContentBackground,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.TextPrimary,
                Font = AppTheme.FontDefault,
                SelectionBackColor = AppTheme.Accent,
                SelectionForeColor = Color.White
            },
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppTheme.Separator,
                ForeColor = AppTheme.TextPrimary,
                Font = AppTheme.FontDefaultBold,
                Alignment = DataGridViewContentAlignment.MiddleLeft
            }
        };

        _dgvSpareList.Columns.AddRange([
            new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Name", Width = 250 },
            new DataGridViewTextBoxColumn { Name = "Phone", HeaderText = "Phone", Width = 150 },
            new DataGridViewTextBoxColumn { Name = "Email", HeaderText = "Email", Width = 200 },
            new DataGridViewTextBoxColumn { Name = "LotNumber", HeaderText = "Lot Number", Width = 100 }
        ]);

        _lblMessage = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = AppTheme.FontDefault,
            ForeColor = AppTheme.TextSecondary,
            Visible = false
        };

        Controls.Add(_dgvSpareList);
        Controls.Add(_lblMessage);
    }

    private void LoadData()
    {
        try
        {
            using var db = new BocceDbContext();
            var leagueId = AppParameterService.GetDefaultLeagueId(db);

            if (!leagueId.HasValue)
            {
                ShowMessage("No default league selected");
                return;
            }

            var spareListData = db.SpareLists
                .Where(s => s.LeagueId == leagueId.Value && s.IsActive)
                .Include(s => s.Player)
                .AsNoTracking()
                .OrderBy(s => s.Player.LastName)
                .ThenBy(s => s.Player.FirstName)
                .Select(s => new
                {
                    Name = s.Player.LastName + ", " + s.Player.FirstName,
                    Phone = s.Player.Phone ?? "",
                    Email = s.Player.Email ?? "",
                    LotNumber = s.Player.LotNumber ?? ""
                })
                .ToList();

            if (spareListData.Count == 0)
            {
                ShowMessage("No players on the spare list for the selected league");
                return;
            }

            _dgvSpareList.Rows.Clear();
            foreach (var player in spareListData)
            {
                _dgvSpareList.Rows.Add(player.Name, player.Phone, player.Email, player.LotNumber);
            }

            _dgvSpareList.Visible = true;
            _lblMessage.Visible = false;
        }
        catch (Exception ex)
        {
            ShowMessage($"Error loading spare list: {ex.Message}");
        }
    }

    private void ShowMessage(string message)
    {
        _lblMessage.Text = message;
        _dgvSpareList.Visible = false;
        _lblMessage.Visible = true;
    }

    private void PrintReport()
    {
        try
        {
            using var db = new BocceDbContext();
            var leagueId = AppParameterService.GetDefaultLeagueId(db);
            if (!leagueId.HasValue) return;

            var league = db.Leagues.FirstOrDefault(l => l.Id == leagueId.Value);
            var leagueName = league?.Name ?? "Spare List";

            using var doc = new PrintDocument();
            doc.DocumentName = $"{leagueName} - Spare List";
            doc.PrintPage += (_, e) => PrintPageContent(e, leagueName);

            var printDialog = new PrintDialog { Document = doc };
            if (printDialog.ShowDialog(this) == DialogResult.OK)
            {
                doc.Print();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error printing: {ex.Message}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void PrintPageContent(PrintPageEventArgs e, string leagueName)
    {
        var g = e.Graphics;
        float y = 40;
        var font = new Font("Consolas", 10);
        var fontBold = new Font("Consolas", 10, FontStyle.Bold);
        var fontTitle = new Font("Consolas", 14, FontStyle.Bold);

        // Title
        g.DrawString($"{leagueName} - Spare List", fontTitle, Brushes.Black, 40, y);
        y += 30;

        // Column headers
        g.DrawString("Name", fontBold, Brushes.Black, 40, y);
        g.DrawString("Phone", fontBold, Brushes.Black, 300, y);
        g.DrawString("Email", fontBold, Brushes.Black, 450, y);
        g.DrawString("Lot #", fontBold, Brushes.Black, 680, y);
        y += 20;

        g.DrawLine(Pens.Black, 40, y, 750, y);
        y += 10;

        // Data rows
        foreach (DataGridViewRow row in _dgvSpareList.Rows)
        {
            if (y > e.MarginBounds.Bottom - 40)
            {
                e.HasMorePages = true;
                return;
            }

            g.DrawString(row.Cells[0].Value?.ToString() ?? "", font, Brushes.Black, 40, y);
            g.DrawString(row.Cells[1].Value?.ToString() ?? "", font, Brushes.Black, 300, y);
            g.DrawString(row.Cells[2].Value?.ToString() ?? "", font, Brushes.Black, 450, y);
            g.DrawString(row.Cells[3].Value?.ToString() ?? "", font, Brushes.Black, 680, y);
            y += 20;
        }

        e.HasMorePages = false;
        font.Dispose();
        fontBold.Dispose();
        fontTitle.Dispose();
    }

    private void ExportToCsv()
    {
        if (_dgvSpareList.Rows.Count == 0)
        {
            MessageBox.Show("No data to export", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var saveDialog = new SaveFileDialog
        {
            Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            FileName = $"SpareList_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        if (saveDialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            using var writer = new StreamWriter(saveDialog.FileName);

            // Write headers
            var headers = new[] { "Name", "Phone", "Email", "Lot Number" };
            writer.WriteLine(string.Join(",", headers.Select(h => $"\"{h}\"")));

            // Write rows
            foreach (DataGridViewRow row in _dgvSpareList.Rows)
            {
                var values = new[]
                {
                    row.Cells[0].Value?.ToString() ?? "",
                    row.Cells[1].Value?.ToString() ?? "",
                    row.Cells[2].Value?.ToString() ?? "",
                    row.Cells[3].Value?.ToString() ?? ""
                };
                writer.WriteLine(string.Join(",", values.Select(v => $"\"{v}\"")));
            }

            MessageBox.Show($"Spare list exported to {saveDialog.FileName}", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error exporting: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

using System.Drawing.Printing;
using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.Services;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Panels;

public class PlayoffSchedulePanel : UserControl
{
    private int? _seasonId;

    private TabControl                  _tabs        = null!;
    private DataGridView                _grid        = null!;
    private BracketVisualizationControl _bracket     = null!;
    private Label                       _lblStatus   = null!;
    private readonly List<int>          _matchIdByRow = [];   // grid row → matchId

    public PlayoffSchedulePanel()
    {
        Dock      = DockStyle.Fill;
        BackColor = AppTheme.ContentBackground;
        BuildUi();
        AppParameterService.DefaultsChanged += OnDefaultsChanged;
        Load += (_, _) => LoadData();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) AppParameterService.DefaultsChanged -= OnDefaultsChanged;
        base.Dispose(disposing);
    }

    private void OnDefaultsChanged(object? sender, DefaultsChangedEventArgs e) =>
        BeginInvoke(LoadData);

    // ── UI ────────────────────────────────────────────────────────────────────

    private void BuildUi()
    {
        var toolbar = new Panel
        {
            Dock = DockStyle.Top, Height = 46, BackColor = AppTheme.Surface,
            Padding = new Padding(10, 8, 10, 0),
        };

        var btnRefresh = new Button
        {
            Text = "Refresh", Location = new Point(10, 8), Size = new Size(90, 30),
            Font = AppTheme.FontDefault,
            BackColor = Color.FromArgb(80, 100, 130), ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
        };
        btnRefresh.FlatAppearance.BorderSize = 0;
        btnRefresh.Click += (_, _) => LoadData();
        toolbar.Controls.Add(btnRefresh);

        var btnPrint = new Button
        {
            Text = "Print", Location = new Point(110, 8), Size = new Size(80, 30),
            Font = AppTheme.FontDefault,
            BackColor = Color.FromArgb(60, 100, 60), ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
        };
        btnPrint.FlatAppearance.BorderSize = 0;
        btnPrint.Click += OnPrint;
        toolbar.Controls.Add(btnPrint);

        _lblStatus = new Label
        {
            Location = new Point(200, 15), AutoSize = true,
            Font = AppTheme.FontSmall, ForeColor = AppTheme.TextMuted,
        };
        toolbar.Controls.Add(_lblStatus);

        _tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.FontDefault,
            Padding = new Point(12, 4),
        };

        // ── Tab 1: Text Schedule ──────────────────────────────────────────────
        var tabList = new TabPage("Schedule List") { BackColor = AppTheme.ContentBackground };

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.FontDefault,
            BackgroundColor = AppTheme.Surface,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight = 30,
            RowHeadersVisible = false,
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
            RowTemplate = { Height = 26 },
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        };

        _grid.Columns.Add(Col("Round",   "Round",      90));
        _grid.Columns.Add(Col("Game",    "Game #",     60));
        _grid.Columns.Add(Col("Top",     "Top Team",  160));
        _grid.Columns.Add(Col("Bottom",  "Bot Team",  160));
        _grid.Columns.Add(Col("Court",   "Court",      80));
        _grid.Columns.Add(Col("Date",    "Date",      110));
        _grid.Columns.Add(Col("Time",    "Time",       80));
        _grid.Columns.Add(Col("Score",   "Score",      80));
        _grid.Columns.Add(Col("Status",  "Status",     90));

        _grid.CellDoubleClick += OnGridDoubleClick;
        tabList.Controls.Add(_grid);

        // ── Tab 2: Bracket Visualization ──────────────────────────────────────
        var tabBracket = new TabPage("Bracket") { BackColor = AppTheme.ContentBackground };

        _bracket = new BracketVisualizationControl { Dock = DockStyle.Fill };
        _bracket.MatchClicked += OnBracketMatchClicked;
        tabBracket.Controls.Add(_bracket);

        _tabs.TabPages.Add(tabList);
        _tabs.TabPages.Add(tabBracket);

        Controls.Add(_tabs);
        Controls.Add(toolbar);
    }

    // ── Data ──────────────────────────────────────────────────────────────────

    private void LoadData()
    {
        using var db = new BocceDbContext();
        var seasonId = AppParameterService.GetDefaultSeasonId(db);
        _seasonId    = seasonId;

        if (!seasonId.HasValue)
        {
            _lblStatus.Text = "No default season.";
            return;
        }

        bool generated = db.PlayoffConfigs
            .Any(c => c.SeasonId == seasonId.Value && c.IsGenerated);

        if (!generated)
        {
            _lblStatus.Text = "Bracket not generated yet — use Playoff Setup.";
            _grid.Rows.Clear();
            return;
        }

        LoadGrid(db, seasonId.Value);
        _bracket.Load(seasonId.Value);
        _lblStatus.Text = "";
    }

    private void LoadGrid(BocceDbContext db, int seasonId)
    {
        var matches = db.PlayoffMatches
            .Include(m => m.PlayoffRound)
            .Include(m => m.Team1)
            .Include(m => m.Team2)
            .Include(m => m.Court)
            .Where(m => m.SeasonId == seasonId)
            .OrderBy(m => m.PlayoffRound!.RoundNumber)
            .ThenBy(m => m.BracketSlot)
            .ToList();

        var gameScores = db.PlayoffGames
            .Where(g => matches.Select(m => m.Id).Contains(g.PlayoffMatchId))
            .GroupBy(g => g.PlayoffMatchId)
            .ToDictionary(g => g.Key, g => (T1: g.Sum(x => x.Team1Score), T2: g.Sum(x => x.Team2Score)));

        _grid.Rows.Clear();
        _matchIdByRow.Clear();
        int gameNum = 1;
        foreach (var m in matches)
        {
            gameScores.TryGetValue(m.Id, out var sc);
            string score = sc.T1 > 0 || sc.T2 > 0 ? $"{sc.T1} – {sc.T2}" : "";
            string court = m.Court != null ? $"Court {m.Court.CourtNumber}" : "";
            string date  = m.ScheduledDate?.ToString("ddd MMM d") ?? "";
            string time  = m.ScheduledTime?.ToString("HHmm") ?? "";

            _grid.Rows.Add(
                m.PlayoffRound?.RoundName ?? $"Round {m.PlayoffRound?.RoundNumber}",
                gameNum++,
                m.Team1?.EffectiveDisplayName ?? "TBD",
                m.Team2?.EffectiveDisplayName ?? "TBD",
                court, date, time, score,
                m.Status == "completed" ? "Done" : "Pending");
            _matchIdByRow.Add(m.Id);
        }
    }

    // ── Score entry — bracket click or grid double-click ─────────────────────

    private void OnBracketMatchClicked(object? sender, int matchId) =>
        OpenScoreEntry(matchId);

    private void OnGridDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _matchIdByRow.Count) return;
        OpenScoreEntry(_matchIdByRow[e.RowIndex]);
    }

    private void OpenScoreEntry(int matchId)
    {
        using var popup = new ScoreEntryPopup(matchId);
        if (popup.ShowDialog(this) == DialogResult.OK)
            LoadData();
    }

    // ── Print ─────────────────────────────────────────────────────────────────

    private const float DocHdrH   = 36f;
    private const float PrintMargin = 25f;  // hundredths-of-inch, matches other reports

    private void OnPrint(object? sender, EventArgs e)
    {
        if (_seasonId == null) { _lblStatus.Text = "No season selected."; return; }

        string docHeader;
        using (var db = new BocceDbContext())
        {
            if (!db.PlayoffConfigs.Any(c => c.SeasonId == _seasonId.Value && c.IsGenerated))
            { _lblStatus.Text = "No bracket to print."; return; }

            var season   = db.Seasons.Include(s => s.League).FirstOrDefault(s => s.Id == _seasonId.Value);
            var clubName = AppParameterService.GetAppParameter(db, "ClubName") ?? "";
            docHeader = string.Join("  —  ",
                new[] { clubName, season?.League.Name, season?.Name, "Playoff Schedule" }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
        }

        var pd = new PrintDocument { DocumentName = "Playoff Schedule" };

        // Slim margins matching other reports
        pd.QueryPageSettings += (_, qe) =>
        {
            qe.PageSettings.Landscape = true;
            qe.PageSettings.Margins   = new Margins((int)PrintMargin, (int)PrintMargin,
                                                    (int)PrintMargin, (int)PrintMargin);
        };

        bool firstPage = true;
        pd.BeginPrint += (_, _) => firstPage = true;
        pd.PrintPage  += (_, pe) =>
        {
            var b = pe.MarginBounds;
            var g = pe.Graphics!;

            // Document header band on every page
            DrawPageHeader(g, b, docHeader);
            var content = new RectangleF(b.Left, b.Top + DocHdrH + 4, b.Width, b.Height - DocHdrH - 4);

            if (firstPage)
            {
                float tableH   = MeasureTableHeight();
                float bracketH = content.Height - tableH - 8;

                if (bracketH / content.Height >= 0.35f)
                {
                    // Both fit on one page
                    DrawTextTable(g, new RectangleF(content.Left, content.Top, content.Width, tableH));
                    _bracket.DrawTo(g, new RectangleF(content.Left, content.Top + tableH + 8,
                                                       content.Width, bracketH));
                    pe.HasMorePages = false;
                }
                else
                {
                    // Text page 1, bracket page 2
                    DrawTextTable(g, content);
                    pe.HasMorePages = true;
                }
                firstPage = false;
            }
            else
            {
                _bracket.DrawTo(g, content);
                pe.HasMorePages = false;
            }
        };

        PrintPreviewService.ShowPrintPreview(this, pd);
    }

    // Navy header band matching other reports (Consolas 11pt Bold, white text)
    private static void DrawPageHeader(Graphics g, RectangleF b, string text)
    {
        using var navyBr = new SolidBrush(AppTheme.NavHeader);
        g.FillRectangle(navyBr, b.Left, b.Top, b.Width, DocHdrH);
        using var hdrFont = new Font("Consolas", 11f, FontStyle.Bold);
        using var csf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(text, hdrFont, Brushes.White,
            new RectangleF(b.Left, b.Top, b.Width, DocHdrH), csf);
    }

    // Height of the schedule table (header + data rows, no title — header band replaces it)
    private float MeasureTableHeight() =>
        16f                          // column header row
        + _grid.Rows.Count * 15f     // data rows
        + 4f;                        // gap below

    // Compact text schedule table — no title (page header band carries the title)
    private void DrawTextTable(Graphics g, RectangleF bounds)
    {
        using var hdrFont  = new Font("Consolas",  7.5f, FontStyle.Bold);
        using var rowFont  = new Font("Consolas",  7.5f, FontStyle.Regular);
        using var colHdrBr = new SolidBrush(AppTheme.GridHeaderBackground);
        using var whiteBr  = new SolidBrush(Color.White);
        using var blackBr  = new SolidBrush(Color.Black);
        using var altBr    = new SolidBrush(Color.FromArgb(240, 244, 250));
        using var linePen  = new Pen(AppTheme.GridLines, 0.5f);
        using var lft      = new StringFormat { Alignment = StringAlignment.Near,
                                                LineAlignment = StringAlignment.Center,
                                                Trimming = StringTrimming.EllipsisCharacter };

        float[] widths   = [95, 38, 130, 130, 55, 72, 48, 60, 56];
        string[] headers = ["Round", "Game", "Top Team", "Bot Team", "Court", "Date", "Time", "Score", "Status"];
        float rowH = 15f;
        float y = bounds.Top;

        // Column headers
        float x = bounds.Left;
        for (int i = 0; i < headers.Length; i++)
        {
            g.FillRectangle(colHdrBr, x, y, widths[i], rowH);
            g.DrawString(headers[i], hdrFont, whiteBr, new RectangleF(x + 2, y, widths[i] - 4, rowH), lft);
            x += widths[i];
        }
        y += rowH;

        // Data rows
        for (int ri = 0; ri < _grid.Rows.Count; ri++)
        {
            if (y + rowH > bounds.Bottom) break;
            var row = _grid.Rows[ri];
            if (ri % 2 == 1) g.FillRectangle(altBr, bounds.Left, y, widths.Sum(), rowH);
            x = bounds.Left;
            for (int ci = 0; ci < widths.Length; ci++)
            {
                string val = row.Cells[ci].Value?.ToString() ?? "";
                g.DrawString(val, rowFont, blackBr, new RectangleF(x + 2, y, widths[ci] - 4, rowH), lft);
                x += widths[ci];
            }
            g.DrawLine(linePen, bounds.Left, y + rowH, bounds.Left + widths.Sum(), y + rowH);
            y += rowH;
        }
    }

    // ── Grid column helper ────────────────────────────────────────────────────

    private static DataGridViewTextBoxColumn Col(string name, string header, int width) => new()
    {
        Name = name, HeaderText = header, Width = width, ReadOnly = true,
    };
}

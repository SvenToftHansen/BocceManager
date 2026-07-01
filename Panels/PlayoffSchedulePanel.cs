using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.Services;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Panels;

public class PlayoffSchedulePanel : UserControl
{
    private int? _seasonId;

    private TabControl                _tabs    = null!;
    private DataGridView              _grid    = null!;
    private BracketVisualizationControl _bracket = null!;
    private Label                     _lblStatus = null!;

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

        _lblStatus = new Label
        {
            Location = new Point(112, 15), AutoSize = true,
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
        int gameNum = 1;
        foreach (var m in matches)
        {
            gameScores.TryGetValue(m.Id, out var sc);
            string score = sc.T1 > 0 || sc.T2 > 0 ? $"{sc.T1} – {sc.T2}" : "";
            string court = m.Court != null ? $"Court {m.Court.CourtNumber}" : "";
            string date  = m.ScheduledDate?.ToString("ddd MMM d") ?? "";
            string time  = m.ScheduledTime?.ToString("h:mm tt") ?? "";

            _grid.Rows.Add(
                m.PlayoffRound?.RoundName ?? $"Round {m.PlayoffRound?.RoundNumber}",
                gameNum++,
                m.Team1?.EffectiveDisplayName ?? "TBD",
                m.Team2?.EffectiveDisplayName ?? "TBD",
                court, date, time, score,
                m.Status == "completed" ? "Done" : "Pending");
        }
    }

    // ── Score entry via bracket click ─────────────────────────────────────────

    private void OnBracketMatchClicked(object? sender, int matchId)
    {
        using (var popup = new ScoreEntryPopup(matchId))
        {
            if (popup.ShowDialog(this) == DialogResult.OK)
                LoadData(); // refresh both tabs
        }
    }

    // ── Grid column helper ────────────────────────────────────────────────────

    private static DataGridViewTextBoxColumn Col(string name, string header, int width) => new()
    {
        Name = name, HeaderText = header, Width = width, ReadOnly = true,
    };
}

using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.Services;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Panels;

public class StandingsPanel : UserControl
{
    private int? _seasonId;
    private TabControl _tabs   = null!;
    private Label      _lblStatus = null!;

    public StandingsPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;
        BuildUi();
        AppParameterService.DefaultsChanged += OnDefaultsChanged;
        LoadData();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            AppParameterService.DefaultsChanged -= OnDefaultsChanged;
        base.Dispose(disposing);
    }

    private void OnDefaultsChanged(object? sender, DefaultsChangedEventArgs e) => LoadData();

    // ── UI ────────────────────────────────────────────────────────────────────

    private void BuildUi()
    {
        var toolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 48,
            BackColor = AppTheme.Surface,
            Padding = new Padding(8, 0, 8, 0)
        };
        toolbar.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.Separator);
            e.Graphics.DrawLine(pen, 0, toolbar.Height - 1, toolbar.Width, toolbar.Height - 1);
        };

        var btnPrint = MakeBtn("Print", Color.FromArgb(60, 100, 160), 8);
        btnPrint.Click += (_, _) => PrintCurrentTab();
        toolbar.Controls.Add(btnPrint);

        _lblStatus = new Label
        {
            AutoSize = true,
            Font = AppTheme.FontDefault,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(102, 15)
        };
        toolbar.Controls.Add(_lblStatus);

        _tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.FontDefault,
            Padding = new Point(12, 4)
        };

        Controls.Add(_tabs);
        Controls.Add(toolbar);
    }

    private static Button MakeBtn(string text, Color bg, int x) => new()
    {
        Text = text, Font = AppTheme.FontDefault,
        Height = 30, Width = 80, Location = new Point(x, 9),
        BackColor = bg, ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
        FlatAppearance = { BorderSize = 0 }
    };

    // ── Data ──────────────────────────────────────────────────────────────────

    private void LoadData()
    {
        _tabs.TabPages.Clear();
        _lblStatus.Text = "";

        try
        {
            using var db = new BocceDbContext();
            _seasonId = AppParameterService.GetDefaultSeasonId(db);

            if (!_seasonId.HasValue) { _lblStatus.Text = "No season selected."; return; }

            var season = db.Seasons.Find(_seasonId.Value);
            if (season == null) { _lblStatus.Text = "Season not found."; return; }

            var allRows = db.Standings
                .Where(s => s.SeasonId == _seasonId.Value)
                .OrderBy(s => s.DivisionId).ThenBy(s => s.DivisionRank).ThenBy(s => s.DivisionSeed)
                .ToList();

            if (allRows.Count == 0) { _lblStatus.Text = "No scores entered yet."; return; }

            bool isGamesMode = season.ScoringMode == "games_mode";

            var divIds = allRows.Select(r => r.DivisionId).Distinct().ToList();
            var divNames = db.Divisions
                .Where(d => divIds.Contains(d.Id))
                .OrderBy(d => d.Name)
                .ToDictionary(d => d.Id, d => d.Name);

            // One tab per division
            foreach (var (divId, divName) in divNames)
            {
                var rows = allRows.Where(r => r.DivisionId == divId).ToList();
                if (rows.Count == 0) continue;

                var page = MakePage(divName);
                var grid = BuildDivisionGrid(rows, isGamesMode);
                grid.Dock = DockStyle.Fill;
                page.Controls.Add(grid);
                _tabs.TabPages.Add(page);
            }

            // Season Seed tab — all teams ranked by playoff eligibility order
            var seasonRows = allRows.OrderBy(r => r.SeasonSeed).ToList();
            var seedPage = MakePage("Season Seed");
            var seedGrid = BuildSeasonSeedGrid(seasonRows, divNames, season.TeamsInPlayoffs);
            seedGrid.Dock = DockStyle.Fill;
            seedPage.Controls.Add(seedGrid);
            _tabs.TabPages.Add(seedPage);
        }
        catch (Exception ex)
        {
            _lblStatus.Text = $"Error: {ex.Message}";
        }
    }

    private static TabPage MakePage(string title) => new(title)
    {
        BackColor = AppTheme.ContentBackground,
        Padding   = new Padding(0)
    };

    // ── Division standings grid ───────────────────────────────────────────────

    private static DataGridView BuildDivisionGrid(List<StandingView> rows, bool isGamesMode)
    {
        var grid = MakeGrid();
        bool h2hUsed = rows.Any(r => r.H2HPlusMinus != 0 || r.H2HWins != 0);

        grid.Columns.AddRange(
            Col("Rank", "#",    40, mid: true, tip: "Division rank (DENSE_RANK — tied teams share a number)"),
            Col("Seed", "Seed", 44, mid: true, tip: "Division seed for bracket (always unique)"),
            Col("Team", "Team", 200),
            Col("GP",   isGamesMode ? "GP" : "MP", 40, mid: true, tip: isGamesMode ? "Games Played" : "Matches Played"),
            Col("W",    "W",    36, mid: true, tip: "Wins"),
            Col("T",    "T",    36, mid: true, tip: "Ties"),
            Col("L",    "L",    36, mid: true, tip: "Non-forfeit losses"),
            Col("F",    "F",    36, mid: true, tip: "Forfeit losses"),
            Col("Pts",  "Pts",  48, mid: true, tip: "Standings points"),
            Col("PM",   "+/-",  52, mid: true, tip: "Plus/Minus")
        );

        if (h2hUsed)
        {
            grid.Columns.Add(Col("H2HPM", "H2H+/-", 58, mid: true, tip: "Head-to-head plus/minus vs tied opponents"));
            grid.Columns.Add(Col("H2HW",  "H2HW",   50, mid: true, tip: "Head-to-head wins vs tied opponents"));
        }

        foreach (var r in rows)
        {
            var vals = new List<object?>
            {
                r.DivisionRank,
                r.DivisionSeed,
                r.TeamName,
                isGamesMode ? r.GamesPlayed : r.MatchesPlayed,
                r.Wins, r.Ties, r.Losses, r.Forfeits,
                r.StandingsPoints,
                PmStr(r.PlusMinus)
            };
            if (h2hUsed) { vals.Add(PmStr(r.H2HPlusMinus)); vals.Add(r.H2HWins); }

            int idx = grid.Rows.Add(vals.Cast<object>().ToArray());
            ApplyRowStyle(grid.Rows[idx], idx);

            // Bold the 1st-place row
            if (r.DivisionRank == 1)
                grid.Rows[idx].DefaultCellStyle.Font = AppTheme.FontDefaultBold;
        }

        return grid;
    }

    // ── Season seed grid ─────────────────────────────────────────────────────

    private static DataGridView BuildSeasonSeedGrid(
        List<StandingView> rows, Dictionary<int, string> divNames, int teamsInPlayoffs)
    {
        var grid = MakeGrid();

        grid.Columns.AddRange(
            Col("Seed",  "Seed",     50,  mid: true, tip: "Season seed — playoff order"),
            Col("Team",  "Team",     180),
            Col("Div",   "Division", 120, tip: "Division"),
            Col("DivR",  "Div #",    48,  mid: true, tip: "Division rank"),
            Col("Pts",   "Pts",      48,  mid: true, tip: "Standings points"),
            Col("PM",    "+/-",      52,  mid: true, tip: "Plus/Minus"),
            Col("W",     "W",        36,  mid: true, tip: "Wins"),
            Col("T",     "T",        36,  mid: true, tip: "Ties"),
            Col("L",     "L",        36,  mid: true, tip: "Losses"),
            Col("F",     "F",        36,  mid: true, tip: "Forfeits")
        );

        for (int i = 0; i < rows.Count; i++)
        {
            var r   = rows[i];
            int idx = grid.Rows.Add(
                r.SeasonSeed,
                r.TeamName,
                divNames.TryGetValue(r.DivisionId, out var dn) ? dn : "?",
                r.DivisionRank,
                r.StandingsPoints,
                PmStr(r.PlusMinus),
                r.Wins, r.Ties, r.Losses, r.Forfeits
            );

            ApplyRowStyle(grid.Rows[idx], i);

            // Highlight the playoff cut line — bold teams that qualify
            bool qualifies = teamsInPlayoffs > 0 && r.SeasonSeed <= teamsInPlayoffs;
            if (qualifies)
                grid.Rows[idx].DefaultCellStyle.Font = AppTheme.FontDefaultBold;

            // Faint separator after the last qualifying team
            if (teamsInPlayoffs > 0 && r.SeasonSeed == teamsInPlayoffs)
                grid.Rows[idx].DividerHeight = 2;
        }

        return grid;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static DataGridView MakeGrid() => new()
    {
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        RowHeadersVisible = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
        BackgroundColor = AppTheme.ContentBackground,
        GridColor = AppTheme.Separator,
        BorderStyle = BorderStyle.None,
        Font = AppTheme.FontDefault,
        ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.FontDefaultBold,
            Padding = new Padding(4, 0, 4, 0)
        },
        DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = AppTheme.ContentBackground,
            ForeColor = AppTheme.TextPrimary,
            Padding = new Padding(4, 2, 4, 2),
            SelectionBackColor = AppTheme.NavSelected,
            SelectionForeColor = Color.White
        },
        RowTemplate = { Height = 26 },
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
        ColumnHeadersHeight = 28
    };

    private static DataGridViewTextBoxColumn Col(
        string name, string header, int width,
        bool mid = false, string? tip = null) => new()
    {
        Name = name,
        HeaderText = header,
        Width = width,
        ReadOnly = true,
        SortMode = DataGridViewColumnSortMode.NotSortable,
        DefaultCellStyle  = new DataGridViewCellStyle { Alignment = mid ? DataGridViewContentAlignment.MiddleCenter : DataGridViewContentAlignment.MiddleLeft },
        HeaderCell = { Style = { Alignment = mid ? DataGridViewContentAlignment.MiddleCenter : DataGridViewContentAlignment.MiddleLeft }, ToolTipText = tip ?? header }
    };

    private static void ApplyRowStyle(DataGridViewRow row, int index)
    {
        if (index % 2 == 1)
            row.DefaultCellStyle.BackColor = Color.FromArgb(245, 247, 250);
    }

    private static string PmStr(int v) => v >= 0 ? $"+{v}" : $"{v}";

    private void PrintCurrentTab()
    {
        // TODO: implement print preview for standings
        MessageBox.Show("Print not yet implemented.", "Standings", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}

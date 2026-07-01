using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.Services;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Panels;

public class PlayoffSetupPanel : UserControl
{
    // ── State ─────────────────────────────────────────────────────────────────

    private int?          _seasonId;
    private PlayoffConfig? _config;
    private List<(int Id, string Name)> _seasonTeams = [];

    // ── Controls ──────────────────────────────────────────────────────────────

    private NumericUpDown _numMatchDuration = null!;
    private ComboBox      _cboDisplayMode   = null!;
    private Label         _lblByeCount      = null!;
    private Label         _lblTeamCount     = null!;
    private DataGridView  _gridSeeding      = null!;
    private DataGridView  _gridDays         = null!;
    private Label         _lblPreview       = null!;
    private Button        _btnGenerate      = null!;
    private Label         _lblStatus        = null!;

    public PlayoffSetupPanel()
    {
        Dock        = DockStyle.Fill;
        BackColor   = AppTheme.ContentBackground;
        AutoScroll  = true;

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
        var scroll = new Panel
        {
            Dock = DockStyle.Fill, AutoScroll = true, BackColor = AppTheme.ContentBackground
        };
        Controls.Add(scroll);

        var inner = new Panel
        {
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding  = new Padding(20), BackColor = AppTheme.ContentBackground
        };
        scroll.Controls.Add(inner);

        int y = 0;

        // ── Section: Parameters ───────────────────────────────────────────────
        y = AddSectionHeader(inner, "Playoff Parameters", y);

        AddLabel(inner, "Teams in Playoffs:", 0, y);
        _lblTeamCount = AddReadonlyLabel(inner, "—", 200, y);
        AddLabel(inner, "Byes:", 370, y);
        _lblByeCount = AddReadonlyLabel(inner, "—", 430, y);
        y += 34;

        AddLabel(inner, "Match Duration (mins):", 0, y);
        _numMatchDuration = new NumericUpDown
        {
            Location = new Point(200, y), Size = new Size(80, 28),
            Minimum = 30, Maximum = 240, Value = 90,
            Font = AppTheme.FontDefault, TextAlign = HorizontalAlignment.Center,
        };
        _numMatchDuration.ValueChanged += (_, _) => RefreshPreview();
        inner.Controls.Add(_numMatchDuration);
        y += 34;

        AddLabel(inner, "Display Mode:", 0, y);
        _cboDisplayMode = new ComboBox
        {
            Location = new Point(200, y), Size = new Size(160, 28),
            Font = AppTheme.FontDefault, DropDownStyle = ComboBoxStyle.DropDownList,
        };
        _cboDisplayMode.Items.AddRange(["Scale to Fit", "Scroll"]);
        _cboDisplayMode.SelectedIndex = 0;
        inner.Controls.Add(_cboDisplayMode);
        y += 44;

        // ── Section: Team Seeding ─────────────────────────────────────────────
        y = AddSectionHeader(inner, "Team Seeding", y);

        _gridSeeding = MakeGrid(inner, 0, y, 520, 200);
        _gridSeeding.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Seed", Name = "Seed", Width = 60, ReadOnly = true,
        });
        var teamCol = new DataGridViewComboBoxColumn
        {
            HeaderText = "Team", Name = "Team", Width = 440, DisplayStyleForCurrentCellOnly = true,
        };
        _gridSeeding.Columns.Add(teamCol);
        _gridSeeding.CellValueChanged   += OnSeedingChanged;
        _gridSeeding.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_gridSeeding.IsCurrentCellDirty) _gridSeeding.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };
        y += 210;

        var btnReset = new Button
        {
            Text = "Reset from Standings", Location = new Point(0, y),
            Size = new Size(180, 28), Font = AppTheme.FontDefault,
            BackColor = Color.FromArgb(100, 110, 120), ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
        };
        btnReset.FlatAppearance.BorderSize = 0;
        btnReset.Click += OnResetFromStandings;
        inner.Controls.Add(btnReset);
        y += 38;

        // ── Section: Day Parameters ───────────────────────────────────────────
        y = AddSectionHeader(inner, "Day / Round Parameters", y);

        _gridDays = MakeGrid(inner, 0, y, 680, 160);
        _gridDays.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Day", Name = "Day", Width = 40, ReadOnly = true });
        _gridDays.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Date (yyyy-mm-dd)", Name = "Date", Width = 140 });
        _gridDays.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Start", Name = "Start", Width = 80 });
        _gridDays.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "End", Name = "End", Width = 80 });
        _gridDays.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Gap (mins)", Name = "Gap", Width = 90 });
        _gridDays.CellValueChanged += (_, _) => RefreshPreview();
        y += 170;

        // ── Section: Schedule Preview ─────────────────────────────────────────
        y = AddSectionHeader(inner, "Schedule Preview", y);

        _lblPreview = new Label
        {
            Location = new Point(0, y), AutoSize = true,
            Font = AppTheme.FontDefault, ForeColor = AppTheme.TextSecondary,
            MaximumSize = new Size(700, 0),
        };
        inner.Controls.Add(_lblPreview);
        y += 60;

        // ── Actions ───────────────────────────────────────────────────────────
        var btnSave = new Button
        {
            Text = "Save Config", Location = new Point(0, y),
            Size = new Size(120, 32), Font = AppTheme.FontDefault,
            BackColor = AppTheme.ButtonSuccess, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += OnSaveConfig;
        inner.Controls.Add(btnSave);

        _btnGenerate = new Button
        {
            Text = "Generate Bracket", Location = new Point(134, y),
            Size = new Size(150, 32), Font = AppTheme.FontDefault,
            BackColor = Color.FromArgb(60, 110, 180), ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
        };
        _btnGenerate.FlatAppearance.BorderSize = 0;
        _btnGenerate.Click += OnGenerateBracket;
        inner.Controls.Add(_btnGenerate);

        _lblStatus = new Label
        {
            Location = new Point(0, y + 40), AutoSize = true,
            Font = AppTheme.FontSmall, ForeColor = AppTheme.TextMuted,
        };
        inner.Controls.Add(_lblStatus);
    }

    // ── Data loading ──────────────────────────────────────────────────────────

    private void LoadData()
    {
        using var db  = new BocceDbContext();
        var seasonId  = AppParameterService.GetDefaultSeasonId(db);
        _seasonId     = seasonId;

        if (!seasonId.HasValue)
        {
            _lblStatus.Text = "No default season selected.";
            return;
        }

        var season = db.Seasons.Find(seasonId.Value);
        if (season == null) return;

        int teamCount = season.TeamsInPlayoffs;
        _lblTeamCount.Text = teamCount.ToString();
        _lblByeCount.Text  = PlayoffService.GetByeCount(teamCount).ToString();

        // Load or create config
        _config = db.PlayoffConfigs
            .Include(c => c.DayParams)
            .FirstOrDefault(c => c.SeasonId == seasonId.Value);

        if (_config == null)
        {
            _config = new PlayoffConfig { SeasonId = seasonId.Value };
            db.PlayoffConfigs.Add(_config);
            db.SaveChanges();
            _config = db.PlayoffConfigs.Include(c => c.DayParams)
                .First(c => c.SeasonId == seasonId.Value);
        }

        _numMatchDuration.Value        = _config.MatchDurationMins;
        _cboDisplayMode.SelectedIndex  = _config.DisplayMode == "Scroll" ? 1 : 0;

        // Load teams
        _seasonTeams = db.Teams
            .Include(t => t.Division)
            .Where(t => t.Division.SeasonId == seasonId.Value && t.IsActive)
            .OrderBy(t => t.SortOrder)
            .Select(t => new { t.Id, Name = t.EffectiveDisplayName })
            .AsEnumerable()
            .Select(t => (t.Id, t.Name))
            .ToList();

        PopulateSeedingGrid(db, seasonId.Value, teamCount);
        PopulateDayGrid(teamCount);
        RefreshPreview();

        _lblStatus.Text = _config.IsGenerated ? "Bracket already generated." : "";
    }

    private void PopulateSeedingGrid(BocceDbContext db, int seasonId, int teamCount)
    {
        // Refresh team dropdown
        var teamCol = (DataGridViewComboBoxColumn)_gridSeeding.Columns["Team"]!;
        teamCol.Items.Clear();
        teamCol.Items.Add("");
        foreach (var t in _seasonTeams) teamCol.Items.Add(t.Name);

        // Prefer saved seedings; fall back to standings SeasonSeed order
        var savedSeeds = db.PlayoffSeedings
            .Where(s => s.SeasonId == seasonId)
            .OrderBy(s => s.Seed)
            .ToList();

        List<(int Seed, int TeamId)> seedMap;
        if (savedSeeds.Count > 0)
        {
            seedMap = savedSeeds.Select(s => (s.Seed, s.TeamId)).ToList();
        }
        else
        {
            // Auto-populate from standings view (SeasonSeed = cross-division rank)
            seedMap = db.Standings
                .Where(s => s.SeasonId == seasonId)
                .OrderBy(s => s.SeasonSeed)
                .Take(teamCount)
                .AsEnumerable()
                .Select((s, i) => (Seed: i + 1, s.TeamId))
                .ToList();
        }

        _gridSeeding.Rows.Clear();
        for (int seed = 1; seed <= teamCount; seed++)
        {
            var entry    = seedMap.FirstOrDefault(s => s.Seed == seed);
            string name  = entry.TeamId > 0
                ? (_seasonTeams.FirstOrDefault(t => t.Id == entry.TeamId).Name ?? "")
                : "";
            _gridSeeding.Rows.Add(seed.ToString(), name);
        }
    }

    private void OnResetFromStandings(object? sender, EventArgs e)
    {
        if (_seasonId == null) return;
        using var db     = new BocceDbContext();
        var season       = db.Seasons.Find(_seasonId.Value);
        if (season == null) return;
        int teamCount    = season.TeamsInPlayoffs;

        var standings = db.Standings
            .Where(s => s.SeasonId == _seasonId.Value)
            .OrderBy(s => s.SeasonSeed)
            .Take(teamCount)
            .AsEnumerable()
            .Select((s, i) => (Seed: i + 1, s.TeamId))
            .ToList();

        for (int i = 0; i < _gridSeeding.Rows.Count; i++)
        {
            var entry = standings.FirstOrDefault(s => s.Seed == i + 1);
            string name = entry.TeamId > 0
                ? (_seasonTeams.FirstOrDefault(t => t.Id == entry.TeamId).Name ?? "")
                : "";
            _gridSeeding.Rows[i].Cells["Team"].Value = name;
        }

        _lblStatus.Text = "Seedings reloaded from standings.";
    }

    private void PopulateDayGrid(int teamCount)
    {
        int totalRounds = PlayoffService.GetRoundCount(teamCount);

        // Ensure enough day rows for all rounds (estimate 2 rounds per day)
        int daysNeeded = (int)Math.Ceiling(totalRounds / 2.0);

        _gridDays.Rows.Clear();

        var existing = _config?.DayParams.OrderBy(d => d.DayNumber).ToList() ?? [];

        for (int d = 1; d <= Math.Max(daysNeeded, existing.Count); d++)
        {
            var dp = existing.FirstOrDefault(x => x.DayNumber == d);
            _gridDays.Rows.Add(
                d.ToString(),
                dp?.GameDate.ToString("yyyy-MM-dd") ?? "",
                dp?.StartTime.ToString("HH:mm") ?? "08:30",
                dp?.EndTime.ToString("HH:mm")   ?? "18:00",
                dp?.DurationBetweenRoundsMins.ToString() ?? "30"
            );
        }
    }

    private void RefreshPreview()
    {
        if (_seasonId == null) return;

        using var db = new BocceDbContext();
        var season   = db.Seasons.Find(_seasonId.Value);
        if (season == null) return;

        int teamCount = season.TeamsInPlayoffs;
        if (teamCount < 2) { _lblPreview.Text = "No playoff teams configured."; return; }

        var dayParams = ParseDayGrid();
        if (!dayParams.Any()) { _lblPreview.Text = "Enter day parameters above."; return; }

        using var db2 = new BocceDbContext();
        int courtCount = db2.SeasonCourts.Count(sc => sc.SeasonId == _seasonId.Value);

        var schedule = PlayoffService.ComputeRoundSchedule(
            teamCount, (int)_numMatchDuration.Value, dayParams, Math.Max(1, courtCount));

        var lines = schedule
            .GroupBy(s => s.Date == DateOnly.MinValue ? "Unscheduled" : $"Day {dayParams.FirstOrDefault(d => d.GameDate == s.Date)?.DayNumber} — {s.Date:ddd MMM d}")
            .Select(g => $"{g.Key}: {string.Join(", ", g.Select(r => r.Name))}");

        _lblPreview.Text = string.Join("\n", lines);
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void OnSeedingChanged(object? sender, DataGridViewCellEventArgs e) { }

    private void OnSaveConfig(object? sender, EventArgs e)
    {
        if (_seasonId == null || _config == null) return;

        try
        {
            using var db = new BocceDbContext();

            var cfg = db.PlayoffConfigs.Include(c => c.DayParams)
                .First(c => c.Id == _config.Id);

            cfg.MatchDurationMins = (int)_numMatchDuration.Value;
            cfg.DisplayMode       = _cboDisplayMode.SelectedIndex == 1 ? "Scroll" : "ScaleToFit";

            // Save day params
            db.PlayoffDayParams.RemoveRange(cfg.DayParams);
            foreach (var dp in ParseDayGrid())
                cfg.DayParams.Add(dp);

            // Save seedings
            db.PlayoffSeedings.Where(s => s.SeasonId == _seasonId.Value).ExecuteDelete();
            foreach (DataGridViewRow row in _gridSeeding.Rows)
            {
                if (!int.TryParse(row.Cells["Seed"].Value?.ToString(), out int seed)) continue;
                string? teamName = row.Cells["Team"].Value?.ToString();
                if (string.IsNullOrWhiteSpace(teamName)) continue;
                var team = _seasonTeams.FirstOrDefault(t => t.Name == teamName);
                if (team.Id == 0) continue;
                db.PlayoffSeedings.Add(new PlayoffSeeding
                {
                    SeasonId = _seasonId.Value, Seed = seed, TeamId = team.Id
                });
            }

            db.SaveChanges();
            _lblStatus.Text = "Config saved.";
            RefreshPreview();
        }
        catch (Exception ex)
        {
            _lblStatus.Text = $"Error: {ex.Message}";
            AppLogger.Error(ex, "PlayoffSetupPanel.OnSaveConfig");
        }
    }

    private void OnGenerateBracket(object? sender, EventArgs e)
    {
        if (_seasonId == null) return;

        var confirm = MessageBox.Show(
            "This will delete any existing playoff bracket and scores. Continue?",
            "Generate Bracket", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes) return;

        try
        {
            OnSaveConfig(null, EventArgs.Empty); // save first
            using var db = new BocceDbContext();
            PlayoffService.GenerateBracket(db, _seasonId.Value);
            _lblStatus.Text = "Bracket generated successfully.";
        }
        catch (Exception ex)
        {
            _lblStatus.Text = $"Error: {ex.Message}";
            AppLogger.Error(ex, "PlayoffSetupPanel.OnGenerateBracket");
        }
    }

    // ── Parsing helpers ───────────────────────────────────────────────────────

    private List<PlayoffDayParams> ParseDayGrid()
    {
        var result = new List<PlayoffDayParams>();
        foreach (DataGridViewRow row in _gridDays.Rows)
        {
            if (!int.TryParse(row.Cells["Day"].Value?.ToString(), out int dayNum)) continue;
            if (!DateOnly.TryParse(row.Cells["Date"].Value?.ToString(), out var date)) continue;
            if (!TimeOnly.TryParse(row.Cells["Start"].Value?.ToString(), out var start)) continue;
            if (!TimeOnly.TryParse(row.Cells["End"].Value?.ToString(), out var end)) continue;
            if (!int.TryParse(row.Cells["Gap"].Value?.ToString(), out int gap)) gap = 30;

            result.Add(new PlayoffDayParams
            {
                PlayoffConfigId           = _config?.Id ?? 0,
                DayNumber                 = dayNum,
                GameDate                  = date,
                StartTime                 = start,
                EndTime                   = end,
                DurationBetweenRoundsMins = gap,
            });
        }
        return result;
    }

    // ── UI helpers ────────────────────────────────────────────────────────────

    private static int AddSectionHeader(Control parent, string text, int y)
    {
        parent.Controls.Add(new Label
        {
            Text = text, Location = new Point(0, y), AutoSize = true,
            Font = AppTheme.FontDefaultBold, ForeColor = AppTheme.TextPrimary,
        });
        parent.Controls.Add(new Panel
        {
            Location = new Point(0, y + 22), Size = new Size(700, 1),
            BackColor = AppTheme.Separator,
        });
        return y + 34;
    }

    private static void AddLabel(Control parent, string text, int x, int y)
    {
        parent.Controls.Add(new Label
        {
            Text = text, Location = new Point(x, y + 4), AutoSize = true,
            Font = AppTheme.FontDefault, ForeColor = AppTheme.TextSecondary,
        });
    }

    private static Label AddReadonlyLabel(Control parent, string text, int x, int y)
    {
        var lbl = new Label
        {
            Text = text, Location = new Point(x, y + 4), AutoSize = true,
            Font = AppTheme.FontDefaultBold, ForeColor = AppTheme.TextPrimary,
        };
        parent.Controls.Add(lbl);
        return lbl;
    }

    private static DataGridView MakeGrid(Control parent, int x, int y, int w, int h)
    {
        var grid = new DataGridView
        {
            Location = new Point(x, y), Size = new Size(w, h),
            Font = AppTheme.FontDefault,
            BackgroundColor = AppTheme.Surface,
            BorderStyle = BorderStyle.FixedSingle,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight = 28,
            RowHeadersVisible = false,
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
            RowTemplate = { Height = 26 },
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.CellSelect,
        };
        parent.Controls.Add(grid);
        return grid;
    }
}

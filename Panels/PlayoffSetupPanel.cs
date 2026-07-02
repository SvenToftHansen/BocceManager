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

    private TabControl    _tabs             = null!;
    private NumericUpDown _numTiebreakerBalls = null!;
    private ComboBox      _cboDisplayMode     = null!;
    private Label         _lblByeCount      = null!;
    private Label         _lblTeamCount     = null!;
    private DataGridView  _gridSeeding      = null!;
    private DataGridView  _gridDays         = null!;
    private Label         _lblPreview       = null!;
    private Button        _btnGenerate      = null!;
    private Label         _lblStatus        = null!;
    private FlowLayoutPanel _pnlCourts      = null!;

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
        // Main layout: tabs on top, buttons on bottom
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2,
            Padding = Padding.Empty, Margin = Padding.Empty,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));

        // Build tabs
        _tabs = new TabControl
        {
            Dock = DockStyle.Fill, Font = AppTheme.FontDefault, Padding = new Point(10, 6)
        };
        _tabs.TabPages.Add(BuildConfigTab());
        _tabs.TabPages.Add(BuildSeedingTab());
        _tabs.TabPages.Add(BuildDaysTab());
        _tabs.TabPages.Add(BuildPreviewTab());
        layout.Controls.Add(_tabs, 0, 0);

        // Bottom toolbar
        var toolbar = new Panel
        {
            Dock = DockStyle.Fill, BackColor = AppTheme.Surface, Padding = new Padding(12, 10, 12, 10)
        };

        var btnSave = new Button
        {
            Text = "Save Config", Location = new Point(0, 0),
            Size = new Size(120, 32), Font = AppTheme.FontDefault,
            BackColor = AppTheme.ButtonSuccess, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += OnSaveConfig;
        toolbar.Controls.Add(btnSave);

        _btnGenerate = new Button
        {
            Text = "Generate Bracket", Location = new Point(134, 0),
            Size = new Size(150, 32), Font = AppTheme.FontDefault,
            BackColor = Color.FromArgb(60, 110, 180), ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
        };
        _btnGenerate.FlatAppearance.BorderSize = 0;
        _btnGenerate.Click += OnGenerateBracket;
        toolbar.Controls.Add(_btnGenerate);

        _lblStatus = new Label
        {
            Location = new Point(300, 8), AutoSize = true,
            Font = AppTheme.FontSmall, ForeColor = AppTheme.TextMuted,
        };
        toolbar.Controls.Add(_lblStatus);

        layout.Controls.Add(toolbar, 0, 1);
        Controls.Add(layout);
    }

    private TabPage BuildConfigTab()
    {
        var page = new TabPage("  Configuration  ") { BackColor = AppTheme.ContentBackground };
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = AppTheme.ContentBackground };

        var inner = new Panel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(20), BackColor = AppTheme.ContentBackground };
        scroll.Controls.Add(inner);
        page.Controls.Add(scroll);

        int y = 0;

        // Playoff Parameters
        y = AddSectionHeader(inner, "Playoff Parameters", y);

        AddLabel(inner, "Teams in Playoffs:", 0, y);
        _lblTeamCount = AddReadonlyLabel(inner, "—", 200, y);
        AddLabel(inner, "Byes:", 370, y);
        _lblByeCount = AddReadonlyLabel(inner, "—", 430, y);
        y += 34;

        AddLabel(inner, "Tiebreaker Balls:", 0, y);
        _numTiebreakerBalls = new NumericUpDown
        {
            Location = new Point(200, y), Size = new Size(80, 28),
            Minimum = 1, Maximum = 20, Value = 1,
            Font = AppTheme.FontDefault, TextAlign = HorizontalAlignment.Center,
        };
        inner.Controls.Add(_numTiebreakerBalls);
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

        // Playoff Courts
        y = AddSectionHeader(inner, "Playoff Courts", y);

        _pnlCourts = new FlowLayoutPanel
        {
            Location = new Point(0, y),
            Size = new Size(680, 36),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = AppTheme.ContentBackground,
        };
        inner.Controls.Add(_pnlCourts);

        return page;
    }

    private TabPage BuildSeedingTab()
    {
        var page = new TabPage("  Team Seeding  ") { BackColor = AppTheme.ContentBackground };
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = AppTheme.ContentBackground };

        var inner = new Panel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(20), BackColor = AppTheme.ContentBackground };
        scroll.Controls.Add(inner);
        page.Controls.Add(scroll);

        int y = 0;

        y = AddSectionHeader(inner, "Team Seeding", y);

        _gridSeeding = MakeGrid(inner, 0, y, 520, 300);
        _gridSeeding.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Seed", Name = "Seed", Width = 60, ReadOnly = true });
        var teamCol = new DataGridViewComboBoxColumn { HeaderText = "Team", Name = "Team", Width = 440, DisplayStyleForCurrentCellOnly = true };
        _gridSeeding.Columns.Add(teamCol);
        _gridSeeding.CellValueChanged += OnSeedingChanged;
        _gridSeeding.CurrentCellDirtyStateChanged += (_, _) => {
            if (_gridSeeding.IsCurrentCellDirty) _gridSeeding.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };
        y += 310;

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

        return page;
    }

    private TabPage BuildDaysTab()
    {
        var page = new TabPage("  Day / Round Parameters  ") { BackColor = AppTheme.ContentBackground };
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = AppTheme.ContentBackground };

        var inner = new Panel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(20), BackColor = AppTheme.ContentBackground };
        scroll.Controls.Add(inner);
        page.Controls.Add(scroll);

        int y = 0;

        y = AddSectionHeader(inner, "Day / Round Parameters", y);

        _gridDays = MakeGrid(inner, 0, y, 680, 200);
        _gridDays.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Day", Name = "Day", Width = 40, ReadOnly = true });
        _gridDays.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Date (yyyy-mm-dd)", Name = "Date", Width = 140 });
        _gridDays.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Start", Name = "Start", Width = 80 });
        _gridDays.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "End", Name = "End", Width = 80 });
        _gridDays.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Match Length (mins)", Name = "Gap", Width = 130 });
        _gridDays.CellValueChanged += (_, _) => RefreshPreview();
        y += 210;

        var btnAddDay = new Button
        {
            Text = "+ Add Day", Location = new Point(0, y),
            Size = new Size(100, 26), Font = AppTheme.FontDefault,
            BackColor = Color.FromArgb(80, 100, 120), ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
        };
        btnAddDay.FlatAppearance.BorderSize = 0;
        btnAddDay.Click += (_, _) => AddDayRow();
        inner.Controls.Add(btnAddDay);

        var btnDeleteDay = new Button
        {
            Text = "- Delete Day", Location = new Point(110, y),
            Size = new Size(100, 26), Font = AppTheme.FontDefault,
            BackColor = AppTheme.ButtonDanger, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
        };
        btnDeleteDay.FlatAppearance.BorderSize = 0;
        btnDeleteDay.Click += (_, _) => DeleteDayRow();
        inner.Controls.Add(btnDeleteDay);

        return page;
    }

    private TabPage BuildPreviewTab()
    {
        var page = new TabPage("  Schedule Preview  ") { BackColor = AppTheme.ContentBackground };
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = AppTheme.ContentBackground };

        var inner = new Panel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(20), BackColor = AppTheme.ContentBackground };
        scroll.Controls.Add(inner);
        page.Controls.Add(scroll);

        int y = 0;

        y = AddSectionHeader(inner, "Schedule Preview", y);

        _lblPreview = new Label
        {
            Location = new Point(0, y), AutoSize = true,
            Font = AppTheme.FontDefault, ForeColor = AppTheme.TextSecondary,
            MaximumSize = new Size(700, 0),
        };
        inner.Controls.Add(_lblPreview);

        return page;
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

        _numTiebreakerBalls.Value      = Clamp(_config.TiebreakerBalls,     _numTiebreakerBalls.Minimum, _numTiebreakerBalls.Maximum);
        _cboDisplayMode.SelectedIndex  = _config.DisplayMode == "Scroll" ? 1 : 0;

        // Load teams
        _seasonTeams = db.Teams
            .Include(t => t.Division).ThenInclude(d => d.DaySlot)
            .Include(t => t.Division).ThenInclude(d => d.TimeSlot)
            .Where(t => t.Division.SeasonId == seasonId.Value && t.IsActive)
            .OrderBy(t => t.SortOrder)
            .AsEnumerable()
            .Select(t =>
            {
                string slot = string.Join(" ", new[]
                {
                    t.Division.DaySlot?.DayAbbr,
                    t.Division.TimeSlot?.Timeslot12h,
                }.Where(s => !string.IsNullOrEmpty(s)));
                string label = t.EffectiveDisplayName
                             + (slot.Length > 0 ? $"    {slot}" : "");
                return (t.Id, Name: label);
            })
            .ToList();

        PopulateSeedingGrid(db, seasonId.Value, teamCount);
        PopulateDayGrid(teamCount, season.PlayoffStartDate);
        PopulateCourtCheckboxes(db);
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

    private void PopulateDayGrid(int teamCount, DateOnly? playoffStartDate)
    {
        int totalRounds = PlayoffService.GetRoundCount(teamCount);
        // Default: one row per round (worst case, user can consolidate rounds onto one day)
        int daysDefault = totalRounds;

        _gridDays.Rows.Clear();

        var existing = _config?.DayParams.OrderBy(d => d.DayNumber).ToList() ?? [];

        for (int d = 1; d <= Math.Max(daysDefault, existing.Count); d++)
        {
            var dp = existing.FirstOrDefault(x => x.DayNumber == d);

            string dateStr = dp?.GameDate.ToString("yyyy-MM-dd")
                ?? (playoffStartDate.HasValue
                    ? playoffStartDate.Value.AddDays(d - 1).ToString("yyyy-MM-dd")
                    : "");

            _gridDays.Rows.Add(
                d.ToString(),
                dateStr,
                dp?.StartTime.ToString("HHmm") ?? "0830",
                dp?.EndTime.ToString("HHmm")   ?? "1800",
                dp?.MatchLengthMins.ToString() ?? "120"
            );
        }
    }

    private void AddDayRow()
    {
        int nextDay = _gridDays.Rows.Count + 1;

        // Default date = last row's date + 1 day
        string dateStr = "";
        if (_gridDays.Rows.Count > 0)
        {
            string? lastDate = _gridDays.Rows[_gridDays.Rows.Count - 1].Cells["Date"].Value?.ToString();
            if (DateOnly.TryParse(lastDate, out var d))
                dateStr = d.AddDays(1).ToString("yyyy-MM-dd");
        }

        _gridDays.Rows.Add(nextDay.ToString(), dateStr, "0830", "1800", "120");
        RefreshPreview();
    }

    private void DeleteDayRow()
    {
        if (_gridDays.SelectedRows.Count == 0)
        {
            MessageBox.Show("Select a day to delete.", "Delete Day", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        int selectedIndex = _gridDays.SelectedRows[0].Index;
        if (selectedIndex >= 0 && selectedIndex < _gridDays.Rows.Count)
        {
            _gridDays.Rows.RemoveAt(selectedIndex);
            // Renumber remaining days
            for (int i = 0; i < _gridDays.Rows.Count; i++)
                _gridDays.Rows[i].Cells["Day"].Value = (i + 1).ToString();
            RefreshPreview();
        }
    }

    private void PopulateCourtCheckboxes(BocceDbContext db)
    {
        _pnlCourts.Controls.Clear();

        // All active courts available in the system
        var allCourts = db.Courts
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ToList();

        // Courts already selected for this playoff
        var selected = _config == null
            ? new HashSet<int>()
            : db.PlayoffCourts
                .Where(pc => pc.PlayoffConfigId == _config.Id)
                .Select(pc => pc.CourtId)
                .ToHashSet();

        foreach (var court in allCourts)
        {
            var chk = new CheckBox
            {
                Text      = $"Court {court.CourtNumber}",
                Tag       = court.Id,
                Checked   = selected.Contains(court.Id),
                Font      = AppTheme.FontDefault,
                ForeColor = AppTheme.TextPrimary,
                AutoSize  = true,
                Margin    = new Padding(0, 4, 16, 4),
            };
            _pnlCourts.Controls.Add(chk);
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
        // Use checked playoff courts; fall back to season courts if none checked yet
        int courtCount = _pnlCourts.Controls.OfType<CheckBox>().Count(c => c.Checked);
        if (courtCount == 0)
            courtCount = db2.SeasonCourts.Count(sc => sc.SeasonId == _seasonId.Value);

        var schedule = PlayoffService.ComputeRoundSchedule(
            teamCount, 0, dayParams, Math.Max(1, courtCount));

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

            cfg.TiebreakerBalls    = (int)_numTiebreakerBalls.Value;
            cfg.DisplayMode        = _cboDisplayMode.SelectedIndex == 1 ? "Scroll" : "ScaleToFit";

            // Save day params
            db.PlayoffDayParams.RemoveRange(cfg.DayParams);
            foreach (var dp in ParseDayGrid())
                cfg.DayParams.Add(dp);

            // Save playoff court selections
            db.PlayoffCourts.Where(pc => pc.PlayoffConfigId == _config.Id).ExecuteDelete();
            int sortOrder = 0;
            foreach (Control ctrl in _pnlCourts.Controls)
            {
                if (ctrl is CheckBox chk && chk.Checked && chk.Tag is int courtId)
                    db.PlayoffCourts.Add(new PlayoffCourt
                    {
                        PlayoffConfigId = _config.Id, CourtId = courtId, SortOrder = sortOrder++
                    });
            }

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
            // Reload from DB first — picks up any Season changes (e.g. TeamsInPlayoffs)
            // made since this panel was last loaded, then save and generate.
            LoadData();

            if (_config == null || _seasonId == null)
            { _lblStatus.Text = "No playoff config found."; return; }

            OnSaveConfig(null, EventArgs.Empty); // save current grid values

            using var db = new BocceDbContext();
            PlayoffService.GenerateBracket(db, _seasonId.Value);

            // Reload display to reflect generated state
            LoadData();
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
            if (!ParseHhmm(row.Cells["Start"].Value?.ToString(), out var start)) continue;
            if (!ParseHhmm(row.Cells["End"].Value?.ToString(),   out var end))   continue;
            if (!int.TryParse(row.Cells["Gap"].Value?.ToString(), out int gap)) gap = 30;

            result.Add(new PlayoffDayParams
            {
                PlayoffConfigId           = _config?.Id ?? 0,
                DayNumber                 = dayNum,
                GameDate                  = date,
                StartTime                 = start,
                EndTime                   = end,
                MatchLengthMins = gap,
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

    private static decimal Clamp(int value, decimal min, decimal max) =>
        Math.Max(min, Math.Min(max, value));

    // Accepts 4-digit no-colon format (2000, 0830, 830) or standard HH:mm.
    private static bool ParseHhmm(string? s, out TimeOnly t)
    {
        t = default;
        if (string.IsNullOrWhiteSpace(s)) return false;
        s = s.Trim();
        if (!s.Contains(':') && int.TryParse(s.PadLeft(4, '0'), out int hhmm))
        {
            int h = hhmm / 100, m = hhmm % 100;
            if (h is >= 0 and <= 23 && m is >= 0 and <= 59) { t = new TimeOnly(h, m); return true; }
            return false;
        }
        return TimeOnly.TryParse(s, out t);
    }
}

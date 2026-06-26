using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.Services;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Panels;

public class LookingForTeamPanel : UserControl
{
    // ── State ──────────────────────────────────────────────────────────────────
    private int? _leagueId;
    private int? _seasonId;
    private int? _selectedLftId;
    private int? _selectedGroupId;
    private bool _isLoadingData;
    private bool _isDirty;

    // ── Left — grid ────────────────────────────────────────────────────────────
    private DataGridView _grid          = null!;
    private ComboBox     _cmbDisplayMode = null!;

    // ── Right — detail ─────────────────────────────────────────────────────────
    private Label          _lblPlacedAs   = null!;
    private CheckedListBox _clbPrefDays   = null!;
    private CheckedListBox _clbPrefTimes  = null!;
    private ComboBox       _cmbPrefTeam   = null!;
    private TextBox        _txtNotes      = null!;
    private DataGridView   _grpGrid       = null!;
    private Button         _btnAddMember  = null!;
    private Button         _btnRemMember  = null!;
    private Button         _btnRenameGrp  = null!;
    private Button         _btnDeleteGrp  = null!;

    // ── Toolbar buttons ────────────────────────────────────────────────────────
    private Button _btnAddPlayer = null!;
    private Button _btnRemove    = null!;
    private Button _btnSave      = null!;
    private Button _btnCancel    = null!;

    private SplitContainer _mainSplit     = null!;
    private const int      PreferredLeftW = 550;

    // ── Constructor ────────────────────────────────────────────────────────────
    public LookingForTeamPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;
        BuildUi();
        AppParameterService.DefaultsChanged += OnDefaultsChanged;
        LoadContext();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            AppParameterService.DefaultsChanged -= OnDefaultsChanged;
        base.Dispose(disposing);
    }

    private void OnDefaultsChanged(object? sender, DefaultsChangedEventArgs e) => LoadContext();

    private void LoadContext()
    {
        _isLoadingData = true;
        try
        {
            using var db = new BocceDbContext();
            _leagueId = AppParameterService.GetDefaultLeagueId(db);
            _seasonId = AppParameterService.GetDefaultSeasonId(db);
        }
        catch
        {
            _leagueId = null;
            _seasonId = null;
        }
        finally
        {
            LoadDayTimeData();
            LoadPreferredTeamCombo();
            LoadGrid();
            _isLoadingData = false;
        }
    }

    // ── UI Construction ────────────────────────────────────────────────────────
    private void BuildUi()
    {
        var mainTabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.FontDefault
        };

        // Tab 1: "Looking for Placement" - grid + detail
        var tabLfp = new TabPage("Looking for Placement");
        var tab1Panel = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.ContentBackground };

        _mainSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.ContentBackground,
            Panel1MinSize = 0, Panel2MinSize = 0
        };

        // Left panel: grid + toolbar at bottom
        var leftPanel = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.ContentBackground };
        leftPanel.Controls.Add(BuildGrid());
        leftPanel.Controls.Add(BuildLeftToolbar());

        _mainSplit.Panel1.Controls.Add(leftPanel);
        _mainSplit.Panel2.Controls.Add(BuildDetailPanel());
        _mainSplit.SizeChanged   += (_, _) => SafeApplySplit();
        _mainSplit.HandleCreated += (_, _) => BeginInvoke(new Action(SafeApplySplit));

        tab1Panel.Controls.Add(_mainSplit);
        tabLfp.Controls.Add(tab1Panel);

        // Tab 2: "Placement" - placeholder
        var tabPlacement = new TabPage("Placement");
        var placeholderLbl = new Label
        {
            Text = "Placement workflow - Coming soon", Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = AppTheme.FontDefault, ForeColor = AppTheme.TextMuted
        };
        tabPlacement.Controls.Add(placeholderLbl);

        mainTabs.TabPages.AddRange([tabLfp, tabPlacement]);
        Controls.Add(mainTabs);
    }

    private Control BuildLeftToolbar()
    {
        var toolbar = new Panel { Dock = DockStyle.Bottom, Height = 46, BackColor = AppTheme.Surface };

        _btnAddPlayer = new Button
        {
            Text = "+ Add Player to LFT", Location = new Point(12, 8), Size = new Size(165, 30),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.ButtonSuccess, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 }
        };
        _btnAddPlayer.Click += (_, _) => AddPlayerToLft();

        _btnRemove = new Button
        {
            Text = "Remove", Location = new Point(189, 8), Size = new Size(90, 30),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.ButtonDanger, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 }, Enabled = false
        };
        _btnRemove.Click += (_, _) => RemoveFromLft();

        var modeLbl = new Label
        {
            Text = "Mode:", Location = new Point(291, 14), AutoSize = true,
            Font = AppTheme.FontDefault, ForeColor = AppTheme.TextSecondary
        };

        _cmbDisplayMode = new ComboBox
        {
            Location = new Point(337, 11), Size = new Size(200, 26),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = AppTheme.FontDefault, BackColor = AppTheme.Surface, ForeColor = AppTheme.TextPrimary
        };
        _cmbDisplayMode.Items.AddRange(["GROUPS", "ALL PLAYERS"]);
        _cmbDisplayMode.SelectedIndex = 0;
        _cmbDisplayMode.SelectedIndexChanged += (_, _) => LoadGrid();

        toolbar.Controls.AddRange([_btnAddPlayer, _btnRemove, modeLbl, _cmbDisplayMode]);
        return toolbar;
    }

    private void SafeApplySplit()
    {
        if (_mainSplit.Width <= 1) return;
        const int leftMin = 350, rightMin = 250;
        int total = Math.Max(0, _mainSplit.Width - 1);
        int lMin = leftMin, rMin = rightMin;
        if (lMin + rMin > total)
        {
            double ratio = leftMin / (double)(leftMin + rightMin);
            lMin = (int)Math.Floor(total * ratio);
            rMin = total - lMin;
        }
        _mainSplit.Panel1MinSize = lMin;
        _mainSplit.Panel2MinSize = rMin;
        int maxLeft = _mainSplit.Width - rMin;
        int clamped = Math.Max(lMin, Math.Min(PreferredLeftW, maxLeft));
        if (clamped > 0) _mainSplit.SplitterDistance = clamped;
    }

    private Control BuildGrid()
    {
        var pnl = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.ContentBackground };

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false, ReadOnly = true,
            AllowUserToAddRows = false, RowHeadersVisible = false, AllowUserToResizeRows = false,
            BorderStyle = BorderStyle.None,
            BackgroundColor = AppTheme.ContentBackground,
            Font = AppTheme.FontDefault, RowTemplate = { Height = 28 },
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            EnableHeadersVisualStyles = false,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppTheme.GridHeaderBackground, ForeColor = AppTheme.GridHeaderText,
                SelectionBackColor = AppTheme.GridHeaderBackground, SelectionForeColor = AppTheme.GridHeaderText,
                Font = AppTheme.FontGridHeader
            }
        };
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "GId",    Visible = false });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "GPid",   Visible = false });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "GGrpId", Visible = false });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "GName",  HeaderText = "Name",  FillWeight = 35 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "GPhone", HeaderText = "Phone", FillWeight = 20 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "GEmail", HeaderText = "Email", FillWeight = 30 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "GGrp",   HeaderText = "Group", FillWeight = 15 });
        _grid.SelectionChanged += OnGridSelectionChanged;

        // Add grid first, then label — WinForms docks Top after Fill in reverse z-order
        pnl.Controls.Add(_grid);
        pnl.Controls.Add(new Label
        {
            Text = "Players List", Dock = DockStyle.Top, Height = 22, Padding = new Padding(8, 4, 0, 0),
            Font = AppTheme.FontDefault, ForeColor = AppTheme.TextSecondary
        });
        return pnl;
    }

    private Control BuildDetailPanel()
    {
        var outer = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.ContentBackground, Padding = new Padding(8) };
        var scrollPane = new Panel { Dock = DockStyle.Fill, AutoScroll = true };

        int y = 0, contentWidth = 300;

        // Placed As label
        _lblPlacedAs = new Label
        {
            Location = new Point(0, y), AutoSize = true,
            Font = AppTheme.FontDefault, ForeColor = AppTheme.TextSecondary, Visible = false
        };
        scrollPane.Controls.Add(_lblPlacedAs);
        y += 25;

        // Group Members label
        scrollPane.Controls.Add(Lbl("Group Members", 0, y));
        y += 20;

        // Group grid - fills width, 18px per row for max 5 players (~100px height)
        _grpGrid = new DataGridView
        {
            Location = new Point(0, y), Size = new Size(contentWidth, 100),
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false, ReadOnly = true,
            AllowUserToAddRows = false, RowHeadersVisible = false,
            BorderStyle = BorderStyle.FixedSingle,
            BackgroundColor = AppTheme.ContentBackground,
            Font = AppTheme.FontDefault, RowTemplate = { Height = 18 },
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            EnableHeadersVisualStyles = false,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppTheme.GridHeaderBackground, ForeColor = AppTheme.GridHeaderText,
                SelectionBackColor = AppTheme.GridHeaderBackground, SelectionForeColor = AppTheme.GridHeaderText,
                Font = AppTheme.FontGridHeader
            }
        };
        _grpGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "GLftId", Visible = false });
        _grpGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "GName",  HeaderText = "Member", FillWeight = 50 });
        _grpGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "GPhone", HeaderText = "Phone",  FillWeight = 25 });
        _grpGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "GEmail", HeaderText = "Email",  FillWeight = 25 });
        _grpGrid.SelectionChanged += (_, _) =>
            _btnRemMember.Enabled = _grpGrid.SelectedRows.Count > 0;
        scrollPane.Controls.Add(_grpGrid);
        y += 110;

        // Group buttons
        var grpBtnPanel = new Panel { Location = new Point(0, y), Size = new Size(contentWidth, 28) };
        _btnAddMember = new Button
        {
            Text = "+ Add", Left = 0, Top = 0, Width = 70, Height = 22,
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.ButtonSuccess, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 }, Enabled = false
        };
        _btnAddMember.Click += (_, _) => AddMember();

        _btnRemMember = new Button
        {
            Text = "Remove", Left = 75, Top = 0, Width = 70, Height = 22,
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.ButtonDanger, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 }, Enabled = false
        };
        _btnRemMember.Click += (_, _) => RemoveMember();

        _btnRenameGrp = new Button
        {
            Text = "Rename", Left = 150, Top = 0, Width = 70, Height = 22,
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.Accent, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 }, Enabled = false
        };
        _btnRenameGrp.Click += (_, _) => RenameGroup();

        _btnDeleteGrp = new Button
        {
            Text = "Delete", Left = 225, Top = 0, Width = 70, Height = 22,
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.ButtonDanger, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 }, Enabled = false
        };
        _btnDeleteGrp.Click += (_, _) => DeleteGroup();

        grpBtnPanel.Controls.AddRange([_btnAddMember, _btnRemMember, _btnRenameGrp, _btnDeleteGrp]);
        scrollPane.Controls.Add(grpBtnPanel);
        y += 35;

        // Preferred Days label above
        scrollPane.Controls.Add(Lbl("Preferred Days", 0, y));
        y += 18;

        // Days and Times on same row
        _clbPrefDays = new CheckedListBox
        {
            Location = new Point(0, y), Size = new Size(contentWidth / 2 - 5, 80),
            CheckOnClick = true,
            Font = AppTheme.FontDefault, BackColor = AppTheme.Surface, ForeColor = AppTheme.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle
        };
        _clbPrefDays.ItemCheck += (_, _) => BeginInvoke(() => { if (!_isLoadingData) OnFieldChanged(null, EventArgs.Empty); });
        scrollPane.Controls.Add(_clbPrefDays);

        // Preferred Times label above
        scrollPane.Controls.Add(Lbl("Preferred Times", contentWidth / 2 + 5, y - 18));

        _clbPrefTimes = new CheckedListBox
        {
            Location = new Point(contentWidth / 2 + 5, y), Size = new Size(contentWidth / 2 - 5, 80),
            CheckOnClick = true,
            Font = AppTheme.FontDefault, BackColor = AppTheme.Surface, ForeColor = AppTheme.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle
        };
        _clbPrefTimes.ItemCheck += (_, _) => BeginInvoke(() => { if (!_isLoadingData) OnFieldChanged(null, EventArgs.Empty); });
        scrollPane.Controls.Add(_clbPrefTimes);
        y += 95;

        // Preferred Team label above
        scrollPane.Controls.Add(Lbl("Preferred Team", 0, y));
        y += 18;

        _cmbPrefTeam = new ComboBox
        {
            Location = new Point(0, y), Size = new Size(contentWidth, 24),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = AppTheme.FontDefault, BackColor = AppTheme.Surface, ForeColor = AppTheme.TextPrimary
        };
        _cmbPrefTeam.SelectedIndexChanged += OnFieldChanged;
        scrollPane.Controls.Add(_cmbPrefTeam);
        y += 32;

        // Notes label above
        scrollPane.Controls.Add(Lbl("Notes", 0, y));
        y += 18;

        _txtNotes = new TextBox
        {
            Location = new Point(0, y), Size = new Size(contentWidth, 40),
            Multiline = true, ScrollBars = ScrollBars.None,
            Font = AppTheme.FontDefault, BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary, BorderStyle = BorderStyle.FixedSingle
        };
        _txtNotes.TextChanged += OnFieldChanged;
        scrollPane.Controls.Add(_txtNotes);
        y += 50;

        // Save/Cancel buttons
        var btnBar = new Panel { Location = new Point(0, y), Size = new Size(150, 30) };
        _btnSave = new Button
        {
            Text = "Save", Location = new Point(0, 0), Size = new Size(70, 24),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.Accent, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 }, Enabled = false
        };
        _btnSave.Click += (_, _) => SaveEntry();

        _btnCancel = new Button
        {
            Text = "Cancel", Location = new Point(75, 0), Size = new Size(70, 24),
            FlatStyle = FlatStyle.Flat, Font = AppTheme.FontButton, Cursor = Cursors.Hand,
            FlatAppearance = { BorderSize = 1 }, Visible = false
        };
        _btnCancel.Click += (_, _) => CancelEdit();
        btnBar.Controls.AddRange([_btnSave, _btnCancel]);
        scrollPane.Controls.Add(btnBar);

        outer.Controls.Add(scrollPane);
        return outer;
    }

    // ── Data loading ───────────────────────────────────────────────────────────
    private void LoadDayTimeData()
    {
        var (days, times) = GetFilteredDayTimeData();

        _clbPrefDays.Items.Clear();
        foreach (var d in days)
            _clbPrefDays.Items.Add(new DayItem(d.Id, d.DayName, d.DayAbbr));

        _clbPrefTimes.Items.Clear();
        foreach (var t in times)
            _clbPrefTimes.Items.Add(new TimeItem(t.Id, t.Timeslot12h, t.Timeslot24h));
    }

    /// <summary>
    /// Gets the filtered list of days and times actually used in active divisions of the current season.
    /// If no season is selected, returns all active days/times as fallback.
    /// </summary>
    private (List<DaySlot> Days, List<TimeSlot> Times) GetFilteredDayTimeData()
    {
        try
        {
            using var db = new BocceDbContext();

            // If no season selected, return all active days/times
            if (!_seasonId.HasValue)
            {
                var allDays = db.DaySlots
                    .Where(d => d.IsActive)
                    .OrderBy(d => d.DayNbr)
                    .ToList();
                var allTimes = db.TimeSlots
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.SortOrder ?? 999)
                    .ToList();
                return (allDays, allTimes);
            }

            // Get distinct DaySlotIds and TimeSlotIds from active divisions in this season
            var activeDivisions = db.Divisions
                .Where(div => div.SeasonId == _seasonId.Value && div.IsActive)
                .ToList();

            var usedDayIds = activeDivisions
                .Where(div => div.DaySlotId.HasValue)
                .Select(div => div.DaySlotId!.Value)
                .Distinct()
                .ToHashSet();

            var usedTimeIds = activeDivisions
                .Where(div => div.TimeSlotId.HasValue)
                .Select(div => div.TimeSlotId!.Value)
                .Distinct()
                .ToHashSet();

            // Load only DaySlots and TimeSlots that are used
            var days = db.DaySlots
                .Where(d => usedDayIds.Contains(d.Id))
                .OrderBy(d => d.DayNbr)
                .ToList();

            var times = db.TimeSlots
                .Where(t => usedTimeIds.Contains(t.Id))
                .OrderBy(t => t.SortOrder ?? 999)
                .ToList();

            return (days, times);
        }
        catch
        {
            return (new List<DaySlot>(), new List<TimeSlot>());
        }
    }

    private void LoadPreferredTeamCombo()
    {
        _cmbPrefTeam.Items.Clear();
        _cmbPrefTeam.Items.Add(new TeamItem(null, "(any team)"));
        if (!_seasonId.HasValue) { _cmbPrefTeam.SelectedIndex = 0; return; }
        try
        {
            using var db = new BocceDbContext();
            var teams = db.Teams
                .Include(t => t.Division)
                .Include(t => t.TeamPlayers)
                .Where(t => t.Division.SeasonId == _seasonId.Value && t.IsActive)
                .OrderBy(t => t.Division.SortName).ThenBy(t => t.SortOrder)
                .AsEnumerable()
                .ToList();
            foreach (var t in teams)
            {
                string dayTime = ExtractDayTime(t.SystemName);
                string display = $"{t.TeamLetter} — {t.EffectiveDisplayName} ({t.TeamPlayers.Count}) {dayTime}".Trim();
                _cmbPrefTeam.Items.Add(new TeamItem(t.Id, display));
            }
        }
        catch { }
        _cmbPrefTeam.SelectedIndex = 0;
    }

    private static string ExtractDayTime(string systemName)
    {
        if (string.IsNullOrEmpty(systemName)) return "";
        var parts = systemName.Split('-');
        if (parts.Length >= 3)
        {
            string day = parts[1].ToUpper();
            string time = parts[2];
            if (time.Length == 4 && int.TryParse(time, out int timeVal))
            {
                int hour = timeVal / 100;
                int min = timeVal % 100;
                return $"({day}-{hour:D2}{min:D2})";
            }
            return $"({day}-{time})";
        }
        return "";
    }

    private void LoadGrid()
    {
        _isLoadingData = true;
        try
        {
            _grid.Rows.Clear();
            _grid.ClearSelection();
            if (!_leagueId.HasValue || !_seasonId.HasValue) return;

            bool showGroups = _cmbDisplayMode.SelectedIndex == 0;
            try
            {
                using var db = new BocceDbContext();
                var query = db.LookingForTeams
                    .Include(l => l.Player)
                    .Include(l => l.Group)
                    .Where(l => l.LeagueId == _leagueId.Value && l.SeasonId == _seasonId.Value);

                if (showGroups)
                    query = query.Where(l => l.Group != null && l.Group.GroupLeaderId == l.Id);
                else
                    query = query.ToList().AsQueryable();

                var list = query.ToList();

                var groupDict = db.LookingForTeamGroups
                    .Where(g => g.SeasonId == _seasonId.Value)
                    .ToDictionary(g => g.Id, g => g);

                var sorted = list
                    .OrderBy(l => l.LookingForTeamGroupId.HasValue && groupDict.ContainsKey(l.LookingForTeamGroupId.Value)
                        ? $"{groupDict[l.LookingForTeamGroupId.Value].Name}_{l.LookingForTeamGroupId.Value}"
                        : "zzz_NoGroup")
                    .ThenBy(l => l.Player.LastName)
                    .ThenBy(l => l.Player.FirstName)
                    .ToList();

                foreach (var e in sorted)
                {
                    string name = $"{e.Player.LastName}, {e.Player.FirstName}".Trim().TrimStart(',').Trim();
                    string grpLabel = "(no group)";
                    if (e.LookingForTeamGroupId.HasValue && groupDict.ContainsKey(e.LookingForTeamGroupId.Value))
                    {
                        var grp = groupDict[e.LookingForTeamGroupId.Value];
                        grpLabel = grp.Name ?? "";
                    }
                    _grid.Rows.Add(e.Id, e.PlayerId, e.LookingForTeamGroupId,
                        name, e.Player.Phone ?? "", e.Player.Email ?? "", grpLabel);
                }
            }
            catch { }

            // Always start with no selection — DataGridView auto-selects first row
            // which causes the Remove button to appear active when it isn't
            _grid.ClearSelection();
            _grid.CurrentCell = null;

            if (_selectedLftId.HasValue)
            {
                bool found = false;
                for (int i = 0; i < _grid.Rows.Count; i++)
                {
                    if (Convert.ToInt32(_grid.Rows[i].Cells["GId"].Value) == _selectedLftId.Value)
                    {
                        _isLoadingData = true; // suppress OnGridSelectionChanged during restore
                        _grid.Rows[i].Selected = true;
                        _grid.CurrentCell = _grid.Rows[i].Cells["GName"];
                        _isLoadingData = false;
                        found = true;
                        break;
                    }
                }
                if (!found) ClearDetail();
            }
            else
            {
                ClearDetail();
            }
        }
        finally { _isLoadingData = false; }
    }

    // ── Selection ──────────────────────────────────────────────────────────────
    private void OnGridSelectionChanged(object? sender, EventArgs e)
    {
        if (_isLoadingData) return;
        if (_grid.SelectedRows.Count == 0) { ClearDetail(); return; }
        if (_isDirty && !ConfirmDiscard()) { _grid.ClearSelection(); return; }

        _selectedLftId = Convert.ToInt32(_grid.SelectedRows[0].Cells["GId"].Value);
        var grpCell    = _grid.SelectedRows[0].Cells["GGrpId"].Value;
        _selectedGroupId = grpCell != null && grpCell != DBNull.Value ? Convert.ToInt32(grpCell) : (int?)null;
        LoadDetail(_selectedLftId.Value);
    }

    private void LoadDetail(int lftId)
    {
        _isLoadingData = true;
        try
        {
            using var db = new BocceDbContext();
            var e = db.LookingForTeams
                .Include(l => l.Team)
                .Include(l => l.PreferredDays)
                .Include(l => l.PreferredTimes)
                .FirstOrDefault(l => l.Id == lftId);
            if (e == null) return;

            bool isPlaced = e.TeamId.HasValue;

            if (isPlaced && e.Team != null)
            {
                _lblPlacedAs.Text    = $"Placed on: {e.Team.EffectiveDisplayName}";
                _lblPlacedAs.Visible = true;
            }
            else
            {
                _lblPlacedAs.Visible = false;
            }

            var checkedDayIds = e.PreferredDays.Select(d => d.DaySlotId).ToHashSet();
            for (int i = 0; i < _clbPrefDays.Items.Count; i++)
            {
                bool check = _clbPrefDays.Items[i] is DayItem d && d.Id.HasValue && checkedDayIds.Contains(d.Id.Value);
                _clbPrefDays.SetItemChecked(i, check);
            }

            var checkedTimeIds = e.PreferredTimes.Select(t => t.TimeSlotId).ToHashSet();
            for (int i = 0; i < _clbPrefTimes.Items.Count; i++)
            {
                bool check = _clbPrefTimes.Items[i] is TimeItem t && t.Id.HasValue && checkedTimeIds.Contains(t.Id.Value);
                _clbPrefTimes.SetItemChecked(i, check);
            }

            _cmbPrefTeam.SelectedIndex = 0;
            if (e.PreferredTeamId.HasValue)
            {
                for (int i = 1; i < _cmbPrefTeam.Items.Count; i++)
                {
                    if (_cmbPrefTeam.Items[i] is TeamItem t && t.Id == e.PreferredTeamId)
                    { _cmbPrefTeam.SelectedIndex = i; break; }
                }
            }

            _txtNotes.Text = e.Notes ?? "";

            _btnRemove.Enabled    = true;
            _btnAddMember.Enabled = _selectedGroupId.HasValue;
            _btnRenameGrp.Enabled = _selectedGroupId.HasValue;
            _btnDeleteGrp.Enabled = _selectedGroupId.HasValue;
            _btnSave.Enabled      = false;
            _btnCancel.Visible    = false;
            ClearDirty();

            LoadGroupMembers(lftId, e.LookingForTeamGroupId);
        }
        finally { _isLoadingData = false; }
    }

    private void LoadGroupMembers(int currentLftId, int? groupId)
    {
        _grpGrid.Rows.Clear();
        if (!groupId.HasValue) return;
        try
        {
            using var db = new BocceDbContext();
            var members = db.LookingForTeams
                .Include(l => l.Player)
                .Where(l => l.LookingForTeamGroupId == groupId.Value)
                .OrderBy(l => l.Player.LastName).ThenBy(l => l.Player.FirstName)
                .ToList();
            foreach (var m in members)
            {
                string name = $"{m.Player.LastName}, {m.Player.FirstName}".Trim().TrimStart(',').Trim();
                string marker = m.Id == currentLftId ? " ◆" : "";
                _grpGrid.Rows.Add(m.Id, name + marker, m.Player.Phone ?? "", m.Player.Email ?? "");
            }
        }
        catch { }
    }

    private void ClearDetail()
    {
        _isLoadingData = true;
        try
        {
            _selectedLftId       = null;
            _selectedGroupId     = null;
            _lblPlacedAs.Visible = false;
            for (int i = 0; i < _clbPrefDays.Items.Count; i++)
                _clbPrefDays.SetItemChecked(i, false);
            for (int i = 0; i < _clbPrefTimes.Items.Count; i++)
                _clbPrefTimes.SetItemChecked(i, false);
            if (_cmbPrefTeam.Items.Count > 0) _cmbPrefTeam.SelectedIndex = 0;
            _txtNotes.Text        = "";
            _grpGrid.Rows.Clear();
            _btnRemove.Enabled    = false;
            _btnSave.Enabled      = false;
            _btnCancel.Visible    = false;
            _btnAddMember.Enabled = false;
            _btnRemMember.Enabled = false;
            _btnRenameGrp.Enabled = false;
            _btnDeleteGrp.Enabled = false;
            ClearDirty();
        }
        finally { _isLoadingData = false; }
    }

    // ── Dirty tracking ─────────────────────────────────────────────────────────
    private void OnFieldChanged(object? sender, EventArgs e)
    {
        if (_isLoadingData) return;
        _isDirty           = true;
        _btnSave.Enabled   = true;
        _btnCancel.Visible = true;
    }

    private void ClearDirty() { _isDirty = false; _btnSave.Enabled = false; }

    // ── Actions ────────────────────────────────────────────────────────────────
    private void AddPlayerToLft()
    {
        if (!_leagueId.HasValue || !_seasonId.HasValue)
        {
            MessageBox.Show("Select a league and season first.", "Looking For Team",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Build exclusion set: players already in LFT this season
        HashSet<int> alreadyInLft = [];
        try
        {
            using var db = new BocceDbContext();
            alreadyInLft = db.LookingForTeams
                .Where(l => l.LeagueId == _leagueId.Value && l.SeasonId == _seasonId.Value)
                .Select(l => l.PlayerId).ToHashSet();
        }
        catch { }

        List<int> picked = PickPlayersDialog("Add Player(s) to Looking For Team", excludeIds: alreadyInLft, showCreateNew: true);
        if (picked.Count == 0) return;

        if (picked.Count > 5)
        {
            MessageBox.Show("Maximum 5 players per group. Please select fewer players.", "Too Many Players",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Ask for shared preferences (days, times, team, notes) before any DB writes
        var details = PromptLftDetails();
        if (details == null) return;

        // For 2+ players, ask whether they go as a group or as solo individuals
        bool asGroup = true;
        int? leaderPlayerId = null;
        if (picked.Count > 1)
        {
            var ans = MessageBox.Show($"Add {picked.Count} players as a group?",
                "Multiple Players", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (ans == DialogResult.Cancel) return;
            asGroup = ans == DialogResult.Yes;
        }

        if (asGroup && picked.Count > 1)
        {
            using var db = new BocceDbContext();
            leaderPlayerId = PromptSelectGroupLeader(picked, db);
            if (!leaderPlayerId.HasValue) return;
        }

        // Each picked player: check team membership, create LFT entry, track IDs
        var createdEntries = new Dictionary<int, int>(); // playerId → lftId
        foreach (int playerId in picked)
        {
            if (!CheckAndHandleTeamMembership(playerId)) continue;
            try
            {
                using var db = new BocceDbContext();
                var entry = new LookingForTeam
                {
                    LeagueId        = _leagueId.Value,
                    SeasonId        = _seasonId.Value,
                    PlayerId        = playerId,
                    PreferredTeamId = details.PrefTeamId,
                    Notes           = details.Notes.NullIfEmpty()
                };
                db.LookingForTeams.Add(entry);
                db.SaveChanges();

                foreach (int dayId in details.PrefDayIds)
                    db.LookingForTeamPreferredDays.Add(new LookingForTeamPreferredDay
                        { LookingForTeamId = entry.Id, DaySlotId = dayId });
                foreach (int timeId in details.PrefTimeIds)
                    db.LookingForTeamPreferredTimes.Add(new LookingForTeamPreferredTime
                        { LookingForTeamId = entry.Id, TimeSlotId = timeId });
                db.SaveChanges();

                createdEntries[playerId] = entry.Id;
                AppLogger.Info("Added player {PlayerId} to LFT for season {SeasonId}", playerId, _seasonId.Value);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not add player:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        if (createdEntries.Count == 0) return;

        // Now create groups — every player gets a group (even solo = group of 1)
        if (asGroup && createdEntries.Count > 0)
        {
            // Multi-player group (or forced single): one group for all
            int leaderLftId = leaderPlayerId.HasValue && createdEntries.ContainsKey(leaderPlayerId.Value)
                ? createdEntries[leaderPlayerId.Value]
                : createdEntries.Values.First();

            int leaderPid = leaderPlayerId ?? createdEntries.Keys.First();
            try
            {
                using var db = new BocceDbContext();
                var grp = CreateGroupForLeader(db, leaderPid, createdEntries.Count);
                grp.GroupLeaderId = leaderLftId;
                db.SaveChanges();

                foreach (var lftId in createdEntries.Values)
                {
                    var lft = db.LookingForTeams.Find(lftId);
                    if (lft != null) lft.LookingForTeamGroupId = grp.Id;
                }
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not create group:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        else
        {
            // Solos: each player gets their own group of 1
            foreach (var kvp in createdEntries)
            {
                try
                {
                    using var db = new BocceDbContext();
                    var grp = CreateGroupForLeader(db, kvp.Key, 1);
                    grp.GroupLeaderId = kvp.Value;
                    db.SaveChanges();

                    var lft = db.LookingForTeams.Find(kvp.Value);
                    if (lft != null) lft.LookingForTeamGroupId = grp.Id;
                    db.SaveChanges();
                }
                catch { }
            }
        }

        _selectedLftId = createdEntries.Values.First();
        LoadGrid();
    }

    private LookingForTeamGroup CreateGroupForLeader(BocceDbContext db, int leaderPlayerId, int memberCount)
    {
        var player   = db.Players.First(p => p.Id == leaderPlayerId);
        string baseName = player.LastName;

        // Count existing groups in this season whose name starts with the same last name
        var existing = db.LookingForTeamGroups
            .Where(g => g.SeasonId == _seasonId!.Value && g.Name != null && g.Name.StartsWith(baseName + " ("))
            .ToList();
        int disambiguator = existing.Count;

        var grp = new LookingForTeamGroup
        {
            LeagueId  = _leagueId!.Value,
            SeasonId  = _seasonId!.Value,
            Name      = $"{baseName} ({memberCount}.{disambiguator})",
            CreatedAt = DateTime.UtcNow
        };
        db.LookingForTeamGroups.Add(grp);
        db.SaveChanges();
        return grp;
    }

    private void UpdateGroupName(BocceDbContext db, int groupId)
    {
        var grp = db.LookingForTeamGroups.Find(groupId);
        if (grp == null) return;
        int count = db.LookingForTeams.Count(l => l.LookingForTeamGroupId == groupId);
        if (grp.Name == null) return;

        // Replace the count portion: "Smith (3.0)" → "Smith (4.0)"
        int parenOpen = grp.Name.LastIndexOf('(');
        int dotPos    = grp.Name.LastIndexOf('.');
        int parenClose = grp.Name.LastIndexOf(')');
        if (parenOpen >= 0 && dotPos > parenOpen && parenClose > dotPos)
        {
            string suffix = grp.Name[(dotPos)..parenClose]; // ".0"
            string basePart = grp.Name[..parenOpen].TrimEnd();
            grp.Name = $"{basePart} ({count}{suffix})";
        }
    }

    private void RemoveFromLft()
    {
        if (!_selectedLftId.HasValue) return;

        bool isGroupMode = _cmbDisplayMode.SelectedIndex == 0;

        try
        {
            using var db = new BocceDbContext();
            var e = db.LookingForTeams.Include(l => l.Group).First(l => l.Id == _selectedLftId.Value);
            if (e?.TeamId.HasValue == true)
            {
                MessageBox.Show("This player has already been placed on a team. Remove the team assignment first.",
                    "Cannot Remove", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (isGroupMode && e.LookingForTeamGroupId.HasValue)
            {
                var groupId = e.LookingForTeamGroupId.Value;
                var groupSize = db.LookingForTeams.Count(l => l.LookingForTeamGroupId == groupId);

                if (MessageBox.Show($"This will remove ALL {groupSize} players in this group.\n\nContinue?",
                        "Remove Entire Group", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;

                // Break circular FK: null GroupLeaderId before deleting LFT rows
                var grp = db.LookingForTeamGroups.Find(groupId);
                if (grp != null) { grp.GroupLeaderId = null; db.SaveChanges(); }

                var groupMembers = db.LookingForTeams
                    .Where(l => l.LookingForTeamGroupId == groupId)
                    .ToList();

                foreach (var member in groupMembers)
                {
                    member.LookingForTeamGroupId = null;
                }
                db.SaveChanges();

                db.LookingForTeams.RemoveRange(groupMembers);
                if (grp != null) db.LookingForTeamGroups.Remove(grp);
                db.SaveChanges();
            }
            else
            {
                if (MessageBox.Show("Remove this player from the Looking for Team list?",
                        "Confirm Remove", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;

                int? groupId = e.LookingForTeamGroupId;
                if (groupId.HasValue)
                {
                    var group = db.LookingForTeamGroups.Find(groupId.Value);
                    if (group?.GroupLeaderId == e.Id)
                    {
                        var otherMembers = db.LookingForTeams
                            .Where(l => l.LookingForTeamGroupId == groupId && l.Id != e.Id)
                            .ToList();

                        if (otherMembers.Count > 0)
                        {
                            int? newLeaderId = PromptSelectGroupLeader(
                                otherMembers.Select(m => m.PlayerId).ToList(), db);
                            if (!newLeaderId.HasValue) return;

                            var newLeader = otherMembers.FirstOrDefault(m => m.PlayerId == newLeaderId);
                            if (newLeader != null && group != null)
                            {
                                group.GroupLeaderId = newLeader.Id;
                                db.Entry(group).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                            }
                        }
                    }

                    e.LookingForTeamGroupId = null;
                    db.LookingForTeams.Remove(e);
                    db.SaveChanges();
                    DissolveGroupIfSingleton(db, groupId.Value);
                }
                else
                {
                    db.LookingForTeams.Remove(e);
                    db.SaveChanges();
                }
            }

            _selectedLftId = null; _selectedGroupId = null;
            ClearDetail();
            LoadGrid();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not remove:\n{ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SaveEntry()
    {
        if (!_selectedLftId.HasValue) return;

        var checkedDayIds = _clbPrefDays.CheckedItems
            .OfType<DayItem>()
            .Where(d => d.Id.HasValue)
            .Select(d => d.Id!.Value)
            .ToHashSet();

        var checkedTimeIds = _clbPrefTimes.CheckedItems
            .OfType<TimeItem>()
            .Where(t => t.Id.HasValue)
            .Select(t => t.Id!.Value)
            .ToHashSet();

        int? prefTeamId = (_cmbPrefTeam.SelectedItem as TeamItem)?.Id;

        try
        {
            using var db = new BocceDbContext();
            var e = db.LookingForTeams
                .Include(l => l.PreferredDays)
                .Include(l => l.PreferredTimes)
                .FirstOrDefault(l => l.Id == _selectedLftId.Value);
            if (e == null) return;

            e.PreferredTeamId = prefTeamId;
            e.Notes           = _txtNotes.Text.Trim().NullIfEmpty();

            var existingDayIds = e.PreferredDays.Select(d => d.DaySlotId).ToHashSet();
            foreach (var del in e.PreferredDays.Where(d => !checkedDayIds.Contains(d.DaySlotId)).ToList())
                db.LookingForTeamPreferredDays.Remove(del);
            foreach (int addId in checkedDayIds.Where(id => !existingDayIds.Contains(id)))
                db.LookingForTeamPreferredDays.Add(new LookingForTeamPreferredDay
                    { LookingForTeamId = e.Id, DaySlotId = addId });

            var existingTimeIds = e.PreferredTimes.Select(t => t.TimeSlotId).ToHashSet();
            foreach (var del in e.PreferredTimes.Where(t => !checkedTimeIds.Contains(t.TimeSlotId)).ToList())
                db.LookingForTeamPreferredTimes.Remove(del);
            foreach (int addId in checkedTimeIds.Where(id => !existingTimeIds.Contains(id)))
                db.LookingForTeamPreferredTimes.Add(new LookingForTeamPreferredTime
                    { LookingForTeamId = e.Id, TimeSlotId = addId });

            db.SaveChanges();
            AppLogger.Info("Updated LFT entry {Id}", _selectedLftId.Value);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not save:\n{ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        ClearDirty();
        _btnCancel.Visible = false;
        LoadGrid();
    }

    private void CancelEdit()
    {
        _isDirty = false; _btnCancel.Visible = false;
        if (_selectedLftId.HasValue) LoadDetail(_selectedLftId.Value);
        else ClearDetail();
    }

    // ── Group management ───────────────────────────────────────────────────────
    private void AddMember()
    {
        if (!_selectedLftId.HasValue || !_leagueId.HasValue || !_seasonId.HasValue) return;

        HashSet<int> alreadyInLft = [];
        try
        {
            using var db = new BocceDbContext();
            alreadyInLft = db.LookingForTeams
                .Where(l => l.LeagueId == _leagueId.Value && l.SeasonId == _seasonId.Value)
                .Select(l => l.PlayerId).ToHashSet();
        }
        catch { }

        int? pickedPlayerId = PickPlayerDialog("Add Group Member", excludeIds: alreadyInLft, showCreateNew: true);
        if (!pickedPlayerId.HasValue) return;

        if (!CheckAndHandleTeamMembership(pickedPlayerId.Value)) return;

        try
        {
            using var db = new BocceDbContext();

            // Check group size limit before adding
            var currentLft = db.LookingForTeams.Find(_selectedLftId.Value);
            if (currentLft?.LookingForTeamGroupId.HasValue == true)
            {
                int currentSize = db.LookingForTeams.Count(l => l.LookingForTeamGroupId == currentLft.LookingForTeamGroupId.Value);
                if (currentSize >= 5)
                {
                    MessageBox.Show("Maximum 5 players per group.", "Group Full",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            int newLftId = EnsureLftEntry(db, pickedPlayerId.Value);
            MergeIntoGroup(db, _selectedLftId.Value, newLftId);

            // Update group name to reflect new count
            var updatedLft = db.LookingForTeams.Find(_selectedLftId.Value);
            if (updatedLft?.LookingForTeamGroupId.HasValue == true)
                UpdateGroupName(db, updatedLft.LookingForTeamGroupId.Value);
            db.SaveChanges();

            AppLogger.Info("Added player {PlayerId} to group via LFT {LftId}", pickedPlayerId.Value, _selectedLftId.Value);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not add member:\n{ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            using var db = new BocceDbContext();
            _selectedGroupId = db.LookingForTeams.Find(_selectedLftId.Value)?.LookingForTeamGroupId;
        }
        catch { }

        LoadGrid();
        if (_selectedLftId.HasValue) LoadDetail(_selectedLftId.Value);
    }

    private void RemoveMember()
    {
        if (_grpGrid.SelectedRows.Count == 0) return;
        int memberLftId   = Convert.ToInt32(_grpGrid.SelectedRows[0].Cells["GLftId"].Value);
        string memberName = _grpGrid.SelectedRows[0].Cells["GName"].Value?.ToString() ?? "member";

        if (MessageBox.Show($"Remove {memberName} from this group?\n(They will remain in LFT in their own group of 1.)",
                "Remove from Group", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        try
        {
            using var db = new BocceDbContext();
            var member = db.LookingForTeams.FirstOrDefault(l => l.Id == memberLftId);
            if (member == null) return;

            int? groupId = member.LookingForTeamGroupId;
            if (!groupId.HasValue) return;

            var group = db.LookingForTeamGroups.Find(groupId.Value);
            bool isLeader = group?.GroupLeaderId == memberLftId;

            // Remove this member from the group
            member.LookingForTeamGroupId = null;
            db.SaveChanges();

            var remaining = db.LookingForTeams
                .Include(l => l.Player)
                .Where(l => l.LookingForTeamGroupId == groupId.Value)
                .ToList();

            if (remaining.Count == 0)
            {
                // Group is now empty — dissolve it
                if (group != null) { group.GroupLeaderId = null; db.SaveChanges(); db.LookingForTeamGroups.Remove(group); db.SaveChanges(); }
            }
            else if (isLeader)
            {
                // Leader removed — auto-select if 1 remains, else prompt
                int? newLeaderPid = PromptSelectGroupLeader(remaining.Select(m => m.PlayerId).ToList(), db);
                if (!newLeaderPid.HasValue) { member.LookingForTeamGroupId = groupId; db.SaveChanges(); return; }

                var newLeaderLft = remaining.First(m => m.PlayerId == newLeaderPid.Value);
                if (group != null)
                {
                    group.GroupLeaderId = newLeaderLft.Id;
                    db.Entry(group).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                }
                UpdateGroupName(db, groupId.Value);
                db.SaveChanges();
                AppLogger.Info("New leader LFT {LftId} set for group {GroupId}", newLeaderLft.Id, groupId.Value);
            }
            else
            {
                // Non-leader removed — just update the count in the name
                UpdateGroupName(db, groupId.Value);
                db.SaveChanges();
            }

            // Removed player gets their own group of 1
            var soloGrp = CreateGroupForLeader(db, member.PlayerId, 1);
            soloGrp.GroupLeaderId = member.Id;
            member.LookingForTeamGroupId = soloGrp.Id;
            db.SaveChanges();

            AppLogger.Info("Removed LFT {LftId} from group {GroupId}, placed in solo group {SoloGroupId}", memberLftId, groupId, soloGrp.Id);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not remove from group:\n{ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            using var db = new BocceDbContext();
            _selectedGroupId = db.LookingForTeams.Find(_selectedLftId!.Value)?.LookingForTeamGroupId;
        }
        catch { }

        LoadGrid();
        if (_selectedLftId.HasValue) LoadDetail(_selectedLftId.Value);
    }

    private void RenameGroup()
    {
        if (!_selectedGroupId.HasValue) return;

        try
        {
            using var db = new BocceDbContext();
            var members = db.LookingForTeams
                .Include(l => l.Player)
                .Where(l => l.LookingForTeamGroupId == _selectedGroupId.Value)
                .OrderBy(l => l.Player.LastName).ThenBy(l => l.Player.FirstName)
                .ToList();

            if (members.Count == 0) return;

            int? selectedMemberId = PromptSelectGroupMember("Rename group to member:", members);
            if (!selectedMemberId.HasValue) return;

            var selectedMember = members.FirstOrDefault(m => m.Id == selectedMemberId.Value);
            if (selectedMember == null) return;

            var group = db.LookingForTeamGroups.Find(_selectedGroupId.Value);
            if (group != null)
            {
                string baseName   = selectedMember.Player.LastName;
                int memberCount   = members.Count;
                // Disambiguator: count other groups with the same base name (excluding this one)
                int disambiguator = db.LookingForTeamGroups
                    .Where(g => g.SeasonId == _seasonId!.Value && g.Id != group.Id
                                && g.Name != null && g.Name.StartsWith(baseName + " ("))
                    .Count();
                group.Name = $"{baseName} ({memberCount}.{disambiguator})";
                db.SaveChanges();
                AppLogger.Info("Renamed group {GroupId} to {Name}", _selectedGroupId.Value, group.Name);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not rename group:\n{ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        LoadGrid();
        if (_selectedLftId.HasValue) LoadDetail(_selectedLftId.Value);
    }

    private void DeleteGroup()
    {
        if (!_selectedGroupId.HasValue) return;

        if (MessageBox.Show("Delete this entire group? Each member will remain in LFT in their own group of 1.",
                "Delete Group", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        try
        {
            using var db = new BocceDbContext();
            var members = db.LookingForTeams
                .Where(l => l.LookingForTeamGroupId == _selectedGroupId.Value)
                .ToList();

            foreach (var member in members)
                member.LookingForTeamGroupId = null;

            db.SaveChanges();

            var group = db.LookingForTeamGroups.Find(_selectedGroupId.Value);
            if (group != null)
            {
                db.LookingForTeamGroups.Remove(group);
                db.SaveChanges();
                AppLogger.Info("Deleted group {GroupId} with {Count} members", _selectedGroupId.Value, members.Count);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not delete group:\n{ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _selectedGroupId = null;
        ClearDetail();
        LoadGrid();
    }

    private int? PromptSelectGroupMember(string prompt, List<LookingForTeam> members)
    {
        const int lblH = 32, btnBarH = 46;
        int gridH   = Math.Max(members.Count * 28 + 28, 60); // rows + header
        int formH   = lblH + gridH + btnBarH
                      + SystemInformation.CaptionHeight + SystemInformation.BorderSize.Height * 2 + 8;

        using var form = new Form
        {
            Text = "Select Group Member", Width = 420, Height = formH,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false,
            BackColor = AppTheme.ContentBackground
        };

        var lbl = new Label
        {
            Text = prompt, Location = new Point(12, 8), Size = new Size(390, 24),
            Font = AppTheme.FontDefault, ForeColor = AppTheme.TextPrimary
        };

        var grid = new DataGridView
        {
            Location = new Point(0, lblH), Size = new Size(form.ClientSize.Width, gridH),
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false, ReadOnly = true,
            AllowUserToAddRows = false, RowHeadersVisible = false,
            BorderStyle = BorderStyle.None,
            BackgroundColor = AppTheme.ContentBackground,
            Font = AppTheme.FontDefault, RowTemplate = { Height = 28 },
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            EnableHeadersVisualStyles = false,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppTheme.GridHeaderBackground, ForeColor = AppTheme.GridHeaderText,
                SelectionBackColor = AppTheme.GridHeaderBackground, SelectionForeColor = AppTheme.GridHeaderText,
                Font = AppTheme.FontGridHeader
            }
        };
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "LftId", Visible = false });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name",  HeaderText = "Name",  FillWeight = 50 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Phone", HeaderText = "Phone", FillWeight = 25 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Email", HeaderText = "Email", FillWeight = 25 });

        foreach (var m in members)
        {
            string name = $"{m.Player.LastName}, {m.Player.FirstName}".Trim().TrimStart(',').Trim();
            grid.Rows.Add(m.Id, name, m.Player.Phone ?? "", m.Player.Email ?? "");
        }

        int barY = lblH + gridH;
        var bar = new Panel { Location = new Point(0, barY), Size = new Size(form.ClientSize.Width, btnBarH), BackColor = AppTheme.Surface };
        var btnOk = new Button
        {
            Text = "Select", DialogResult = DialogResult.OK, Left = 12, Top = 8, Width = 100, Height = 30,
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.Accent, ForeColor = Color.White,
            Font = AppTheme.FontButton, FlatAppearance = { BorderSize = 0 }
        };
        var btnCxl = new Button
        {
            Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 124, Top = 8, Width = 80, Height = 30,
            FlatStyle = FlatStyle.Flat, Font = AppTheme.FontButton
        };
        bar.Controls.AddRange([btnOk, btnCxl]);

        form.Controls.AddRange([lbl, grid, bar]);
        form.AcceptButton = btnOk;
        form.CancelButton = btnCxl;

        if (form.ShowDialog(this) != DialogResult.OK || grid.SelectedRows.Count == 0) return null;
        var v = grid.SelectedRows[0].Cells[0].Value;
        return v != null && v != DBNull.Value ? Convert.ToInt32(v) : null;
    }

    private int? PromptNewGroupLeader(int groupId, int excludeLftId)
    {
        try
        {
            using var db = new BocceDbContext();
            var members = db.LookingForTeams
                .Include(l => l.Player)
                .Where(l => l.LookingForTeamGroupId == groupId && l.Id != excludeLftId)
                .OrderBy(l => l.Player.LastName).ThenBy(l => l.Player.FirstName)
                .ToList();

            if (members.Count == 0) return null;

            return PromptSelectGroupMember("Who should be the new group leader?", members);
        }
        catch { return null; }
    }

    // ── Group helpers ──────────────────────────────────────────────────────────
    private int EnsureLftEntry(BocceDbContext db, int playerId)
    {
        var existing = db.LookingForTeams.FirstOrDefault(l =>
            l.PlayerId == playerId && l.LeagueId == _leagueId!.Value && l.SeasonId == _seasonId!.Value);
        if (existing != null) return existing.Id;

        var entry = new LookingForTeam
        {
            LeagueId = _leagueId!.Value,
            SeasonId = _seasonId!.Value,
            PlayerId = playerId
        };
        db.LookingForTeams.Add(entry);
        db.SaveChanges();
        return entry.Id;
    }

    private void MergeIntoGroup(BocceDbContext db, int lftId1, int lftId2)
    {
        var e1 = db.LookingForTeams.Include(l => l.Player).First(l => l.Id == lftId1);
        var e2 = db.LookingForTeams.Include(l => l.Player).First(l => l.Id == lftId2);

        if (!e1.LookingForTeamGroupId.HasValue && !e2.LookingForTeamGroupId.HasValue)
        {
            string groupName = e1.Player.LastName;
            var grp = new LookingForTeamGroup
            {
                LeagueId = _leagueId!.Value,
                SeasonId = _seasonId!.Value,
                Name = groupName,
                CreatedAt = DateTime.UtcNow
            };
            db.LookingForTeamGroups.Add(grp);
            db.SaveChanges();
            e1.LookingForTeamGroupId = grp.Id;
            e2.LookingForTeamGroupId = grp.Id;
        }
        else if (e1.LookingForTeamGroupId.HasValue && !e2.LookingForTeamGroupId.HasValue)
        {
            e2.LookingForTeamGroupId = e1.LookingForTeamGroupId;
        }
        else if (!e1.LookingForTeamGroupId.HasValue && e2.LookingForTeamGroupId.HasValue)
        {
            e1.LookingForTeamGroupId = e2.LookingForTeamGroupId;
        }
        else if (e1.LookingForTeamGroupId != e2.LookingForTeamGroupId)
        {
            int keepId = e1.LookingForTeamGroupId!.Value;
            int dropId = e2.LookingForTeamGroupId!.Value;
            foreach (var m in db.LookingForTeams.Where(l => l.LookingForTeamGroupId == dropId).ToList())
                m.LookingForTeamGroupId = keepId;
            db.SaveChanges();
            var dropGrp = db.LookingForTeamGroups.Find(dropId);
            if (dropGrp != null) db.LookingForTeamGroups.Remove(dropGrp);
        }

        db.SaveChanges();
        _selectedGroupId = e1.LookingForTeamGroupId;
    }

    private static void DissolveGroupIfSingleton(BocceDbContext db, int groupId)
    {
        var remaining = db.LookingForTeams.Where(l => l.LookingForTeamGroupId == groupId).ToList();
        if (remaining.Count <= 1)
        {
            foreach (var m in remaining) m.LookingForTeamGroupId = null;
            db.SaveChanges();
            var grp = db.LookingForTeamGroups.Find(groupId);
            if (grp != null) { db.LookingForTeamGroups.Remove(grp); db.SaveChanges(); }
        }
    }

    private static string GroupLabel(int count) => count switch
    {
        2 => "Pair",
        3 => "Trio",
        4 => "Quad",
        _ => $"Group({count})"
    };

    // ── Team membership check ──────────────────────────────────────────────────
    private bool CheckAndHandleTeamMembership(int playerId)
    {
        if (!_seasonId.HasValue) return true;
        List<(int TeamPlayerId, string TeamDisplay)> memberships = [];
        try
        {
            using var db = new BocceDbContext();
            memberships = db.TeamPlayers
                .Include(tp => tp.Team)
                .Where(tp => tp.PlayerId == playerId && tp.Team.Division.SeasonId == _seasonId.Value)
                .AsEnumerable()
                .Select(tp => (tp.Id, $"{tp.Team.TeamLetter} — {tp.Team.EffectiveDisplayName}"))
                .ToList();
        }
        catch { return true; }

        if (memberships.Count == 0) return true;
        return PromptTeamRemoval(playerId, memberships);
    }

    private bool PromptTeamRemoval(int playerId, List<(int TeamPlayerId, string TeamDisplay)> memberships)
    {
        using var form = new Form
        {
            Text = "Player is on a Team", Width = 460, Height = 280,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false,
            BackColor = AppTheme.ContentBackground
        };

        var lbl = new Label
        {
            Text = "This player is already on team(s) this season.\nCheck team(s) to remove them from before adding to LFT:",
            Location = new Point(12, 10), Size = new Size(420, 40),
            Font = AppTheme.FontDefault, ForeColor = AppTheme.TextPrimary
        };
        var clb = new CheckedListBox
        {
            Location = new Point(12, 56), Size = new Size(420, 120),
            CheckOnClick = true, Font = AppTheme.FontDefault,
            BackColor = AppTheme.Surface, BorderStyle = BorderStyle.FixedSingle
        };
        foreach (var (_, display) in memberships) clb.Items.Add(display);

        var bar = new Panel { Dock = DockStyle.Bottom, Height = 46, BackColor = AppTheme.Surface };
        var btnOk  = new Button { Text = "Continue", DialogResult = DialogResult.OK,     Left = 12,  Top = 8, Width = 100, Height = 30, FlatStyle = FlatStyle.Flat, BackColor = AppTheme.Accent, ForeColor = Color.White, Font = AppTheme.FontButton, FlatAppearance = { BorderSize = 0 } };
        var btnCxl = new Button { Text = "Cancel",   DialogResult = DialogResult.Cancel, Left = 124, Top = 8, Width = 80,  Height = 30, FlatStyle = FlatStyle.Flat, Font = AppTheme.FontButton };
        bar.Controls.AddRange([btnOk, btnCxl]);
        form.Controls.AddRange([lbl, clb, bar]);
        form.AcceptButton = btnOk; form.CancelButton = btnCxl;

        if (form.ShowDialog(this) != DialogResult.OK) return false;

        var toRemove = clb.CheckedIndices.Cast<int>().Select(i => memberships[i].TeamPlayerId).ToList();
        if (toRemove.Count > 0)
        {
            try
            {
                using var db = new BocceDbContext();
                foreach (int tpId in toRemove)
                {
                    var tp = db.TeamPlayers.Find(tpId);
                    if (tp != null) db.TeamPlayers.Remove(tp);
                }
                db.SaveChanges();
                AppLogger.Info("Removed player {PlayerId} from {Count} team(s) before LFT add", playerId, toRemove.Count);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error removing from team:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        return true;
    }

    // ── Dialogs ────────────────────────────────────────────────────────────────
    private int? PromptSelectGroupLeader(List<int> playerIds, BocceDbContext db)
    {
        // Only one candidate — no dialog needed, auto-select them
        if (playerIds.Count == 1) return playerIds[0];

        var players = playerIds
            .Select(id => db.Players.FirstOrDefault(p => p.Id == id))
            .Where(p => p != null)
            .ToList();

        if (players.Count == 0) return null;
        if (players.Count == 1) return players[0]!.Id;

        // Calculate positions up front — no Dock, fully absolute
        const int lblY    = 12;
        const int radiosY = 38;
        const int btnH    = 44;
        int radioHeight   = players.Count * 28;
        int btnsY         = radiosY + radioHeight + 10;
        int clientH       = btnsY + btnH + 8;
        int formH         = clientH + SystemInformation.CaptionHeight + SystemInformation.BorderSize.Height * 2;

        using var form = new Form
        {
            Text = "Select Group Leader", Width = 380, Height = formH,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false,
            BackColor = AppTheme.ContentBackground
        };

        var lbl = new Label
        {
            Text = "Select the group leader:",
            Location = new Point(12, lblY), AutoSize = true,
            Font = AppTheme.FontDefault, ForeColor = AppTheme.TextPrimary
        };

        var radioPanel = new Panel { Location = new Point(12, radiosY), Width = 340, Height = radioHeight };
        var radios = new List<(RadioButton Radio, int PlayerId)>();
        int ry = 0;
        foreach (var player in players)
        {
            var radio = new RadioButton
            {
                Text = $"{player!.LastName}, {player.FirstName}",
                Location = new Point(0, ry), AutoSize = true,
                Font = AppTheme.FontDefault, ForeColor = AppTheme.TextPrimary
            };
            if (radios.Count == 0) radio.Checked = true;
            radioPanel.Controls.Add(radio);
            radios.Add((radio, player.Id));
            ry += 28;
        }

        var btnPanel = new Panel
        {
            Location = new Point(0, btnsY), Size = new Size(form.ClientSize.Width, btnH),
            BackColor = AppTheme.Surface
        };
        var btnOk = new Button
        {
            Text = "OK", Location = new Point(8, 10), Size = new Size(90, 24),
            DialogResult = DialogResult.OK,
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.Accent, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 }
        };
        var btnCancel = new Button
        {
            Text = "Cancel", Location = new Point(106, 10), Size = new Size(90, 24),
            DialogResult = DialogResult.Cancel,
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.Surface, ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 1 }
        };
        btnPanel.Controls.AddRange([btnOk, btnCancel]);

        form.Controls.AddRange([lbl, radioPanel, btnPanel]);
        form.AcceptButton = btnOk;
        form.CancelButton = btnCancel;

        if (form.ShowDialog(this) == DialogResult.OK)
        {
            var selected = radios.FirstOrDefault(r => r.Radio.Checked);
            return selected.PlayerId > 0 ? selected.PlayerId : (int?)null;
        }
        return null;
    }

    private List<int> PickPlayersDialog(string title, HashSet<int>? excludeIds, bool showCreateNew)
    {
        using var form = new Form
        {
            Text = title, Width = 560, Height = 560,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false,
            BackColor = AppTheme.ContentBackground
        };

        var filterBar = new Panel { Dock = DockStyle.Top, Height = 62, BackColor = AppTheme.Surface };
        var chkExTeam  = new CheckBox { Text = "Exclude players already on a team", Location = new Point(10, 10), AutoSize = true, Font = AppTheme.FontDefault, ForeColor = AppTheme.TextPrimary, Checked = true };
        var chkExSpare = new CheckBox { Text = "Exclude spare list members",        Location = new Point(10, 36), AutoSize = true, Font = AppTheme.FontDefault, ForeColor = AppTheme.TextPrimary };
        filterBar.Controls.AddRange([chkExTeam, chkExSpare]);

        var search = new TextBox
        {
            Dock = DockStyle.Top, Font = AppTheme.FontDefault, Height = 30,
            PlaceholderText = "Search by name...", BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary, BorderStyle = BorderStyle.FixedSingle
        };

        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = true, ReadOnly = true,
            AllowUserToAddRows = false, RowHeadersVisible = false,
            BorderStyle = BorderStyle.None,
            BackgroundColor = AppTheme.ContentBackground,
            Font = AppTheme.FontDefault, RowTemplate = { Height = 28 },
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            EnableHeadersVisualStyles = false,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppTheme.GridHeaderBackground, ForeColor = AppTheme.GridHeaderText,
                SelectionBackColor = AppTheme.GridHeaderBackground, SelectionForeColor = AppTheme.GridHeaderText,
                Font = AppTheme.FontGridHeader
            }
        };
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "PId", Visible = false });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Name", FillWeight = 40 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Phone", HeaderText = "Phone", FillWeight = 30 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Email", HeaderText = "Email", FillWeight = 30 });

        var teamSet  = new HashSet<int>();
        var spareSet = new HashSet<int>();
        List<(int Id, string Name, string Phone, string Email)> all = [];

        try
        {
            using var db = new BocceDbContext();
            if (_seasonId.HasValue)
                teamSet = db.TeamPlayers
                    .Where(tp => tp.Team.Division.SeasonId == _seasonId.Value)
                    .Select(tp => tp.PlayerId).Distinct().ToHashSet();
            if (_leagueId.HasValue)
                spareSet = db.SpareLists
                    .Where(s => s.LeagueId == _leagueId.Value && s.IsActive)
                    .Select(s => s.PlayerId).ToHashSet();
            all = db.Players
                .Where(p => p.IsActive)
                .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
                .AsEnumerable()
                .Where(p => excludeIds == null || !excludeIds.Contains(p.Id))
                .Select(p => (p.Id,
                    $"{p.LastName}, {p.FirstName}".Trim().TrimStart(',').Trim(),
                    p.Phone ?? "", p.Email ?? ""))
                .ToList();
        }
        catch { }

        void Filter(string q)
        {
            grid.Rows.Clear();
            foreach (var (id, name, phone, email) in all)
            {
                if (chkExTeam.Checked  && teamSet.Contains(id))  continue;
                if (chkExSpare.Checked && spareSet.Contains(id)) continue;
                if (!string.IsNullOrWhiteSpace(q) && !SearchQueryService.MatchesAnyTerm(name, q)) continue;
                grid.Rows.Add(id, name, phone, email);
            }
        }

        search.TextChanged        += (_, _) => Filter(search.Text);
        chkExTeam.CheckedChanged  += (_, _) => Filter(search.Text);
        chkExSpare.CheckedChanged += (_, _) => Filter(search.Text);
        Filter("");

        var bar = new Panel { Dock = DockStyle.Bottom, Height = 46, BackColor = AppTheme.Surface };
        var btnOk  = new Button { Text = "Select", DialogResult = DialogResult.OK,     Left = 12,  Top = 8, Width = 100, Height = 30, FlatStyle = FlatStyle.Flat, BackColor = AppTheme.Accent, ForeColor = Color.White, Font = AppTheme.FontButton, FlatAppearance = { BorderSize = 0 } };
        var btnCxl = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 124, Top = 8, Width = 80,  Height = 30, FlatStyle = FlatStyle.Flat, Font = AppTheme.FontButton };
        bar.Controls.AddRange([btnOk, btnCxl]);

        Panel? newPersonBar = null;
        if (showCreateNew)
        {
            newPersonBar = new Panel { Dock = DockStyle.Bottom, Height = 42, BackColor = AppTheme.ContentBackground };
            var btnNew = new Button
            {
                Text = "+ New Person...", Left = 8, Top = 6, Width = 130, Height = 30,
                FlatStyle = FlatStyle.Flat, BackColor = AppTheme.ButtonSuccess, ForeColor = Color.White,
                Font = AppTheme.FontButton, FlatAppearance = { BorderSize = 0 }
            };
            btnNew.Click += (_, _) =>
            {
                int? newId = CreateNewPlayerInline(form);
                if (newId.HasValue) { form.Tag = newId; form.DialogResult = DialogResult.OK; }
            };
            newPersonBar.Controls.Add(btnNew);
        }

        var controls = new List<Control> { grid, bar, search, filterBar };
        if (newPersonBar != null) controls.Add(newPersonBar);
        form.Controls.AddRange([.. controls]);
        form.AcceptButton = btnOk;
        form.CancelButton = btnCxl;

        var result = new List<int>();
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            if (form.Tag is int createdId)
                result.Add(createdId);
            else
                result.AddRange(grid.SelectedRows.Cast<DataGridViewRow>()
                    .Select(r => r.Cells[0].Value)
                    .Where(v => v != null && v != DBNull.Value)
                    .Select(v => Convert.ToInt32(v)));
        }
        return result;
    }

    private int? PickPlayerDialog(string title, HashSet<int>? excludeIds, bool showCreateNew)
    {
        using var form = new Form
        {
            Text = title, Width = 560, Height = 560,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false,
            BackColor = AppTheme.ContentBackground
        };

        var filterBar = new Panel { Dock = DockStyle.Top, Height = 62, BackColor = AppTheme.Surface };
        var chkExTeam  = new CheckBox { Text = "Exclude players already on a team", Location = new Point(10, 10), AutoSize = true, Font = AppTheme.FontDefault, ForeColor = AppTheme.TextPrimary, Checked = true };
        var chkExSpare = new CheckBox { Text = "Exclude spare list members",        Location = new Point(10, 36), AutoSize = true, Font = AppTheme.FontDefault, ForeColor = AppTheme.TextPrimary };
        filterBar.Controls.AddRange([chkExTeam, chkExSpare]);

        var search = new TextBox
        {
            Dock = DockStyle.Top, Font = AppTheme.FontDefault, Height = 30,
            PlaceholderText = "Search by name...", BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary, BorderStyle = BorderStyle.FixedSingle
        };

        var grid = MakePickerGrid(["PId", "Name", "Phone", "Email"]);
        grid.DoubleClick += (_, _) => { if (grid.SelectedRows.Count > 0) form.DialogResult = DialogResult.OK; };

        var teamSet  = new HashSet<int>();
        var spareSet = new HashSet<int>();
        List<(int Id, string Name, string Phone, string Email)> all = [];

        try
        {
            using var db = new BocceDbContext();
            if (_seasonId.HasValue)
                teamSet = db.TeamPlayers
                    .Where(tp => tp.Team.Division.SeasonId == _seasonId.Value)
                    .Select(tp => tp.PlayerId).Distinct().ToHashSet();
            if (_leagueId.HasValue)
                spareSet = db.SpareLists
                    .Where(s => s.LeagueId == _leagueId.Value && s.IsActive)
                    .Select(s => s.PlayerId).ToHashSet();
            all = db.Players
                .Where(p => p.IsActive)
                .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
                .AsEnumerable()
                .Where(p => excludeIds == null || !excludeIds.Contains(p.Id))
                .Select(p => (p.Id,
                    $"{p.LastName}, {p.FirstName}".Trim().TrimStart(',').Trim(),
                    p.Phone ?? "", p.Email ?? ""))
                .ToList();
        }
        catch { }

        void Filter(string q)
        {
            grid.Rows.Clear();
            foreach (var (id, name, phone, email) in all)
            {
                if (chkExTeam.Checked  && teamSet.Contains(id))  continue;
                if (chkExSpare.Checked && spareSet.Contains(id)) continue;
                if (!string.IsNullOrWhiteSpace(q) && !SearchQueryService.MatchesAnyTerm(name, q)) continue;
                grid.Rows.Add(id, name, phone, email);
            }
        }

        search.TextChanged        += (_, _) => Filter(search.Text);
        chkExTeam.CheckedChanged  += (_, _) => Filter(search.Text);
        chkExSpare.CheckedChanged += (_, _) => Filter(search.Text);
        Filter("");

        var bar = PickerBar(form);
        ((Button)bar.Controls[0]!).Text = "Select";

        Panel? newPersonBar = null;
        if (showCreateNew)
        {
            newPersonBar = new Panel { Dock = DockStyle.Bottom, Height = 42, BackColor = AppTheme.ContentBackground };
            var btnNew = new Button
            {
                Text = "+ New Person...", Left = 8, Top = 6, Width = 130, Height = 30,
                FlatStyle = FlatStyle.Flat, BackColor = AppTheme.ButtonSuccess, ForeColor = Color.White,
                Font = AppTheme.FontButton, FlatAppearance = { BorderSize = 0 }
            };
            btnNew.Click += (_, _) =>
            {
                int? newId = CreateNewPlayerInline(form);
                if (newId.HasValue) { form.Tag = newId; form.DialogResult = DialogResult.OK; }
            };
            newPersonBar.Controls.Add(btnNew);
        }

        var controls = new List<Control> { grid, bar, search, filterBar };
        if (newPersonBar != null) controls.Add(newPersonBar);
        form.Controls.AddRange([.. controls]);
        form.AcceptButton = (Button)bar.Controls[0];
        form.CancelButton = (Button)bar.Controls[1];

        if (form.ShowDialog(this) != DialogResult.OK) return null;

        if (form.Tag is int createdId) return createdId;
        if (grid.SelectedRows.Count > 0)
        {
            var v = grid.SelectedRows[0].Cells[0].Value;
            if (v != null && v != DBNull.Value) return Convert.ToInt32(v);
        }
        return null;
    }

    private int? CreateNewPlayerInline(Control parent)
    {
        using var form = new Form
        {
            Text = "New Player", Width = 420, Height = 256,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false,
            BackColor = AppTheme.ContentBackground
        };

        var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 12, 16, 0), BackColor = AppTheme.ContentBackground };
        const int lx = 0, fx = 130, fw = 240;
        string[] labels = ["First Name", "Last Name", "Email", "Phone"];
        var txts = new TextBox[4];
        for (int i = 0; i < 4; i++)
        {
            int y = 10 + i * 36;
            pnl.Controls.Add(Lbl(labels[i], lx, y));
            txts[i] = new TextBox { Location = new Point(fx, y), Size = new Size(fw, 26), Font = AppTheme.FontDefault, BackColor = AppTheme.Surface };
            pnl.Controls.Add(txts[i]);
        }

        var bar = new Panel { Dock = DockStyle.Bottom, Height = 46, BackColor = AppTheme.Surface };
        var btnOk  = new Button { Text = "Create", DialogResult = DialogResult.OK,     Left = 12,  Top = 8, Width = 80, Height = 30, FlatStyle = FlatStyle.Flat, BackColor = AppTheme.ButtonSuccess, ForeColor = Color.White, Font = AppTheme.FontButton, FlatAppearance = { BorderSize = 0 } };
        var btnCxl = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 104, Top = 8, Width = 80, Height = 30, FlatStyle = FlatStyle.Flat, Font = AppTheme.FontButton };
        bar.Controls.AddRange([btnOk, btnCxl]);
        form.Controls.AddRange([pnl, bar]);
        form.AcceptButton = btnOk; form.CancelButton = btnCxl;

        if (form.ShowDialog(parent) != DialogResult.OK) return null;

        string first = txts[0].Text.Trim(), last = txts[1].Text.Trim();
        if (string.IsNullOrEmpty(first) && string.IsNullOrEmpty(last))
        {
            MessageBox.Show("At minimum a first or last name is required.", "Validation",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }

        try
        {
            using var db = new BocceDbContext();
            var player = new Player
            {
                FirstName = first, LastName = last,
                Email = txts[2].Text.Trim().NullIfEmpty(),
                Phone = txts[3].Text.Trim().NullIfEmpty(),
                IsActive = true
            };
            db.Players.Add(player);
            db.SaveChanges();
            AppLogger.Info("Created player {Id} {Name} inline from LFT", player.Id, player.FullName);
            return player.Id;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not create player:\n{ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return null;
        }
    }

    private record LftInitDetails(int? PrefTeamId, List<int> PrefDayIds, List<int> PrefTimeIds, string Notes);

    private LftInitDetails? PromptLftDetails()
    {
        using var form = new Form
        {
            Text = "Looking For Team — Details", Width = 480, Height = 440,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false,
            BackColor = AppTheme.ContentBackground
        };
        var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 12, 16, 0), BackColor = AppTheme.ContentBackground };

        int y = 10; const int lx = 0, fx = 160, fw = 270;

        pnl.Controls.Add(Lbl("Preferred Days", lx, y));
        var clbDays = new CheckedListBox
        {
            Location = new Point(fx, y), Size = new Size(fw, 90), CheckOnClick = true,
            Font = AppTheme.FontDefault, BackColor = AppTheme.Surface, BorderStyle = BorderStyle.FixedSingle
        };
        foreach (var d in _clbPrefDays.Items.Cast<object>()) clbDays.Items.Add(d);
        pnl.Controls.Add(clbDays);
        y += 100;

        pnl.Controls.Add(Lbl("Preferred Times", lx, y));
        var clbTimes = new CheckedListBox
        {
            Location = new Point(fx, y), Size = new Size(fw, 90), CheckOnClick = true,
            Font = AppTheme.FontDefault, BackColor = AppTheme.Surface, BorderStyle = BorderStyle.FixedSingle
        };
        foreach (var t in _clbPrefTimes.Items.Cast<object>()) clbTimes.Items.Add(t);
        pnl.Controls.Add(clbTimes);
        y += 100;

        pnl.Controls.Add(Lbl("Preferred Team", lx, y));
        var cmbTeam = new ComboBox
        {
            Location = new Point(fx, y), Size = new Size(fw, 26),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = AppTheme.FontDefault, BackColor = AppTheme.Surface
        };
        foreach (var item in _cmbPrefTeam.Items.Cast<object>()) cmbTeam.Items.Add(item);
        if (cmbTeam.Items.Count > 0) cmbTeam.SelectedIndex = 0;
        pnl.Controls.Add(cmbTeam);
        y += 36;

        pnl.Controls.Add(Lbl("Notes", lx, y));
        var txtNotes = new TextBox
        {
            Location = new Point(fx, y), Size = new Size(fw, 56), Multiline = true,
            Font = AppTheme.FontDefault, BackColor = AppTheme.Surface, BorderStyle = BorderStyle.FixedSingle
        };
        pnl.Controls.Add(txtNotes);

        var bar = new Panel { Dock = DockStyle.Bottom, Height = 46, BackColor = AppTheme.Surface };
        var btnOk  = new Button { Text = "Add",    DialogResult = DialogResult.OK,     Left = 12,  Top = 8, Width = 80, Height = 30, FlatStyle = FlatStyle.Flat, BackColor = AppTheme.ButtonSuccess, ForeColor = Color.White, Font = AppTheme.FontButton, FlatAppearance = { BorderSize = 0 } };
        var btnCxl = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 104, Top = 8, Width = 80, Height = 30, FlatStyle = FlatStyle.Flat, Font = AppTheme.FontButton };
        bar.Controls.AddRange([btnOk, btnCxl]);
        form.Controls.AddRange([pnl, bar]);
        form.AcceptButton = btnOk; form.CancelButton = btnCxl;

        if (form.ShowDialog(this) != DialogResult.OK) return null;

        var dayIds  = clbDays.CheckedItems.OfType<DayItem>().Where(d => d.Id.HasValue).Select(d => d.Id!.Value).ToList();
        var timeIds = clbTimes.CheckedItems.OfType<TimeItem>().Where(t => t.Id.HasValue).Select(t => t.Id!.Value).ToList();
        int? teamId = (cmbTeam.SelectedItem as TeamItem)?.Id;
        return new LftInitDetails(teamId, dayIds, timeIds, txtNotes.Text.Trim());
    }

    // ── Helper factories ───────────────────────────────────────────────────────
    private static DataGridView MakePickerGrid(string[] columns)
    {
        var g = new DataGridView
        {
            Dock = DockStyle.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false, ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false,
            BorderStyle = BorderStyle.None, BackgroundColor = AppTheme.ContentBackground,
            Font = AppTheme.FontDefault, RowTemplate = { Height = 28 },
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            EnableHeadersVisualStyles = false,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppTheme.GridHeaderBackground, ForeColor = AppTheme.GridHeaderText,
                SelectionBackColor = AppTheme.GridHeaderBackground, SelectionForeColor = AppTheme.GridHeaderText,
                Font = AppTheme.FontGridHeader
            }
        };
        bool first = true;
        foreach (var col in columns)
        {
            g.Columns.Add(new DataGridViewTextBoxColumn { Name = col, HeaderText = col });
            if (first) { g.Columns[col].Visible = false; first = false; }
        }
        return g;
    }

    private static Panel PickerBar(Form form)
    {
        var bar = new Panel { Dock = DockStyle.Bottom, Height = 46, BackColor = AppTheme.Surface };
        var ok  = new Button { Text = "OK",     DialogResult = DialogResult.OK,     Left = 12,  Top = 8, Width = 100, Height = 30, FlatStyle = FlatStyle.Flat, BackColor = AppTheme.Accent, ForeColor = Color.White, Font = AppTheme.FontButton, FlatAppearance = { BorderSize = 0 } };
        var cxl = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 124, Top = 8, Width = 80,  Height = 30, FlatStyle = FlatStyle.Flat, Font = AppTheme.FontButton };
        bar.Controls.AddRange([ok, cxl]);
        return bar;
    }

    private bool ConfirmDiscard()
        => MessageBox.Show("You have unsaved changes. Discard them?", "Unsaved Changes",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;

    private static Label Lbl(string text, int x, int y) => new()
    {
        Text = text, Font = AppTheme.FontDefaultBold, ForeColor = AppTheme.TextPrimary,
        AutoSize = true, Location = new Point(x, y + 3)
    };

    private sealed record DayItem(int? Id, string DayName, string DayAbbr)
    {
        public override string ToString() => DayName;
    }

    private sealed record TimeItem(int? Id, string Time12h, string Time24h)
    {
        public override string ToString() => Time12h;
    }

    private sealed record TeamItem(int? Id, string Display)
    {
        public override string ToString() => Display;
    }
}

file static class LftStringExtensions
{
    public static string? NullIfEmpty(this string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s;
}

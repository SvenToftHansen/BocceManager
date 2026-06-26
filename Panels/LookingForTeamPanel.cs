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
    private List<DivisionItem> _allDivisions = [];

    // ── Left — grid ────────────────────────────────────────────────────────────
    private DataGridView _grid    = null!;
    private ComboBox     _cmbShow = null!;

    // ── Right — detail ─────────────────────────────────────────────────────────
    private Label          _lblPlayerName = null!;
    private Label          _lblPlacedAs   = null!;
    private TabControl     _tabs          = null!;
    private TabPage        _tabDetails    = null!;
    private TabPage        _tabGroup      = null!;
    private CheckedListBox _clbDivisions  = null!;
    private ComboBox       _cmbPrefTeam   = null!;
    private CheckBox       _chkRegDate    = null!;
    private DateTimePicker _dtpRegDate    = null!;
    private TextBox        _txtNotes      = null!;
    private DataGridView   _grpGrid       = null!;
    private Button         _btnAddMember  = null!;
    private Button         _btnRemMember  = null!;

    // ── Toolbar buttons ────────────────────────────────────────────────────────
    private Button _btnAddPlayer = null!;
    private Button _btnRemove    = null!;
    private Button _btnAssign    = null!;
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
            LoadDivisionData();
            LoadPreferredTeamCombo();
            LoadGrid();
            _isLoadingData = false;
        }
    }

    // ── UI Construction ────────────────────────────────────────────────────────
    private void BuildUi()
    {
        var topBar = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = AppTheme.Surface };

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

        _btnAssign = new Button
        {
            Text = "Assign to Team...", Location = new Point(291, 8), Size = new Size(145, 30),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.Accent, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 }, Enabled = false
        };
        _btnAssign.Click += (_, _) => AssignToTeam();

        var showLbl = new Label
        {
            Text = "Show:", Location = new Point(454, 14), AutoSize = true,
            Font = AppTheme.FontDefault, ForeColor = AppTheme.TextSecondary
        };
        _cmbShow = new ComboBox
        {
            Location = new Point(500, 11), Size = new Size(170, 26),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = AppTheme.FontDefault, BackColor = AppTheme.Surface, ForeColor = AppTheme.TextPrimary
        };
        _cmbShow.Items.AddRange(["Unplaced only", "All"]);
        _cmbShow.SelectedIndex = 0;
        _cmbShow.SelectedIndexChanged += (_, _) => LoadGrid();

        topBar.Controls.AddRange([_btnAddPlayer, _btnRemove, _btnAssign, showLbl, _cmbShow]);

        var bottomBar = new Panel { Dock = DockStyle.Bottom, Height = 46, BackColor = AppTheme.Surface };
        _btnSave = new Button
        {
            Text = "Save", Location = new Point(12, 8), Size = new Size(90, 30),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.Accent, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 }, Enabled = false
        };
        _btnSave.Click += (_, _) => SaveEntry();

        _btnCancel = new Button
        {
            Text = "Cancel", Location = new Point(114, 8), Size = new Size(80, 30),
            FlatStyle = FlatStyle.Flat, Font = AppTheme.FontButton, Cursor = Cursors.Hand,
            FlatAppearance = { BorderSize = 1 }, Visible = false
        };
        _btnCancel.Click += (_, _) => CancelEdit();
        bottomBar.Controls.AddRange([_btnSave, _btnCancel]);

        _mainSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.ContentBackground,
            Panel1MinSize = 0, Panel2MinSize = 0
        };
        _mainSplit.Panel1.Controls.Add(BuildGrid());
        _mainSplit.Panel2.Controls.Add(BuildDetailPanel());
        _mainSplit.SizeChanged   += (_, _) => SafeApplySplit();
        _mainSplit.HandleCreated += (_, _) => BeginInvoke(new Action(SafeApplySplit));

        Controls.Add(_mainSplit);
        Controls.Add(bottomBar);
        Controls.Add(topBar);
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
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
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
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "GId",    Visible = false });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "GPid",   Visible = false });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "GGrpId", Visible = false });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "GName",  HeaderText = "Name",      FillWeight = 28 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "GPhone", HeaderText = "Phone",     FillWeight = 17 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "GEmail", HeaderText = "Email",     FillWeight = 25 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "GGrp",   HeaderText = "Group",     FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "GDate",  HeaderText = "Reg. Date", FillWeight = 12 });
        _grid.SelectionChanged += OnGridSelectionChanged;

        var pnl = new Panel { Dock = DockStyle.Fill };
        pnl.Controls.Add(_grid);
        return pnl;
    }

    private Control BuildDetailPanel()
    {
        var outer = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.ContentBackground };

        _lblPlayerName = new Label
        {
            Location = new Point(12, 10), Size = new Size(380, 28),
            Font = AppTheme.FontDefaultBold, ForeColor = AppTheme.TextPrimary
        };
        _lblPlacedAs = new Label
        {
            Location = new Point(12, 40), AutoSize = true,
            Font = AppTheme.FontDefault, ForeColor = AppTheme.TextSecondary, Visible = false
        };

        _tabs = new TabControl
        {
            Location = new Point(0, 70),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
            Size = new Size(400, 400),
            Font = AppTheme.FontDefault
        };
        outer.Resize += (_, _) =>
        {
            _tabs.Size = new Size(outer.ClientSize.Width, Math.Max(100, outer.ClientSize.Height - 70));
        };

        _tabDetails = new TabPage("Details");
        _tabGroup   = new TabPage("Group");
        _tabs.TabPages.AddRange([_tabDetails, _tabGroup]);

        BuildDetailsTab();
        BuildGroupTab();

        outer.Controls.AddRange([_lblPlayerName, _lblPlacedAs, _tabs]);
        return outer;
    }

    private void BuildDetailsTab()
    {
        var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 10, 12, 0), AutoScroll = true };

        int y = 10;
        const int lx = 0, fx = 170, fw = 240;

        pnl.Controls.Add(Lbl("Preferred Divisions", lx, y));
        _clbDivisions = new CheckedListBox
        {
            Location = new Point(fx, y), Size = new Size(fw, 110),
            CheckOnClick = true,
            Font = AppTheme.FontDefault, BackColor = AppTheme.Surface, ForeColor = AppTheme.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle
        };
        _clbDivisions.ItemCheck += (_, _) => BeginInvoke(() => { if (!_isLoadingData) OnFieldChanged(null, EventArgs.Empty); });
        pnl.Controls.Add(_clbDivisions);
        y += 120;

        pnl.Controls.Add(Lbl("Preferred Team", lx, y));
        _cmbPrefTeam = new ComboBox
        {
            Location = new Point(fx, y), Size = new Size(fw, 26),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = AppTheme.FontDefault, BackColor = AppTheme.Surface, ForeColor = AppTheme.TextPrimary
        };
        _cmbPrefTeam.SelectedIndexChanged += OnFieldChanged;
        pnl.Controls.Add(_cmbPrefTeam);
        y += 36;

        _chkRegDate = new CheckBox
        {
            Text = "Registered Date", Location = new Point(lx, y + 3), AutoSize = true,
            Font = AppTheme.FontDefaultBold, ForeColor = AppTheme.TextPrimary
        };
        _dtpRegDate = new DateTimePicker
        {
            Location = new Point(fx, y), Size = new Size(fw, 26),
            Format = DateTimePickerFormat.Short, Value = DateTime.Today, Enabled = false
        };
        _chkRegDate.CheckedChanged += (_, _) =>
        {
            _dtpRegDate.Enabled = _chkRegDate.Checked;
            OnFieldChanged(null, EventArgs.Empty);
        };
        _dtpRegDate.ValueChanged += OnFieldChanged;
        pnl.Controls.Add(_chkRegDate);
        pnl.Controls.Add(_dtpRegDate);
        y += 36;

        pnl.Controls.Add(Lbl("Notes", lx, y));
        _txtNotes = new TextBox
        {
            Location = new Point(fx, y), Size = new Size(fw, 80),
            Multiline = true, ScrollBars = ScrollBars.Vertical,
            Font = AppTheme.FontDefault, BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary, BorderStyle = BorderStyle.FixedSingle
        };
        _txtNotes.TextChanged += OnFieldChanged;
        pnl.Controls.Add(_txtNotes);

        _tabDetails.Controls.Add(pnl);
    }

    private void BuildGroupTab()
    {
        var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };

        _grpGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
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
        _grpGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "GLftId", Visible = false });
        _grpGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "GName",  HeaderText = "Member", FillWeight = 50 });
        _grpGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "GPhone", HeaderText = "Phone",  FillWeight = 25 });
        _grpGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "GEmail", HeaderText = "Email",  FillWeight = 25 });

        var btnBar = new Panel { Dock = DockStyle.Bottom, Height = 42, BackColor = AppTheme.Surface };
        _btnAddMember = new Button
        {
            Text = "+ Add Member", Left = 8, Top = 6, Width = 120, Height = 30,
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.ButtonSuccess, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 }, Enabled = false
        };
        _btnAddMember.Click += (_, _) => AddMember();

        _btnRemMember = new Button
        {
            Text = "Remove from Group", Left = 140, Top = 6, Width = 150, Height = 30,
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.ButtonDanger, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 }, Enabled = false
        };
        _btnRemMember.Click += (_, _) => RemoveMember();

        _grpGrid.SelectionChanged += (_, _) =>
            _btnRemMember.Enabled = _grpGrid.SelectedRows.Count > 0;

        btnBar.Controls.AddRange([_btnAddMember, _btnRemMember]);
        pnl.Controls.AddRange([_grpGrid, btnBar]);
        _tabGroup.Controls.Add(pnl);
    }

    // ── Data loading ───────────────────────────────────────────────────────────
    private void LoadDivisionData()
    {
        _allDivisions.Clear();
        if (!_seasonId.HasValue) { _clbDivisions?.Items.Clear(); return; }
        try
        {
            using var db = new BocceDbContext();
            var divs = db.Divisions
                .Where(d => d.SeasonId == _seasonId.Value)
                .OrderBy(d => d.SortName).ThenBy(d => d.Name)
                .Select(d => new { d.Id, d.Name })
                .ToList();
            foreach (var d in divs)
                _allDivisions.Add(new DivisionItem(d.Id, d.Name));
        }
        catch { }

        _clbDivisions.Items.Clear();
        foreach (var d in _allDivisions)
            _clbDivisions.Items.Add(d);
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

            bool unplacedOnly = _cmbShow.SelectedIndex == 0;
            try
            {
                using var db = new BocceDbContext();
                var query = db.LookingForTeams
                    .Include(l => l.Player)
                    .Where(l => l.LeagueId == _leagueId.Value && l.SeasonId == _seasonId.Value);

                if (unplacedOnly) query = query.Where(l => l.TeamId == null);

                var list = query
                    .OrderBy(l => l.Player.LastName).ThenBy(l => l.Player.FirstName)
                    .ToList();

                var groupCounts = list
                    .Where(l => l.LookingForTeamGroupId.HasValue)
                    .GroupBy(l => l.LookingForTeamGroupId!.Value)
                    .ToDictionary(g => g.Key, g => g.Count());

                foreach (var e in list)
                {
                    string name = $"{e.Player.LastName}, {e.Player.FirstName}".Trim().TrimStart(',').Trim();
                    string date = e.RegisteredDate.HasValue ? e.RegisteredDate.Value.ToString("yyyy-MM-dd") : "";
                    string grpLabel = e.LookingForTeamGroupId.HasValue
                        ? GroupLabel(groupCounts.GetValueOrDefault(e.LookingForTeamGroupId.Value, 1))
                        : "Solo";
                    _grid.Rows.Add(e.Id, e.PlayerId, e.LookingForTeamGroupId,
                        name, e.Player.Phone ?? "", e.Player.Email ?? "", grpLabel, date);
                }
            }
            catch { }

            if (_selectedLftId.HasValue)
            {
                for (int i = 0; i < _grid.Rows.Count; i++)
                {
                    if (Convert.ToInt32(_grid.Rows[i].Cells["GId"].Value) == _selectedLftId.Value)
                    { _grid.Rows[i].Selected = true; break; }
                }
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
                .Include(l => l.PreferredDivisions)
                .FirstOrDefault(l => l.Id == lftId);
            if (e == null) return;

            bool isPlaced = e.TeamId.HasValue;
            string playerName = _grid.SelectedRows.Count > 0
                ? _grid.SelectedRows[0].Cells["GName"].Value?.ToString() ?? "" : "";

            _lblPlayerName.Text = playerName;

            if (isPlaced && e.Team != null)
            {
                _lblPlacedAs.Text    = $"Placed on: {e.Team.EffectiveDisplayName}";
                _lblPlacedAs.Visible = true;
            }
            else
            {
                _lblPlacedAs.Visible = false;
            }

            var checkedIds = e.PreferredDivisions.Select(d => d.DivisionId).ToHashSet();
            for (int i = 0; i < _clbDivisions.Items.Count; i++)
            {
                bool check = _clbDivisions.Items[i] is DivisionItem d && d.Id.HasValue && checkedIds.Contains(d.Id.Value);
                _clbDivisions.SetItemChecked(i, check);
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

            if (e.RegisteredDate.HasValue)
            {
                _chkRegDate.Checked = true;
                _dtpRegDate.Value   = e.RegisteredDate.Value.ToDateTime(TimeOnly.MinValue);
                _dtpRegDate.Enabled = true;
            }
            else
            {
                _chkRegDate.Checked = false;
                _dtpRegDate.Value   = DateTime.Today;
                _dtpRegDate.Enabled = false;
            }

            _txtNotes.Text = e.Notes ?? "";

            _btnRemove.Enabled    = true;
            _btnAssign.Enabled    = !isPlaced;
            _btnAddMember.Enabled = true;
            _btnSave.Enabled      = false;
            _btnCancel.Visible    = false;
            ClearDirty();

            LoadGroupMembers(lftId, e.LookingForTeamGroupId);
            UpdateGroupTabTitle(e.LookingForTeamGroupId);
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

    private void UpdateGroupTabTitle(int? groupId)
    {
        if (!groupId.HasValue) { _tabGroup.Text = "Group"; return; }
        try
        {
            using var db = new BocceDbContext();
            var grp = db.LookingForTeamGroups.FirstOrDefault(g => g.Id == groupId.Value);
            if (grp == null) { _tabGroup.Text = "Group"; return; }
            int count = db.LookingForTeams.Count(l => l.LookingForTeamGroupId == groupId.Value);
            string title = grp.Name ?? $"Group {groupId}";
            _tabGroup.Text = $"{title} ({count})";
        }
        catch { _tabGroup.Text = "Group"; }
    }

    private void ClearDetail()
    {
        _isLoadingData = true;
        try
        {
            _selectedLftId       = null;
            _selectedGroupId     = null;
            _lblPlayerName.Text  = "";
            _lblPlacedAs.Visible = false;
            for (int i = 0; i < _clbDivisions.Items.Count; i++)
                _clbDivisions.SetItemChecked(i, false);
            if (_cmbPrefTeam.Items.Count > 0) _cmbPrefTeam.SelectedIndex = 0;
            _chkRegDate.Checked   = false;
            _dtpRegDate.Value     = DateTime.Today;
            _dtpRegDate.Enabled   = false;
            _txtNotes.Text        = "";
            _grpGrid.Rows.Clear();
            _tabGroup.Text        = "Group";
            _btnRemove.Enabled    = false;
            _btnAssign.Enabled    = false;
            _btnSave.Enabled      = false;
            _btnCancel.Visible    = false;
            _btnAddMember.Enabled = false;
            _btnRemMember.Enabled = false;
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

        if (picked.Count > 1)
        {
            var ans = MessageBox.Show($"Add {picked.Count} players as a group or as solos?",
                "Multiple Players", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (ans == DialogResult.Cancel) return;
            bool asGroup = ans == DialogResult.Yes;

            var details = PromptLftDetails();
            if (details == null) return;

            int? groupId = null;
            if (asGroup)
            {
                try
                {
                    using var db = new BocceDbContext();
                    var p1 = db.Players.FirstOrDefault(p => p.Id == picked[0]);
                    string groupName = p1 != null ? $"{p1.LastName}'s Group" : "Group";
                    var grp = new LookingForTeamGroup
                    {
                        LeagueId = _leagueId.Value,
                        SeasonId = _seasonId.Value,
                        Name = groupName,
                        CreatedAt = DateTime.UtcNow
                    };
                    db.LookingForTeamGroups.Add(grp);
                    db.SaveChanges();
                    groupId = grp.Id;
                }
                catch { }
            }

            int firstLftId = 0;
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
                        Notes           = details.Notes.NullIfEmpty(),
                        RegisteredDate  = details.RegDate,
                        LookingForTeamGroupId = groupId
                    };
                    db.LookingForTeams.Add(entry);
                    db.SaveChanges();

                    foreach (int divId in details.PrefDivisionIds)
                        db.LookingForTeamDivisions.Add(new LookingForTeamDivision
                            { LookingForTeamId = entry.Id, DivisionId = divId });
                    db.SaveChanges();

                    if (firstLftId == 0) firstLftId = entry.Id;
                    AppLogger.Info("Added player {PlayerId} to LFT for season {SeasonId}", playerId, _seasonId.Value);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not add player:\n{ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            _selectedLftId = firstLftId > 0 ? firstLftId : null;
            LoadGrid();
            return;
        }

        // Single player - existing flow
        if (!CheckAndHandleTeamMembership(picked[0])) return;

        var singleDetails = PromptLftDetails();
        if (singleDetails == null) return;

        int newLftId;
        try
        {
            using var db = new BocceDbContext();
            var entry = new LookingForTeam
            {
                LeagueId        = _leagueId.Value,
                SeasonId        = _seasonId.Value,
                PlayerId        = picked[0],
                PreferredTeamId = singleDetails.PrefTeamId,
                Notes           = singleDetails.Notes.NullIfEmpty(),
                RegisteredDate  = singleDetails.RegDate
            };
            db.LookingForTeams.Add(entry);
            db.SaveChanges();

            foreach (int divId in singleDetails.PrefDivisionIds)
                db.LookingForTeamDivisions.Add(new LookingForTeamDivision
                    { LookingForTeamId = entry.Id, DivisionId = divId });
            db.SaveChanges();
            newLftId = entry.Id;
            AppLogger.Info("Added player {PlayerId} to LFT for season {SeasonId}", picked[0], _seasonId.Value);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not add player:\n{ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _selectedLftId = newLftId;
        LoadGrid();
    }

    private void RemoveFromLft()
    {
        if (!_selectedLftId.HasValue) return;

        try
        {
            using var db = new BocceDbContext();
            var e = db.LookingForTeams.Find(_selectedLftId.Value);
            if (e?.TeamId.HasValue == true)
            {
                MessageBox.Show("This player has already been placed on a team. Remove the team assignment first.",
                    "Cannot Remove", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }
        catch { return; }

        if (MessageBox.Show("Remove this player from the Looking for Team list?",
                "Confirm Remove", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        try
        {
            using var db = new BocceDbContext();
            var e = db.LookingForTeams
                .Include(l => l.PreferredDivisions)
                .FirstOrDefault(l => l.Id == _selectedLftId.Value);
            if (e != null)
            {
                int? groupId = e.LookingForTeamGroupId;
                if (groupId.HasValue)
                {
                    e.LookingForTeamGroupId = null;
                    db.SaveChanges();
                    DissolveGroupIfSingleton(db, groupId.Value);
                }
                db.LookingForTeams.Remove(e);
                db.SaveChanges();
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

        var checkedDivIds = _clbDivisions.CheckedItems
            .OfType<DivisionItem>()
            .Where(d => d.Id.HasValue)
            .Select(d => d.Id!.Value)
            .ToHashSet();

        int? prefTeamId   = (_cmbPrefTeam.SelectedItem as TeamItem)?.Id;
        DateOnly? regDate = _chkRegDate.Checked ? DateOnly.FromDateTime(_dtpRegDate.Value) : null;

        try
        {
            using var db = new BocceDbContext();
            var e = db.LookingForTeams
                .Include(l => l.PreferredDivisions)
                .FirstOrDefault(l => l.Id == _selectedLftId.Value);
            if (e == null) return;

            e.PreferredTeamId = prefTeamId;
            e.Notes           = _txtNotes.Text.Trim().NullIfEmpty();
            e.RegisteredDate  = regDate;

            var existingIds = e.PreferredDivisions.Select(d => d.DivisionId).ToHashSet();
            foreach (var del in e.PreferredDivisions.Where(d => !checkedDivIds.Contains(d.DivisionId)).ToList())
                db.LookingForTeamDivisions.Remove(del);
            foreach (int addId in checkedDivIds.Where(id => !existingIds.Contains(id)))
                db.LookingForTeamDivisions.Add(new LookingForTeamDivision
                    { LookingForTeamId = e.Id, DivisionId = addId });

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

    private void AssignToTeam()
    {
        if (!_selectedLftId.HasValue) return;
        if (_isDirty)
        {
            MessageBox.Show("Save your changes before assigning.", "Unsaved Changes",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string playerName = _lblPlayerName.Text;
        int? partnerPlayerId = null;

        try
        {
            using var db = new BocceDbContext();
            var player = db.Players.FirstOrDefault(p => p.Id == (int)(_grid.SelectedRows[0].Cells["GPid"].Value ?? 0));
            if (player?.PartnerPlayerId.HasValue == true)
                partnerPlayerId = player.PartnerPlayerId;
        }
        catch { }

        List<(int LftId, string Name)> groupPeers = [];
        if (_selectedGroupId.HasValue)
        {
            try
            {
                using var db = new BocceDbContext();
                groupPeers = db.LookingForTeams
                    .Include(l => l.Player)
                    .Where(l => l.LookingForTeamGroupId == _selectedGroupId.Value
                             && l.Id != _selectedLftId.Value
                             && l.TeamId == null)
                    .AsEnumerable()
                    .Select(l => (l.Id,
                        $"{l.Player.LastName}, {l.Player.FirstName}".Trim().TrimStart(',').Trim()))
                    .ToList();
            }
            catch { }
        }

        int? teamId = PickTeamDialog($"Assign {playerName} to team:");
        if (!teamId.HasValue) return;

        List<int> toAssign = [_selectedLftId.Value];
        int teamCapacity = 5;
        try
        {
            using var db = new BocceDbContext();
            var team = db.Teams.Include(t => t.Division).Include(t => t.TeamPlayers).FirstOrDefault(t => t.Id == teamId.Value);
            if (team != null)
            {
                teamCapacity = team.Division.PlayersPerTeamMaximum ?? 5;
                int roomNeeded = 1;
                if (partnerPlayerId.HasValue) roomNeeded++;
                if (groupPeers.Count > 0) roomNeeded += groupPeers.Count;
                int availableRoom = teamCapacity - team.TeamPlayers.Count;

                if (availableRoom < roomNeeded)
                {
                    string msg = $"Not enough room on this team. ";
                    if (partnerPlayerId.HasValue)
                        msg += $"Player {playerName} has a partner, needs {roomNeeded} spots but only {availableRoom} available.";
                    else if (groupPeers.Count > 0)
                        msg += $"Group needs {roomNeeded} spots but only {availableRoom} available.";
                    else
                        msg += $"Needs {roomNeeded} spots but only {availableRoom} available.";

                    var choice = MessageBox.Show($"{msg}\n\nAssign anyway?", "Not Enough Room",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (choice != DialogResult.Yes) return;
                }
            }
        }
        catch { }

        if (groupPeers.Count > 0)
        {
            int teamCount = 0;
            try { using var db = new BocceDbContext(); teamCount = db.TeamPlayers.Count(tp => tp.TeamId == teamId.Value); }
            catch { }

            var peers = groupPeers.Take(Math.Max(0, teamCapacity - teamCount - 1)).ToList();
            if (peers.Count > 0)
            {
                string peerNames = string.Join("\n", peers.Select(p => $"  • {p.Name}"));
                string msg = $"{playerName} is in a group with:\n{peerNames}\n\nAdd group members to the same team?";
                var ans = MessageBox.Show(msg, "Group Members", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (ans == DialogResult.Cancel) return;
                if (ans == DialogResult.Yes) toAssign.AddRange(peers.Select(p => p.LftId));
            }
        }

        foreach (int lid in toAssign)
        {
            try
            {
                using var db = new BocceDbContext();
                var (ok, msg) = TeamApplicantService.PlaceLftPlayer(db, lid, teamId.Value);
                if (!ok && lid == _selectedLftId.Value)
                {
                    MessageBox.Show(msg, "Assignment Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        ClearDetail();
        LoadGrid();
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
            int newLftId = EnsureLftEntry(db, pickedPlayerId.Value);
            MergeIntoGroup(db, _selectedLftId.Value, newLftId);
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

        if (MessageBox.Show($"Remove {memberName} from this group?\n(They will remain in the LFT list as Solo.)",
                "Remove from Group", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        try
        {
            using var db = new BocceDbContext();
            var member = db.LookingForTeams.Find(memberLftId);
            if (member == null) return;
            int? groupId = member.LookingForTeamGroupId;
            member.LookingForTeamGroupId = null;
            db.SaveChanges();
            if (groupId.HasValue) DissolveGroupIfSingleton(db, groupId.Value);
            AppLogger.Info("Removed LFT {LftId} from group {GroupId}", memberLftId, groupId);
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

    // ── Group helpers ──────────────────────────────────────────────────────────
    private int EnsureLftEntry(BocceDbContext db, int playerId)
    {
        var existing = db.LookingForTeams.FirstOrDefault(l =>
            l.PlayerId == playerId && l.LeagueId == _leagueId!.Value && l.SeasonId == _seasonId!.Value);
        if (existing != null) return existing.Id;

        var entry = new LookingForTeam
        {
            LeagueId       = _leagueId!.Value,
            SeasonId       = _seasonId!.Value,
            PlayerId       = playerId,
            RegisteredDate = DateOnly.FromDateTime(DateTime.Today)
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
            string groupName = $"{e1.Player.LastName}'s Group";
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
        _ => count <= 1 ? "Solo" : $"Group({count})"
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

    private record LftInitDetails(int? PrefTeamId, List<int> PrefDivisionIds, string Notes, DateOnly? RegDate);

    private LftInitDetails? PromptLftDetails()
    {
        using var form = new Form
        {
            Text = "Looking For Team — Details", Width = 480, Height = 370,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false,
            BackColor = AppTheme.ContentBackground
        };
        var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 12, 16, 0), BackColor = AppTheme.ContentBackground };

        int y = 10; const int lx = 0, fx = 160, fw = 270;

        pnl.Controls.Add(Lbl("Preferred Divisions", lx, y));
        var clbDiv = new CheckedListBox
        {
            Location = new Point(fx, y), Size = new Size(fw, 90), CheckOnClick = true,
            Font = AppTheme.FontDefault, BackColor = AppTheme.Surface, BorderStyle = BorderStyle.FixedSingle
        };
        foreach (var d in _allDivisions) clbDiv.Items.Add(d);
        pnl.Controls.Add(clbDiv);
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

        var chkDate = new CheckBox
        {
            Text = "Registered Date", Location = new Point(lx, y + 3), AutoSize = true,
            Font = AppTheme.FontDefaultBold, ForeColor = AppTheme.TextPrimary
        };
        var dtpDate = new DateTimePicker
        {
            Location = new Point(fx, y), Size = new Size(fw, 26),
            Format = DateTimePickerFormat.Short, Value = DateTime.Today, Enabled = false
        };
        chkDate.CheckedChanged += (_, _) => dtpDate.Enabled = chkDate.Checked;
        pnl.Controls.Add(chkDate); pnl.Controls.Add(dtpDate);
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

        var divIds  = clbDiv.CheckedItems.OfType<DivisionItem>().Where(d => d.Id.HasValue).Select(d => d.Id!.Value).ToList();
        int? teamId = (cmbTeam.SelectedItem as TeamItem)?.Id;
        DateOnly? dt = chkDate.Checked ? DateOnly.FromDateTime(dtpDate.Value) : null;
        return new LftInitDetails(teamId, divIds, txtNotes.Text.Trim(), dt);
    }

    private int? PickTeamDialog(string prompt)
    {
        using var form = new Form
        {
            Text = "Select Team", Width = 600, Height = 540,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false,
            BackColor = AppTheme.ContentBackground
        };

        var filterBar = new Panel { Dock = DockStyle.Top, Height = 62, BackColor = AppTheme.Surface };
        var chkMax4 = new CheckBox { Text = "Only teams with room (≤ 4 players)",           Location = new Point(10, 10), AutoSize = true, Font = AppTheme.FontDefault, ForeColor = AppTheme.TextPrimary };
        var chkMax3 = new CheckBox { Text = "Only teams with lots of room (≤ 3 players)", Location = new Point(10, 36), AutoSize = true, Font = AppTheme.FontDefault, ForeColor = AppTheme.TextPrimary };
        filterBar.Controls.AddRange([chkMax4, chkMax3]);

        var hint = new Label
        {
            Dock = DockStyle.Top, Height = 28, Text = $"  {prompt}",
            Font = AppTheme.FontDefault, ForeColor = AppTheme.TextMuted, BackColor = AppTheme.Surface,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var grid = MakePickerGrid(["TId", "Division", "Team", "Time"]);
        grid.Columns["Time"].FillWeight = 15;
        grid.DoubleClick += (_, _) => { if (grid.SelectedRows.Count > 0) form.DialogResult = DialogResult.OK; };

        List<(int Id, string Div, string Display, string DayTime, int Count, int MaxCapacity)> teams = [];
        try
        {
            using var db = new BocceDbContext();
            teams = db.Teams
                .Include(t => t.Division)
                .Include(t => t.TeamPlayers)
                .Where(t => t.Division.SeasonId == _seasonId && t.IsActive)
                .OrderBy(t => t.Division.SortName).ThenBy(t => t.SortOrder)
                .AsEnumerable()
                .Select(t => (t.Id, t.Division.Name,
                    $"{t.TeamLetter} — {t.EffectiveDisplayName} ({t.TeamPlayers.Count})",
                    ExtractDayTime(t.SystemName),
                    t.TeamPlayers.Count,
                    t.Division.PlayersPerTeamMaximum ?? 5))
                .ToList();
        }
        catch { }

        void Filter()
        {
            grid.Rows.Clear();
            foreach (var (id, div, display, dayTime, count, max) in teams)
            {
                if (chkMax4.Checked && count >= 5) continue;
                if (chkMax3.Checked && count >= 4) continue;
                grid.Rows.Add(id, div, display, dayTime);
            }
            if (grid.Rows.Count > 0) grid.Rows[0].Selected = true;
        }

        chkMax4.CheckedChanged += (_, _) => Filter();
        chkMax3.CheckedChanged += (_, _) => Filter();
        Filter();

        var bar = PickerBar(form);
        ((Button)bar.Controls[0]!).Text = "Assign";
        form.Controls.AddRange([grid, bar, hint, filterBar]);
        form.AcceptButton = (Button)bar.Controls[0];
        form.CancelButton = (Button)bar.Controls[1];

        if (form.ShowDialog(this) == DialogResult.OK && grid.SelectedRows.Count > 0)
        {
            var v = grid.SelectedRows[0].Cells[0].Value;
            if (v != null && v != DBNull.Value) return Convert.ToInt32(v);
        }
        return null;
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

    private sealed record DivisionItem(int? Id, string Name)
    {
        public override string ToString() => Name;
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

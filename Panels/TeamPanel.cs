using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.Services;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Panels;

public class TeamPanel : UserControl
{
    private enum TeamMode { View, Edit, Create }
    private TeamMode _teamMode = TeamMode.View;
    private bool _isLoadingData = false;

    private int? _selectedLeagueId;
    private int? _selectedSeasonId;
    private int? _selectedDivisionId;
    private int? _selectedTeamId;
    private List<(int Id, string Display)> _allTeams = [];

    // Left panel
    private ComboBox _cmbDivision = null!;
    private TextBox _txtSearch = null!;
    private ListBox _lstTeams = null!;

    // Editor tab
    private ComboBox _cmbDivisionEditor = null!;
    private Label _lblSystemName = null!;
    private TextBox _txtDisplayName = null!;
    private CheckBox _chkActive = null!;
    private ComboBox _cmbCaptain = null!;
    private Label _lblCreated = null!;

    // Players tab
    private DataGridView _lstTeamPlayers = null!;
    private Button _btnAddPlayer = null!;
    private Button _btnRemovePlayer = null!;
    private ComboBox _cmbCaptainPlayers = null!;

    // Toolbar
    private Button _btnEdit = null!;
    private Button _btnDelete = null!;
    private Button _btnSave = null!;
    private Button _btnCancel = null!;
    private Button _btnNew = null!;

    private TabControl _tabs = null!;

    public TeamPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;
        BuildUI();
        AppParameterService.DefaultsChanged += OnDefaultsChanged;
        LoadContext();
    }

    private void OnDefaultsChanged(object? sender, DefaultsChangedEventArgs e)
    {
        LoadContext();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            AppParameterService.DefaultsChanged -= OnDefaultsChanged;
        base.Dispose(disposing);
    }

    private void BuildUI()
    {
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            Panel1MinSize = 0,
            Panel2MinSize = 0,
            BackColor = AppTheme.ContentBackground
        };

        void Apply()
        {
            if (split.Width <= 1) return;
            const int preferred = 220, desiredLeft = 180, desiredRight = 400;
            int maxTotal = Math.Max(0, split.Width - 1);
            int leftMin = desiredLeft;
            int rightMin = desiredRight;
            if (leftMin + rightMin > maxTotal)
            {
                if (maxTotal == 0) { leftMin = 0; rightMin = 0; }
                else { double r = desiredLeft / (double)(desiredLeft + desiredRight); leftMin = (int)Math.Floor(maxTotal * r); rightMin = maxTotal - leftMin; }
            }
            split.Panel1MinSize = leftMin;
            split.Panel2MinSize = rightMin;
            int maxLeft = split.Width - rightMin;
            if (maxLeft < leftMin) maxLeft = leftMin;
            int dist = Math.Max(leftMin, Math.Min(preferred, maxLeft));
            split.FixedPanel = FixedPanel.Panel1;
            split.IsSplitterFixed = true;
            if (dist > 0) split.SplitterDistance = dist;
        }

        split.SizeChanged += (_, _) => Apply();
        split.HandleCreated += (_, _) => BeginInvoke(new Action(Apply));

        BuildLeftPanel(split.Panel1);
        BuildRightPanel(split.Panel2);
        Controls.Add(split);
    }

    private void BuildLeftPanel(SplitterPanel panel)
    {
        panel.BackColor = AppTheme.Surface;
        panel.Padding = new Padding(8, 8, 8, 8);

        var lblTitle = new Label
        {
            Dock = DockStyle.Top,
            Text = "Teams",
            Font = AppTheme.FontSmallBold,
            ForeColor = AppTheme.TextPrimary,
            Height = 22,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var lblDivision = new Label
        {
            Dock = DockStyle.Top,
            Text = "Division:",
            Font = AppTheme.FontDefault,
            ForeColor = AppTheme.TextPrimary,
            Height = 20,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _cmbDivision = new ComboBox
        {
            Dock = DockStyle.Top,
            Font = AppTheme.FontDefault,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Height = 28
        };
        _cmbDivision.SelectedIndexChanged += OnDivisionSelected;

        _txtSearch = new TextBox
        {
            Dock = DockStyle.Top,
            Font = AppTheme.FontDefault,
            ForeColor = AppTheme.TextSecondary,
            BackColor = AppTheme.ContentBackground,
            Text = "Search...",
            Height = 28
        };
        _txtSearch.Enter += (_, _) => { if (_txtSearch.Text == "Search...") { _txtSearch.Text = ""; _txtSearch.ForeColor = AppTheme.TextPrimary; } };
        _txtSearch.Leave += (_, _) => { if (string.IsNullOrEmpty(_txtSearch.Text)) { _txtSearch.Text = "Search..."; _txtSearch.ForeColor = AppTheme.TextSecondary; } };
        _txtSearch.TextChanged += FilterTeamListHandler;

        _lstTeams = new ListBox
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.FontDefault,
            BackColor = AppTheme.ContentBackground,
            ForeColor = AppTheme.TextPrimary,
            BorderStyle = BorderStyle.None,
            IntegralHeight = false
        };
        _lstTeams.SelectedIndexChanged += OnTeamSelected;

        _btnNew = new Button
        {
            Dock = DockStyle.Bottom,
            Text = "+ New Team",
            Height = 32,
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.ButtonSuccess,
            ForeColor = Color.White,
            Font = AppTheme.FontButton,
            Cursor = Cursors.Hand,
            FlatAppearance = { BorderSize = 0 }
        };
        _btnNew.Click += (_, _) => StartNewTeam();

        panel.Controls.Add(_lstTeams);
        panel.Controls.Add(_txtSearch);
        panel.Controls.Add(_cmbDivision);
        panel.Controls.Add(lblDivision);
        panel.Controls.Add(lblTitle);
        panel.Controls.Add(_btnNew);
    }

    private void BuildRightPanel(SplitterPanel panel)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54f));

        var tabs = BuildTabs();
        var toolbar = BuildToolbar();

        layout.Controls.Add(tabs, 0, 0);
        layout.Controls.Add(toolbar, 0, 1);
        panel.Controls.Add(layout);
    }

    private TabControl BuildTabs()
    {
        _tabs = new TabControl { Dock = DockStyle.Fill, Font = AppTheme.FontDefault, Padding = new Point(16, 6) };
        _tabs.TabPages.Add(BuildEditorTab());
        _tabs.TabPages.Add(BuildPlayersTab());
        return _tabs;
    }

    private TabPage BuildEditorTab()
    {
        var page = new TabPage("  Editor  ");
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = AppTheme.ContentBackground };

        const int lx = 20, ix = 220, iw = 420;
        int y = 20;
        var cc = new List<Control>();
        void Add(params Control[] items) => cc.AddRange(items);

        Add(Lbl("Division", lx, y));
        _cmbDivisionEditor = new ComboBox { Location = new Point(ix, y), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList, Font = AppTheme.FontDefault };
        _cmbDivisionEditor.SelectedIndexChanged += OnEditorDivisionChanged;
        Add(_cmbDivisionEditor); y += 44;

        Add(Lbl("System Name", lx, y));
        _lblSystemName = new Label { Location = new Point(ix, y + 3), AutoSize = true, Font = AppTheme.FontDefaultBold, ForeColor = AppTheme.TextPrimary };
        Add(_lblSystemName, Hint("Auto-computed", ix + 120, y + 4)); y += 38;

        Add(Lbl("Display Name", lx, y));
        _txtDisplayName = new TextBox { Location = new Point(ix, y), Width = 420, Font = AppTheme.FontDefault, Height = 26 };
        Add(_txtDisplayName, Hint("Optional; auto-set from captain", ix + 220, y + 4)); y += 38;

        Add(Lbl("Active", lx, y));
        _chkActive = new CheckBox { Location = new Point(ix, y), AutoSize = true, Font = AppTheme.FontDefault, ForeColor = AppTheme.TextPrimary, Checked = true };
        Add(_chkActive); y += 38;

        Add(Lbl("Created", lx, y));
        _lblCreated = new Label { Location = new Point(ix, y + 3), AutoSize = true, Font = AppTheme.FontDefault, ForeColor = AppTheme.TextSecondary };
        Add(_lblCreated); y += 44;

        Add(Sep(lx, y, iw + ix - lx)); y += 10;
        Add(SecHdr("Captain", lx, y)); y += 34;

        Add(Lbl("Captain", lx, y));
        _cmbCaptain = new ComboBox { Location = new Point(ix, y), Width = 280, DropDownStyle = ComboBoxStyle.DropDownList, Font = AppTheme.FontDefault, Enabled = false };
        _cmbCaptain.SelectedIndexChanged += OnCaptainChanged;
        Add(_cmbCaptain, Hint("Choose from players on team", ix + 290, y + 4)); y += 44;

        scroll.Controls.AddRange([.. cc]);
        page.Controls.Add(scroll);
        return page;
    }

    private TabPage BuildPlayersTab()
    {
        var page = new TabPage("  Players  ");
        var playerPanel = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.ContentBackground };

        // DataGridView for team players (matching DivisionPanel style)
        _lstTeamPlayers = new DataGridView
        {
            Dock = DockStyle.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            BorderStyle = BorderStyle.None,
            BackgroundColor = AppTheme.ContentBackground,
            GridColor = AppTheme.GridLines,
            Font = AppTheme.FontDefault,
            ReadOnly = true,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppTheme.GridHeaderBackground,
                ForeColor = AppTheme.GridHeaderText,
                SelectionBackColor = AppTheme.GridHeaderBackground,
                SelectionForeColor = AppTheme.GridHeaderText,
                Font = AppTheme.FontGridHeader,
                Padding = new Padding(4, 0, 0, 0)
            },
            EnableHeadersVisualStyles = false,
            RowTemplate = { Height = 28 },
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        _lstTeamPlayers.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = AppTheme.GridAlternateRow };
        _lstTeamPlayers.Columns.Add(new DataGridViewTextBoxColumn { Name = "PlId", Visible = false });
        _lstTeamPlayers.Columns.Add(new DataGridViewTextBoxColumn { Name = "PlName", HeaderText = "Player", FillWeight = 70 });
        _lstTeamPlayers.Columns.Add(new DataGridViewTextBoxColumn { Name = "PlRole", HeaderText = "Role", FillWeight = 30 });
        _lstTeamPlayers.SelectionChanged += (_, _) => _btnRemovePlayer.Enabled = _lstTeamPlayers.SelectedRows.Count > 0;

        // Captain selector strip
        var captainStrip = new Panel { Dock = DockStyle.Bottom, Height = 42, BackColor = AppTheme.Surface, Padding = new Padding(10, 8, 8, 6) };
        var captainLbl = new Label { Text = "Captain:", Left = 10, Top = 12, AutoSize = true, Font = AppTheme.FontDefaultBold, ForeColor = AppTheme.TextPrimary };
        _cmbCaptainPlayers = new ComboBox { Left = 85, Top = 8, Width = 260, DropDownStyle = ComboBoxStyle.DropDownList, Font = AppTheme.FontDefault, Enabled = false };
        _cmbCaptainPlayers.SelectedIndexChanged += (_, _) => OnCaptainChanged();
        captainStrip.Controls.AddRange([captainLbl, _cmbCaptainPlayers]);

        // Button panel (right side)
        var playerBtns = new Panel { Dock = DockStyle.Right, Width = 148, BackColor = AppTheme.ContentBackground, Padding = new Padding(8) };
        _btnAddPlayer = new Button
        {
            Text = "Add Player",
            Location = new Point(8, 0),
            Size = new Size(132, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.ButtonSuccess,
            ForeColor = Color.White,
            Font = AppTheme.FontButton,
            Cursor = Cursors.Hand,
            FlatAppearance = { BorderSize = 0 },
            Enabled = false
        };
        _btnAddPlayer.Click += (_, _) => AddPlayerToTeam();

        _btnRemovePlayer = new Button
        {
            Text = "Remove Player",
            Location = new Point(8, 38),
            Size = new Size(132, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.ButtonDanger,
            ForeColor = Color.White,
            Font = AppTheme.FontButton,
            Cursor = Cursors.Hand,
            FlatAppearance = { BorderSize = 0 },
            Enabled = false
        };
        _btnRemovePlayer.Click += (_, _) => RemovePlayerFromTeam();

        playerBtns.Controls.AddRange([_btnAddPlayer, _btnRemovePlayer]);

        // Dock order: Fill first, then Right, Bottom
        playerPanel.Controls.Add(_lstTeamPlayers);
        playerPanel.Controls.Add(playerBtns);
        playerPanel.Controls.Add(captainStrip);

        page.Controls.Add(playerPanel);
        return page;
    }

    private void SafeApplySplitDistance()
    {
        if (_tabs?.TabPages.Count < 2) return;
        var split = _tabs.TabPages[1].Controls.OfType<Panel>().FirstOrDefault()?.Controls.OfType<SplitContainer>().FirstOrDefault();
        if (split == null || split.Height <= 1) return;
        split.SplitterDistance = Math.Max(100, split.Height / 2);
    }

    private Panel BuildToolbar()
    {
        var toolbar = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 54,
            BackColor = AppTheme.Surface,
            Padding = new Padding(12, 10, 12, 10)
        };

        _btnEdit = new Button
        {
            Text = "Edit Team",
            Location = new Point(12, 10),
            Size = new Size(130, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.Accent,
            ForeColor = Color.White,
            Font = AppTheme.FontButton,
            Cursor = Cursors.Hand,
            FlatAppearance = { BorderSize = 0 },
            Visible = false
        };
        _btnEdit.Click += (_, _) => EnterEditMode();

        _btnDelete = new Button
        {
            Text = "Delete Team",
            Location = new Point(150, 10),
            Size = new Size(140, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.ButtonDanger,
            ForeColor = Color.White,
            Font = AppTheme.FontButton,
            Cursor = Cursors.Hand,
            FlatAppearance = { BorderSize = 0 },
            Visible = false,
            Enabled = false
        };
        _btnDelete.Click += (_, _) => DeleteTeam();

        _btnSave = new Button
        {
            Text = "Save Team",
            Location = new Point(12, 10),
            Size = new Size(130, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.Accent,
            ForeColor = Color.White,
            Font = AppTheme.FontButton,
            Cursor = Cursors.Hand,
            FlatAppearance = { BorderSize = 0 },
            Visible = false
        };
        _btnSave.Click += (_, _) => SaveTeam();

        _btnCancel = new Button
        {
            Text = "Cancel",
            Location = new Point(150, 10),
            Size = new Size(140, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.FontButton,
            Cursor = Cursors.Hand,
            FlatAppearance = { BorderSize = 1, BorderColor = AppTheme.Separator },
            Visible = false
        };
        _btnCancel.Click += (_, _) => ExitEditMode();

        toolbar.Controls.AddRange([_btnEdit, _btnDelete, _btnSave, _btnCancel]);
        return toolbar;
    }

    private void LoadContext()
    {
        try
        {
            using var db = new BocceDbContext();
            _selectedLeagueId = AppParameterService.GetDefaultLeagueId(db);
            _selectedSeasonId = AppParameterService.GetDefaultSeasonId(db);
        }
        catch { }
        LoadDivisionCombo();
        LoadTeamList();
    }

    private void LoadDivisionCombo()
    {
        _isLoadingData = true;
        _cmbDivision.SelectedIndexChanged -= OnDivisionSelected;
        _cmbDivisionEditor.SelectedIndexChanged -= OnEditorDivisionChanged;
        _cmbDivision.Items.Clear();
        _cmbDivisionEditor.Items.Clear();

        if (_selectedSeasonId.HasValue)
        {
            try
            {
                using var db = new BocceDbContext();
                var divisions = db.Divisions
                    .Where(d => d.SeasonId == _selectedSeasonId.Value)
                    .OrderBy(d => d.SortName).ThenBy(d => d.Name)
                    .Select(d => new { d.Id, Display = d.Name + (d.IsActive ? "" : " (inactive)") })
                    .ToList();

                _cmbDivision.Items.Add(new DivItem(0, "(select division)"));
                _cmbDivisionEditor.Items.Add(new DivItem(0, "(select division)"));
                foreach (var div in divisions)
                {
                    _cmbDivision.Items.Add(new DivItem(div.Id, div.Display));
                    _cmbDivisionEditor.Items.Add(new DivItem(div.Id, div.Display));
                }
            }
            catch { }
        }

        _cmbDivision.SelectedIndex = _selectedDivisionId.HasValue ? FindDivisionIndex(_selectedDivisionId.Value) : 0;
        _cmbDivisionEditor.SelectedIndex = 0;
        _cmbDivision.SelectedIndexChanged += OnDivisionSelected;
        _cmbDivisionEditor.SelectedIndexChanged += OnEditorDivisionChanged;
        _isLoadingData = false;
    }

    private int FindDivisionIndex(int divId)
    {
        for (int i = 0; i < _cmbDivision.Items.Count; i++)
            if (_cmbDivision.Items[i] is DivItem d && d.Id == divId)
                return i;
        return 0;
    }

    private void OnDivisionSelected(object? sender, EventArgs e)
    {
        try
        {
            if (_isLoadingData) return;
            var div = _cmbDivision.SelectedItem as DivItem;
            _selectedDivisionId = div?.Id > 0 ? div.Id : null;
            LoadTeamList();
            ClearEditor();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnDivisionSelected error: {ex}");
        }
    }

    private void OnEditorDivisionChanged(object? sender, EventArgs e)
    {
        if (_selectedTeamId == null || _teamMode != TeamMode.Edit) return;
        RecomputeSystemName();
    }

    private void FilterTeamListHandler(object? sender, EventArgs e) => FilterTeamList();

    private void RecomputeSystemName()
    {
        var div = _cmbDivisionEditor.SelectedItem as DivItem;
        if (div == null || div.Id == 0)
        {
            _lblSystemName.Text = "";
            return;
        }

        try
        {
            using var db = new BocceDbContext();
            var division = db.Divisions.Find(div.Id);
            if (division != null)
            {
                var team = db.Teams.Find(_selectedTeamId);
                if (team != null)
                    _lblSystemName.Text = $"{team.TeamLetter}-{division.ShortName}";
            }
        }
        catch { }
    }

    private void LoadTeamList()
    {
        _allTeams.Clear();
        if (_selectedDivisionId.HasValue)
        {
            try
            {
                using var db = new BocceDbContext();
                _allTeams = db.Teams
                    .Where(t => t.DivisionId == _selectedDivisionId.Value && !t.IsByeTeam)
                    .OrderBy(t => t.TeamLetter)
                    .Select(t => new
                    {
                        t.Id,
                        Display = (t.DisplayName ?? t.SystemName) + (t.IsActive ? "" : " (inactive)")
                    })
                    .AsEnumerable()
                    .Select(t => (t.Id, t.Display))
                    .ToList();
            }
            catch { }
        }
        FilterTeamList();
        if (_lstTeams.Items.Count > 0 && _lstTeams.SelectedIndex < 0)
            _lstTeams.SelectedIndex = 0;
        else if (_lstTeams.Items.Count == 0)
            ClearEditor();
    }

    private void FilterTeamList()
    {
        var query = _txtSearch.Text == "Search..." ? "" : _txtSearch.Text;
        var prev = _lstTeams.SelectedItem is TeamListItem sel ? sel.Id : (int?)null;

        _isLoadingData = true;
        try
        {
            _lstTeams.BeginUpdate();
            _lstTeams.Items.Clear();
            foreach (var (id, display) in _allTeams)
                if (SearchQueryService.MatchesAnyTerm(display, query))
                    _lstTeams.Items.Add(new TeamListItem(id, display));
            _lstTeams.EndUpdate();
        }
        finally { _isLoadingData = false; }

        if (prev.HasValue)
            for (int i = 0; i < _lstTeams.Items.Count; i++)
                if (_lstTeams.Items[i] is TeamListItem ti && ti.Id == prev.Value)
                { _lstTeams.SelectedIndex = i; return; }
    }

    private void OnTeamSelected(object? sender, EventArgs e)
    {
        try
        {
            if (_isLoadingData) return;
            if (_lstTeams.SelectedItem is TeamListItem ti)
                LoadTeam(ti.Id);
            else
                ClearEditor();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnTeamSelected error: {ex}");
        }
    }

    private void LoadTeam(int teamId)
    {
        _selectedTeamId = teamId;
        try
        {
            using var db = new BocceDbContext();
            var team = db.Teams
                .Include(t => t.Division)
                .FirstOrDefault(t => t.Id == teamId);

            if (team == null)
            {
                ClearEditor();
                return;
            }

            try
            {
                if (_cmbDivisionEditor != null)
                {
                    _cmbDivisionEditor.SelectedIndexChanged -= OnEditorDivisionChanged;
                    _cmbDivisionEditor.SelectedIndex = 0;
                    for (int i = 0; i < _cmbDivisionEditor.Items.Count; i++)
                    {
                        if (_cmbDivisionEditor.Items[i] is DivItem d && d.Id == team.DivisionId)
                        {
                            _cmbDivisionEditor.SelectedIndex = i;
                            break;
                        }
                    }
                    _cmbDivisionEditor.SelectedIndexChanged += OnEditorDivisionChanged;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadTeam - division combo error: {ex}");
            }

            try
            {
                if (_lblSystemName != null) _lblSystemName.Text = team.SystemName ?? "";
                if (_txtDisplayName != null) _txtDisplayName.Text = team.DisplayName ?? "";
                if (_chkActive != null) _chkActive.Checked = team.IsActive;
                if (_lblCreated != null) _lblCreated.Text = team.CreatedAt.ToString("yyyy-MM-dd");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadTeam - controls error: {ex}");
            }

            try
            {
                LoadTeamPlayers(teamId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadTeam - LoadTeamPlayers error: {ex}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadTeam outer error: {ex}");
            ClearEditor();
        }

        try
        {
            _teamMode = TeamMode.View;
            SetEditModeUI();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadTeam - SetEditModeUI error: {ex}");
        }
    }

    private void LoadTeamPlayers(int teamId)
    {
        _isLoadingData = true;
        try
        {
            _lstTeamPlayers.Rows.Clear();

            int? captainId = null;
            var playerItems = new List<CaptainItem>();

            try
            {
                using var db = new BocceDbContext();
                var team = db.Teams.AsNoTracking().FirstOrDefault(t => t.Id == teamId);
                if (team != null)
                {
                    captainId = team.CaptainPlayerId;

                    var tps = db.TeamPlayers
                        .Include(tp => tp.Player)
                        .AsNoTracking()
                        .Where(tp => tp.TeamId == teamId && tp.Player != null)
                        .OrderBy(tp => tp.Player.LastName)
                        .ThenBy(tp => tp.Player.FirstName)
                        .ToList();

                    playerItems.Add(new CaptainItem(0, "(none)"));

                    foreach (var tp in tps)
                    {
                        if (tp.Player != null)
                        {
                            string lastName = tp.Player.LastName ?? "";
                            string firstName = tp.Player.FirstName ?? "";
                            string name = $"{lastName}, {firstName}".Trim();
                            string role = tp.Role == "captain" ? "Captain" : "Player";
                            if (!string.IsNullOrWhiteSpace(name))
                            {
                                _lstTeamPlayers.Rows.Add(tp.PlayerId, name, role);
                                playerItems.Add(new CaptainItem(tp.PlayerId, name));
                            }
                        }
                    }
                    _lstTeamPlayers.ClearSelection();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadTeamPlayers error: {ex}");
            }

            try
            {
                if (_cmbCaptainPlayers == null || _cmbCaptain == null) return;

                // CRITICAL: Keep events unsubscribed during entire population to prevent recursion
                _cmbCaptainPlayers.SelectedIndexChanged -= OnCaptainChanged;
                _cmbCaptain.SelectedIndexChanged -= OnCaptainChanged;

                try
                {
                    _cmbCaptain.Items.Clear();
                    _cmbCaptainPlayers.Items.Clear();

                    foreach (var item in playerItems)
                    {
                        _cmbCaptain.Items.Add(item);
                        _cmbCaptainPlayers.Items.Add(item);
                    }

                    // Set SelectedIndex while events are still unsubscribed
                    if (_cmbCaptain.Items.Count > 0)
                        _cmbCaptain.SelectedIndex = 0;
                    if (_cmbCaptainPlayers.Items.Count > 0)
                        _cmbCaptainPlayers.SelectedIndex = 0;

                    // Find and set captain if one exists
                    if (captainId.HasValue && captainId.Value > 0)
                    {
                        for (int i = 0; i < _cmbCaptain.Items.Count; i++)
                        {
                            if (_cmbCaptain.Items[i] is CaptainItem c && c.Id == captainId.Value)
                            {
                                _cmbCaptain.SelectedIndex = i;
                                _cmbCaptainPlayers.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                }
                finally
                {
                    // ALWAYS re-subscribe, even if an error occurred
                    _cmbCaptain.SelectedIndexChanged += OnCaptainChanged;
                    _cmbCaptainPlayers.SelectedIndexChanged += OnCaptainChanged;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating captain dropdown: {ex}");
            }

            _btnAddPlayer.Enabled = _selectedTeamId.HasValue && (_teamMode == TeamMode.Edit || _teamMode == TeamMode.Create);
        }
        finally
        {
            _isLoadingData = false;
        }
    }

    private void AddPlayerToTeam()
    {
        if (!_selectedTeamId.HasValue || !_selectedDivisionId.HasValue) return;
        int teamId = _selectedTeamId.Value;
        int divisionId = _selectedDivisionId.Value;

        // Get max players limit upfront
        int currentPlayerCount = 0;
        int maxPlayersPerTeam = 0;

        try
        {
            using var db = new Data.BocceDbContext();
            var team = db.Teams.Find(teamId);
            var division = db.Divisions.Include(d => d.Season).Include(d => d.Season!.League).FirstOrDefault(d => d.Id == divisionId);
            if (team == null || division == null) return;

            currentPlayerCount = db.TeamPlayers.Count(tp => tp.TeamId == teamId);
            maxPlayersPerTeam = ResolveMaxPlayers();

            if (maxPlayersPerTeam == 0)
            {
                MessageBox.Show(
                    "Cannot add players: Max players per team is not configured.\n\n" +
                    "Set a value in Division Parameters or Season Parameters.",
                    "Configuration Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int availableSlots = maxPlayersPerTeam - currentPlayerCount;
            if (availableSlots <= 0)
            {
                MessageBox.Show(
                    $"This team is at maximum capacity ({maxPlayersPerTeam} players).\n\n" +
                    "Remove players before adding new ones.",
                    "Team at Capacity", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }
        catch { return; }

        // Loop picker until valid selection is made
        while (true)
        {
            var excludeIds = new HashSet<int>();
            try
            {
                using var db = new Data.BocceDbContext();
                // Exclude players already on ANY team in this division
                excludeIds = db.TeamPlayers
                    .Where(tp => tp.Team.DivisionId == divisionId)
                    .Select(tp => tp.PlayerId)
                    .ToHashSet();
            }
            catch { }

            var playerIds = PickPlayersMultiple(excludeIds);
            if (playerIds.Count == 0) return;

            int availableSlots = maxPlayersPerTeam - currentPlayerCount;

            // Validate selection
            if (playerIds.Count > availableSlots)
            {
                MessageBox.Show(
                    $"You selected {playerIds.Count} player(s) but only {availableSlots} slot(s) are available.\n\n" +
                    $"Team max: {maxPlayersPerTeam}  |  Current: {currentPlayerCount}  |  Available: {availableSlots}\n\n" +
                    $"Please deselect {playerIds.Count - availableSlots} player(s) and try again.",
                    "Selection Exceeds Capacity", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                continue;
            }

            // Valid selection - proceed with adding players
            try
            {
                using var db = new Data.BocceDbContext();
                int count = 0;
                var skipped = new List<string>();

                foreach (var playerId in playerIds)
                {
                    // Check if player already on this team
                    var alreadyOnThisTeam = db.TeamPlayers.Any(tp => tp.TeamId == teamId && tp.PlayerId == playerId);
                    if (alreadyOnThisTeam) continue;

                    // Check if player already on another team in this division (should not happen due to picker, but validate)
                    var alreadyInDivision = db.TeamPlayers
                        .Any(tp => tp.Team.DivisionId == divisionId && tp.PlayerId == playerId);
                    if (alreadyInDivision)
                    {
                        var player = db.Players.Find(playerId);
                        skipped.Add(player?.FullName ?? $"Player {playerId}");
                        continue;
                    }

                    // Check if player would exceed team limit (3 teams max in league)
                    var teamCountInLeague = db.TeamPlayers
                        .Where(tp => tp.PlayerId == playerId && tp.Team.Division.Season.LeagueId == _selectedLeagueId)
                        .Select(tp => tp.TeamId)
                        .Distinct()
                        .Count();
                    if (teamCountInLeague >= 3)
                    {
                        var player = db.Players.Find(playerId);
                        skipped.Add($"{player?.FullName ?? $"Player {playerId}"} (already on 3 teams)");
                        continue;
                    }

                    db.TeamPlayers.Add(new TeamPlayer
                    {
                        TeamId    = teamId,
                        PlayerId  = playerId,
                        Role      = "player",
                        JoinedDate = DateOnly.FromDateTime(DateTime.Today)
                    });

                    // Keep LookingForTeam history: assigned entries get TeamId set.
                    if (_selectedSeasonId.HasValue)
                    {
                        var lft = db.LookingForTeams.FirstOrDefault(l =>
                            l.PlayerId == playerId &&
                            (!_selectedLeagueId.HasValue || l.LeagueId == _selectedLeagueId.Value) &&
                            l.SeasonId == _selectedSeasonId.Value);
                        if (lft != null)
                            lft.TeamId = teamId;
                    }

                    count++;
                }

                if (count > 0) db.SaveChanges();

                var msg = $"Added {count} player(s) to team.\n\nTeam now has {currentPlayerCount + count}/{maxPlayersPerTeam} players.";
                if (skipped.Count > 0)
                    msg += $"\n\nSkipped {skipped.Count}:\n  - " + string.Join("\n  - ", skipped);

                MessageBox.Show(msg, count > 0 ? "Success" : "Info", MessageBoxButtons.OK,
                    count > 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                break;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not add player(s):\n{ex.Message}", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        LoadTeamPlayers(teamId);
        FilterTeamList();
        SetEditModeUI();
    }

    private void RemovePlayerFromTeam()
    {
        if (_selectedTeamId == null) return;

        var dgv = _lstTeamPlayers as DataGridView;
        if (dgv?.SelectedRows.Count == 0) return;

        var row = dgv.SelectedRows[0];
        if (row.Cells["PlId"].Value == null) return;

        int playerId = Convert.ToInt32(row.Cells["PlId"].Value);
        string playerName = row.Cells["PlName"].Value?.ToString() ?? "";

        if (MessageBox.Show($"Remove \"{playerName}\" from this team?\n\nThe player is NOT deleted - only the team assignment.",
            "Confirm Remove", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;

        try
        {
            using var db = new BocceDbContext();
            var tp = db.TeamPlayers.FirstOrDefault(x => x.TeamId == _selectedTeamId.Value && x.PlayerId == playerId);
            if (tp != null)
                db.TeamPlayers.Remove(tp);

            var team = db.Teams.Include(t => t.Division).ThenInclude(d => d.Season).FirstOrDefault(t => t.Id == _selectedTeamId.Value);
            if (team?.CaptainPlayerId == playerId)
            {
                team.CaptainPlayerId = null;
                team.DisplayName = null;
            }

            if (team != null && _selectedSeasonId.HasValue)
            {
                var lft = db.LookingForTeams.FirstOrDefault(l =>
                    l.PlayerId == playerId &&
                    l.LeagueId == team.Division.Season.LeagueId &&
                    l.SeasonId == _selectedSeasonId.Value &&
                    l.TeamId == _selectedTeamId.Value);
                if (lft != null)
                    lft.TeamId = null;
            }

            db.SaveChanges();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Remove failed:\n{ex.Message}", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        LoadTeamPlayers(_selectedTeamId.Value);
        LoadTeamList();
    }

    private void OnCaptainChanged(object? sender = null, EventArgs? e = null)
    {
        try
        {
            if (_selectedTeamId == null || _tabs?.TabPages.Count == 0) return;
            var combo = _tabs.SelectedIndex == 0 ? _cmbCaptain : _cmbCaptainPlayers;
            var item = combo?.SelectedItem as CaptainItem;
            int playerId = item?.Id ?? 0;

        if (playerId > 0)
        {
            try
            {
                using var db = new BocceDbContext();
                var team = db.Teams.Include(t => t.Division).ThenInclude(d => d.Season).FirstOrDefault(t => t.Id == _selectedTeamId.Value);
                if (team == null) return;

                var otherCaptaincy = db.Teams
                    .Where(t => t.Division.Season.LeagueId == team.Division.Season.LeagueId
                             && t.Id != _selectedTeamId.Value
                             && t.CaptainPlayerId == playerId)
                    .Select(t => t.DisplayName ?? t.SystemName)
                    .FirstOrDefault();

                if (!string.IsNullOrEmpty(otherCaptaincy))
                {
                    MessageBox.Show(
                        $"This player is already captain of: {otherCaptaincy}\n\nA player can only be captain of one team.",
                        "Captain Already Assigned", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    var idx = FindCaptainItemIndex(0);
                    if (_tabs?.SelectedIndex == 0)
                        _cmbCaptain.SelectedIndex = idx;
                    else
                        _cmbCaptainPlayers.SelectedIndex = idx;
                    return;
                }
            }
            catch { }
        }

        try
        {
            using var db = new BocceDbContext();
            var team = db.Teams.Find(_selectedTeamId.Value);
            if (team == null) return;

            var tps = db.TeamPlayers.Where(tp => tp.TeamId == _selectedTeamId.Value).ToList();
            foreach (var tp in tps)
                tp.Role = tp.PlayerId == playerId ? "captain" : "player";

            if (playerId > 0)
            {
                team.CaptainPlayerId = playerId;
                var captain = db.Players.Find(playerId);
                if (captain != null)
                    team.DisplayName = $"{team.TeamLetter}-{captain.LastName}";
            }
            else
            {
                team.CaptainPlayerId = null;
                team.DisplayName = null;
            }

            db.SaveChanges();
        }
        catch { return; }

            if (_tabs?.SelectedIndex == 1 && _cmbCaptainPlayers?.Items.Count > 0)
            {
                var idx = FindCaptainItemIndex(playerId);
                _cmbCaptainPlayers.SelectedIndexChanged -= OnCaptainChanged;
                _cmbCaptainPlayers.SelectedIndex = idx;
                _cmbCaptainPlayers.SelectedIndexChanged += OnCaptainChanged;
            }

            // Only reload UI if this is a user-initiated change (not during initial data loading)
            if (!_isLoadingData)
            {
                LoadTeamList();
                if (_selectedTeamId.HasValue)
                    LoadTeamPlayers(_selectedTeamId.Value);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnCaptainChanged error: {ex}");
        }
    }

    private int FindCaptainItemIndex(int id)
    {
        if (_tabs == null) return 0;
        var combo = _tabs.SelectedIndex == 0 ? _cmbCaptain : _cmbCaptainPlayers;
        if (combo == null) return 0;
        for (int i = 0; i < combo.Items.Count; i++)
            if (combo.Items[i] is CaptainItem c && c.Id == id)
                return i;
        return 0;
    }

    private void StartNewTeam()
    {
        if (!_selectedDivisionId.HasValue)
        {
            MessageBox.Show("Select a division first.", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _isLoadingData = true;
        _lstTeams.SelectedIndex = -1;
        _isLoadingData = false;
        ClearEditor();
        _teamMode = TeamMode.Create;
        SetEditModeUI();

        for (int i = 0; i < _cmbDivisionEditor.Items.Count; i++)
            if (_cmbDivisionEditor.Items[i] is DivItem d && d.Id == _selectedDivisionId)
            { _cmbDivisionEditor.SelectedIndex = i; break; }
    }

    private void EnterEditMode()
    {
        if (_selectedTeamId == null) return;
        _teamMode = TeamMode.Edit;
        SetEditModeUI();
    }

    private void ExitEditMode()
    {
        _teamMode = TeamMode.View;
        SetEditModeUI();
        if (_selectedTeamId.HasValue)
            LoadTeam(_selectedTeamId.Value);
    }

    private void SetEditModeUI()
    {
        try
        {
            bool isEditMode = _teamMode == TeamMode.Edit;
            bool isCreateMode = _teamMode == TeamMode.Create;

            Color bgColor = isCreateMode ? AppTheme.CreateModeBackground
                          : isEditMode ? AppTheme.EditModeBackground
                          : AppTheme.ContentBackground;

            if (_tabs != null)
            {
                _tabs.BackColor = bgColor;
                foreach (TabPage page in _tabs.TabPages)
                {
                    page.BackColor = bgColor;
                    SetControlBackgroundRecursive(page, bgColor);
                }
            }

            if (_cmbDivisionEditor != null) _cmbDivisionEditor.Enabled = isEditMode || isCreateMode;
            if (_txtDisplayName != null) _txtDisplayName.Enabled = isEditMode || isCreateMode;
            if (_chkActive != null) _chkActive.Enabled = isEditMode || isCreateMode;
            if (_cmbCaptain != null) _cmbCaptain.Enabled = (isEditMode || isCreateMode) && _selectedTeamId.HasValue && _cmbCaptain.Items.Count > 1;
            if (_cmbCaptainPlayers != null) _cmbCaptainPlayers.Enabled = (isEditMode || isCreateMode) && _selectedTeamId.HasValue && _cmbCaptainPlayers.Items.Count > 1;
            if (_btnAddPlayer != null) _btnAddPlayer.Enabled = (isEditMode || isCreateMode) && _selectedTeamId.HasValue;
            if (_btnRemovePlayer != null) _btnRemovePlayer.Enabled = (isEditMode || isCreateMode) && _lstTeamPlayers?.SelectedRows.Count > 0;

            if (_btnEdit != null) _btnEdit.Visible = !isEditMode && !isCreateMode && _selectedTeamId.HasValue;
            if (_btnDelete != null)
            {
                _btnDelete.Visible = !isEditMode && !isCreateMode && _selectedTeamId.HasValue;
                _btnDelete.Enabled = !isEditMode && !isCreateMode && _selectedTeamId.HasValue;
            }
            if (_btnSave != null) _btnSave.Visible = isEditMode || isCreateMode;
            if (_btnCancel != null) _btnCancel.Visible = isEditMode || isCreateMode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SetEditModeUI error: {ex}");
        }
    }

    private void SetControlBackgroundRecursive(Control parent, Color bgColor)
    {
        foreach (Control ctrl in parent.Controls)
        {
            if (ctrl is Panel or GroupBox)
            {
                ctrl.BackColor = bgColor;
                SetControlBackgroundRecursive(ctrl, bgColor);
            }
            else if (ctrl.HasChildren)
            {
                SetControlBackgroundRecursive(ctrl, bgColor);
            }
        }
    }

    private void SaveTeam()
    {
        if (!_selectedDivisionId.HasValue && !(_cmbDivisionEditor.SelectedItem is DivItem divItem && divItem.Id > 0))
        {
            MessageBox.Show("Select a division.", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            using var db = new BocceDbContext();
            Team team;

            if (_selectedTeamId.HasValue)
            {
                team = db.Teams.Find(_selectedTeamId.Value) ?? throw new Exception("Team not found.");
            }
            else
            {
                int divId = _cmbDivisionEditor.SelectedItem is DivItem d ? d.Id : 0;
                if (divId == 0) throw new Exception("Invalid division selected.");

                var division = db.Divisions.Find(divId);
                if (division == null) throw new Exception("Division not found.");

                var existingTeams = db.Teams.Where(t => t.DivisionId == divId && !t.IsByeTeam)
                    .OrderBy(t => t.TeamLetter).ToList();
                char nextLetter = existingTeams.Count > 0
                    ? (char)(existingTeams.Max(t => t.TeamLetter[0]) + 1)
                    : 'A';

                int maxTeams = ResolveMaxTeams(divId);
                if (maxTeams > 0 && existingTeams.Count >= maxTeams)
                {
                    MessageBox.Show($"Maximum of {maxTeams} teams already reached.", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                team = new Team
                {
                    DivisionId = divId,
                    TeamLetter = nextLetter.ToString(),
                    SystemName = $"{nextLetter}-{division.ShortName}",
                    IsActive = true
                };
                db.Teams.Add(team);
            }

            int divisionId = _cmbDivisionEditor.SelectedItem is DivItem dItem ? dItem.Id : team.DivisionId;
            if (divisionId != team.DivisionId)
            {
                team.DivisionId = divisionId;
                var newDiv = db.Divisions.Find(divisionId);
                if (newDiv != null)
                    team.SystemName = $"{team.TeamLetter}-{newDiv.ShortName}";
            }

            team.DisplayName = string.IsNullOrWhiteSpace(_txtDisplayName.Text) ? null : _txtDisplayName.Text.Trim();
            team.IsActive = _chkActive.Checked;

            db.SaveChanges();
            int savedId = team.Id;

            MessageBox.Show("Team saved.", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _selectedTeamId = savedId;
            LoadTeamList();
            for (int i = 0; i < _lstTeams.Items.Count; i++)
                if (_lstTeams.Items[i] is TeamListItem ti && ti.Id == savedId)
                { _lstTeams.SelectedIndex = i; break; }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Save failed:\n{ex.Message}", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void DeleteTeam()
    {
        if (_selectedTeamId == null || !_selectedDivisionId.HasValue) return;

        string teamName = _lstTeams.SelectedItem is TeamListItem ti ? ti.Display : "Team";

        if (MessageBox.Show($"Permanently delete \"{teamName}\"?\n\nThis action CANNOT be undone.",
            "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;

        int playerCount = 0;
        try
        {
            using var db = new BocceDbContext();
            playerCount = db.TeamPlayers.Count(tp => tp.TeamId == _selectedTeamId.Value);
        }
        catch { }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("You are about to permanently delete the following:");
        sb.AppendLine();
        if (playerCount > 0)
            sb.AppendLine($"  Player assignments removed .......... {playerCount}");
        sb.AppendLine();
        sb.AppendLine("Remaining teams will be re-lettered (A, B, C...).");
        sb.AppendLine();
        sb.AppendLine("This cannot be undone. Continue?");

        if (MessageBox.Show(sb.ToString(), "Confirm Cascade Impact",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;

        try
        {
            using var db = new BocceDbContext();
            var team = db.Teams
                .Include(t => t.TeamPlayers)
                .Include(t => t.Division).ThenInclude(d => d.Season)
                .FirstOrDefault(t => t.Id == _selectedTeamId.Value);
            if (team != null)
            {
                var leagueId = team.Division.Season.LeagueId;
                var divId = team.DivisionId;

                foreach (var tp in team.TeamPlayers)
                {
                    var lft = db.LookingForTeams.FirstOrDefault(l =>
                        l.PlayerId == tp.PlayerId &&
                        l.LeagueId == leagueId &&
                        l.TeamId == team.Id);
                    if (lft != null)
                        lft.TeamId = null;
                }

                db.TeamPlayers.RemoveRange(team.TeamPlayers);
                db.Teams.Remove(team);
                db.SaveChanges();

                ResequenceTeams(divId, db);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Delete failed:\n{ex.Message}", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        MessageBox.Show("Team deleted.", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Information);
        _selectedTeamId = null;
        LoadTeamList();
    }

    private void ResequenceTeams(int divisionId, BocceDbContext db)
    {
        var division = db.Divisions.Find(divisionId);
        if (division == null) return;

        var teams = db.Teams
            .Where(t => t.DivisionId == divisionId && !t.IsByeTeam)
            .OrderBy(t => t.TeamLetter)
            .ToList();

        char letter = 'A';
        foreach (var team in teams)
        {
            team.TeamLetter = letter.ToString();
            team.SystemName = $"{letter}-{division.ShortName}";
            if (team.CaptainPlayerId.HasValue)
            {
                var captain = db.Players.Find(team.CaptainPlayerId.Value);
                if (captain != null)
                    team.DisplayName = $"{letter}-{captain.LastName}";
            }
            letter++;
        }
        db.SaveChanges();
    }

    private int ResolveMaxPlayers()
    {
        if (!_selectedDivisionId.HasValue) return 0;
        try
        {
            using var db = new BocceDbContext();
            var div = db.Divisions.Include(d => d.Season).FirstOrDefault(d => d.Id == _selectedDivisionId.Value);
            if (div == null) return 0;

            if ((div.PlayersPerTeamMaximum ?? 0) > 0)
                return div.PlayersPerTeamMaximum.Value;
            if ((div.Season?.PlayersPerTeamMaximum ?? 0) > 0)
                return div.Season.PlayersPerTeamMaximum.Value;

            var league = db.Leagues.Find(div.Season?.LeagueId ?? 0);
            return league?.PlayersPerTeamMaximum ?? 0;
        }
        catch { return 0; }
    }

    private int ResolveMaxTeams(int divisionId)
    {
        try
        {
            using var db = new BocceDbContext();
            var div = db.Divisions.Include(d => d.Season).FirstOrDefault(d => d.Id == divisionId);
            if (div == null) return 0;

            if (div.TeamsInDivision > 0) return div.TeamsInDivision;
            if ((div.Season?.MaxTeamsInDivision ?? 0) > 0) return div.Season.MaxTeamsInDivision;

            var league = db.Leagues.Find(div.Season?.LeagueId ?? 0);
            return league?.MaxTeamsInDivision ?? 0;
        }
        catch { return 0; }
    }

    private void ClearEditor()
    {
        _selectedTeamId = null;
        if (_cmbDivisionEditor != null) _cmbDivisionEditor.SelectedIndex = 0;
        if (_lblSystemName != null) _lblSystemName.Text = "";
        if (_txtDisplayName != null) _txtDisplayName.Text = "";
        if (_chkActive != null) _chkActive.Checked = true;
        if (_lblCreated != null) _lblCreated.Text = "";
        if (_cmbCaptain != null) _cmbCaptain.Items.Clear();
        if (_cmbCaptainPlayers != null) _cmbCaptainPlayers.Items.Clear();
        if (_lstTeamPlayers != null) _lstTeamPlayers.Rows.Clear();
        SetEditModeUI();
    }

    // Helper records
    private sealed record TeamListItem(int Id, string Display) { public override string ToString() => Display; }
    private sealed record PlayerItem(int Id, string Display) { public override string ToString() => Display; }
    private sealed record DivItem(int Id, string Display) { public override string ToString() => Display; }
    private sealed record CaptainItem(int Id, string Display) { public override string ToString() => Display; }

    // UI helper methods
    private static Label Lbl(string text, int x, int y) => new()
    {
        Text = text, Font = AppTheme.FontDefaultBold, ForeColor = AppTheme.TextPrimary,
        AutoSize = true, Location = new Point(x, y + 3)
    };

    private static Label Hint(string text, int x, int y) => new()
    {
        Text = text, AutoSize = true, Font = AppTheme.FontSmall,
        ForeColor = AppTheme.TextMuted, Location = new Point(x, y)
    };

    private static Panel Sep(int x, int y, int w) => new()
    {
        Location = new Point(x, y), Size = new Size(w, 1), BackColor = AppTheme.Separator
    };

    private static Label SecHdr(string text, int x, int y) => new()
    {
        Text = text, Location = new Point(x, y), AutoSize = true,
        Font = AppTheme.FontSectionHeading, ForeColor = AppTheme.Accent
    };

    // ── Multi-select player picker ──────────────────────────────────────────────
    private List<int> PickPlayersMultiple(HashSet<int> excludeIds)
    {
        var result = new List<int>();
        using var form = new Form
        {
            Text = "Add Players to Team", Width = 750, Height = 600,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false,
            BackColor = AppTheme.ContentBackground
        };

        // Load all available players
        List<(int Id, string Name)> allPlayers = [];
        HashSet<int> lookingForTeam = [];
        try
        {
            using var db = new Data.BocceDbContext();
            allPlayers = db.Players
                .Where(p => p.IsActive && !excludeIds.Contains(p.Id))
                .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
                .ToList()
                .Select(p => (p.Id, $"{p.LastName}, {p.FirstName}"))
                .ToList();

            if (_selectedSeasonId.HasValue)
            {
                lookingForTeam = db.LookingForTeams
                    .Where(l => l.SeasonId == _selectedSeasonId.Value && !l.TeamId.HasValue)
                    .Select(l => l.PlayerId)
                    .ToHashSet();
            }
        }
        catch { }

        // Search box
        var searchBox = new TextBox
        {
            Location = new Point(10, 8), Width = 300, Height = 28,
            Font = AppTheme.FontDefault, PlaceholderText = "Search... (OR delimiters: | \\ / : ;)",
            BackColor = AppTheme.Surface, ForeColor = AppTheme.TextPrimary, BorderStyle = BorderStyle.FixedSingle
        };

        // Left side: Available players
        var lblAvailable = new Label { Text = "Available Players", Font = AppTheme.FontDefaultBold, AutoSize = true, Location = new Point(10, 42) };
        var cmbAvailable = new CheckedListBox
        {
            Location = new Point(10, 62), Width = 300, Height = 440,
            Font = AppTheme.FontDefault, BackColor = AppTheme.Surface, ForeColor = AppTheme.TextPrimary,
            CheckOnClick = true
        };

        // Right side: Selected players
        var lblSelected = new Label { Text = "Players to Add", Font = AppTheme.FontDefaultBold, AutoSize = true, Location = new Point(430, 42) };
        var cmbSelected = new CheckedListBox
        {
            Location = new Point(430, 62), Width = 300, Height = 440,
            Font = AppTheme.FontDefault, BackColor = AppTheme.Surface, ForeColor = AppTheme.TextPrimary,
            CheckOnClick = true
        };

        // Middle buttons
        var btnAdd = new Button
        {
            Text = "Add  >>", Width = 80, Height = 30, Top = 200, Left = 320,
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.Accent, ForeColor = Color.White, Font = AppTheme.FontButton
        };
        var btnRemove = new Button
        {
            Text = "<< Remove", Width = 80, Height = 30, Top = 240, Left = 320,
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.Accent, ForeColor = Color.White, Font = AppTheme.FontButton
        };

        // Bottom buttons
        var btnOk = new Button
        {
            Text = "Add Players", DialogResult = DialogResult.OK, Left = 430, Top = 520, Width = 140, Height = 30,
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.Accent, ForeColor = Color.White, Font = AppTheme.FontButton
        };
        var btnCancel = new Button
        {
            Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 580, Top = 520, Width = 140, Height = 30,
            FlatStyle = FlatStyle.Flat, Font = AppTheme.FontButton
        };

        // Filter function with multi-delimiter OR search
        void RefreshAvailable(string query)
        {
            cmbAvailable.Items.Clear();
            foreach (var (id, name) in allPlayers)
            {
                // Skip if already in selected
                if (cmbSelected.Items.Cast<IntItem>().Any(x => x.Id == id)) continue;

                bool matches = SearchQueryService.MatchesAnyTerm(name, query);

                if (matches)
                {
                    string displayName = lookingForTeam.Contains(id) ? $"◆ {name}" : name;
                    cmbAvailable.Items.Add(new IntItem(id, displayName));
                }
            }
        }
        RefreshAvailable("");

        searchBox.TextChanged += (_, _) => RefreshAvailable(searchBox.Text);

        btnAdd.Click += (_, _) =>
        {
            var toMove = new List<(int Index, IntItem Item)>();
            for (int i = cmbAvailable.Items.Count - 1; i >= 0; i--)
            {
                if (cmbAvailable.GetItemChecked(i))
                {
                    var item = (IntItem)cmbAvailable.Items[i];
                    toMove.Add((i, item));
                }
            }

            foreach (var (_, item) in toMove)
                cmbSelected.Items.Add(item);

            // Clear checkboxes in available
            for (int i = 0; i < cmbAvailable.Items.Count; i++)
                cmbAvailable.SetItemChecked(i, false);

            RefreshAvailable(searchBox.Text);
        };

        btnRemove.Click += (_, _) =>
        {
            var toRemove = new List<(int Index, IntItem Item)>();
            for (int i = cmbSelected.Items.Count - 1; i >= 0; i--)
            {
                if (cmbSelected.GetItemChecked(i))
                {
                    var item = (IntItem)cmbSelected.Items[i];
                    toRemove.Add((i, item));
                }
            }

            foreach (var (_, item) in toRemove)
                cmbSelected.Items.Remove(item);

            // Clear checkboxes in selected
            for (int i = 0; i < cmbSelected.Items.Count; i++)
                cmbSelected.SetItemChecked(i, false);

            RefreshAvailable(searchBox.Text);
        };

        form.Controls.AddRange([searchBox, lblAvailable, cmbAvailable, btnAdd, btnRemove, lblSelected, cmbSelected, btnOk, btnCancel]);
        form.AcceptButton = btnOk;
        form.CancelButton = btnCancel;

        if (form.ShowDialog(this) == DialogResult.OK)
            result = cmbSelected.Items.Cast<IntItem>().Select(x => x.Id).ToList();

        return result;
    }

    private sealed record IntItem(int Id, string Name) { public override string ToString() => Name; }
}

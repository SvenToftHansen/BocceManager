using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.Services;
using BocceManager.UI.Controls;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Panels;

public class LeaguePanel : UserControl
{
    private int? _selectedLeagueId;
    private bool _isLoadingData = false;
    private bool _isDirty = false;
    private bool _isCreatingNew = false;

    // Left panel
    private TextBox _txtSearch  = null!;
    private ListBox _lstLeagues = null!;

    // Editor fields
    private TextBox       _txtName        = null!;
    private TextBox       _txtDescription = null!;
    private RichTextBox   _rtbRules       = null!;
    private CheckBox      _chkActive      = null!;
    private Label         _lblCreatedAt   = null!;
    private ThemedNumericUpDown _numMin         = null!;
    private ThemedNumericUpDown _numMax         = null!;
    private ThemedNumericUpDown _numMaxTeams    = null!;
    private Button        _btnAdd         = null!;
    private Button        _btnSave        = null!;
    private Button        _btnCancel      = null!;
    private Button        _btnDelete      = null!;

    // Seasons tab
    private DataGridView _seasonsGrid = null!;

    // All leagues for search filtering
    private List<(int Id, string Display)> _allLeagues = [];

    public LeaguePanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;
        BuildUI();
        AppParameterService.DefaultsChanged += OnDefaultsChanged;
        LoadLeagueList();
    }

    private void OnDefaultsChanged(object? sender, DefaultsChangedEventArgs e)
    {
        LoadLeagueList();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            AppParameterService.DefaultsChanged -= OnDefaultsChanged;
        base.Dispose(disposing);
    }

    // ── Build UI ──────────────────────────────────────────────────────────────

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
            int leftMin  = desiredLeft;
            int rightMin = desiredRight;
            if (leftMin + rightMin > maxTotal)
            {
                if (maxTotal == 0) { leftMin = 0; rightMin = 0; }
                else { double r = desiredLeft / (double)(desiredLeft + desiredRight); leftMin = (int)Math.Floor(maxTotal * r); rightMin = maxTotal - leftMin; }
            }
            split.Panel1MinSize   = leftMin;
            split.Panel2MinSize   = rightMin;
            int maxLeft = split.Width - rightMin;
            if (maxLeft < leftMin) maxLeft = leftMin;
            int dist = Math.Max(leftMin, Math.Min(preferred, maxLeft));
            split.FixedPanel      = FixedPanel.Panel1;
            split.IsSplitterFixed = true;
            if (dist > 0) split.SplitterDistance = dist;
        }

        split.SizeChanged   += (_, _) => Apply();
        split.HandleCreated += (_, _) => BeginInvoke(new Action(Apply));

        BuildLeftPanel(split.Panel1);
        BuildRightPanel(split.Panel2);

        Controls.Add(split);
    }

    private void BuildLeftPanel(SplitterPanel panel)
    {
        panel.BackColor = AppTheme.Surface;
        panel.Padding = new Padding(8, 8, 8, 8);

        _txtSearch = new TextBox
        {
            Dock = DockStyle.Top,
            Font = AppTheme.FontDefault,
            ForeColor = AppTheme.TextSecondary,
            BackColor = AppTheme.ContentBackground,
            Text = "Search...",
            Height = 28,
            Margin = new Padding(0, 0, 0, 6)
        };
        _txtSearch.Enter += (_, _) => { if (_txtSearch.Text == "Search...") { _txtSearch.Text = ""; _txtSearch.ForeColor = AppTheme.TextPrimary; } };
        _txtSearch.Leave += (_, _) => { if (string.IsNullOrEmpty(_txtSearch.Text)) { _txtSearch.Text = "Search..."; _txtSearch.ForeColor = AppTheme.TextSecondary; } };
        _txtSearch.TextChanged += (_, _) => FilterLeagueList();

        _lstLeagues = new ListBox
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.FontDefault,
            BackColor = AppTheme.ContentBackground,
            ForeColor = AppTheme.TextPrimary,
            BorderStyle = BorderStyle.None,
            IntegralHeight = false
        };
        _lstLeagues.SelectedIndexChanged += OnListLeagueSelected;

        // Search label
        var lblSearch = new Label
        {
            Dock = DockStyle.Top,
            Text = "Leagues",
            Font = AppTheme.FontSmallBold,
            ForeColor = AppTheme.TextPrimary,
            Height = 22,
            TextAlign = ContentAlignment.MiddleLeft
        };

        panel.Controls.Add(_lstLeagues);
        panel.Controls.Add(_txtSearch);
        panel.Controls.Add(lblSearch);
    }

    private void BuildRightPanel(SplitterPanel panel)
    {
        var tabs = BuildTabs();
        tabs.Dock = DockStyle.Fill;
        panel.Controls.Add(tabs);
    }

    private TabControl BuildTabs()
    {
        var tabs = new TabControl
        {
            Dock = DockStyle.Fill, Font = AppTheme.FontDefault, Padding = new Point(16, 6)
        };
        tabs.TabPages.Add(BuildEditorTab());
        tabs.TabPages.Add(BuildSeasonsTab());
        return tabs;
    }

    // ── Editor Tab ────────────────────────────────────────────────────────────

    private TabPage BuildEditorTab()
    {
        var page   = new TabPage("  Editor  ");
        var scroll = new Panel
        {
            Dock = DockStyle.Fill, AutoScroll = true, BackColor = AppTheme.ContentBackground
        };

        const int labelX = 20;
        const int inputX = 200;
        const int inputW = 480;
        int y = 20;

        Label Lbl(string text, int top) => new()
        {
            Text = text, Font = AppTheme.FontDefaultBold, ForeColor = AppTheme.TextPrimary,
            AutoSize = true, Location = new Point(labelX, top + 3)
        };

        // Name
        var lblName = Lbl("Name *", y);
        _txtName = new TextBox
        {
            Location = new Point(inputX, y), Size = new Size(inputW, 26),
            Font = AppTheme.FontDefault, BackColor = AppTheme.ContentBackground, ForeColor = AppTheme.TextPrimary
        };
        _txtName.TextChanged += (_, _) => MarkDirty();
        y += 44;

        // Description
        var lblDesc = Lbl("Description", y);
        _txtDescription = new TextBox
        {
            Location = new Point(inputX, y), Size = new Size(inputW, 26),
            Font = AppTheme.FontDefault, BackColor = AppTheme.ContentBackground, ForeColor = AppTheme.TextPrimary
        };
        _txtDescription.TextChanged += (_, _) => MarkDirty();
        y += 44;

        // Rule Additions / Changes
        var lblRules = new Label
        {
            Text = "Rule Additions /\nChanges",
            Font = AppTheme.FontDefaultBold, ForeColor = AppTheme.TextPrimary,
            AutoSize = false, Size = new Size(172, 48),
            Location = new Point(labelX, y + 3), TextAlign = ContentAlignment.TopLeft
        };
        _rtbRules = new RichTextBox
        {
            Location = new Point(inputX, y), Size = new Size(inputW, 150),
            Font = AppTheme.FontDefault, BackColor = AppTheme.ContentBackground, ForeColor = AppTheme.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle, ScrollBars = RichTextBoxScrollBars.Vertical
        };
        _rtbRules.TextChanged += (_, _) => MarkDirty();
        var lblRulesHint = new Label
        {
            Text = "Optional - captures any rule variations or additions specific to this league, " +
                   "applied alongside the club's official rules document. Most leagues leave this blank.",
            AutoSize = true, MaximumSize = new Size(inputW, 0),
            Font = AppTheme.FontSmall, ForeColor = AppTheme.TextMuted,
            Location = new Point(inputX, y + 154)
        };
        y += 260;

        // Players Per Team Minimum
        var lblMin = Lbl("Players / Team Min", y);
        _numMin = NumericBox(inputX, y);
        _numMin.ValueChanged += (_, _) => MarkDirty();
        var lblMinHint = Hint("  Seasons and Divisions inherit this unless overridden.", inputX + 100, y + 4);
        y += 44;

        // Players Per Team Maximum
        var lblMax = Lbl("Players / Team Max", y);
        _numMax = NumericBox(inputX, y);
        _numMax.ValueChanged += (_, _) => MarkDirty();
        var lblMaxHint = Hint("  Seasons and Divisions inherit this unless overridden.", inputX + 100, y + 4);
        y += 44;

        // Max Teams in Division
        var lblMaxTeams = Lbl("Max Teams / Division", y);
        _numMaxTeams = NumericBox(inputX, y);
        _numMaxTeams.ValueChanged += (_, _) => MarkDirty();
        var lblMaxTeamsHint = Hint("  Seasons and Divisions inherit this unless overridden. 0 = no limit.", inputX + 100, y + 4);
        y += 44;

        // Active
        var lblActive = Lbl("Active", y);
        _chkActive = new CheckBox
        {
            Location = new Point(inputX, y), AutoSize = true,
            Font = AppTheme.FontDefault, ForeColor = AppTheme.TextPrimary, Checked = true
        };
        _chkActive.CheckedChanged += (_, _) => MarkDirty();
        y += 38;

        // Created (read-only)
        var lblCreatedLbl = Lbl("Created", y);
        _lblCreatedAt = new Label
        {
            Location = new Point(inputX, y + 3), AutoSize = true,
            Font = AppTheme.FontDefault, ForeColor = AppTheme.TextSecondary
        };

        scroll.Controls.AddRange([
            lblName, _txtName, lblDesc, _txtDescription, lblRules, _rtbRules, lblRulesHint,
            lblMin, _numMin, lblMinHint,
            lblMax, _numMax, lblMaxHint,
            lblMaxTeams, _numMaxTeams, lblMaxTeamsHint,
            lblActive, _chkActive, lblCreatedLbl, _lblCreatedAt
        ]);

        // Toolbar
        var toolbar = new Panel
        {
            Height = 54, BackColor = AppTheme.Surface, Padding = new Padding(12, 10, 12, 10)
        };

        _btnAdd = new Button
        {
            Text = "+ Add League", Location = new Point(12, 10), Size = new Size(140, 32),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.ButtonSuccess, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 }
        };
        _btnAdd.Click += (_, _) => AddLeague();

        _btnSave = new Button
        {
            Text = "Save Changes", Location = new Point(160, 10), Size = new Size(140, 32),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.Accent, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 },
            Enabled = false
        };
        _btnSave.Click += (_, _) => SaveLeague();

        _btnCancel = new Button
        {
            Text = "Cancel", Location = new Point(308, 10), Size = new Size(140, 32),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.Surface, ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand,
            FlatAppearance = { BorderSize = 1, BorderColor = AppTheme.Separator },
            Visible = false
        };
        _btnCancel.Click += (_, _) => CancelAddLeague();

        _btnDelete = new Button
        {
            Text = "Delete League", Location = new Point(308, 10), Size = new Size(140, 32),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.ButtonDanger, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand,
            FlatAppearance = { BorderSize = 0 }, Enabled = false
        };
        _btnDelete.Click += (_, _) => DeleteLeague();

        toolbar.Controls.AddRange([_btnAdd, _btnSave, _btnCancel, _btnDelete]);
        page.Controls.Add(MakeLayout(scroll, toolbar));
        return page;
    }

    private static ThemedNumericUpDown NumericBox(int x, int y) => new()
    {
        Location = new Point(x, y), Size = new Size(90, 26),
        Font = AppTheme.FontDefault, Minimum = 0, Maximum = 99,
        Value = 0, DecimalPlaces = 0
    };

    private static Label Hint(string text, int x, int y) => new()
    {
        Text = text, AutoSize = true,
        Font = AppTheme.FontSmall, ForeColor = AppTheme.TextMuted,
        Location = new Point(x, y)
    };

    // ── Seasons Tab ───────────────────────────────────────────────────────────

    private TabPage BuildSeasonsTab()
    {
        var page = new TabPage("  Seasons  ");

        _seasonsGrid = new DataGridView
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
                BackColor          = AppTheme.GridHeaderBackground,
                ForeColor          = AppTheme.GridHeaderText,
                SelectionBackColor = AppTheme.GridHeaderBackground,
                SelectionForeColor = AppTheme.GridHeaderText,
                Font               = AppTheme.FontGridHeader,
                Padding            = new Padding(4, 0, 0, 0)
            },
            EnableHeadersVisualStyles = false,
            RowTemplate = { Height = 30 },
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            Cursor = Cursors.Hand
        };
        _seasonsGrid.AlternatingRowsDefaultCellStyle =
            new DataGridViewCellStyle { BackColor = AppTheme.GridAlternateRow };

        _seasonsGrid.Columns.Add(new DataGridViewTextBoxColumn  { Name = "SeasonId",  Visible = false,              FillWeight = 1,  MinimumWidth = 2   });
        _seasonsGrid.Columns.Add(new DataGridViewTextBoxColumn  { Name = "Name",       HeaderText = "Season Name",   FillWeight = 28, MinimumWidth = 130 });
        _seasonsGrid.Columns.Add(new DataGridViewTextBoxColumn  { Name = "StartDate",  HeaderText = "Start Date",    FillWeight = 13, MinimumWidth = 90  });
        _seasonsGrid.Columns.Add(new DataGridViewTextBoxColumn  { Name = "Weeks",      HeaderText = "Weeks",         FillWeight = 7,  MinimumWidth = 55  });
        _seasonsGrid.Columns.Add(new DataGridViewTextBoxColumn  { Name = "Divisions",  HeaderText = "Divisions",     FillWeight = 10, MinimumWidth = 70  });
        _seasonsGrid.Columns.Add(new DataGridViewTextBoxColumn  { Name = "Teams",      HeaderText = "Teams",         FillWeight = 8,  MinimumWidth = 60  });
        _seasonsGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Current",    HeaderText = "Current",       FillWeight = 8,  MinimumWidth = 60  });
        _seasonsGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Active",     HeaderText = "Active",        FillWeight = 8,  MinimumWidth = 60  });

        _seasonsGrid.CellDoubleClick += OnSeasonDoubleClick;

        var toolbar = new Panel
        {
            Height = 46, BackColor = AppTheme.Surface, Padding = new Padding(12, 8, 12, 8)
        };

        var btnOpen = new Button
        {
            Text = "Open Season Editor", Location = new Point(12, 8), Size = new Size(160, 30),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.Accent, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 }
        };
        btnOpen.Click += (_, _) => OpenSelectedSeason();

        var hint = new Label
        {
            Text = "Double-click a row to open the Season Editor.",
            Location = new Point(190, 14), AutoSize = true,
            Font = AppTheme.FontSmall, ForeColor = AppTheme.TextMuted
        };

        toolbar.Controls.AddRange([btnOpen, hint]);
        page.Controls.Add(MakeLayout(_seasonsGrid, toolbar));
        return page;
    }

    // ── Data Loading ──────────────────────────────────────────────────────────

    private void LoadLeagueList()
    {
        _isLoadingData = true;
        try
        {
            int? defaultLeagueId = null;
            try
            {
                using var db = new BocceDbContext();
                _allLeagues = db.Leagues
                    .OrderBy(l => l.Name)
                    .Select(l => new { l.Id, l.Name, l.IsActive })
                    .AsEnumerable()
                    .Select(l => (l.Id, l.Name + (l.IsActive ? "" : " (inactive)")))
                    .ToList();
                defaultLeagueId = AppParameterService.GetDefaultLeagueId(db);
            }
            catch { }

            FilterLeagueList();

            // Select default league
            if (defaultLeagueId.HasValue)
                SelectInList(defaultLeagueId.Value);
            else if (_selectedLeagueId.HasValue)
                SelectInList(_selectedLeagueId.Value);
        }
        finally
        {
            _isLoadingData = false;
        }
    }

    private void FilterLeagueList()
    {
        var query = _txtSearch.Text == "Search..." ? "" : _txtSearch.Text;

        _isLoadingData = true;
        _lstLeagues.SelectedIndexChanged -= OnListLeagueSelected;
        _lstLeagues.BeginUpdate();

        var prevId = _selectedLeagueId;
        _lstLeagues.Items.Clear();

        foreach (var (id, display) in _allLeagues)
        {
            if (SearchQueryService.MatchesAnyTerm(display, query))
                _lstLeagues.Items.Add(new ListItem(id, display));
        }

        _lstLeagues.EndUpdate();
        _lstLeagues.SelectedIndexChanged += OnListLeagueSelected;
        _isLoadingData = false;

        // Restore selection
        if (prevId.HasValue)
            SelectInList(prevId.Value);
    }

    private void SelectInList(int leagueId)
    {
        for (int i = 0; i < _lstLeagues.Items.Count; i++)
        {
            if (_lstLeagues.Items[i] is ListItem li && li.Id == leagueId)
            {
                _lstLeagues.SelectedIndex = i;
                return;
            }
        }
    }

    private void OnListLeagueSelected(object? sender, EventArgs e)
    {
        if (_isLoadingData) return;
        if (_lstLeagues.SelectedItem is ListItem li)
            LoadLeague(li.Id);
        else
            ClearEditorForm();
    }

    private void LoadLeague(int leagueId)
    {
        _selectedLeagueId = leagueId;

        try
        {
            using var db = new BocceDbContext();
            var league = db.Leagues.Find(leagueId);
            if (league == null) return;
            _txtName.Text        = league.Name;
            _txtDescription.Text = league.Description ?? "";
            _rtbRules.Text       = league.RulesText ?? "";
            _chkActive.Checked   = league.IsActive;
            _lblCreatedAt.Text   = league.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            _numMin.Value        = league.PlayersPerTeamMinimum ?? 0;
            _numMax.Value        = league.PlayersPerTeamMaximum ?? 0;
            _numMaxTeams.Value   = league.MaxTeamsInDivision;
        }
        catch { }

        LoadSeasons(leagueId);
        UpdateDeleteButtonState();
        ClearDirty();
        _isCreatingNew = false;
        _btnCancel.Visible = false;
        _btnDelete.Visible = true;
    }

    private void ClearEditorForm()
    {
        _selectedLeagueId    = null;
        _isCreatingNew = false;
        _txtName.Text        = "";
        _txtDescription.Text = "";
        _rtbRules.Text       = "";
        _chkActive.Checked   = true;
        _lblCreatedAt.Text   = "";
        _numMin.Value        = 0;
        _numMax.Value        = 0;
        _numMaxTeams.Value   = 0;
        _btnDelete.Enabled   = false;
        _btnCancel.Visible   = false;
        _seasonsGrid.Rows.Clear();
        ClearDirty();
    }

    private void LoadSeasons(int leagueId)
    {
        _seasonsGrid.Rows.Clear();
        try
        {
            using var db = new BocceDbContext();
            var rows = db.Seasons
                .Where(s => s.LeagueId == leagueId)
                .OrderByDescending(s => s.StartDate)
                .Select(s => new
                {
                    s.Id, s.Name, s.StartDate, s.WeeksInSeason, s.IsCurrent,
                    Divisions = db.Divisions.Count(d => d.SeasonId == s.Id),
                    Teams     = db.Teams.Count(t => t.Division.SeasonId == s.Id),
                    s.IsActive
                }).ToList();

            foreach (var s in rows)
                _seasonsGrid.Rows.Add(
                    s.Id, s.Name,
                    s.StartDate?.ToString("yyyy-MM-dd") ?? "-",
                    s.WeeksInSeason > 0 ? s.WeeksInSeason.ToString() : "-",
                    s.Divisions, s.Teams, s.IsCurrent, s.IsActive);
            _seasonsGrid.ClearSelection();
        }
        catch { }
    }

    // ── New / Edit League ─────────────────────────────────────────────────────

    private void AddLeague()
    {
        _isCreatingNew = true;
        _lstLeagues.SelectedIndexChanged -= OnListLeagueSelected;
        _lstLeagues.ClearSelected();
        _lstLeagues.SelectedIndexChanged += OnListLeagueSelected;
        ClearEditorForm();
        _btnCancel.Visible = true;
        _btnDelete.Visible = false;
        _txtName.Focus();
    }

    private void CancelAddLeague()
    {
        _isCreatingNew = false;
        _btnCancel.Visible = false;
        _btnDelete.Visible = _selectedLeagueId.HasValue;
        ClearEditorForm();
        LoadLeagueList();
    }

    private void UpdateDeleteButtonState()
    {
        _btnDelete.Enabled = _selectedLeagueId.HasValue;
    }

    private void MarkDirty()
    {
        if (_isLoadingData || _btnSave == null) return;
        _isDirty = true;
        _btnSave.Enabled = true;
    }

    private void ClearDirty()
    {
        if (_btnSave == null) return;
        _isDirty = false;
        _btnSave.Enabled = false;
    }

    // ── Save ──────────────────────────────────────────────────────────────────

    private void SaveLeague()
    {
        var name = _txtName.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            MessageBox.Show("League name is required.", "Golden Vista Bocce League Master",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtName.Focus();
            return;
        }

        int?  newMin = _numMin.Value > 0 ? (int)_numMin.Value : (int?)null;
        int?  newMax = _numMax.Value > 0 ? (int)_numMax.Value : (int?)null;
        int?  oldMin = null, oldMax = null;
        int   savedId;
        bool  isNew = !_selectedLeagueId.HasValue;

        try
        {
            using var db = new BocceDbContext();

            if (_selectedLeagueId.HasValue)
            {
                var league = db.Leagues.Find(_selectedLeagueId.Value);
                if (league == null) return;
                oldMin = league.PlayersPerTeamMinimum;
                oldMax = league.PlayersPerTeamMaximum;
                league.Name                  = name;
                league.Description           = NullIfEmpty(_txtDescription.Text);
                league.RulesText             = NullIfEmpty(_rtbRules.Text);
                league.IsActive              = _chkActive.Checked;
                league.PlayersPerTeamMinimum = newMin;
                league.PlayersPerTeamMaximum = newMax;
                league.MaxTeamsInDivision    = (int)_numMaxTeams.Value;
                db.SaveChanges();
                savedId = league.Id;
                _lblCreatedAt.Text = league.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            }
            else
            {
                var league = new League
                {
                    Name                  = name,
                    Description           = NullIfEmpty(_txtDescription.Text),
                    RulesText             = NullIfEmpty(_rtbRules.Text),
                    IsActive              = _chkActive.Checked,
                    PlayersPerTeamMinimum = newMin,
                    PlayersPerTeamMaximum = newMax,
                    MaxTeamsInDivision    = (int)_numMaxTeams.Value
                };
                db.Leagues.Add(league);
                db.SaveChanges();
                savedId = league.Id;
                _selectedLeagueId  = league.Id;
                _btnDelete.Enabled = true;
                _lblCreatedAt.Text = league.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Save failed:\n\n{ex.Message}", "Golden Vista Bocce League Master",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        MessageBox.Show("League saved.", "Golden Vista Bocce League Master",
            MessageBoxButtons.OK, MessageBoxIcon.Information);

        ClearDirty();
        _isCreatingNew = false;
        _btnCancel.Visible = false;
        _btnDelete.Visible = true;

        if (!isNew && (oldMin != newMin || oldMax != newMax))
            OfferPropagate(savedId, oldMin, oldMax, newMin, newMax);

        LoadLeagueList();
        SelectInList(savedId);
    }

    private void OfferPropagate(int leagueId, int? oldMin, int? oldMax, int? newMin, int? newMax)
    {
        int? propMin = oldMin != newMin ? newMin : (int?)null;
        int? propMax = oldMax != newMax ? newMax : (int?)null;

        List<PropagateTarget> targets;
        try
        {
            using var db = new BocceDbContext();
            targets = BuildPropagateTargets(db, leagueId, propMin, propMax);
        }
        catch { return; }

        if (targets.Count == 0) return;

        using var dlg = new PropagateDialog(oldMin, oldMax, propMin, propMax, targets);
        if (dlg.ShowDialog(this) != DialogResult.OK || dlg.ApprovedTargets.Count == 0)
            return;

        try
        {
            using var db = new BocceDbContext();
            foreach (var t in dlg.ApprovedTargets)
            {
                if (t.EntityType == "Season")
                {
                    var s = db.Seasons.Find(t.Id);
                    if (s == null) continue;
                    if (propMin.HasValue) s.PlayersPerTeamMinimum = propMin;
                    if (propMax.HasValue) s.PlayersPerTeamMaximum = propMax;
                }
                else
                {
                    var d = db.Divisions.Find(t.Id);
                    if (d == null) continue;
                    if (propMin.HasValue) d.PlayersPerTeamMinimum = propMin;
                    if (propMax.HasValue) d.PlayersPerTeamMaximum = propMax;
                }
            }
            db.SaveChanges();
            MessageBox.Show($"Updated {dlg.ApprovedTargets.Count} record(s).", "Golden Vista Bocce League Master",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Propagation failed:\n\n{ex.Message}", "Golden Vista Bocce League Master",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static List<PropagateTarget> BuildPropagateTargets(
        BocceDbContext db, int leagueId, int? newMin, int? newMax)
    {
        var targets = new List<PropagateTarget>();
        var seasons = db.Seasons.Where(s => s.LeagueId == leagueId).OrderBy(s => s.Name).ToList();

        foreach (var s in seasons)
        {
            targets.Add(new PropagateTarget(
                "Season", s.Id, s.Name, "",
                s.PlayersPerTeamMinimum, s.PlayersPerTeamMaximum, newMin, newMax));

            var divisions = db.Divisions.Where(d => d.SeasonId == s.Id).OrderBy(d => d.Name).ToList();
            foreach (var d in divisions)
            {
                targets.Add(new PropagateTarget(
                    "Division", d.Id, d.Name, s.Name,
                    d.PlayersPerTeamMinimum, d.PlayersPerTeamMaximum, newMin, newMax));
            }
        }
        return targets;
    }

    // ── Delete League (cascade) ───────────────────────────────────────────────

    private void DeleteLeague()
    {
        if (!_selectedLeagueId.HasValue) return;
        int leagueId = _selectedLeagueId.Value;
        CascadeImpact impact;
        try
        {
            using var db = new BocceDbContext();
            impact = LeagueService.ComputeDeleteImpact(db, leagueId);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not compute impact:\n\n{ex.Message}", "Golden Vista Bocce League Master",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Permanently delete \"{impact.LeagueName}\"?");
        sb.AppendLine();
        sb.AppendLine("The following data will be PERMANENTLY deleted:");
        sb.AppendLine();
        if (impact.Seasons.Count > 0)
        {
            sb.AppendLine($"  Seasons ({impact.Seasons.Count}):");
            foreach (var s in impact.Seasons)
                sb.AppendLine($"    - {s.Name} - {s.Divisions} division(s), {s.Teams} team(s)");
        }
        else sb.AppendLine("  0 Seasons");
        sb.AppendLine();
        sb.AppendLine($"  Total divisions ............... {impact.TotalDivisions}");
        sb.AppendLine($"  Total teams ................... {impact.TotalTeams}");
        sb.AppendLine($"  Spare lists ................... {impact.SpareListCount}");
        sb.AppendLine($"  Officials ..................... {impact.OfficialCount}");
        sb.AppendLine($"  Announcements ................. {impact.AnnouncementCount}");
        sb.AppendLine($"  Email lists ................... {impact.EmailListCount}");
        sb.AppendLine($"  League parameters ............. {impact.ParameterCount}");
        sb.AppendLine();
        sb.AppendLine("Players are NOT deleted - they belong to the app, not this league.");
        sb.AppendLine();
        sb.AppendLine("This cannot be undone. Continue?");

        if (MessageBox.Show(sb.ToString(), "Confirm Cascade Delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;

        try
        {
            using var db = new BocceDbContext();
            LeagueService.ExecuteCascadeDelete(db, leagueId);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Delete failed:\n\n{ex.Message}", "Golden Vista Bocce League Master",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        MessageBox.Show("League deleted.", "Golden Vista Bocce League Master",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
        _selectedLeagueId = null;
        LoadLeagueList();
        if (_lstLeagues.Items.Count == 0) ClearEditorForm();
    }

    // ── Season Navigation ─────────────────────────────────────────────────────

    private void OnSeasonDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        OpenSelectedSeason();
    }

    private void OpenSelectedSeason()
    {
        if (_seasonsGrid.SelectedRows.Count == 0)
        {
            MessageBox.Show("Select a season first.", "Golden Vista Bocce League Master",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        int seasonId = Convert.ToInt32(_seasonsGrid.SelectedRows[0].Cells["SeasonId"].Value);
        (FindForm() as MainForm)?.NavigateToSeasons(seasonId);
    }

    // ── Layout Helper ─────────────────────────────────────────────────────────

    private static TableLayoutPanel MakeLayout(Control fill, Panel toolbar)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2,
            Padding = Padding.Empty, Margin = Padding.Empty,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, toolbar.Height));
        fill.Dock    = DockStyle.Fill;
        toolbar.Dock = DockStyle.Fill;
        layout.Controls.Add(fill,    0, 0);
        layout.Controls.Add(toolbar, 0, 1);
        return layout;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string? NullIfEmpty(string s) =>
        string.IsNullOrWhiteSpace(s) ? null : s;

    private sealed record ListItem(int Id, string Name)
    {
        public override string ToString() => Name;
    }
}

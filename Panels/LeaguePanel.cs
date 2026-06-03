using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.Services;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Panels;

public class LeaguePanel : UserControl
{
    private int? _selectedLeagueId;

    // Header
    private ComboBox _leagueCombo = null!;

    // Editor tab
    private TextBox      _txtName        = null!;
    private TextBox      _txtDescription = null!;
    private RichTextBox  _rtbRules       = null!;
    private CheckBox     _chkActive      = null!;
    private Label        _lblCreatedAt   = null!;
    private NumericUpDown _numMin        = null!;
    private NumericUpDown _numMax        = null!;
    private Button       _btnSave        = null!;
    private Button       _btnDelete      = null!;

    // Seasons tab
    private DataGridView _seasonsGrid = null!;

    public LeaguePanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;
        BuildUI();
        LoadLeagueList();
    }

    // ── Build UI ─────────────────────────────────────────────────────────────

    private void BuildUI()
    {
        var header = BuildHeader();
        var tabs   = BuildTabs();
        Controls.Add(tabs);
        Controls.Add(header);
    }

    private Panel BuildHeader()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Top, Height = 54,
            BackColor = AppTheme.Surface, Padding = new Padding(12, 8, 12, 8)
        };

        var lbl = new Label
        {
            Text = "League:", Font = AppTheme.FontDefaultBold, ForeColor = AppTheme.TextPrimary,
            AutoSize = true, Location = new Point(12, 17)
        };

        _leagueCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList, Font = AppTheme.FontDefault,
            Width = 340, Location = new Point(lbl.PreferredWidth + 22, 13)
        };
        _leagueCombo.SelectedIndexChanged += OnLeagueSelected;

        var btnNew = new Button
        {
            Text = "+ New League", Location = new Point(_leagueCombo.Right + 16, 12),
            Size = new Size(130, 30), FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.ButtonSuccess, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand,
            FlatAppearance = { BorderSize = 0 }
        };
        btnNew.Click += (_, _) => StartNewLeague();

        panel.Controls.AddRange([lbl, _leagueCombo, btnNew]);
        return panel;
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

    // ── Editor Tab ───────────────────────────────────────────────────────────

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
        y += 44;

        // Description
        var lblDesc = Lbl("Description", y);
        _txtDescription = new TextBox
        {
            Location = new Point(inputX, y), Size = new Size(inputW, 26),
            Font = AppTheme.FontDefault, BackColor = AppTheme.ContentBackground, ForeColor = AppTheme.TextPrimary
        };
        y += 44;

        // Rules Text
        var lblRules = Lbl("Rules Text", y);
        _rtbRules = new RichTextBox
        {
            Location = new Point(inputX, y), Size = new Size(inputW, 150),
            Font = AppTheme.FontDefault, BackColor = AppTheme.ContentBackground, ForeColor = AppTheme.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle, ScrollBars = RichTextBoxScrollBars.Vertical
        };
        y += 166;

        // Players Per Team Minimum
        var lblMin = Lbl("Players / Team Min", y);
        _numMin = NumericBox(inputX, y);
        var lblMinHint = Hint("  Seasons and Divisions inherit this unless overridden.", inputX + 100, y + 4);
        y += 44;

        // Players Per Team Maximum
        var lblMax = Lbl("Players / Team Max", y);
        _numMax = NumericBox(inputX, y);
        var lblMaxHint = Hint("  Seasons and Divisions inherit this unless overridden.", inputX + 100, y + 4);
        y += 44;

        // Active
        var lblActive = Lbl("Active", y);
        _chkActive = new CheckBox
        {
            Location = new Point(inputX, y), AutoSize = true,
            Font = AppTheme.FontDefault, ForeColor = AppTheme.TextPrimary, Checked = true
        };
        y += 38;

        // Created (read-only)
        var lblCreatedLbl = Lbl("Created", y);
        _lblCreatedAt = new Label
        {
            Location = new Point(inputX, y + 3), AutoSize = true,
            Font = AppTheme.FontDefault, ForeColor = AppTheme.TextSecondary
        };

        scroll.Controls.AddRange([
            lblName, _txtName, lblDesc, _txtDescription, lblRules, _rtbRules,
            lblMin, _numMin, lblMinHint,
            lblMax, _numMax, lblMaxHint,
            lblActive, _chkActive, lblCreatedLbl, _lblCreatedAt
        ]);

        // Toolbar
        var toolbar = new Panel
        {
            Height = 54, BackColor = AppTheme.Surface, Padding = new Padding(12, 10, 12, 10)
        };

        _btnSave = new Button
        {
            Text = "Save Changes", Location = new Point(12, 10), Size = new Size(130, 32),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.Accent, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 }
        };
        _btnSave.Click += (_, _) => SaveLeague();

        _btnDelete = new Button
        {
            Text = "Delete League", Location = new Point(160, 10), Size = new Size(130, 32),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.ButtonDanger, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand,
            FlatAppearance = { BorderSize = 0 }, Enabled = false
        };
        _btnDelete.Click += (_, _) => DeleteLeague();

        toolbar.Controls.AddRange([_btnSave, _btnDelete]);
        page.Controls.Add(MakeLayout(scroll, toolbar));
        return page;
    }

    private static NumericUpDown NumericBox(int x, int y) => new()
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
                BackColor = AppTheme.GridHeaderBackground,
                ForeColor = AppTheme.GridHeaderText,
                Font = AppTheme.FontGridHeader,
                Padding = new Padding(4, 0, 0, 0)
            },
            EnableHeadersVisualStyles = false,
            RowTemplate = { Height = 30 },
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            Cursor = Cursors.Hand
        };
        _seasonsGrid.AlternatingRowsDefaultCellStyle =
            new DataGridViewCellStyle { BackColor = AppTheme.GridAlternateRow };

        _seasonsGrid.Columns.Add(new DataGridViewTextBoxColumn  { Name = "SeasonId",  Visible = false,              FillWeight = 1,  MinimumWidth = 2  });
        _seasonsGrid.Columns.Add(new DataGridViewTextBoxColumn  { Name = "Name",       HeaderText = "Season Name",   FillWeight = 28, MinimumWidth = 130 });
        _seasonsGrid.Columns.Add(new DataGridViewTextBoxColumn  { Name = "StartDate",  HeaderText = "Start Date",    FillWeight = 14, MinimumWidth = 90  });
        _seasonsGrid.Columns.Add(new DataGridViewTextBoxColumn  { Name = "EndDate",    HeaderText = "End Date",      FillWeight = 14, MinimumWidth = 90  });
        _seasonsGrid.Columns.Add(new DataGridViewTextBoxColumn  { Name = "Divisions",  HeaderText = "Divisions",     FillWeight = 10, MinimumWidth = 70  });
        _seasonsGrid.Columns.Add(new DataGridViewTextBoxColumn  { Name = "Teams",      HeaderText = "Teams",         FillWeight = 10, MinimumWidth = 60  });
        _seasonsGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Active",     HeaderText = "Active",        FillWeight = 9,  MinimumWidth = 60  });

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

    // ── Data Loading ─────────────────────────────────────────────────────────

    private void LoadLeagueList()
    {
        _leagueCombo.SelectedIndexChanged -= OnLeagueSelected;
        _leagueCombo.Items.Clear();

        try
        {
            using var db = new BocceDbContext();
            foreach (var l in db.Leagues.OrderBy(l => l.Name).ToList())
                _leagueCombo.Items.Add(new ComboItem(l.Id, l.Name + (l.IsActive ? "" : " (inactive)")));
        }
        catch { }

        _leagueCombo.SelectedIndexChanged += OnLeagueSelected;

        if (_leagueCombo.Items.Count > 0)
            _leagueCombo.SelectedIndex = 0;
        else
            ClearEditorForm();
    }

    private void OnLeagueSelected(object? sender, EventArgs e)
    {
        if (_leagueCombo.SelectedItem is ComboItem item)
            LoadLeague(item.Id);
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
        }
        catch { }

        _btnDelete.Enabled = true;
        LoadSeasons(leagueId);
    }

    private void ClearEditorForm()
    {
        _selectedLeagueId    = null;
        _txtName.Text        = "";
        _txtDescription.Text = "";
        _rtbRules.Text       = "";
        _chkActive.Checked   = true;
        _lblCreatedAt.Text   = "";
        _numMin.Value        = 0;
        _numMax.Value        = 0;
        _btnDelete.Enabled   = false;
        _seasonsGrid.Rows.Clear();
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
                    s.Id, s.Name, s.StartDate, s.EndDate,
                    Divisions = db.Divisions.Count(d => d.SeasonId == s.Id),
                    Teams     = db.Teams.Count(t => t.Division.SeasonId == s.Id),
                    s.IsActive
                }).ToList();

            foreach (var s in rows)
                _seasonsGrid.Rows.Add(
                    s.Id, s.Name,
                    s.StartDate?.ToString("yyyy-MM-dd") ?? "—",
                    s.EndDate?.ToString("yyyy-MM-dd") ?? "—",
                    s.Divisions, s.Teams, s.IsActive);
        }
        catch { }
    }

    // ── New / Save League ─────────────────────────────────────────────────────

    private void StartNewLeague()
    {
        _leagueCombo.SelectedIndexChanged -= OnLeagueSelected;
        _leagueCombo.SelectedIndex = -1;
        _leagueCombo.SelectedIndexChanged += OnLeagueSelected;
        ClearEditorForm();
        _txtName.Focus();
    }

    private void SaveLeague()
    {
        var name = _txtName.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            MessageBox.Show("League name is required.", "BocceManager",
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
                league.Name                 = name;
                league.Description          = NullIfEmpty(_txtDescription.Text);
                league.RulesText            = NullIfEmpty(_rtbRules.Text);
                league.IsActive             = _chkActive.Checked;
                league.PlayersPerTeamMinimum = newMin;
                league.PlayersPerTeamMaximum = newMax;
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
                    PlayersPerTeamMaximum = newMax
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
            MessageBox.Show($"Save failed:\n\n{ex.Message}", "BocceManager",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        MessageBox.Show("League saved.", "BocceManager",
            MessageBoxButtons.OK, MessageBoxIcon.Information);

        // Offer propagation if min/max changed on an existing league that has children
        if (!isNew && (oldMin != newMin || oldMax != newMax))
            OfferPropagate(savedId, oldMin, oldMax, newMin, newMax);

        LoadLeagueList();
        SelectLeagueInCombo(savedId);
    }

    private void OfferPropagate(int leagueId, int? oldMin, int? oldMax, int? newMin, int? newMax)
    {
        // Only propagate the fields that actually changed
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
            MessageBox.Show($"Updated {dlg.ApprovedTargets.Count} record(s).", "BocceManager",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Propagation failed:\n\n{ex.Message}", "BocceManager",
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

    private void SelectLeagueInCombo(int leagueId)
    {
        for (int i = 0; i < _leagueCombo.Items.Count; i++)
        {
            if (_leagueCombo.Items[i] is ComboItem ci && ci.Id == leagueId)
            {
                _leagueCombo.SelectedIndex = i;
                return;
            }
        }
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
            MessageBox.Show($"Could not compute impact:\n\n{ex.Message}", "BocceManager",
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
                sb.AppendLine($"    • {s.Name}  —  {s.Divisions} division(s), {s.Teams} team(s)");
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
        sb.AppendLine("Players are NOT deleted — they belong to the app, not this league.");
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
            MessageBox.Show($"Delete failed:\n\n{ex.Message}", "BocceManager",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        MessageBox.Show("League deleted.", "BocceManager",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
        _selectedLeagueId = null;
        LoadLeagueList();
        if (_leagueCombo.Items.Count == 0) ClearEditorForm();
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
            MessageBox.Show("Select a season first.", "BocceManager",
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

    private sealed record ComboItem(int Id, string Name)
    {
        public override string ToString() => Name;
    }
}

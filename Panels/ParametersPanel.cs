using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Panels;

public class ParametersPanel : UserControl
{
    public ParametersPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;
        BuildUI();
    }

    private void BuildUI()
    {
        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.FontDefault,
            Padding = new Point(16, 6)
        };

        tabs.TabPages.Add(BuildAppTab());
        tabs.TabPages.Add(BuildLeagueTab());
        tabs.TabPages.Add(BuildSeasonTab());
        tabs.TabPages.Add(BuildDivisionTab());
        tabs.TabPages.Add(BuildTeamTab());
        tabs.TabPages.Add(BuildPlayerTab());

        Controls.Add(tabs);
    }

    // ── App Level ────────────────────────────────────────────────────────────

    private TabPage BuildAppTab()
    {
        var page = new TabPage("  App Level  ");
        var grid = MakeGrid();

        LoadAppParams(grid);

        var toolbar = MakeToolbar(
            () => AddRow(grid),
            () => DeleteRow(grid),
            () => SaveAppParams(grid));

        var container = MakeTabContainer(null, grid, toolbar);
        page.Controls.Add(container);
        return page;
    }

    private static void LoadAppParams(DataGridView grid)
    {
        grid.Rows.Clear();
        try
        {
            using var db = new BocceDbContext();
            foreach (var p in db.AppParameters.OrderBy(p => p.Key))
                grid.Rows.Add(p.Id, p.Key, p.Value, p.Description ?? "");
        }
        catch { }
    }

    private static void SaveAppParams(DataGridView grid)
    {
        var rows = GridRows(grid);
        try
        {
            using var db = new BocceDbContext();
            db.AppParameters.RemoveRange(db.AppParameters);
            foreach (var (key, value, desc) in rows)
                db.AppParameters.Add(new AppParameter { Key = key, Value = value, Description = NullIfEmpty(desc) });
            db.SaveChanges();
        }
        catch (Exception ex) { ShowError(ex); return; }
        LoadAppParams(grid);
        ShowSaved();
    }

    // ── League Level ─────────────────────────────────────────────────────────

    private TabPage BuildLeagueTab()
    {
        var page   = new TabPage("  League Level  ");
        var grid   = MakeGrid();
        var combo  = MakeCombo();

        List<(int Id, string Name)> leagues = [];
        try
        {
            using var db = new BocceDbContext();
            leagues = db.Leagues.Where(l => l.IsActive).OrderBy(l => l.Name)
                        .Select(l => new { l.Id, l.Name }).ToList()
                        .Select(l => (l.Id, l.Name)).ToList();
        }
        catch { }

        foreach (var (id, name) in leagues)
            combo.Items.Add(new ComboItem(id, name));

        combo.SelectedIndexChanged += (_, _) =>
        {
            if (combo.SelectedItem is ComboItem item)
                LoadLeagueParams(grid, item.Id);
            else
                grid.Rows.Clear();
        };

        var toolbar = MakeToolbar(
            () => AddRow(grid),
            () => DeleteRow(grid),
            () => { if (combo.SelectedItem is ComboItem item) SaveLeagueParams(grid, item.Id); });

        var header = MakeSelectorHeader("League:", combo);
        var container = MakeTabContainer(header, grid, toolbar);
        page.Controls.Add(container);

        if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        return page;
    }

    private static void LoadLeagueParams(DataGridView grid, int leagueId)
    {
        grid.Rows.Clear();
        try
        {
            using var db = new BocceDbContext();
            foreach (var p in db.LeagueParameters.Where(p => p.LeagueId == leagueId).OrderBy(p => p.Key))
                grid.Rows.Add(p.Id, p.Key, p.Value, p.Description ?? "");
        }
        catch { }
    }

    private static void SaveLeagueParams(DataGridView grid, int leagueId)
    {
        var rows = GridRows(grid);
        try
        {
            using var db = new BocceDbContext();
            db.LeagueParameters.RemoveRange(db.LeagueParameters.Where(p => p.LeagueId == leagueId));
            foreach (var (key, value, desc) in rows)
                db.LeagueParameters.Add(new LeagueParameter { LeagueId = leagueId, Key = key, Value = value, Description = NullIfEmpty(desc) });
            db.SaveChanges();
        }
        catch (Exception ex) { ShowError(ex); return; }
        LoadLeagueParams(grid, leagueId);
        ShowSaved();
    }

    // ── Season Level ─────────────────────────────────────────────────────────

    private TabPage BuildSeasonTab()
    {
        var page  = new TabPage("  Season Level  ");
        var grid  = MakeGrid();
        var combo = MakeCombo();

        List<(int Id, string Name)> seasons = [];
        try
        {
            using var db = new BocceDbContext();
            seasons = db.Seasons.Include(s => s.League).OrderBy(s => s.League.Name).ThenBy(s => s.Name)
                        .Select(s => new { s.Id, Label = s.League.Name + " — " + s.Name }).ToList()
                        .Select(s => (s.Id, s.Label)).ToList();
        }
        catch { }

        foreach (var (id, name) in seasons)
            combo.Items.Add(new ComboItem(id, name));

        combo.SelectedIndexChanged += (_, _) =>
        {
            if (combo.SelectedItem is ComboItem item)
                LoadSeasonParams(grid, item.Id);
            else
                grid.Rows.Clear();
        };

        var toolbar = MakeToolbar(
            () => AddRow(grid),
            () => DeleteRow(grid),
            () => { if (combo.SelectedItem is ComboItem item) SaveSeasonParams(grid, item.Id); });

        var header = MakeSelectorHeader("Season:", combo);
        var container = MakeTabContainer(header, grid, toolbar);
        page.Controls.Add(container);

        if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        return page;
    }

    private static void LoadSeasonParams(DataGridView grid, int seasonId)
    {
        grid.Rows.Clear();
        try
        {
            using var db = new BocceDbContext();
            foreach (var p in db.SeasonParameters.Where(p => p.SeasonId == seasonId).OrderBy(p => p.Key))
                grid.Rows.Add(p.Id, p.Key, p.Value, p.Description ?? "");
        }
        catch { }
    }

    private static void SaveSeasonParams(DataGridView grid, int seasonId)
    {
        var rows = GridRows(grid);
        try
        {
            using var db = new BocceDbContext();
            db.SeasonParameters.RemoveRange(db.SeasonParameters.Where(p => p.SeasonId == seasonId));
            foreach (var (key, value, desc) in rows)
                db.SeasonParameters.Add(new SeasonParameter { SeasonId = seasonId, Key = key, Value = value, Description = NullIfEmpty(desc) });
            db.SaveChanges();
        }
        catch (Exception ex) { ShowError(ex); return; }
        LoadSeasonParams(grid, seasonId);
        ShowSaved();
    }

    // ── Division Level ────────────────────────────────────────────────────────

    private TabPage BuildDivisionTab()
    {
        var page  = new TabPage("  Division Level  ");
        var grid  = MakeGrid();
        var combo = MakeCombo();

        List<(int Id, string Name)> divisions = [];
        try
        {
            using var db = new BocceDbContext();
            divisions = db.Divisions.Include(d => d.Season)
                          .OrderBy(d => d.Season.Name).ThenBy(d => d.Name)
                          .Select(d => new { d.Id, Label = d.Season.Name + " — " + d.Name }).ToList()
                          .Select(d => (d.Id, d.Label)).ToList();
        }
        catch { }

        foreach (var (id, name) in divisions)
            combo.Items.Add(new ComboItem(id, name));

        combo.SelectedIndexChanged += (_, _) =>
        {
            if (combo.SelectedItem is ComboItem item)
                LoadDivisionParams(grid, item.Id);
            else
                grid.Rows.Clear();
        };

        var toolbar = MakeToolbar(
            () => AddRow(grid),
            () => DeleteRow(grid),
            () => { if (combo.SelectedItem is ComboItem item) SaveDivisionParams(grid, item.Id); });

        var header = MakeSelectorHeader("Division:", combo);
        var container = MakeTabContainer(header, grid, toolbar);
        page.Controls.Add(container);

        if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        return page;
    }

    private static void LoadDivisionParams(DataGridView grid, int divisionId)
    {
        grid.Rows.Clear();
        try
        {
            using var db = new BocceDbContext();
            foreach (var p in db.DivisionParameters.Where(p => p.DivisionId == divisionId).OrderBy(p => p.Key))
                grid.Rows.Add(p.Id, p.Key, p.Value, p.Description ?? "");
        }
        catch { }
    }

    private static void SaveDivisionParams(DataGridView grid, int divisionId)
    {
        var rows = GridRows(grid);
        try
        {
            using var db = new BocceDbContext();
            db.DivisionParameters.RemoveRange(db.DivisionParameters.Where(p => p.DivisionId == divisionId));
            foreach (var (key, value, desc) in rows)
                db.DivisionParameters.Add(new DivisionParameter { DivisionId = divisionId, Key = key, Value = value, Description = NullIfEmpty(desc) });
            db.SaveChanges();
        }
        catch (Exception ex) { ShowError(ex); return; }
        LoadDivisionParams(grid, divisionId);
        ShowSaved();
    }

    // ── Team Level ───────────────────────────────────────────────────────────

    private TabPage BuildTeamTab()
    {
        var page  = new TabPage("  Team Level  ");
        var grid  = MakeGrid();
        var combo = MakeCombo();

        List<(int Id, string Name)> teams = [];
        try
        {
            using var db = new BocceDbContext();
            teams = db.Teams.Include(t => t.Division)
                      .Where(t => t.IsActive)
                      .OrderBy(t => t.Division.Name).ThenBy(t => t.EffectiveDisplayName)
                      .Select(t => new { t.Id, Label = t.Division.Name + " — " + (t.DisplayName ?? t.SystemName) }).ToList()
                      .Select(t => (t.Id, t.Label)).ToList();
        }
        catch { }

        foreach (var (id, name) in teams)
            combo.Items.Add(new ComboItem(id, name));

        combo.SelectedIndexChanged += (_, _) =>
        {
            if (combo.SelectedItem is ComboItem item)
                LoadTeamParams(grid, item.Id);
            else
                grid.Rows.Clear();
        };

        var toolbar = MakeToolbar(
            () => AddRow(grid),
            () => DeleteRow(grid),
            () => { if (combo.SelectedItem is ComboItem item) SaveTeamParams(grid, item.Id); });

        var header = MakeSelectorHeader("Team:", combo);
        var container = MakeTabContainer(header, grid, toolbar);
        page.Controls.Add(container);

        if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        return page;
    }

    private static void LoadTeamParams(DataGridView grid, int teamId)
    {
        grid.Rows.Clear();
        try
        {
            using var db = new BocceDbContext();
            foreach (var p in db.TeamParameters.Where(p => p.TeamId == teamId).OrderBy(p => p.Key))
                grid.Rows.Add(p.Id, p.Key, p.Value, p.Description ?? "");
        }
        catch { }
    }

    private static void SaveTeamParams(DataGridView grid, int teamId)
    {
        var rows = GridRows(grid);
        try
        {
            using var db = new BocceDbContext();
            db.TeamParameters.RemoveRange(db.TeamParameters.Where(p => p.TeamId == teamId));
            foreach (var (key, value, desc) in rows)
                db.TeamParameters.Add(new TeamParameter { TeamId = teamId, Key = key, Value = value, Description = NullIfEmpty(desc) });
            db.SaveChanges();
        }
        catch (Exception ex) { ShowError(ex); return; }
        LoadTeamParams(grid, teamId);
        ShowSaved();
    }

    // ── Player Level ─────────────────────────────────────────────────────────

    private TabPage BuildPlayerTab()
    {
        var page  = new TabPage("  Player Level  ");
        var grid  = MakeGrid();
        var combo = MakeCombo();

        List<(int Id, string Name)> players = [];
        try
        {
            using var db = new BocceDbContext();
            players = db.Players.Where(p => p.IsActive)
                        .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
                        .Select(p => new { p.Id, Label = p.LastName + ", " + p.FirstName }).ToList()
                        .Select(p => (p.Id, p.Label)).ToList();
        }
        catch { }

        foreach (var (id, name) in players)
            combo.Items.Add(new ComboItem(id, name));

        combo.SelectedIndexChanged += (_, _) =>
        {
            if (combo.SelectedItem is ComboItem item)
                LoadPlayerParams(grid, item.Id);
            else
                grid.Rows.Clear();
        };

        var toolbar = MakeToolbar(
            () => AddRow(grid),
            () => DeleteRow(grid),
            () => { if (combo.SelectedItem is ComboItem item) SavePlayerParams(grid, item.Id); });

        var header = MakeSelectorHeader("Player:", combo);
        var container = MakeTabContainer(header, grid, toolbar);
        page.Controls.Add(container);

        if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        return page;
    }

    private static void LoadPlayerParams(DataGridView grid, int playerId)
    {
        grid.Rows.Clear();
        try
        {
            using var db = new BocceDbContext();
            foreach (var p in db.PlayerParameters.Where(p => p.PlayerId == playerId).OrderBy(p => p.Key))
                grid.Rows.Add(p.Id, p.Key, p.Value, p.Description ?? "");
        }
        catch { }
    }

    private static void SavePlayerParams(DataGridView grid, int playerId)
    {
        var rows = GridRows(grid);
        try
        {
            using var db = new BocceDbContext();
            db.PlayerParameters.RemoveRange(db.PlayerParameters.Where(p => p.PlayerId == playerId));
            foreach (var (key, value, desc) in rows)
                db.PlayerParameters.Add(new PlayerParameter { PlayerId = playerId, Key = key, Value = value, Description = NullIfEmpty(desc) });
            db.SaveChanges();
        }
        catch (Exception ex) { ShowError(ex); return; }
        LoadPlayerParams(grid, playerId);
        ShowSaved();
    }

    // ── UI Builders ──────────────────────────────────────────────────────────

    private static DataGridView MakeGrid()
    {
        var grid = new DataGridView
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
        };

        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", Visible = false, FillWeight = 1, MinimumWidth = 1 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Key",         HeaderText = "Key",         FillWeight = 25, MinimumWidth = 120 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Value",       HeaderText = "Value",       FillWeight = 30, MinimumWidth = 120 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "Description", FillWeight = 45, MinimumWidth = 100 });

        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            { BackColor = AppTheme.GridAlternateRow };

        return grid;
    }

    private static ComboBox MakeCombo() => new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Font = AppTheme.FontDefault,
        Width = 380,
        Height = 28
    };

    private static Panel MakeSelectorHeader(string label, ComboBox combo)
    {
        var panel = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = AppTheme.Surface, Padding = new Padding(12, 8, 12, 8) };

        var lbl = new Label
        {
            Text = label,
            Font = AppTheme.FontDefaultBold,
            ForeColor = AppTheme.TextPrimary,
            AutoSize = true,
            Location = new Point(12, 14)
        };

        combo.Location = new Point(lbl.PreferredWidth + 20, 10);
        panel.Controls.Add(lbl);
        panel.Controls.Add(combo);
        return panel;
    }

    private static Panel MakeToolbar(Action onAdd, Action onDelete, Action onSave)
    {
        var panel = new Panel { Dock = DockStyle.Bottom, Height = 46, BackColor = AppTheme.Surface, Padding = new Padding(12, 8, 12, 8) };

        Button Btn(string text, Color back, int x) => new Button
        {
            Text = text,
            Location = new Point(x, 8),
            Size = new Size(120, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = back,
            ForeColor = Color.White,
            Font = AppTheme.FontButton,
            Cursor = Cursors.Hand,
            FlatAppearance = { BorderSize = 0 }
        };

        var btnAdd    = Btn("+ Add Row",       AppTheme.ButtonSuccess, 0);
        var btnDelete = Btn("Delete Selected", AppTheme.ButtonDanger,  130);
        var btnSave   = Btn("Save Changes",    AppTheme.Accent,        360);

        btnAdd.Click    += (_, _) => onAdd();
        btnDelete.Click += (_, _) => onDelete();
        btnSave.Click   += (_, _) => onSave();

        panel.Controls.AddRange([btnAdd, btnDelete, btnSave]);
        return panel;
    }

    private static Panel MakeTabContainer(Panel? header, DataGridView grid, Panel toolbar)
    {
        var container = new Panel { Dock = DockStyle.Fill };
        if (header != null) container.Controls.Add(header);
        container.Controls.Add(grid);
        container.Controls.Add(toolbar);
        return container;
    }

    // ── Grid Helpers ─────────────────────────────────────────────────────────

    private static void AddRow(DataGridView grid)
    {
        int idx = grid.Rows.Add("0", "", "", "");
        grid.ClearSelection();
        grid.Rows[idx].Selected = true;
        grid.CurrentCell = grid.Rows[idx].Cells["Key"];
        grid.BeginEdit(true);
    }

    private static void DeleteRow(DataGridView grid)
    {
        if (grid.SelectedRows.Count == 0) return;
        grid.Rows.Remove(grid.SelectedRows[0]);
    }

    private static List<(string Key, string Value, string Desc)> GridRows(DataGridView grid)
    {
        var result = new List<(string, string, string)>();
        foreach (DataGridViewRow row in grid.Rows)
        {
            var key = row.Cells["Key"].Value?.ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(key)) continue;
            var value = row.Cells["Value"].Value?.ToString() ?? "";
            var desc  = row.Cells["Description"].Value?.ToString() ?? "";
            result.Add((key, value, desc));
        }
        return result;
    }

    private static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s;

    private static void ShowSaved()
        => MessageBox.Show("Parameters saved.", "BocceManager", MessageBoxButtons.OK, MessageBoxIcon.Information);

    private static void ShowError(Exception ex)
        => MessageBox.Show($"Save failed:\n\n{ex.Message}", "BocceManager", MessageBoxButtons.OK, MessageBoxIcon.Error);

    // ── ComboItem ────────────────────────────────────────────────────────────

    private sealed record ComboItem(int Id, string Name)
    {
        public override string ToString() => Name;
    }
}

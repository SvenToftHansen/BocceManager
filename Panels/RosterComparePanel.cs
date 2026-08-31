using BocceManager.Data;
using BocceManager.Services;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;
using static BocceManager.Services.RosterCompareService;

namespace BocceManager.Panels;

// Lets the admin load back an edited Team Listing (Full Roster) Excel export and see
// exactly what changed versus the current database, then selectively apply the changes.
public class RosterComparePanel : UserControl
{
    private ComboBox _cmbSeason = null!;
    private Label _lblFile = null!;
    private Button _btnLoadFile = null!;
    private Button _btnSelectAll = null!;
    private Button _btnSelectNone = null!;
    private Button _btnApply = null!;
    private DataGridView _grid = null!;
    private Label _lblStatus = null!;
    private Label _lblUnmatched = null!;

    private string? _loadedFilePath;

    private sealed class SeasonItem
    {
        public int Id { get; set; }
        public string Label { get; set; } = "";
        public override string ToString() => Label;
    }

    public RosterComparePanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;
        BuildUi();
        LoadSeasons();
    }

    private void BuildUi()
    {
        var title = new Label
        {
            Text = "Roster Compare",
            Dock = DockStyle.Top,
            Height = 32,
            Font = AppTheme.FontSectionHeading,
            ForeColor = AppTheme.TextPrimary,
            Padding = new Padding(8, 4, 0, 0)
        };

        var toolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 114,
            BackColor = AppTheme.Surface,
            Padding = new Padding(12, 8, 12, 8)
        };

        var lblSeason = new Label
        {
            Text = "Season:", Location = new Point(12, 12), AutoSize = true,
            Font = AppTheme.FontDefaultBold, ForeColor = AppTheme.TextPrimary
        };
        _cmbSeason = new ComboBox
        {
            Location = new Point(80, 8), Size = new Size(320, 26),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = AppTheme.FontDefault, BackColor = AppTheme.Surface, ForeColor = AppTheme.TextPrimary
        };

        _btnLoadFile = new Button
        {
            Text = "Load Returned Excel File...", Location = new Point(412, 7), Size = new Size(210, 30),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.Accent, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 }
        };
        _btnLoadFile.Click += (_, _) => LoadFile();

        _lblFile = new Label
        {
            Text = "No file loaded — export the Team Listing (Full Roster) report, send it to your admin, then load their reply here.",
            Location = new Point(12, 46), AutoSize = true,
            Font = AppTheme.FontSmall, ForeColor = AppTheme.TextSecondary
        };

        _btnSelectAll = new Button
        {
            Text = "Select All", Location = new Point(12, 78), Size = new Size(100, 26),
            FlatStyle = FlatStyle.Flat, Font = AppTheme.FontButton, Cursor = Cursors.Hand,
            FlatAppearance = { BorderSize = 1, BorderColor = AppTheme.Separator }, Visible = false
        };
        _btnSelectAll.Click += (_, _) => SetAllChecked(true);

        _btnSelectNone = new Button
        {
            Text = "Select None", Location = new Point(120, 78), Size = new Size(100, 26),
            FlatStyle = FlatStyle.Flat, Font = AppTheme.FontButton, Cursor = Cursors.Hand,
            FlatAppearance = { BorderSize = 1, BorderColor = AppTheme.Separator }, Visible = false
        };
        _btnSelectNone.Click += (_, _) => SetAllChecked(false);

        toolbar.Controls.AddRange([lblSeason, _cmbSeason, _btnLoadFile, _lblFile, _btnSelectAll, _btnSelectNone]);

        _grid = BuildGrid();

        var bottomBar = new Panel { Dock = DockStyle.Bottom, Height = 46, BackColor = AppTheme.Surface };
        _btnApply = new Button
        {
            Text = "Apply Selected", Location = new Point(12, 8), Size = new Size(140, 30),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.ButtonSuccess, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 }, Enabled = false
        };
        _btnApply.Click += (_, _) => ApplySelected();

        _lblStatus = new Label
        {
            Location = new Point(164, 13), AutoSize = true,
            Font = AppTheme.FontDefault, ForeColor = AppTheme.TextSecondary
        };
        bottomBar.Controls.AddRange([_btnApply, _lblStatus]);

        _lblUnmatched = new Label
        {
            Dock = DockStyle.Bottom, Height = 0, AutoSize = false,
            Font = AppTheme.FontSmall, ForeColor = AppTheme.ButtonDanger,
            Padding = new Padding(12, 4, 12, 4), Visible = false
        };

        Controls.Add(_grid);
        Controls.Add(_lblUnmatched);
        Controls.Add(bottomBar);
        Controls.Add(toolbar);
        Controls.Add(title);
    }

    private DataGridView BuildGrid()
    {
        var g = new DataGridView
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
            RowTemplate = { Height = 28 },
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            EnableHeadersVisualStyles = false,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppTheme.GridHeaderBackground, ForeColor = AppTheme.GridHeaderText,
                SelectionBackColor = AppTheme.GridHeaderBackground, SelectionForeColor = AppTheme.GridHeaderText,
                Font = AppTheme.FontGridHeader, Padding = new Padding(4, 0, 0, 0)
            }
        };
        g.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = AppTheme.GridAlternateRow };

        g.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Apply", HeaderText = "", FillWeight = 6, MinimumWidth = 40 });
        g.Columns.Add(new DataGridViewTextBoxColumn { Name = "Team", HeaderText = "Team", FillWeight = 16, ReadOnly = true });
        g.Columns.Add(new DataGridViewTextBoxColumn { Name = "Slot", HeaderText = "Slot", FillWeight = 12, ReadOnly = true });
        g.Columns.Add(new DataGridViewTextBoxColumn { Name = "Change", HeaderText = "Change", FillWeight = 14, ReadOnly = true });
        g.Columns.Add(new DataGridViewTextBoxColumn { Name = "Old", HeaderText = "Current", FillWeight = 21, ReadOnly = true });
        g.Columns.Add(new DataGridViewTextBoxColumn { Name = "New", HeaderText = "From File", FillWeight = 21, ReadOnly = true });
        g.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", FillWeight = 20, ReadOnly = true });

        g.CellValueChanged += (_, e) => { if (e.ColumnIndex == 0) UpdateStatusLine(); };
        g.CurrentCellDirtyStateChanged += (_, _) => { if (g.IsCurrentCellDirty) g.CommitEdit(DataGridViewDataErrorContexts.Commit); };

        return g;
    }

    // ── Season list ────────────────────────────────────────────────────────────
    private void LoadSeasons()
    {
        _cmbSeason.Items.Clear();
        int? defaultSeasonId = null;
        try
        {
            using var db = new BocceDbContext();
            defaultSeasonId = AppParameterService.GetDefaultSeasonId(db);

            var seasons = db.Seasons.Include(s => s.League)
                .OrderByDescending(s => s.Id)
                .Select(s => new { s.Id, s.Name, LeagueName = s.League.Name })
                .ToList();

            foreach (var s in seasons)
                _cmbSeason.Items.Add(new SeasonItem { Id = s.Id, Label = $"{s.LeagueName} — {s.Name}" });
        }
        catch { }

        if (_cmbSeason.Items.Count == 0) return;

        int selectIdx = 0;
        if (defaultSeasonId.HasValue)
        {
            for (int i = 0; i < _cmbSeason.Items.Count; i++)
                if (((SeasonItem)_cmbSeason.Items[i]!).Id == defaultSeasonId.Value) { selectIdx = i; break; }
        }
        _cmbSeason.SelectedIndex = selectIdx;
    }

    // ── Load & compare ─────────────────────────────────────────────────────────
    private void LoadFile()
    {
        if (_cmbSeason.SelectedItem is not SeasonItem season)
        {
            MessageBox.Show("Select a season first.", "Roster Compare", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new OpenFileDialog
        {
            Title = "Load Returned Team Listing Excel File",
            Filter = "Excel files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        RunCompare(season.Id, dlg.FileName);
    }

    private void RunCompare(int seasonId, string filePath)
    {
        RosterCompareResult result;
        try
        {
            result = RosterCompareService.Compare(seasonId, filePath);
        }
        catch (Exception ex)
        {
            AppLogger.Error(ex, "Roster compare failed for {Path}", filePath);
            MessageBox.Show($"Could not read file:\n{ex.Message}", "Roster Compare", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _loadedFilePath = filePath;
        _lblFile.Text = $"Loaded: {Path.GetFileName(filePath)}";

        _grid.Rows.Clear();
        foreach (var d in result.Diffs)
        {
            int rowIdx = _grid.Rows.Add(
                d.Resolvable, d.TeamLabel, d.SlotLabel, KindLabel(d.Kind), d.OldValue, d.NewValue,
                d.Resolvable ? "" : "⚠ Not found in Players — resolve manually");
            _grid.Rows[rowIdx].Tag = d;
            if (!d.Resolvable)
            {
                _grid.Rows[rowIdx].Cells["Apply"].Value = false;
                _grid.Rows[rowIdx].Cells["Apply"].ReadOnly = true;
            }
        }

        _lblUnmatched.Visible = result.UnmatchedTeams.Count > 0;
        if (result.UnmatchedTeams.Count > 0)
        {
            _lblUnmatched.Height = 20;
            _lblUnmatched.Text = $"⚠ Teams in file not found in this season: {string.Join(", ", result.UnmatchedTeams)}";
        }
        else
        {
            _lblUnmatched.Height = 0;
        }

        bool hasDiffs = result.Diffs.Count > 0;
        _btnSelectAll.Visible = hasDiffs;
        _btnSelectNone.Visible = hasDiffs;
        _btnApply.Enabled = hasDiffs;

        UpdateStatusLine();
    }

    private static string KindLabel(DiffKind kind) => kind switch
    {
        DiffKind.PlayerAdded => "Player added",
        DiffKind.PlayerRemoved => "Player removed",
        DiffKind.PlayerChanged => "Player changed",
        DiffKind.PhoneChanged => "Phone changed",
        _ => kind.ToString()
    };

    private void SetAllChecked(bool value)
    {
        foreach (DataGridViewRow row in _grid.Rows)
            if (!row.Cells["Apply"].ReadOnly) row.Cells["Apply"].Value = value;
        UpdateStatusLine();
    }

    private void UpdateStatusLine()
    {
        int total = _grid.Rows.Count;
        int selected = _grid.Rows.Cast<DataGridViewRow>().Count(r => r.Cells["Apply"].Value is true);
        _lblStatus.Text = total == 0
            ? ""
            : $"{selected} of {total} change(s) selected";
    }

    // ── Apply ──────────────────────────────────────────────────────────────────
    private void ApplySelected()
    {
        var selected = _grid.Rows.Cast<DataGridViewRow>()
            .Where(r => r.Cells["Apply"].Value is true && r.Tag is RosterDiff)
            .Select(r => (RosterDiff)r.Tag!)
            .ToList();

        if (selected.Count == 0)
        {
            MessageBox.Show("No changes selected.", "Roster Compare", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show(
            $"Apply {selected.Count} selected change(s) to the database? This cannot be undone.",
            "Confirm Apply", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        (int applied, List<string> errors) result;
        try
        {
            result = RosterCompareService.Apply(selected);
        }
        catch (Exception ex)
        {
            AppLogger.Error(ex, "Roster diff apply failed");
            MessageBox.Show($"Apply failed:\n{ex.Message}", "Roster Compare", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        AppLogger.Info("Roster Compare applied {Applied} change(s), {Errors} error(s)", result.applied, result.errors.Count);

        string msg = $"Applied {result.applied} change(s).";
        if (result.errors.Count > 0)
            msg += $"\n\n{result.errors.Count} could not be applied:\n" + string.Join("\n", result.errors);
        MessageBox.Show(msg, "Apply Complete", MessageBoxButtons.OK,
            result.errors.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);

        // Refresh against the same file to show what, if anything, remains.
        if (_cmbSeason.SelectedItem is SeasonItem season && _loadedFilePath != null)
            RunCompare(season.Id, _loadedFilePath);
    }
}

using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.Services;
using BocceManager.UI.Controls;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Panels;

public class SpareListPanel : UserControl
{
    private sealed class PlayerItem
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = "";
        public override string ToString() => DisplayName;
    }

    private sealed class PlayerLookup
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? LotNumber { get; set; }

        public string DisplayName => string.IsNullOrWhiteSpace(LastName)
            ? FirstName.Trim()
            : string.IsNullOrWhiteSpace(FirstName)
                ? LastName.Trim()
                : $"{LastName}, {FirstName}".Trim();
    }

    private bool _isLoadingData = false;
    private bool _isSwitchingSelection = false;
    private bool _currentSeasonIsLocked = false;
    private List<PlayerLookup> _allPlayers = [];
    private HashSet<int> _currentSparePlayerIds = [];

    private SearchBoxControl _txtSearchAvailable = null!;
    private ListBox _lstAvailablePlayers = null!;
    private SearchBoxControl _txtSearchSpare = null!;
    private ListBox _lstSparePlayers = null!;
    private Button _btnMoveToSpare = null!;
    private Button _btnRemoveFromSpare = null!;
    private Label _lblAvailableCount = null!;
    private Label _lblSpareCount = null!;
    private Dictionary<int, string> _spareNotes = [];

    public SpareListPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;
        BuildUi();
        LoadData();
    }

    private void BuildUi()
    {
        var mainPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.ContentBackground,
            Padding = new Padding(12)
        };

        const int colWidth = 380;
        const int leftPad = 20;
        const int col2Left = leftPad + colWidth + 120;
        const int searchHeight = 30;
        const int buttonHeight = 36;

        // Available Players Column
        var lblAvailable = new Label
        {
            Text = "Available Players",
            Location = new Point(leftPad, 0),
            Size = new Size(colWidth, 24),
            Font = AppTheme.FontDefaultBold,
            ForeColor = AppTheme.TextPrimary
        };

        var searchHintAvailable = new Label
        {
            Text = "Delimiters: |  \\  /  :  ;",
            Location = new Point(leftPad, 26),
            Size = new Size(colWidth, 18),
            Font = AppTheme.FontSmall,
            ForeColor = AppTheme.TextMuted
        };

        _txtSearchAvailable = new SearchBoxControl("Search available...")
        {
            Location = new Point(leftPad, 44),
            Size = new Size(colWidth, searchHeight)
        };
        _txtSearchAvailable.SearchTextChanged += (_, _) => ApplyAvailableFilter();

        _lstAvailablePlayers = new ListBox
        {
            Location = new Point(leftPad, 78),
            Size = new Size(colWidth, 380),
            Font = AppTheme.FontDefault,
            BorderStyle = BorderStyle.FixedSingle,
            IntegralHeight = false,
            SelectionMode = SelectionMode.MultiExtended
        };
        _lstAvailablePlayers.SelectedIndexChanged += (_, _) => OnAvailableListSelection();
        _lstAvailablePlayers.DoubleClick += (_, _) => MoveToSpare();

        _lblAvailableCount = new Label
        {
            Location = new Point(leftPad, 464),
            Size = new Size(colWidth, 20),
            Font = AppTheme.FontSmall,
            ForeColor = AppTheme.TextMuted,
            Text = "0 players"
        };

        mainPanel.Controls.AddRange([lblAvailable, searchHintAvailable, _txtSearchAvailable, _lstAvailablePlayers, _lblAvailableCount]);

        // Transfer Buttons
        _btnMoveToSpare = new Button
        {
            Text = "→ Add",
            Location = new Point(leftPad + colWidth + 10, 100),
            Size = new Size(80, buttonHeight),
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.ButtonSuccess,
            ForeColor = Color.White,
            Font = AppTheme.FontButton,
            Cursor = Cursors.Hand
        };
        _btnMoveToSpare.FlatAppearance.BorderSize = 0;
        _btnMoveToSpare.Click += (_, _) => MoveToSpare();

        _btnRemoveFromSpare = new Button
        {
            Text = "← Remove",
            Location = new Point(leftPad + colWidth + 10, 150),
            Size = new Size(80, buttonHeight),
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.ButtonDanger,
            ForeColor = Color.White,
            Font = AppTheme.FontButton,
            Cursor = Cursors.Hand
        };
        _btnRemoveFromSpare.FlatAppearance.BorderSize = 0;
        _btnRemoveFromSpare.Click += (_, _) => RemoveFromSpare();

        mainPanel.Controls.AddRange([_btnMoveToSpare, _btnRemoveFromSpare]);

        // Spare List Column
        var lblSpare = new Label
        {
            Text = "Spare List",
            Location = new Point(col2Left, 0),
            Size = new Size(colWidth, 24),
            Font = AppTheme.FontDefaultBold,
            ForeColor = AppTheme.TextPrimary
        };

        var searchHintSpare = new Label
        {
            Text = "Delimiters: |  \\  /  :  ;",
            Location = new Point(col2Left, 26),
            Size = new Size(colWidth, 18),
            Font = AppTheme.FontSmall,
            ForeColor = AppTheme.TextMuted
        };

        _txtSearchSpare = new SearchBoxControl("Search spare list...")
        {
            Location = new Point(col2Left, 44),
            Size = new Size(colWidth, searchHeight)
        };
        _txtSearchSpare.SearchTextChanged += (_, _) => ApplySpareFilter();

        _lstSparePlayers = new ListBox
        {
            Location = new Point(col2Left, 78),
            Size = new Size(colWidth, 380),
            Font = AppTheme.FontDefault,
            BorderStyle = BorderStyle.FixedSingle,
            IntegralHeight = false,
            SelectionMode = SelectionMode.MultiExtended
        };
        _lstSparePlayers.SelectedIndexChanged += (_, _) => OnSpareListSelection();
        _lstSparePlayers.DoubleClick += (_, _) => EditSelectedSpareNote();

        _lblSpareCount = new Label
        {
            Location = new Point(col2Left, 464),
            Size = new Size(colWidth, 20),
            Font = AppTheme.FontSmall,
            ForeColor = AppTheme.TextMuted,
            Text = "0 players"
        };

        mainPanel.Controls.AddRange([lblSpare, searchHintSpare, _txtSearchSpare, _lstSparePlayers, _lblSpareCount]);

        Controls.Add(mainPanel);
    }

    private void LoadData()
    {
        if (_isLoadingData) return;

        _isLoadingData = true;
        try
        {
            using var db = new BocceDbContext();
            var leagueId = AppParameterService.GetDefaultLeagueId(db);

            if (!leagueId.HasValue) return;

            // Load all active players
            _allPlayers = db.Players
                .Where(p => p.IsActive)
                .AsNoTracking()
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .Select(p => new PlayerLookup
                {
                    Id = p.Id,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    Email = p.Email,
                    Phone = p.Phone,
                    LotNumber = p.LotNumber
                })
                .ToList();

            // Load current spare list for this league
            _currentSparePlayerIds = db.SpareLists
                .Where(s => s.LeagueId == leagueId.Value && s.IsActive)
                .Select(s => s.PlayerId)
                .ToHashSet();

            _spareNotes = db.SpareLists
                .Where(s => s.LeagueId == leagueId.Value && s.IsActive && s.Notes != null && s.Notes != "")
                .ToDictionary(s => s.PlayerId, s => s.Notes!);

            var currentSeason = FeeService.GetCurrentSeason(db, leagueId.Value);
            _currentSeasonIsLocked = currentSeason?.IsLocked ?? false;

            ApplyAvailableFilter();
            ApplySpareFilter();

            _btnMoveToSpare.Enabled     = !_currentSeasonIsLocked;
            _btnRemoveFromSpare.Enabled = !_currentSeasonIsLocked;
        }
        finally
        {
            _isLoadingData = false;
        }
    }

    private void ApplyAvailableFilter()
    {
        var query = _txtSearchAvailable.SearchText;
        var filtered = _allPlayers
            .Where(p => !_currentSparePlayerIds.Contains(p.Id))
            .Where(p => SearchQueryService.MatchesAnyTerm($"{p.DisplayName} {p.Email} {p.Phone} {p.LotNumber}", query))
            .Select(p => new PlayerItem { Id = p.Id, DisplayName = p.DisplayName })
            .ToList();

        _lstAvailablePlayers.BeginUpdate();
        _lstAvailablePlayers.DataSource = null;
        _lstAvailablePlayers.DataSource = filtered;
        _lstAvailablePlayers.EndUpdate();

        _lblAvailableCount.Text = $"{filtered.Count} player{(filtered.Count != 1 ? "s" : "")}";
    }

    private void ApplySpareFilter()
    {
        var query = _txtSearchSpare.SearchText;
        var spareList = _allPlayers
            .Where(p => _currentSparePlayerIds.Contains(p.Id))
            .Where(p => SearchQueryService.MatchesAnyTerm($"{p.DisplayName} {p.Email} {p.Phone} {p.LotNumber}", query))
            .Select(p => new PlayerItem
            {
                Id = p.Id,
                DisplayName = _spareNotes.TryGetValue(p.Id, out var note)
                    ? $"{p.DisplayName}  — {Truncate(note, 40)}"
                    : p.DisplayName
            })
            .ToList();

        _lstSparePlayers.BeginUpdate();
        _lstSparePlayers.DataSource = null;
        _lstSparePlayers.DataSource = spareList;
        _lstSparePlayers.EndUpdate();

        _lblSpareCount.Text = $"{spareList.Count} player{(spareList.Count != 1 ? "s" : "")}";
    }

    private void OnAvailableListSelection()
    {
        if (_isSwitchingSelection) return;
        _isSwitchingSelection = true;
        try
        {
            if (_lstAvailablePlayers.SelectedItems.Count > 0)
                _lstSparePlayers.ClearSelected();
        }
        finally
        {
            _isSwitchingSelection = false;
        }
    }

    private void OnSpareListSelection()
    {
        if (_isSwitchingSelection) return;
        _isSwitchingSelection = true;
        try
        {
            if (_lstSparePlayers.SelectedItems.Count > 0)
                _lstAvailablePlayers.ClearSelected();
        }
        finally
        {
            _isSwitchingSelection = false;
        }
    }

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength];

    private void EditSelectedSpareNote()
    {
        if (_lstSparePlayers.SelectedItem is not PlayerItem item) return;

        try
        {
            using var db = new BocceDbContext();
            var leagueId = AppParameterService.GetDefaultLeagueId(db);
            if (!leagueId.HasValue) return;

            var sparePlayer = db.SpareLists
                .FirstOrDefault(s => s.LeagueId == leagueId.Value && s.PlayerId == item.Id && s.IsActive);
            if (sparePlayer == null) return;

            using var form = new Form
            {
                Text = $"Note for {item.DisplayName}",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MinimizeBox = false,
                MaximizeBox = false,
                ClientSize = new Size(400, 160),
                BackColor = AppTheme.Surface
            };

            var txt = new TextBox
            {
                Location = new Point(12, 12),
                Size = new Size(376, 60),
                Multiline = true,
                Font = AppTheme.FontDefault,
                Text = sparePlayer.Notes ?? ""
            };

            var btnOk = new Button
            {
                Text = "Save", DialogResult = DialogResult.OK,
                Location = new Point(224, 84), Size = new Size(80, 36),
                FlatStyle = FlatStyle.Flat, BackColor = AppTheme.ButtonSuccess, ForeColor = Color.White, Font = AppTheme.FontButton
            };
            var btnCancel = new Button
            {
                Text = "Cancel", DialogResult = DialogResult.Cancel,
                Location = new Point(308, 84), Size = new Size(80, 36),
                FlatStyle = FlatStyle.Flat, BackColor = AppTheme.Surface, ForeColor = AppTheme.TextPrimary, Font = AppTheme.FontButton
            };

            form.Controls.AddRange([txt, btnOk, btnCancel]);
            form.AcceptButton = btnOk;
            form.CancelButton = btnCancel;

            if (form.ShowDialog(this) != DialogResult.OK) return;

            var newNotes = string.IsNullOrWhiteSpace(txt.Text) ? null : txt.Text.Trim();
            sparePlayer.Notes = newNotes;
            db.SaveChanges();

            if (newNotes != null) _spareNotes[item.Id] = newNotes;
            else _spareNotes.Remove(item.Id);

            ApplySpareFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving note: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void MoveToSpare()
    {
        var selected = _lstAvailablePlayers.SelectedItems
            .Cast<PlayerItem>()
            .Select(p => p.Id)
            .ToList();

        if (selected.Count == 0) return;

        if (_currentSeasonIsLocked)
        {
            MessageBox.Show("The current season is locked. Players cannot be added to the spare list.",
                "Season Locked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            using var db = new BocceDbContext();
            var leagueId = AppParameterService.GetDefaultLeagueId(db);

            if (!leagueId.HasValue) return;

            var currentSeason = FeeService.GetCurrentSeason(db, leagueId.Value);
            if (currentSeason == null)
            {
                MessageBox.Show(
                    "Cannot add to spare list: no season is marked as current for this league.\n\n" +
                    "Please set a current season in Season Settings before adding spare list players.",
                    "No Current Season", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            foreach (var playerId in selected)
            {
                var existing = db.SpareLists.FirstOrDefault(s =>
                    s.LeagueId == leagueId.Value &&
                    s.PlayerId == playerId);

                if (existing == null)
                {
                    db.SpareLists.Add(new SpareList
                    {
                        LeagueId = leagueId.Value,
                        PlayerId = playerId,
                        IsActive = true
                    });
                }
                else if (!existing.IsActive)
                {
                    existing.IsActive = true;
                }
            }

            db.SaveChanges();

            foreach (var playerId in selected)
                FeeService.EnsureSeasonFee(db, playerId, currentSeason.Id);

            foreach (var playerId in selected)
                _currentSparePlayerIds.Add(playerId);

            ApplyAvailableFilter();
            ApplySpareFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error adding to spare list: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RemoveFromSpare()
    {
        var selected = _lstSparePlayers.SelectedItems
            .Cast<PlayerItem>()
            .Select(p => p.Id)
            .ToList();

        if (selected.Count == 0) return;

        if (_currentSeasonIsLocked)
        {
            MessageBox.Show("The current season is locked. Players cannot be removed from the spare list.",
                "Season Locked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            using var db = new BocceDbContext();
            var leagueId = AppParameterService.GetDefaultLeagueId(db);

            if (!leagueId.HasValue) return;

            foreach (var playerId in selected)
            {
                var existing = db.SpareLists.FirstOrDefault(s =>
                    s.LeagueId == leagueId.Value &&
                    s.PlayerId == playerId);

                if (existing != null)
                    db.SpareLists.Remove(existing);
            }

            db.SaveChanges();

            foreach (var playerId in selected)
                FeeService.RescindUnpaidSeasonFees(db, playerId, leagueId.Value);

            foreach (var playerId in selected)
            {
                _currentSparePlayerIds.Remove(playerId);
                _spareNotes.Remove(playerId);
            }

            ApplyAvailableFilter();
            ApplySpareFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error removing from spare list: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

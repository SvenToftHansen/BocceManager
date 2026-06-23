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
    private Panel _notesPanel = null!;
    private Label _lblSelectedPlayer = null!;
    private TextBox _txtNotes = null!;
    private int? _selectedSparePlayerId;

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
        const int col2Left = colWidth + 120;
        const int searchHeight = 30;
        const int buttonHeight = 36;

        // Available Players Column
        var lblAvailable = new Label
        {
            Text = "Available Players",
            Location = new Point(0, 0),
            Size = new Size(colWidth, 24),
            Font = AppTheme.FontDefaultBold,
            ForeColor = AppTheme.TextPrimary
        };

        var searchHintAvailable = new Label
        {
            Text = "Delimiters: |  \\  /  :  ;",
            Location = new Point(0, 26),
            Size = new Size(colWidth, 18),
            Font = AppTheme.FontSmall,
            ForeColor = AppTheme.TextMuted
        };

        _txtSearchAvailable = new SearchBoxControl("Search available...")
        {
            Location = new Point(0, 44),
            Size = new Size(colWidth, searchHeight)
        };
        _txtSearchAvailable.SearchTextChanged += (_, _) => ApplyAvailableFilter();

        _lstAvailablePlayers = new ListBox
        {
            Location = new Point(0, 78),
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
            Location = new Point(0, 464),
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
            Location = new Point(colWidth + 10, 100),
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
            Location = new Point(colWidth + 10, 150),
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
        _lstSparePlayers.DoubleClick += (_, _) => RemoveFromSpare();

        _lblSpareCount = new Label
        {
            Location = new Point(col2Left, 464),
            Size = new Size(colWidth, 20),
            Font = AppTheme.FontSmall,
            ForeColor = AppTheme.TextMuted,
            Text = "0 players"
        };

        mainPanel.Controls.AddRange([lblSpare, searchHintSpare, _txtSearchSpare, _lstSparePlayers, _lblSpareCount]);

        // Notes Panel (Bottom)
        _notesPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 150,
            BackColor = AppTheme.Surface,
            Padding = new Padding(12)
        };

        var lblNotesTitle = new Label
        {
            Text = "Notes",
            Dock = DockStyle.Top,
            Height = 24,
            Font = AppTheme.FontDefaultBold,
            ForeColor = AppTheme.TextPrimary
        };

        _lblSelectedPlayer = new Label
        {
            Text = "(Select a spare player to add notes)",
            Dock = DockStyle.Top,
            Height = 18,
            Font = AppTheme.FontSmall,
            ForeColor = AppTheme.TextMuted,
            Padding = new Padding(0, 2, 0, 4)
        };

        _txtNotes = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.FontDefault,
            Multiline = true,
            WordWrap = true,
            Enabled = false,
            BackColor = AppTheme.ContentBackground,
            ForeColor = AppTheme.TextPrimary
        };
        _txtNotes.TextChanged += (_, _) => SaveSelectedPlayerNotes();

        _notesPanel.Controls.AddRange([_txtNotes, _lblSelectedPlayer, lblNotesTitle]);
        Controls.Add(_notesPanel);

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

            ApplyAvailableFilter();
            ApplySpareFilter();
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
            .Select(p => new PlayerItem { Id = p.Id, DisplayName = p.DisplayName })
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
            {
                _lstSparePlayers.ClearSelected();
                ClearNotes();
            }
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
            {
                _lstAvailablePlayers.ClearSelected();
                LoadSelectedSparePlayerNotes();
            }
            else
            {
                ClearNotes();
            }
        }
        finally
        {
            _isSwitchingSelection = false;
        }
    }

    private void LoadSelectedSparePlayerNotes()
    {
        if (_lstSparePlayers.SelectedItem is not PlayerItem item) return;

        try
        {
            using var db = new BocceDbContext();
            var leagueId = AppParameterService.GetDefaultLeagueId(db);
            if (!leagueId.HasValue) return;

            var sparePlayer = db.SpareLists
                .Where(s => s.LeagueId == leagueId.Value && s.PlayerId == item.Id && s.IsActive)
                .Include(s => s.Player)
                .FirstOrDefault();

            if (sparePlayer != null)
            {
                _selectedSparePlayerId = sparePlayer.Id;
                _lblSelectedPlayer.Text = $"Notes for {item.DisplayName}:";
                _txtNotes.Text = sparePlayer.Notes ?? "";
                _txtNotes.Enabled = true;
            }
            else
            {
                ClearNotes();
            }
        }
        catch
        {
            ClearNotes();
        }
    }

    private void SaveSelectedPlayerNotes()
    {
        if (!_selectedSparePlayerId.HasValue) return;

        try
        {
            using var db = new BocceDbContext();
            var sparePlayer = db.SpareLists.FirstOrDefault(s => s.Id == _selectedSparePlayerId.Value);
            if (sparePlayer != null)
            {
                sparePlayer.Notes = string.IsNullOrWhiteSpace(_txtNotes.Text) ? null : _txtNotes.Text;
                db.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving notes: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ClearNotes()
    {
        _selectedSparePlayerId = null;
        _lblSelectedPlayer.Text = "(Select a spare player to add notes)";
        _txtNotes.Text = "";
        _txtNotes.Enabled = false;
    }

    private void MoveToSpare()
    {
        var selected = _lstAvailablePlayers.SelectedItems
            .Cast<PlayerItem>()
            .Select(p => p.Id)
            .ToList();

        if (selected.Count == 0) return;

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
                _currentSparePlayerIds.Add(playerId);

            ClearNotes();
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
                {
                    db.SpareLists.Remove(existing);
                }
            }

            db.SaveChanges();

            foreach (var playerId in selected)
                _currentSparePlayerIds.Remove(playerId);

            ClearNotes();
            ApplyAvailableFilter();
            ApplySpareFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error removing from spare list: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

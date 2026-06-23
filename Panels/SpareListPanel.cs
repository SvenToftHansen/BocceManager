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
    private List<PlayerLookup> _allPlayers = [];
    private HashSet<int> _currentSparePlayerIds = [];
    private int? _selectedLeagueId;

    private ComboBox _cmbLeague = null!;
    private SearchBoxControl _txtSearchAvailable = null!;
    private ListBox _lstAvailablePlayers = null!;
    private SearchBoxControl _txtSearchSpare = null!;
    private ListBox _lstSparePlayers = null!;
    private Button _btnMoveToSpare = null!;
    private Button _btnRemoveFromSpare = null!;
    private Label _lblAvailableCount = null!;
    private Label _lblSpareCount = null!;

    public SpareListPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;
        BuildUi();
        LoadLeagues();
        LoadData();
    }

    private void BuildUi()
    {
        var headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 80,
            BackColor = AppTheme.Surface,
            Padding = new Padding(12, 8, 12, 8)
        };

        var title = new Label
        {
            Text = "Spare List Management",
            Location = new Point(0, 0),
            Size = new Size(400, 28),
            Font = AppTheme.FontSectionHeading,
            ForeColor = AppTheme.TextPrimary
        };

        var lblLeague = new Label
        {
            Text = "League:",
            Location = new Point(0, 32),
            Size = new Size(80, 24),
            Font = AppTheme.FontDefault,
            ForeColor = AppTheme.TextPrimary,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _cmbLeague = new ComboBox
        {
            Location = new Point(90, 32),
            Size = new Size(300, 24),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = AppTheme.FontDefault
        };
        _cmbLeague.SelectedIndexChanged += (_, _) => OnLeagueSelected();

        headerPanel.Controls.AddRange([title, lblLeague, _cmbLeague]);
        Controls.Add(headerPanel);

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
            Text = "➜ Add",
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
            Text = "➜ Remove",
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

        Controls.Add(mainPanel);
    }

    private void LoadLeagues()
    {
        try
        {
            using var db = new BocceDbContext();
            var leagues = db.Leagues
                .Where(l => l.IsActive)
                .OrderBy(l => l.Name)
                .Select(l => new { l.Id, l.Name })
                .ToList();

            _cmbLeague.DataSource = leagues;
            _cmbLeague.DisplayMember = "Name";
            _cmbLeague.ValueMember = "Id";

            if (_cmbLeague.Items.Count > 0)
                _cmbLeague.SelectedIndex = 0;
        }
        catch { }
    }

    private void OnLeagueSelected()
    {
        if (_cmbLeague.SelectedValue is int leagueId)
        {
            _selectedLeagueId = leagueId;
            LoadData();
        }
    }

    private void LoadData()
    {
        if (_isLoadingData) return;
        if (!_selectedLeagueId.HasValue) return;

        _isLoadingData = true;
        try
        {
            using var db = new BocceDbContext();

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
                .Where(s => s.LeagueId == _selectedLeagueId.Value && s.IsActive)
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

    private void MoveToSpare()
    {
        if (!_selectedLeagueId.HasValue) return;

        var selected = _lstAvailablePlayers.SelectedItems
            .Cast<PlayerItem>()
            .Select(p => p.Id)
            .ToList();

        if (selected.Count == 0) return;

        try
        {
            using var db = new BocceDbContext();

            foreach (var playerId in selected)
            {
                // Check if already exists (shouldn't, but be safe)
                var existing = db.SpareLists.FirstOrDefault(s =>
                    s.LeagueId == _selectedLeagueId.Value &&
                    s.PlayerId == playerId);

                if (existing == null)
                {
                    db.SpareLists.Add(new SpareList
                    {
                        LeagueId = _selectedLeagueId.Value,
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
        if (!_selectedLeagueId.HasValue) return;

        var selected = _lstSparePlayers.SelectedItems
            .Cast<PlayerItem>()
            .Select(p => p.Id)
            .ToList();

        if (selected.Count == 0) return;

        try
        {
            using var db = new BocceDbContext();

            foreach (var playerId in selected)
            {
                var existing = db.SpareLists.FirstOrDefault(s =>
                    s.LeagueId == _selectedLeagueId.Value &&
                    s.PlayerId == playerId);

                if (existing != null)
                {
                    db.SpareLists.Remove(existing);
                }
            }

            db.SaveChanges();

            foreach (var playerId in selected)
                _currentSparePlayerIds.Remove(playerId);

            ApplyAvailableFilter();
            ApplySpareFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error removing from spare list: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

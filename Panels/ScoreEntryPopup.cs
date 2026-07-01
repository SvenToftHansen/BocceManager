using BocceManager.Data;
using BocceManager.Services;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Panels;

/// <summary>
/// Popup for entering playoff match scores: Game 1, Game 2, and an optional
/// tiebreaker when each team wins one game. Aggregate on bracket =
/// G1 score + G2 score + tiebreaker point (0 or 1).
/// </summary>
public class ScoreEntryPopup : Form
{
    private readonly int _matchId;

    // Game score inputs
    private NumericUpDown _numG1T1 = null!;
    private NumericUpDown _numG1T2 = null!;
    private NumericUpDown _numG2T1 = null!;
    private NumericUpDown _numG2T2 = null!;

    // Tiebreaker
    private Panel       _pnlTiebreaker = null!;
    private Label       _lblTieStatus  = null!;
    private RadioButton _rbTb1         = null!;
    private RadioButton _rbTb2         = null!;

    private Label _lblStatus = null!;

    public ScoreEntryPopup(int matchId)
    {
        _matchId        = matchId;
        Text            = "Enter Score";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        StartPosition   = FormStartPosition.CenterParent;
        Size            = new Size(420, 360);
        BackColor       = AppTheme.ContentBackground;
        Font            = AppTheme.FontDefault;

        BuildUi();
        LoadMatchData();
    }

    private void BuildUi()
    {
        int col0 = 20;   // row label x
        int col1 = 150;  // team 1 column x
        int col2 = 270;  // team 2 column x
        int rowH = 36;

        // ── Team name headers ─────────────────────────────────────────────────
        int y = 18;
        Controls.Add(new Label { Location = new Point(col1, y), Size = new Size(110, 22),
            Font = AppTheme.FontDefaultBold, ForeColor = AppTheme.TextPrimary, Text = "—" });
        Controls.Add(new Label { Location = new Point(col2, y), Size = new Size(110, 22),
            Font = AppTheme.FontDefaultBold, ForeColor = AppTheme.TextPrimary, Text = "—" });

        // Store refs so LoadMatchData can fill them in
        _teamLabel1 = (Label)Controls[Controls.Count - 2];
        _teamLabel2 = (Label)Controls[Controls.Count - 1];

        // ── Game 1 ────────────────────────────────────────────────────────────
        y += rowH;
        Controls.Add(new Label { Location = new Point(col0, y + 4), AutoSize = true,
            Font = AppTheme.FontDefault, ForeColor = AppTheme.TextSecondary, Text = "Game 1" });
        _numG1T1 = MakeScore(col1, y); Controls.Add(_numG1T1);
        _numG1T2 = MakeScore(col2, y); Controls.Add(_numG1T2);
        _numG1T1.ValueChanged += (_, _) => UpdateTiebreaker();
        _numG1T2.ValueChanged += (_, _) => UpdateTiebreaker();

        // ── Game 2 ────────────────────────────────────────────────────────────
        y += rowH;
        Controls.Add(new Label { Location = new Point(col0, y + 4), AutoSize = true,
            Font = AppTheme.FontDefault, ForeColor = AppTheme.TextSecondary, Text = "Game 2" });
        _numG2T1 = MakeScore(col1, y); Controls.Add(_numG2T1);
        _numG2T2 = MakeScore(col2, y); Controls.Add(_numG2T2);
        _numG2T1.ValueChanged += (_, _) => UpdateTiebreaker();
        _numG2T2.ValueChanged += (_, _) => UpdateTiebreaker();

        // ── Separator ─────────────────────────────────────────────────────────
        y += rowH + 8;
        Controls.Add(new Panel { Location = new Point(col0, y), Size = new Size(370, 1),
            BackColor = AppTheme.Separator });
        y += 8;

        // ── Tiebreaker section ────────────────────────────────────────────────
        _pnlTiebreaker = new Panel
        {
            Location = new Point(col0, y), Size = new Size(370, 80),
            BackColor = AppTheme.ContentBackground,
        };

        _lblTieStatus = new Label
        {
            Location = new Point(0, 0), Size = new Size(370, 22),
            Font = AppTheme.FontSmall, ForeColor = AppTheme.TextMuted,
            Text = "Tiebreaker: not needed",
        };

        _rbTb1 = new RadioButton
        {
            Location = new Point(0, 26), Size = new Size(180, 24),
            Font = AppTheme.FontDefault, ForeColor = AppTheme.TextPrimary,
            Text = "Team 1 wins", Enabled = false,
        };
        _rbTb2 = new RadioButton
        {
            Location = new Point(190, 26), Size = new Size(170, 24),
            Font = AppTheme.FontDefault, ForeColor = AppTheme.TextPrimary,
            Text = "Team 2 wins", Enabled = false,
        };

        _pnlTiebreaker.Controls.AddRange([_lblTieStatus, _rbTb1, _rbTb2]);
        Controls.Add(_pnlTiebreaker);
        y += 90;

        // ── Status + buttons ──────────────────────────────────────────────────
        _lblStatus = new Label
        {
            Location = new Point(col0, y), Size = new Size(370, 22),
            Font = AppTheme.FontSmall, ForeColor = AppTheme.TextMuted,
        };
        Controls.Add(_lblStatus);
        y += 30;

        var btnSave = new Button
        {
            Text = "Save", Location = new Point(col0, y),
            Size = new Size(100, 34), Font = AppTheme.FontDefault,
            BackColor = AppTheme.ButtonSuccess, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += OnSave;
        Controls.Add(btnSave);

        var btnCancel = new Button
        {
            Text = "Cancel", Location = new Point(col0 + 114, y),
            Size = new Size(90, 34), Font = AppTheme.FontDefault,
            BackColor = AppTheme.ButtonDanger, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
            DialogResult = DialogResult.Cancel,
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        Controls.Add(btnCancel);
    }

    // Stored refs to header labels (set in BuildUi via Controls index)
    private Label _teamLabel1 = null!;
    private Label _teamLabel2 = null!;

    private void LoadMatchData()
    {
        try
        {
            using var db = new BocceDbContext();
            var match    = db.PlayoffMatches
                .Include(m => m.Team1).Include(m => m.Team2)
                .Include(m => m.PlayoffRound)
                .FirstOrDefault(m => m.Id == _matchId);

            if (match == null) { _lblStatus.Text = "Match not found."; return; }

            string t1name = match.Team1?.EffectiveDisplayName ?? "Team 1";
            string t2name = match.Team2?.EffectiveDisplayName ?? "Team 2";
            _teamLabel1.Text = t1name;
            _teamLabel2.Text = t2name;
            _rbTb1.Text = $"{t1name} wins";
            _rbTb2.Text = $"{t2name} wins";

            if (match.PlayoffRound != null)
                Text = $"Score — {match.PlayoffRound.RoundName}";

            // Load tiebreaker ball count for note
            var config = db.PlayoffConfigs.FirstOrDefault(c => c.SeasonId == match.SeasonId);
            if (config != null)
                _lblTieStatus.Text = $"Tiebreaker: {config.TiebreakerBalls} ball(s) — select winner";

            // Restore existing scores
            var games = db.PlayoffGames
                .Where(g => g.PlayoffMatchId == _matchId)
                .OrderBy(g => g.GameNumber)
                .ToList();

            var g1 = games.FirstOrDefault(g => g.GameNumber == 1);
            var g2 = games.FirstOrDefault(g => g.GameNumber == 2);
            var g3 = games.FirstOrDefault(g => g.GameNumber == 3);

            if (g1 != null) { _numG1T1.Value = g1.Team1Score; _numG1T2.Value = g1.Team2Score; }
            if (g2 != null) { _numG2T1.Value = g2.Team1Score; _numG2T2.Value = g2.Team2Score; }
            if (g3 != null)
            {
                _rbTb1.Checked = g3.Team1Score > 0;
                _rbTb2.Checked = g3.Team2Score > 0;
            }

            UpdateTiebreaker();
        }
        catch (Exception ex)
        {
            _lblStatus.Text = $"Error loading: {ex.Message}";
        }
    }

    private void UpdateTiebreaker()
    {
        int t1g1 = (int)_numG1T1.Value, t2g1 = (int)_numG1T2.Value;
        int t1g2 = (int)_numG2T1.Value, t2g2 = (int)_numG2T2.Value;

        // Only evaluate when at least one game has been entered
        bool g1entered = t1g1 > 0 || t2g1 > 0;
        bool g2entered = t1g2 > 0 || t2g2 > 0;

        if (!g1entered || !g2entered)
        {
            _rbTb1.Enabled = false;
            _rbTb2.Enabled = false;
            return;
        }

        int t1wins = (t1g1 > t2g1 ? 1 : 0) + (t1g2 > t2g2 ? 1 : 0);
        int t2wins = (t2g1 > t1g1 ? 1 : 0) + (t2g2 > t1g2 ? 1 : 0);
        bool tied  = t1wins == t2wins;

        _rbTb1.Enabled = tied;
        _rbTb2.Enabled = tied;
        if (!tied)
        {
            _rbTb1.Checked = false;
            _rbTb2.Checked = false;
            _lblTieStatus.Text = t1wins > t2wins
                ? $"{_teamLabel1.Text} leads 2–0 — no tiebreaker"
                : $"{_teamLabel2.Text} leads 2–0 — no tiebreaker";
            _lblTieStatus.ForeColor = AppTheme.TextMuted;
        }
        else
        {
            _lblTieStatus.Text      = "Teams tied 1–1 — tiebreaker required";
            _lblTieStatus.ForeColor = Color.DarkOrange;
        }
    }

    private void OnSave(object? sender, EventArgs e)
    {
        try
        {
            int? tbWinner = null;
            if (_rbTb1.Checked) tbWinner = 1;
            if (_rbTb2.Checked) tbWinner = 2;

            // Validate: if tied, tiebreaker must be selected
            int t1g1 = (int)_numG1T1.Value, t2g1 = (int)_numG1T2.Value;
            int t1g2 = (int)_numG2T1.Value, t2g2 = (int)_numG2T2.Value;
            int t1wins = (t1g1 > t2g1 ? 1 : 0) + (t1g2 > t2g2 ? 1 : 0);
            int t2wins = (t2g1 > t1g1 ? 1 : 0) + (t2g2 > t1g2 ? 1 : 0);

            if (t1wins == t2wins && tbWinner == null)
            {
                _lblStatus.Text      = "Teams are tied — select a tiebreaker winner.";
                _lblStatus.ForeColor = Color.DarkRed;
                return;
            }

            using var db = new BocceDbContext();
            PlayoffService.SaveMatchScore(db, _matchId, t1g1, t2g1, t1g2, t2g2, tbWinner);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _lblStatus.Text      = $"Error: {ex.Message}";
            _lblStatus.ForeColor = AppTheme.TextMuted;
            AppLogger.Error(ex, "ScoreEntryPopup.OnSave matchId={MatchId}", _matchId);
        }
    }

    private static NumericUpDown MakeScore(int x, int y) => new()
    {
        Location = new Point(x, y), Size = new Size(80, 28),
        Minimum = 0, Maximum = 99, Value = 0,
        Font = AppTheme.FontDefault, TextAlign = HorizontalAlignment.Center,
    };
}

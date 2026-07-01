using BocceManager.Data;
using BocceManager.Services;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Panels;

/// <summary>
/// Small popup for entering playoff match scores.
/// Shows both team names with a score field each and a Save button.
/// </summary>
public class ScoreEntryPopup : Form
{
    private readonly int _matchId;

    private Label         _lblTeam1  = null!;
    private Label         _lblTeam2  = null!;
    private NumericUpDown _numScore1 = null!;
    private NumericUpDown _numScore2 = null!;
    private Label         _lblStatus = null!;

    public ScoreEntryPopup(int matchId)
    {
        _matchId = matchId;

        Text            = "Enter Score";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        StartPosition   = FormStartPosition.CenterParent;
        Size            = new Size(320, 230);
        BackColor       = AppTheme.ContentBackground;
        Font            = AppTheme.FontDefault;

        BuildUi();
        LoadMatchData();
    }

    private void BuildUi()
    {
        _lblTeam1 = new Label
        {
            Location = new Point(20, 20), Size = new Size(180, 24),
            Font = AppTheme.FontDefaultBold, ForeColor = AppTheme.TextPrimary, Text = "Team 1",
        };

        _numScore1 = new NumericUpDown
        {
            Location = new Point(210, 18), Size = new Size(70, 28),
            Minimum = 0, Maximum = 99, Value = 0,
            Font = AppTheme.FontDefault, TextAlign = HorizontalAlignment.Center,
        };

        _lblTeam2 = new Label
        {
            Location = new Point(20, 60), Size = new Size(180, 24),
            Font = AppTheme.FontDefaultBold, ForeColor = AppTheme.TextPrimary, Text = "Team 2",
        };

        _numScore2 = new NumericUpDown
        {
            Location = new Point(210, 58), Size = new Size(70, 28),
            Minimum = 0, Maximum = 99, Value = 0,
            Font = AppTheme.FontDefault, TextAlign = HorizontalAlignment.Center,
        };

        var btnSave = new Button
        {
            Text = "Save", Location = new Point(20, 110),
            Size = new Size(100, 34), Font = AppTheme.FontDefault,
            BackColor = AppTheme.ButtonSuccess, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
            DialogResult = DialogResult.None,
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += OnSave;

        var btnCancel = new Button
        {
            Text = "Cancel", Location = new Point(132, 110),
            Size = new Size(80, 34), Font = AppTheme.FontDefault,
            BackColor = AppTheme.ButtonDanger, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
            DialogResult = DialogResult.Cancel,
        };
        btnCancel.FlatAppearance.BorderSize = 0;

        _lblStatus = new Label
        {
            Location = new Point(20, 155), Size = new Size(270, 24),
            Font = AppTheme.FontSmall, ForeColor = AppTheme.TextMuted,
        };

        Controls.AddRange([_lblTeam1, _numScore1, _lblTeam2, _numScore2,
                           btnSave, btnCancel, _lblStatus]);
    }

    private void LoadMatchData()
    {
        try
        {
            using var db  = new BocceDbContext();
            var match     = db.PlayoffMatches
                .Include(m => m.Team1)
                .Include(m => m.Team2)
                .Include(m => m.PlayoffRound)
                .FirstOrDefault(m => m.Id == _matchId);

            if (match == null) { _lblStatus.Text = "Match not found."; return; }

            _lblTeam1.Text = match.Team1?.EffectiveDisplayName ?? "TBD";
            _lblTeam2.Text = match.Team2?.EffectiveDisplayName ?? "TBD";

            // Load existing scores if any
            var games = db.PlayoffGames.Where(g => g.PlayoffMatchId == _matchId).ToList();
            if (games.Count > 0)
            {
                _numScore1.Value = games.Sum(g => g.Team1Score);
                _numScore2.Value = games.Sum(g => g.Team2Score);
            }

            if (match.PlayoffRound != null)
                Text = $"Score — {match.PlayoffRound.RoundName}";
        }
        catch (Exception ex)
        {
            _lblStatus.Text = $"Error loading: {ex.Message}";
        }
    }

    private void OnSave(object? sender, EventArgs e)
    {
        try
        {
            using var db = new BocceDbContext();
            PlayoffService.SaveMatchScore(db, _matchId,
                (int)_numScore1.Value, (int)_numScore2.Value);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _lblStatus.Text = $"Error: {ex.Message}";
            AppLogger.Error(ex, "ScoreEntryPopup.OnSave matchId={MatchId}", _matchId);
        }
    }
}

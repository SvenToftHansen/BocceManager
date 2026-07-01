using BocceManager.Data;
using BocceManager.Services;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Panels;

/// <summary>
/// Playoff match score popup. Text-box entry with auto-jump and auto-12-fill,
/// matching the behaviour of the regular Score Entry panel.
/// Box order: G1-Team1 → G1-Team2 → G2-Team1 → G2-Team2
/// </summary>
public class ScoreEntryPopup : Form
{
    private readonly int _matchId;

    // 0=G1T1  1=G1T2  2=G2T1  3=G2T2
    private readonly TextBox[]     _boxes    = new TextBox[4];
    private readonly List<TextBox> _boxOrder = [];

    // Tiebreaker
    private Panel       _pnlTb     = null!;
    private Label       _lblTbInfo = null!;
    private RadioButton _rbTb1     = null!;
    private RadioButton _rbTb2     = null!;

    private Label _lblTeam1  = null!;
    private Label _lblTeam2  = null!;
    private Label _lblStatus = null!;

    private static readonly Font  s_numFont  = new("Segoe UI", 11f, FontStyle.Bold);
    private static readonly Color s_validGn  = Color.FromArgb(198, 239, 206);
    private static readonly Color s_invalidRd = Color.FromArgb(255, 199, 206);

    // Column positions
    private const int ColLabel = 20;
    private const int ColT1    = 155;
    private const int ColT2    = 255;
    private const int BoxW     = 80;
    private const int BoxH     = 32;

    public ScoreEntryPopup(int matchId)
    {
        _matchId        = matchId;
        Text            = "Enter Score";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        StartPosition   = FormStartPosition.CenterParent;
        Size            = new Size(370, 330);
        BackColor       = AppTheme.ContentBackground;
        Font            = AppTheme.FontDefault;

        BuildUi();
        LoadMatchData();
    }

    // ── UI ────────────────────────────────────────────────────────────────────

    private void BuildUi()
    {
        int y = 16;

        // ── Team name headers ─────────────────────────────────────────────────
        _lblTeam1 = MakeLbl("Team 1", ColT1, y, BoxW, 22, AppTheme.FontDefaultBold);
        _lblTeam2 = MakeLbl("Team 2", ColT2, y, BoxW, 22, AppTheme.FontDefaultBold);
        Controls.Add(_lblTeam1);
        Controls.Add(_lblTeam2);
        y += 30;

        // ── Game 1 ────────────────────────────────────────────────────────────
        Controls.Add(MakeLbl("Game 1", ColLabel, y + 6, 130, 22, AppTheme.FontDefault));
        _boxes[0] = MakeBox(ColT1, y);
        _boxes[1] = MakeBox(ColT2, y);
        Controls.Add(_boxes[0]);
        Controls.Add(_boxes[1]);
        y += BoxH + 8;

        // ── Game 2 ────────────────────────────────────────────────────────────
        Controls.Add(MakeLbl("Game 2", ColLabel, y + 6, 130, 22, AppTheme.FontDefault));
        _boxes[2] = MakeBox(ColT1, y);
        _boxes[3] = MakeBox(ColT2, y);
        Controls.Add(_boxes[2]);
        Controls.Add(_boxes[3]);
        y += BoxH + 10;

        // Box navigation order
        _boxOrder.AddRange(_boxes);

        // ── Wire pairs ────────────────────────────────────────────────────────
        WirePair(_boxes[0], _boxes[1]); // Game 1
        WirePair(_boxes[2], _boxes[3]); // Game 2

        // ── Separator ─────────────────────────────────────────────────────────
        Controls.Add(new Panel
        {
            Location  = new Point(ColLabel, y),
            Size      = new Size(320, 1),
            BackColor = AppTheme.Separator
        });
        y += 8;

        // ── Tiebreaker panel ──────────────────────────────────────────────────
        _pnlTb = new Panel
        {
            Location  = new Point(ColLabel, y),
            Size      = new Size(320, 62),
            BackColor = AppTheme.ContentBackground
        };

        _lblTbInfo = new Label
        {
            Location  = new Point(0, 0), Size = new Size(320, 20),
            Font      = AppTheme.FontSmall, ForeColor = AppTheme.TextMuted,
            Text      = "Tiebreaker: not needed"
        };
        _rbTb1 = new RadioButton
        {
            Location = new Point(0, 24), Size = new Size(155, 24),
            Font = AppTheme.FontDefault, ForeColor = AppTheme.TextPrimary,
            Text = "Team 1 wins", Enabled = false
        };
        _rbTb2 = new RadioButton
        {
            Location = new Point(160, 24), Size = new Size(155, 24),
            Font = AppTheme.FontDefault, ForeColor = AppTheme.TextPrimary,
            Text = "Team 2 wins", Enabled = false
        };

        _pnlTb.Controls.AddRange([_lblTbInfo, _rbTb1, _rbTb2]);
        Controls.Add(_pnlTb);
        y += _pnlTb.Height + 6;

        // ── Status + buttons ──────────────────────────────────────────────────
        _lblStatus = new Label
        {
            Location = new Point(ColLabel, y), Size = new Size(320, 20),
            Font = AppTheme.FontSmall, ForeColor = AppTheme.TextMuted
        };
        Controls.Add(_lblStatus);
        y += 28;

        var btnSave = new Button
        {
            Text = "Save", Location = new Point(ColLabel, y),
            Size = new Size(100, 32), Font = AppTheme.FontDefault,
            BackColor = AppTheme.ButtonSuccess, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += OnSave;
        Controls.Add(btnSave);

        var btnCancel = new Button
        {
            Text = "Cancel", Location = new Point(ColLabel + 110, y),
            Size = new Size(90, 32), Font = AppTheme.FontDefault,
            BackColor = AppTheme.ButtonDanger, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
            DialogResult = DialogResult.Cancel
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        Controls.Add(btnCancel);
    }

    // ── Score box wiring (mirrors ScoreEntryPanel.WirePair) ──────────────────

    private void WirePair(TextBox b1, TextBox b2)
    {
        b1.TextChanged += (_, _) => { ColorPair(b1, b2); AutoAdvance(b1); UpdateTiebreaker(); };
        b2.TextChanged += (_, _) => { ColorPair(b1, b2); AutoAdvance(b2); UpdateTiebreaker(); };

        b1.GotFocus += (_, _) => b1.SelectAll();
        b2.GotFocus += (_, _) => b2.SelectAll();

        b1.KeyPress += (_, e) => ScoreKeyPress(b1, e);
        b2.KeyPress += (_, e) => ScoreKeyPress(b2, e);

        b1.KeyDown += (_, e) => NavigateBox(b1, e);
        b2.KeyDown += (_, e) => NavigateBox(b2, e);

        // When focus leaves b1 and b2 is still empty, auto-fill b2 with the
        // winning score (12) — scorer enters the losing score, 12 fills in.
        b1.Leave += (_, _) =>
        {
            if (int.TryParse(b1.Text.Trim(), out int v) && v is >= 0 and <= 11
                && string.IsNullOrWhiteSpace(b2.Text))
                b2.Text = "12";
        };
    }

    // Auto-advance to next box when entry is unambiguously complete.
    // '1' waits (could start 10 or 11); all other single digits advance immediately.
    private void AutoAdvance(TextBox tb)
    {
        if (!tb.Focused) return;
        string txt = tb.Text;
        bool advance = txt.Length == 2 || (txt.Length == 1 && txt[0] != '1');
        if (!advance) return;
        int idx = _boxOrder.IndexOf(tb);
        if (idx >= 0 && idx + 1 < _boxOrder.Count)
            BeginInvoke(() => _boxOrder[idx + 1].Focus());
    }

    // Digits only, 0-12, no forfeits.
    private void ScoreKeyPress(TextBox tb, KeyPressEventArgs e)
    {
        if (char.IsControl(e.KeyChar)) return;
        if (!char.IsDigit(e.KeyChar)) { e.Handled = true; return; }

        var proposed = tb.Text
            .Remove(tb.SelectionStart, tb.SelectionLength)
            .Insert(tb.SelectionStart, e.KeyChar.ToString());

        if (!int.TryParse(proposed, out int v) || v < 0 || v > 12)
        {
            e.Handled = true;
            return;
        }

        // If proposed == current text (e.g. typing '0' again), TextChanged won't fire.
        if (proposed == tb.Text && proposed.Length == 1 && proposed[0] != '1')
        {
            e.Handled = true;
            int idx = _boxOrder.IndexOf(tb);
            if (idx >= 0 && idx + 1 < _boxOrder.Count)
                BeginInvoke(() => _boxOrder[idx + 1].Focus());
        }
    }

    private void NavigateBox(TextBox tb, KeyEventArgs e)
    {
        int idx = _boxOrder.IndexOf(tb);
        if (idx < 0) return;
        int next = e.KeyCode switch
        {
            Keys.Enter or Keys.Right or Keys.Tab => idx + 1,
            Keys.Left                             => idx - 1,
            _                                     => -1
        };
        if (next < 0 || next >= _boxOrder.Count) return;
        e.SuppressKeyPress = true;
        _boxOrder[next].Focus();
    }

    private static void ColorPair(TextBox b1, TextBox b2)
    {
        if (string.IsNullOrWhiteSpace(b1.Text) || string.IsNullOrWhiteSpace(b2.Text))
        {
            b1.BackColor = b2.BackColor = Color.White;
            return;
        }
        if (!int.TryParse(b1.Text.Trim(), out int a) || !int.TryParse(b2.Text.Trim(), out int b))
        {
            b1.BackColor = b2.BackColor = Color.White;
            return;
        }
        bool valid = (a == 12 && b is >= 0 and <= 11) || (b == 12 && a is >= 0 and <= 11);
        Color c = valid ? s_validGn : s_invalidRd;
        b1.BackColor = b2.BackColor = c;
    }

    // ── Tiebreaker dynamic state ──────────────────────────────────────────────

    private void UpdateTiebreaker()
    {
        int? t1g1 = ParseBox(_boxes[0]), t2g1 = ParseBox(_boxes[1]);
        int? t1g2 = ParseBox(_boxes[2]), t2g2 = ParseBox(_boxes[3]);

        if (t1g1 == null || t2g1 == null || t1g2 == null || t2g2 == null)
        {
            _rbTb1.Enabled = _rbTb2.Enabled = false;
            return;
        }

        int t1wins = (t1g1 > t2g1 ? 1 : 0) + (t1g2 > t2g2 ? 1 : 0);
        int t2wins = (t2g1 > t1g1 ? 1 : 0) + (t2g2 > t1g2 ? 1 : 0);
        bool tied  = t1wins == t2wins;

        _rbTb1.Enabled = _rbTb2.Enabled = tied;
        if (!tied) { _rbTb1.Checked = _rbTb2.Checked = false; }

        _lblTbInfo.Text      = tied ? "Tied 1–1 — select tiebreaker winner" : "No tiebreaker needed";
        _lblTbInfo.ForeColor = tied ? Color.DarkOrange : AppTheme.TextMuted;
    }

    // ── Data load ─────────────────────────────────────────────────────────────

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

            string t1 = match.Team1?.EffectiveDisplayName ?? "Team 1";
            string t2 = match.Team2?.EffectiveDisplayName ?? "Team 2";
            _lblTeam1.Text = t1;
            _lblTeam2.Text = t2;
            _rbTb1.Text    = $"{t1} wins";
            _rbTb2.Text    = $"{t2} wins";

            if (match.PlayoffRound != null)
                Text = $"Score — {match.PlayoffRound.RoundName}";

            var config = db.PlayoffConfigs.FirstOrDefault(c => c.SeasonId == match.SeasonId);
            if (config != null)
                _lblTbInfo.Text = $"Tiebreaker: {config.TiebreakerBalls} ball(s) — select winner below";

            // Restore existing scores
            var games = db.PlayoffGames
                .Where(g => g.PlayoffMatchId == _matchId)
                .OrderBy(g => g.GameNumber)
                .ToList();

            var g1 = games.FirstOrDefault(g => g.GameNumber == 1);
            var g2 = games.FirstOrDefault(g => g.GameNumber == 2);
            var g3 = games.FirstOrDefault(g => g.GameNumber == 3);

            if (g1 != null)
            {
                _boxes[0].Text = g1.Team1Score > 0 ? g1.Team1Score.ToString() : "";
                _boxes[1].Text = g1.Team2Score > 0 ? g1.Team2Score.ToString() : "";
            }
            if (g2 != null)
            {
                _boxes[2].Text = g2.Team1Score > 0 ? g2.Team1Score.ToString() : "";
                _boxes[3].Text = g2.Team2Score > 0 ? g2.Team2Score.ToString() : "";
            }
            if (g3 != null)
            {
                _rbTb1.Checked = g3.Team1Score > 0;
                _rbTb2.Checked = g3.Team2Score > 0;
            }

            UpdateTiebreaker();

            // Focus first empty box
            var first = _boxOrder.FirstOrDefault(b => string.IsNullOrWhiteSpace(b.Text));
            if (first != null) BeginInvoke(() => first.Focus());
        }
        catch (Exception ex) { _lblStatus.Text = $"Error loading: {ex.Message}"; }
    }

    // ── Save ──────────────────────────────────────────────────────────────────

    private void OnSave(object? sender, EventArgs e)
    {
        try
        {
            int? t1g1 = ParseBox(_boxes[0]), t2g1 = ParseBox(_boxes[1]);
            int? t1g2 = ParseBox(_boxes[2]), t2g2 = ParseBox(_boxes[3]);

            if (t1g1 == null || t2g1 == null || t1g2 == null || t2g2 == null)
            {
                _lblStatus.Text      = "Enter scores for both games before saving.";
                _lblStatus.ForeColor = Color.DarkRed;
                return;
            }

            int? tbWinner = _rbTb1.Checked ? 1 : _rbTb2.Checked ? 2 : null;

            int t1wins = (t1g1 > t2g1 ? 1 : 0) + (t1g2 > t2g2 ? 1 : 0);
            int t2wins = (t2g1 > t1g1 ? 1 : 0) + (t2g2 > t1g2 ? 1 : 0);
            if (t1wins == t2wins && tbWinner == null)
            {
                _lblStatus.Text      = "Teams tied — select a tiebreaker winner.";
                _lblStatus.ForeColor = Color.DarkRed;
                return;
            }

            using var db = new BocceDbContext();
            PlayoffService.SaveMatchScore(db, _matchId,
                t1g1.Value, t2g1.Value, t1g2.Value, t2g2.Value, tbWinner);

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

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static int? ParseBox(TextBox tb) =>
        string.IsNullOrWhiteSpace(tb.Text) ? null :
        int.TryParse(tb.Text.Trim(), out int v) ? v : null;

    private static TextBox MakeBox(int x, int y) => new()
    {
        Location    = new Point(x, y),
        Size        = new Size(BoxW, BoxH),
        Font        = s_numFont,
        TextAlign   = HorizontalAlignment.Center,
        BorderStyle = BorderStyle.FixedSingle,
        MaxLength   = 2,
        BackColor   = Color.White
    };

    private static Label MakeLbl(string text, int x, int y, int w, int h, Font font) => new()
    {
        Text      = text,
        Location  = new Point(x, y),
        Size      = new Size(w, h),
        Font      = font,
        ForeColor = AppTheme.TextPrimary,
        TextAlign = ContentAlignment.MiddleLeft
    };
}

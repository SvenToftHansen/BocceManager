using BocceManager.Data;
using BocceManager.Services;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Panels;

/// <summary>
/// Playoff match score popup. Text-box entry with auto-jump and auto-12-fill.
/// Tiebreaker section is hidden and the form compact until a tie is detected.
/// </summary>
public class ScoreEntryPopup : Form
{
    private readonly int _matchId;

    // 0=G1T1  1=G1T2  2=G2T1  3=G2T2
    private readonly TextBox[]     _boxes    = new TextBox[4];
    private readonly List<TextBox> _boxOrder = [];

    // Tiebreaker — hidden until totals are equal
    private Panel       _pnlTb      = null!;
    private Label       _lblTbInfo  = null!;
    private RadioButton _rbTb1      = null!;
    private RadioButton _rbTb2      = null!;

    // Bottom section (status + buttons) — slides down when tiebreaker appears
    private Panel  _pnlBottom = null!;
    private Label  _lblStatus = null!;
    private Button _btnReset  = null!;

    private Label _lblTeam1 = null!;
    private Label _lblTeam2 = null!;

    private static readonly Font  s_numFont   = new("Segoe UI", 11f, FontStyle.Bold);
    private static readonly Color s_validGn   = Color.FromArgb(198, 239, 206);
    private static readonly Color s_invalidRd = Color.FromArgb(255, 199, 206);

    private const int ColLabel  = 20;
    private const int ColT1     = 155;
    private const int ColT2     = 255;
    private const int BoxW      = 80;
    private const int BoxH      = 32;
    private const int TbPanelH  = 70;   // tiebreaker panel height + gap below it

    // Y of the first row after the separator — used to reposition bottom panel
    private int _bottomBaseY;

    public ScoreEntryPopup(int matchId)
    {
        _matchId        = matchId;
        Text            = "Enter Score";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        StartPosition   = FormStartPosition.CenterParent;
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

        _boxOrder.AddRange(_boxes);
        WirePair(_boxes[0], _boxes[1]);
        WirePair(_boxes[2], _boxes[3]);

        // ── Separator ─────────────────────────────────────────────────────────
        Controls.Add(new Panel
        {
            Location  = new Point(ColLabel, y),
            Size      = new Size(320, 1),
            BackColor = AppTheme.Separator
        });
        y += 8;
        _bottomBaseY = y;   // anchor: where bottom panel sits without tiebreaker

        // ── Tiebreaker panel (hidden until tie detected) ──────────────────────
        _pnlTb = new Panel
        {
            Location  = new Point(ColLabel, y),
            Size      = new Size(320, TbPanelH - 8),
            BackColor = AppTheme.ContentBackground,
            Visible   = false,
        };

        _lblTbInfo = new Label
        {
            Location  = new Point(0, 0), Size = new Size(320, 20),
            Font      = AppTheme.FontSmall, ForeColor = Color.DarkOrange,
        };
        _rbTb1 = new RadioButton
        {
            Location = new Point(0, 24), Size = new Size(155, 24),
            Font = AppTheme.FontDefault, ForeColor = AppTheme.TextPrimary,
            Text = "Team 1 wins",
        };
        _rbTb2 = new RadioButton
        {
            Location = new Point(160, 24), Size = new Size(155, 24),
            Font = AppTheme.FontDefault, ForeColor = AppTheme.TextPrimary,
            Text = "Team 2 wins",
        };
        _pnlTb.Controls.AddRange([_lblTbInfo, _rbTb1, _rbTb2]);
        Controls.Add(_pnlTb);

        // ── Bottom panel: status + buttons ────────────────────────────────────
        _pnlBottom = new Panel
        {
            Location  = new Point(ColLabel, y),
            Size      = new Size(320, 68),
            BackColor = AppTheme.ContentBackground,
        };

        _lblStatus = new Label
        {
            Location = new Point(0, 0), Size = new Size(320, 20),
            Font = AppTheme.FontSmall, ForeColor = AppTheme.TextMuted,
        };

        var btnSave = new Button
        {
            Text = "Save", Location = new Point(0, 28),
            Size = new Size(100, 32), Font = AppTheme.FontDefault,
            BackColor = AppTheme.ButtonSuccess, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += OnSave;

        var btnCancel = new Button
        {
            Text = "Cancel", Location = new Point(110, 28),
            Size = new Size(90, 32), Font = AppTheme.FontDefault,
            BackColor = AppTheme.ButtonDanger, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
            DialogResult = DialogResult.Cancel,
        };
        btnCancel.FlatAppearance.BorderSize = 0;

        _btnReset = new Button
        {
            Text = "Reset", Location = new Point(210, 28),
            Size = new Size(90, 32), Font = AppTheme.FontDefault,
            BackColor = Color.FromArgb(220, 220, 220), ForeColor = AppTheme.TextPrimary,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
            Visible = false, // shown only when a score already exists to clear
        };
        _btnReset.FlatAppearance.BorderSize = 0;
        _btnReset.Click += OnReset;

        _pnlBottom.Controls.AddRange([_lblStatus, btnSave, btnCancel, _btnReset]);
        Controls.Add(_pnlBottom);

        // Initial compact size (no tiebreaker)
        ClientSize = new Size(370, _bottomBaseY + _pnlBottom.Height + 14);
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

        b1.Leave += (_, _) =>
        {
            if (int.TryParse(b1.Text.Trim(), out int v) && v is >= 0 and <= 11
                && string.IsNullOrWhiteSpace(b2.Text))
                b2.Text = "12";
        };
    }

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

    private void ScoreKeyPress(TextBox tb, KeyPressEventArgs e)
    {
        if (char.IsControl(e.KeyChar)) return;
        if (!char.IsDigit(e.KeyChar)) { e.Handled = true; return; }

        var proposed = tb.Text
            .Remove(tb.SelectionStart, tb.SelectionLength)
            .Insert(tb.SelectionStart, e.KeyChar.ToString());

        if (!int.TryParse(proposed, out int v) || v < 0 || v > 12)
        { e.Handled = true; return; }

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
        { b1.BackColor = b2.BackColor = Color.White; return; }
        if (!int.TryParse(b1.Text.Trim(), out int a) || !int.TryParse(b2.Text.Trim(), out int b))
        { b1.BackColor = b2.BackColor = Color.White; return; }
        bool valid = (a == 12 && b is >= 0 and <= 11) || (b == 12 && a is >= 0 and <= 11);
        Color c = valid ? s_validGn : s_invalidRd;
        b1.BackColor = b2.BackColor = c;
    }

    // ── Tiebreaker — show/hide based on total points ──────────────────────────

    private void UpdateTiebreaker()
    {
        int? t1g1 = ParseBox(_boxes[0]), t2g1 = ParseBox(_boxes[1]);
        int? t1g2 = ParseBox(_boxes[2]), t2g2 = ParseBox(_boxes[3]);

        if (t1g1 == null || t2g1 == null || t1g2 == null || t2g2 == null)
        {
            SetTiebreakerVisible(false);
            return;
        }

        int t1total = t1g1.Value + t1g2.Value;
        int t2total = t2g1.Value + t2g2.Value;
        bool tied   = t1total == t2total;

        if (!tied)
        {
            SetTiebreakerVisible(false);
            string leader = t1total > t2total ? _lblTeam1.Text : _lblTeam2.Text;
            _lblStatus.Text      = $"{_lblTeam1.Text}: {t1total}  –  {_lblTeam2.Text}: {t2total}  →  {leader} wins";
            _lblStatus.ForeColor = AppTheme.TextSecondary;
        }
        else
        {
            _lblTbInfo.Text = $"Tied {t1total}–{t2total} — select tiebreaker winner";
            _rbTb1.Text     = $"{_lblTeam1.Text} wins";
            _rbTb2.Text     = $"{_lblTeam2.Text} wins";
            SetTiebreakerVisible(true);
            _lblStatus.Text = "";
        }
    }

    private void SetTiebreakerVisible(bool visible)
    {
        if (_pnlTb.Visible == visible) return;

        _pnlTb.Visible = visible;
        if (!visible) { _rbTb1.Checked = _rbTb2.Checked = false; }

        int bottomY = _bottomBaseY + (visible ? TbPanelH : 0);
        _pnlBottom.Location = new Point(ColLabel, bottomY);
        ClientSize = new Size(ClientSize.Width, bottomY + _pnlBottom.Height + 14);
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

            if (match.PlayoffRound != null)
                Text = $"Score — {match.PlayoffRound.RoundName}";

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

            _btnReset.Visible = g1 != null || g2 != null;

            UpdateTiebreaker();

            // Defer focus until the form handle exists (Shown fires after ShowDialog creates the handle)
            var first = _boxOrder.FirstOrDefault(b => string.IsNullOrWhiteSpace(b.Text));
            if (first != null)
                Shown += (_, _) => first.Focus();
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

            if (t1g1.Value + t1g2.Value == t2g1.Value + t2g2.Value && tbWinner == null)
            {
                _lblStatus.Text      = "Teams tied on total points — select a tiebreaker winner.";
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

    private void OnReset(object? sender, EventArgs e)
    {
        var confirm = MessageBox.Show(
            "This clears the score for this match. If the winner had already advanced " +
            "and later matches were played using that result, those matches will be reset too.\n\n" +
            "This cannot be undone. Continue?",
            "Reset Match Score",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);

        if (confirm != DialogResult.Yes) return;

        try
        {
            using var db = new BocceDbContext();
            PlayoffService.ResetMatchScore(db, _matchId);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _lblStatus.Text      = $"Error: {ex.Message}";
            _lblStatus.ForeColor = AppTheme.TextMuted;
            AppLogger.Error(ex, "ScoreEntryPopup.OnReset matchId={MatchId}", _matchId);
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

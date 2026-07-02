using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.Services;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Panels;

/// <summary>
/// GDI+ bracket visualization. Renders a ladder tournament bracket with
/// team names, scores, game info boxes, and connecting lines.
/// Supports ScaleToFit (scales to control bounds) and Scroll (natural size, scrollable).
/// </summary>
public class BracketVisualizationControl : UserControl
{
    // ── Layout constants (unscaled) ───────────────────────────────────────────

    private const int ColWidth    = 180;  // horizontal gap between rounds (more space between columns)
    private const int TeamBoxH    = 134;  // height of one team name row (40% taller match boxes)
    private const int ScoreBoxW   = 210;  // width of aggregate score area
    private const int TeamNameW   = 540;  // width of team name area
    private const int GameBoxH    = 56;   // court/time info box (40% taller)
    private const int MatchH      = TeamBoxH + GameBoxH + TeamBoxH;
    private const int MatchVGap   = 10;   // gap between round-1 match boxes
    private const int ByeLabelH   = 14;   // "Bye N" label above bye team
    private const int LeftPad     = 10;   // minimal left padding
    private const int TopPad      = 12;   // minimal top padding
    private const int FooterH     = 40;   // space reserved for footer note at bottom

    private const float ScoreFontSize    = 36f;   // aggregate score — large and bold
    private const float TeamNameFontSize = 38f;   // team names — bold for all teams
    private const float InfoFontSize     = 24f;   // court/time info

    // ── Data ──────────────────────────────────────────────────────────────────

    private record BracketMatch(
        int Id, int Round, int Slot,
        string? Team1Name, string? Team2Name,
        bool Team1IsBye,                   // this team had a bye in previous round
        int? ByeSeed,                      // if Team1 is a bye, show "Bye N"
        int? Team1Score, int? Team2Score,
        string? CourtName,
        DateOnly? Date, TimeOnly? Time,
        bool IsCompleted,
        int? NextMatchId, bool NextMatchIsTop);

    private List<BracketMatch> _matches = [];
    private int _totalRounds;
    private int _teamCount;
    private bool _scaleToFit = true;
    private int _tiebreakerBalls = 1;

    // Scoring interaction
    public event EventHandler<int>? MatchClicked; // passes matchId

    // ── Geometry (computed once per render) ───────────────────────────────────

    private float _scale = 1f;
    private int   _naturalW;
    private int   _naturalH;
    private Dictionary<int, (int x, int y)> _positions = new();

    // ── Public API ────────────────────────────────────────────────────────────

    public BracketVisualizationControl()
    {
        DoubleBuffered  = true;
        BackColor       = AppTheme.ContentBackground;
        AutoScroll      = true;
    }

    public new void Load(int seasonId)
    {
        using var db = new BocceDbContext();
        var season   = db.Seasons.Find(seasonId);
        if (season == null) return;

        _teamCount    = season.TeamsInPlayoffs;
        _totalRounds  = PlayoffService.GetRoundCount(_teamCount);

        var config       = db.PlayoffConfigs.FirstOrDefault(c => c.SeasonId == seasonId);
        _scaleToFit      = config?.DisplayMode != "Scroll";
        _tiebreakerBalls = config?.TiebreakerBalls ?? 1;

        var rawMatches = db.PlayoffMatches
            .Include(m => m.Team1).Include(m => m.Team2)
            .Include(m => m.Court).Include(m => m.PlayoffRound)
            .Where(m => m.SeasonId == seasonId)
            .OrderBy(m => m.PlayoffRoundId).ThenBy(m => m.BracketSlot)
            .ToList();

        // Build game score sums per match
        var gameScores = db.PlayoffGames
            .Where(g => rawMatches.Select(m => m.Id).Contains(g.PlayoffMatchId))
            .GroupBy(g => g.PlayoffMatchId)
            .ToDictionary(g => g.Key, g => (
                T1: g.Sum(x => x.Team1Score),
                T2: g.Sum(x => x.Team2Score)));

        var seedings = db.PlayoffSeedings
            .Where(s => s.SeasonId == seasonId)
            .ToDictionary(s => s.TeamId, s => s.Seed);

        _matches = rawMatches.Select(m =>
        {
            gameScores.TryGetValue(m.Id, out var sc);
            return new BracketMatch(
                Id:            m.Id,
                Round:         m.PlayoffRound?.RoundNumber ?? 0,
                Slot:          m.BracketSlot,
                Team1Name:     m.Team1?.EffectiveDisplayName,
                Team2Name:     m.Team2?.EffectiveDisplayName,
                Team1IsBye:    m.Seed2 == null && m.Team1Id.HasValue, // single-team = bye slot
                ByeSeed:       m.Team1Id.HasValue && m.Seed2 == null
                                 ? (seedings.TryGetValue(m.Team1Id.Value, out int s) ? s : (int?)null)
                                 : null,
                Team1Score:    sc.T1 > 0 || sc.T2 > 0 ? sc.T1 : null,
                Team2Score:    sc.T1 > 0 || sc.T2 > 0 ? sc.T2 : null,
                CourtName:     m.Court != null ? $"Court {m.Court.CourtNumber}" : null,
                Date:          m.ScheduledDate,
                Time:          m.ScheduledTime,
                IsCompleted:   m.Status == "completed",
                NextMatchId:   m.NextMatchId,
                NextMatchIsTop: m.NextMatchIsTop);
        }).ToList();

        _positions = ComputePositions(_matches.GroupBy(m => m.Round).OrderBy(g => g.Key).ToList());
        ComputeNaturalSize();
        UpdateScrollAndSize();
        Invalidate();
    }

    // ── Size / scale ──────────────────────────────────────────────────────────

    private void ComputeNaturalSize()
    {
        if (_totalRounds == 0 || _positions.Count == 0) { _naturalW = 400; _naturalH = 200; return; }
        // Width: leftPad + each round's box width + (rounds-1) gaps between rounds + right pad
        _naturalW = LeftPad + _totalRounds * (TeamNameW + ScoreBoxW)
                  + (_totalRounds - 1) * ColWidth + 40;
        // Height: derived from actual computed match positions (not a nominal slot count),
        // so byes don't inflate the canvas with unused space.
        _naturalH = _positions.Values.Max(p => p.y) + MatchH + FooterH;
    }

    private void UpdateScrollAndSize()
    {
        if (_scaleToFit)
        {
            AutoScroll = false;
            _scale     = Math.Min(
                (float)ClientSize.Width  / Math.Max(1, _naturalW),
                (float)ClientSize.Height / Math.Max(1, _naturalH));
            _scale = Math.Max(0.2f, _scale);
        }
        else
        {
            AutoScroll = true;
            _scale     = 1f;
            AutoScrollMinSize = new Size(_naturalW, _naturalH);
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        UpdateScrollAndSize();
        Invalidate();
    }

    // ── Painting ──────────────────────────────────────────────────────────────

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        if (!_matches.Any())
        {
            g.DrawString("No bracket generated yet. Use Playoff Setup to generate.",
                AppTheme.FontDefault, new SolidBrush(AppTheme.TextMuted),
                new PointF(40, 40));
            return;
        }

        int scrollX = _scaleToFit ? 0 : HorizontalScroll.Value;
        int scrollY = _scaleToFit ? 0 : VerticalScroll.Value;

        g.TranslateTransform(-scrollX, -scrollY);
        g.ScaleTransform(_scale, _scale);

        var byRound = _matches.GroupBy(m => m.Round).OrderBy(g2 => g2.Key).ToList();

        // Draw connecting lines first (behind boxes)
        DrawConnectors(g, byRound, _positions);

        // Draw match boxes
        foreach (var roundGroup in byRound)
            foreach (var m in roundGroup.OrderBy(x => x.Slot))
                DrawMatch(g, m, _positions[m.Id]);

        // Footer note — tiebreaker rule
        string footer = $"* Each match: aggregate of 2 games total score.  Tie occurs when both teams score the same over both games → Tiebreaker: {_tiebreakerBalls} ball(s), winner scores 1 point.";
        using var footerFont  = new Font(AppTheme.FontSmall.FontFamily, 7.5f, FontStyle.Italic);
        using var footerBrush = new SolidBrush(AppTheme.TextMuted);
        g.DrawString(footer, footerFont, footerBrush, new PointF(LeftPad, _naturalH - 24));
    }

    // ── Position computation ──────────────────────────────────────────────────

    /// <summary>
    /// Positions matches by walking the actual NextMatchId/NextMatchIsTop relationships
    /// instead of a nominal 2^(rounds-1) slot grid. Round 1 matches are packed edge-to-edge
    /// (tight — only MatchVGap between them); every later round's match is centred on its
    /// real child match(es), so bye slots don't reserve wasted vertical space.
    /// </summary>
    private Dictionary<int, (int x, int y)> ComputePositions(
        IList<IGrouping<int, BracketMatch>> byRound)
    {
        var pos = new Dictionary<int, (int, int)>();

        foreach (var roundGroup in byRound)
        {
            int round  = roundGroup.Key;
            int colIdx = round - 1;
            int x      = LeftPad + colIdx * (ColWidth + TeamNameW + ScoreBoxW);

            if (round == 1)
            {
                int i = 0;
                foreach (var m in roundGroup.OrderBy(mm => mm.Slot))
                {
                    pos[m.Id] = (x, TopPad + i * (MatchH + MatchVGap));
                    i++;
                }
                continue;
            }

            foreach (var m in roundGroup.OrderBy(mm => mm.Slot))
            {
                var children = _matches.Where(c => c.NextMatchId == m.Id).ToList();
                var topChild = children.FirstOrDefault(c => c.NextMatchIsTop);
                var botChild = children.FirstOrDefault(c => !c.NextMatchIsTop);

                int y;
                if (topChild != null && botChild != null
                    && pos.TryGetValue(topChild.Id, out var tp) && pos.TryGetValue(botChild.Id, out var bp))
                {
                    int topCenter = tp.Item2 + TeamBoxH / 2;
                    int botCenter = bp.Item2 + TeamBoxH + GameBoxH + TeamBoxH / 2;
                    y = (topCenter + botCenter) / 2 - MatchH / 2;
                }
                else if (botChild != null && pos.TryGetValue(botChild.Id, out var bp2))
                {
                    // Bye-entry round: only a bottom child feeds this match (top team is a
                    // direct bye) — align this match's bottom row to the child's centre.
                    int childCenter = bp2.Item2 + TeamBoxH + GameBoxH + TeamBoxH / 2;
                    y = childCenter - (TeamBoxH + GameBoxH + TeamBoxH / 2);
                }
                else if (topChild != null && pos.TryGetValue(topChild.Id, out var tp2))
                {
                    int childCenter = tp2.Item2 + TeamBoxH / 2;
                    y = childCenter - TeamBoxH / 2;
                }
                else
                {
                    y = TopPad;
                }

                int byeOffset = m.Team1IsBye ? ByeLabelH : 0;
                pos[m.Id] = (x, y + byeOffset);
            }
        }

        return pos;
    }

    // ── Line drawing ──────────────────────────────────────────────────────────

    private void DrawConnectors(
        Graphics g,
        IList<IGrouping<int, BracketMatch>> byRound,
        Dictionary<int, (int x, int y)> positions)
    {
        using var pen = new Pen(Color.Black, 2f);

        foreach (var roundGroup in byRound)
        {
            foreach (var m in roundGroup)
            {
                if (!m.NextMatchId.HasValue) continue;
                if (!positions.TryGetValue(m.Id, out var src)) continue;
                if (!positions.TryGetValue(m.NextMatchId.Value, out var dst)) continue;

                int srcX  = src.x + TeamNameW + ScoreBoxW;
                int srcY  = src.y + TeamBoxH + GameBoxH / 2;

                // If this match is top-slot feed, connect from Team1 row; otherwise Team2 row
                int midY  = m.NextMatchIsTop
                    ? dst.y + TeamBoxH / 2         // top team row of next match
                    : dst.y + TeamBoxH + GameBoxH + TeamBoxH / 2; // bottom team row

                int midX  = dst.x - 10;

                // Horizontal from source to midpoint, then vertical to target
                g.DrawLine(pen, srcX, srcY, midX, srcY);
                g.DrawLine(pen, midX, srcY, midX, midY);
                g.DrawLine(pen, midX, midY, dst.x, midY);
            }
        }
    }

    // ── Match box drawing ─────────────────────────────────────────────────────

    private void DrawMatch(Graphics g, BracketMatch m, (int x, int y) pos)
    {
        int x = pos.x;
        int y = pos.y;

        // ── Bye label ─────────────────────────────────────────────────────────
        if (m.Round > 1 && m.Team1IsBye && m.ByeSeed.HasValue)
        {
            using var byeFont = new Font(AppTheme.FontSmall.FontFamily, 8f, FontStyle.Regular);
            g.DrawString($"Bye {m.ByeSeed}", byeFont,
                new SolidBrush(AppTheme.TextMuted), x + 4, y - ByeLabelH);
        }

        // ── Team 1 row ────────────────────────────────────────────────────────
        DrawTeamRow(g, x, y, m.Team1Name, m.Team1Score, m.IsCompleted && m.Team1Score >= m.Team2Score);

        // ── Game info box (court / date / time) ───────────────────────────────
        int infoY = y + TeamBoxH;
        DrawGameInfoBox(g, x, infoY, m);

        // ── Team 2 row ────────────────────────────────────────────────────────
        int t2Y = infoY + GameBoxH;
        DrawTeamRow(g, x, t2Y, m.Team2Name, m.Team2Score, m.IsCompleted && m.Team2Score > m.Team1Score);

        // ── Outer border ──────────────────────────────────────────────────────
        using var borderPen = new Pen(Color.Black, 1.5f);
        g.DrawRectangle(borderPen, x, y, TeamNameW + ScoreBoxW, MatchH);
    }

    private static void DrawTeamRow(Graphics g, int x, int y, string? name, int? score, bool isWinner)
    {
        var bg = isWinner
            ? Color.FromArgb(230, 245, 230)
            : AppTheme.Surface;

        using var bgBrush = new SolidBrush(bg);
        g.FillRectangle(bgBrush, x, y, TeamNameW + ScoreBoxW, TeamBoxH);

        using var borderPen = new Pen(Color.Black, 1.5f);
        g.DrawRectangle(borderPen, x, y, TeamNameW + ScoreBoxW, TeamBoxH);

        // Team name
        using var nameFont  = new Font(AppTheme.FontDefault.FontFamily, TeamNameFontSize,
                                       FontStyle.Bold);
        using var textBrush = new SolidBrush(AppTheme.TextPrimary);
        using var nameFmt   = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(name ?? "", nameFont, textBrush,
            new RectangleF(x + 3, y + 2, TeamNameW - 6, TeamBoxH - 4), nameFmt);

        // Score box — larger font, prominent
        int sx = x + TeamNameW;
        var scoreBgColor = isWinner ? Color.FromArgb(210, 235, 210) : Color.FromArgb(242, 242, 248);
        using var scoreBg = new SolidBrush(scoreBgColor);
        g.FillRectangle(scoreBg, sx, y, ScoreBoxW, TeamBoxH);
        using var scoreBorder = new Pen(Color.Black, 1.5f);
        g.DrawRectangle(scoreBorder, sx, y, ScoreBoxW, TeamBoxH);

        if (score.HasValue)
        {
            using var scoreFont  = new Font(AppTheme.FontDefault.FontFamily, ScoreFontSize, FontStyle.Bold);
            using var scoreBrush = new SolidBrush(isWinner ? Color.FromArgb(30, 100, 30) : AppTheme.TextPrimary);
            var scoreRect = new RectangleF(sx, y + 1, ScoreBoxW, TeamBoxH - 2);
            using var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(score.Value.ToString(), scoreFont, scoreBrush, scoreRect, fmt);
        }
    }

    private static void DrawGameInfoBox(Graphics g, int x, int y, BracketMatch m)
    {
        using var boxBg = new SolidBrush(Color.FromArgb(230, 235, 242));
        g.FillRectangle(boxBg, x, y, TeamNameW + ScoreBoxW, GameBoxH);
        using var borderPen = new Pen(Color.Black, 1.5f);
        g.DrawRectangle(borderPen, x, y, TeamNameW + ScoreBoxW, GameBoxH);

        // Combine court + day/time onto one or two compact lines
        string line1 = m.CourtName ?? "";
        string line2 = "";
        if (m.Date.HasValue && m.Time.HasValue)
            line2 = $"{m.Date:ddd} {m.Time:HHmm}";
        else if (m.Date.HasValue)
            line2 = m.Date.Value.ToString("ddd MMM d");

        string infoText = line1.Length > 0 && line2.Length > 0
            ? $"{line1}  {line2}"   // single line when both present — saves vertical space
            : line1.Length > 0 ? line1 : line2;

        using var infoFont  = new Font(AppTheme.FontSmall.FontFamily, InfoFontSize, FontStyle.Regular);
        using var infoBrush = new SolidBrush(Color.FromArgb(50, 70, 110));
        using var fmt       = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(infoText, infoFont, infoBrush,
            new RectangleF(x + 2, y, TeamNameW + ScoreBoxW - 4, GameBoxH), fmt);
    }

    // ── Public print API — vector-quality direct render ───────────────────────

    /// <summary>
    /// Renders the bracket directly onto <paramref name="g"/> scaled to fit
    /// <paramref name="bounds"/>. Use this for printing instead of DrawToBitmap.
    /// </summary>
    public void DrawTo(Graphics g, RectangleF bounds)
    {
        if (_matches.Count == 0) return;

        float scale = Math.Min(bounds.Width  / Math.Max(1f, _naturalW),
                               bounds.Height / Math.Max(1f, _naturalH));

        var state = g.Save();
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TranslateTransform(bounds.Left, bounds.Top);
        g.ScaleTransform(scale, scale);

        var byRound = _matches.GroupBy(m => m.Round).OrderBy(gr => gr.Key).ToList();

        DrawConnectors(g, byRound, _positions);
        foreach (var rg in byRound)
            foreach (var m in rg.OrderBy(x => x.Slot))
                DrawMatch(g, m, _positions[m.Id]);

        string footer = $"* Each match: aggregate of 2 games total score.  Tie occurs when both teams score the same over both games → Tiebreaker: {_tiebreakerBalls} ball(s), winner scores 1 point.";
        using var fFont  = new Font(AppTheme.FontSmall.FontFamily, InfoFontSize, FontStyle.Italic);
        using var fBrush = new SolidBrush(AppTheme.TextMuted);
        g.DrawString(footer, fFont, fBrush, new PointF(LeftPad, _naturalH - 16));

        g.Restore(state);
    }

    // ── Click handling (opens score entry) ───────────────────────────────────

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (_matches.Count == 0) return;

        float fx = (e.X + (_scaleToFit ? 0 : HorizontalScroll.Value)) / _scale;
        float fy = (e.Y + (_scaleToFit ? 0 : VerticalScroll.Value)) / _scale;

        foreach (var m in _matches)
        {
            if (!_positions.TryGetValue(m.Id, out var pos)) continue;

            // Entire match bounding box is clickable — but only when both teams are known
            if (string.IsNullOrEmpty(m.Team1Name) || string.IsNullOrEmpty(m.Team2Name)) continue;
            var matchRect = new RectangleF(pos.x, pos.y, TeamNameW + ScoreBoxW, MatchH);
            if (matchRect.Contains(fx, fy))
            {
                MatchClicked?.Invoke(this, m.Id);
                return;
            }
        }
    }
}

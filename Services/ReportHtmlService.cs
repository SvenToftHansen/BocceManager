using System.Drawing;
using System.Text;
using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Services;

// Builds themed, email-safe HTML (nested tables + inline styles — no CSS classes, since mail
// clients strip <style> blocks) for the same reports the app's PrintDocument services draw via
// GDI+. Colors are pulled live from AppTheme so the emailed version always matches whatever
// theme is currently active, same as the printed version. Multi-section print layouts (Standings'
// side-by-side division columns, Team Listing's per-day tables, Schedule's per-template tables)
// are stacked vertically here instead of packed side by side — nested wide tables are unreliable
// in mobile mail clients, and a scrolling email body doesn't need print's page-packing tricks.
public static class ReportHtmlService
{
    private const string Font = "font-family:Segoe UI,Arial,sans-serif;";

    // ── Shared HTML primitives ──────────────────────────────────────────────────

    private static string Esc(string? s) => System.Net.WebUtility.HtmlEncode(s ?? "");
    private static string Hex(Color c) => ColorTranslator.ToHtml(c);

    private static string Band(string text, Color bg, Color fg, int fontPx = 14, int padY = 8) =>
        $"""
        <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="border-collapse:collapse;">
        <tr><td style="{Font}background:{Hex(bg)};color:{Hex(fg)};font-size:{fontPx}px;font-weight:bold;text-align:center;padding:{padY}px 6px;">{Esc(text)}</td></tr>
        </table>
        """;

    private static string TableStart(string marginBottom = "16px") =>
        $"<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"border-collapse:collapse;{Font}font-size:12px;margin-bottom:{marginBottom};\">";

    private static string HeaderRow((string Text, string Align)[] cells, Color bg, Color fg)
    {
        var sb = new StringBuilder("<tr>");
        foreach (var (text, align) in cells)
            sb.Append($"<th style=\"background:{Hex(bg)};color:{Hex(fg)};text-align:{align};font-size:11px;padding:4px 6px;border:1px solid {Hex(bg)};white-space:nowrap;\">{Esc(text)}</th>");
        sb.Append("</tr>");
        return sb.ToString();
    }

    private static string DataRow((string Text, string Align)[] cells, Color borderColor,
        bool alt = false, Color altBg = default, bool bold = false)
    {
        var rowStyle = alt ? $"background:{Hex(altBg)};" : "";
        var sb = new StringBuilder($"<tr style=\"{rowStyle}\">");
        foreach (var (text, align) in cells)
            sb.Append($"<td style=\"text-align:{align};padding:3px 6px;border-bottom:1px solid {Hex(borderColor)};{(bold ? "font-weight:bold;" : "")}\">{Esc(text)}</td>");
        sb.Append("</tr>");
        return sb.ToString();
    }

    private static (string ClubName, string LeagueName, string SeasonName) GetHeaderContext(BocceDbContext db, int leagueId, int? seasonId)
    {
        var clubName = AppParameterService.GetAppParameter(db, "ClubName") ?? "Bocce League";
        var leagueName = db.Leagues.Where(l => l.Id == leagueId).Select(l => l.Name).FirstOrDefault() ?? "";
        var seasonName = seasonId.HasValue
            ? db.Seasons.Where(s => s.Id == seasonId.Value).Select(s => s.Name).FirstOrDefault() ?? "N/A"
            : "N/A";
        return (clubName, leagueName, seasonName);
    }

    // Last scored week in the season — same "week" semantics as StandingsPrintService, which
    // sources Scoring.WeekId from ScheduleDivision.TemplateWeekNumber.
    private static int GetMaxWeek(BocceDbContext db, int seasonId) =>
        (int)(db.Scoring.Where(s => s.SeasonId == seasonId).Max(s => (int?)s.WeekId) ?? 0);

    // The calendar date a given week's matches were played on (the latest MatchDate among that
    // week's ScheduleDivisions rows).
    private static DateOnly? GetWeekEndDate(BocceDbContext db, int weekNumber, List<int> divIds)
    {
        if (weekNumber <= 0) return null;
        var dates = db.ScheduleDivisions
            .Where(sd => divIds.Contains(sd.DivisionId) && sd.TemplateWeekNumber == weekNumber)
            .Select(sd => sd.MatchDate)
            .ToList();
        return dates.Count > 0 ? dates.Max() : null;
    }

    // Public so callers building their own text around the standings (e.g. an email greeting)
    // can quote the same "week ending" date the report header itself uses.
    public static DateOnly? GetStandingsWeekEndDate(int seasonId, int? toWeek = null)
    {
        using var db = new BocceDbContext();
        int maxWeek = GetMaxWeek(db, seasonId);
        int effTo = toWeek ?? maxWeek;
        var divIds = db.Divisions.Where(d => d.SeasonId == seasonId).Select(d => d.Id).ToList();
        return GetWeekEndDate(db, effTo, divIds);
    }

    // fromWeek/toWeek describe what's actually being shown — the full season (default) or a
    // narrower historical cutoff (e.g. "as of week 1" while 3 weeks have been played).
    private static string WeekRangeSuffix(int fromWeek, int toWeek, int maxWeek, DateOnly? toWeekEndDate)
    {
        if (maxWeek <= 0) return "";
        bool isAll = fromWeek <= 1 && toWeek >= maxWeek;
        if (isAll)
            return toWeekEndDate.HasValue
                ? $"  —  Week {maxWeek} (Week Ending {toWeekEndDate.Value:MMM d, yyyy})"
                : $"  —  Week {maxWeek}";
        return fromWeek == toWeek ? $"  —  Week {toWeek}" : $"  —  Weeks {fromWeek}–{toWeek}";
    }

    private static string ToTitle(string? s) =>
        string.IsNullOrEmpty(s) ? "" : char.ToUpper(s[0]) + s.Substring(1).ToLower();

    // ── Spare List ───────────────────────────────────────────────────────────────

    public static string BuildSpareListHtml(int leagueId, int? seasonId)
    {
        using var db = new BocceDbContext();
        var (clubName, leagueName, seasonName) = GetHeaderContext(db, leagueId, seasonId);
        var rows = SpareListReportService.BuildSpareListData(leagueId);

        var sb = new StringBuilder();
        sb.Append(Band($"{clubName} – {leagueName} – {seasonName} - SPARE LIST", AppTheme.NavHeader, Color.White));
        sb.Append(TableStart());
        sb.Append(HeaderRow(
            [("Name", "left"), ("Phone", "left"), ("Email", "left"), ("Lot #", "left"), ("Notes", "left")],
            AppTheme.NavHeader, Color.White));

        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            sb.Append(DataRow(
                [(r.Name, "left"), (r.Phone, "left"), (r.Email, "left"), (r.LotNumber, "left"), (r.Notes, "left")],
                AppTheme.GridLines, alt: i % 2 == 1, altBg: AppTheme.GridAlternateRow));
        }
        sb.Append("</table>");
        return sb.ToString();
    }

    // ── Unpaid Fees ──────────────────────────────────────────────────────────────

    public static string BuildUnpaidFeesHtml(int leagueId, int? seasonId)
    {
        using var db = new BocceDbContext();
        var (clubName, leagueName, seasonName) = GetHeaderContext(db, leagueId, seasonId);
        var sections = UnpaidFeesReportService.BuildSections(seasonId);

        var sb = new StringBuilder();
        sb.Append(Band($"{clubName}  –  {leagueName}  –  {seasonName} - Unpaid Fees", AppTheme.NavHeader, Color.White));

        foreach (var sec in sections)
        {
            if (sec.Rows.Count == 0) continue;

            sb.Append(Band(sec.Title, AppTheme.GridHeaderBackground, Color.White, fontPx: 12, padY: 5));
            sb.Append(TableStart());
            sb.Append(HeaderRow(
                [("Name", "left"), ("Email", "left"), ("Phone", "left"), ("Lot #", "left"), ("Paid", "center")],
                AppTheme.GridHeaderBackground, Color.White));

            for (int i = 0; i < sec.Rows.Count; i++)
            {
                var r = sec.Rows[i];
                sb.Append(DataRow(
                    [(r.Name, "left"), (r.Email, "left"), (r.Phone, "left"), (r.LotNumber, "left"), ("☐", "center")],
                    AppTheme.GridLines, alt: i % 2 == 1, altBg: AppTheme.GridAlternateRow));
            }
            sb.Append("</table>");
        }
        return sb.ToString();
    }

    // ── All Players ──────────────────────────────────────────────────────────────

    public static string BuildAllPlayersHtml(int leagueId, int? seasonId)
    {
        using var db = new BocceDbContext();
        var (clubName, leagueName, seasonName) = GetHeaderContext(db, leagueId, seasonId);
        var filter = new AllPlayersReportService.ReportFilter(false, false, false, false, false, false, false, false, false);
        var data = AllPlayersReportService.BuildData(leagueId, seasonId, filter);

        var sb = new StringBuilder();
        sb.Append(Band($"{clubName} – {leagueName} – {seasonName} - ALL PLAYERS", AppTheme.NavHeader, Color.White));
        sb.Append($"<div style=\"{Font}font-size:11px;color:#555;text-align:center;margin:4px 0 8px;\">{Esc(filter.Describe())}</div>");
        sb.Append(TableStart());
        sb.Append(HeaderRow(
            [("Name", "left"), ("Phone", "left"), ("Email", "left"), ("Lot #", "left"),
             ("Init Fee", "center"), ("Season Fee", "center"), ("On Team", "center"), ("On Spare", "center")],
            AppTheme.GridHeaderBackground, Color.White));

        for (int i = 0; i < data.Count; i++)
        {
            var r = data[i];
            sb.Append(DataRow(
                [(r.Name, "left"), (r.Phone, "left"), (r.Email, "left"), (r.LotNumber, "left"),
                 (CheckMark(r.InitiationPaid), "center"), (CheckMark(r.SeasonPaid), "center"),
                 (r.OnTeam ? "✓" : "", "center"), (r.OnSpareList ? "✓" : "", "center")],
                AppTheme.GridLines, alt: i % 2 == 1, altBg: AppTheme.GridAlternateRow));
        }
        sb.Append("</table>");
        return sb.ToString();

        static string CheckMark(bool? paid) => paid switch { true => "✓", false => "☐", null => "—" };
    }

    // ── Team Listing ─────────────────────────────────────────────────────────────

    public static string BuildTeamListingHtml(int seasonId)
    {
        var sections = TeamsPrintService.BuildSections(seasonId);
        if (sections.Count == 0) return "<p>No teams found for the current season.</p>";

        var sb = new StringBuilder();
        sb.Append(Band(sections[0].DocHeader, AppTheme.NavHeader, Color.White));

        foreach (var sec in sections)
        {
            sb.Append(Band(sec.TimeSlotLabel, AppTheme.NavHeader, Color.White, fontPx: 12, padY: 5));

            foreach (var day in sec.DaySections)
            {
                sb.Append(Band($"{day.DayLabel} - {day.TimeSlot}", AppTheme.NavHover, Color.White, fontPx: 11, padY: 4));
                sb.Append(TableStart());
                sb.Append(HeaderRow(
                    [("Team", "left"), ("Captain", "left"), ("Players", "left"), ("Contact #", "left")],
                    AppTheme.GridHeaderBackground, Color.White));

                foreach (var t in day.Teams)
                    sb.Append(DataRow(
                        [(t.TeamIdentifier, "left"), (t.CaptainName, "left"), (t.AllOtherPlayers, "left"), (t.Phone, "left")],
                        AppTheme.GridLines));

                sb.Append("</table>");
            }
        }
        return sb.ToString();
    }

    // ── Schedule ─────────────────────────────────────────────────────────────────

    public static string BuildScheduleHtml(int seasonId)
    {
        var sections = SchedulePrintService.BuildSections(seasonId);
        if (sections.Count == 0) return "<p>No schedule templates found for the current season.</p>";

        var sb = new StringBuilder();
        sb.Append(Band(sections[0].DocHeader, AppTheme.NavHeader, Color.White));

        foreach (var sec in sections)
        {
            sb.Append($"<div style=\"{Font}font-size:12px;font-weight:bold;margin:10px 0 4px;\">{Esc(sec.TmplTitle)}</div>");
            sb.Append(TableStart());

            (string Text, string Align)[] headerCells =
                [("Week Of", "left"), .. sec.CourtHeaders.Select(h => (h, "center"))];
            sb.Append(HeaderRow(headerCells, AppTheme.GridHeaderBackground, Color.White));

            foreach (var row in sec.Rows)
            {
                (string Text, string Align)[] cells =
                [
                    (row.Week, "left"),
                    .. row.Slots.Select(s => (s.HasValue ? $"{s.Value.S1} vs {s.Value.S2}" : "", "center"))
                ];
                sb.Append(DataRow(cells, AppTheme.GridLines));
            }
            sb.Append("</table>");
        }
        return sb.ToString();
    }

    // ── Standings (division grids; season-seed page not included) ────────────────

    private static string PmStr(int v) => v >= 0 ? $"+{v}" : $"{v}";

    private static string[] StandingsStatHeaders(bool isGamesMode)
    {
        var h = new List<string> { "Seed", isGamesMode ? "GP" : "MP", "W" };
        if (!isGamesMode) h.Add("T");
        h.AddRange(["L", "F", "Pts", "+/-"]);
        return h.ToArray();
    }

    private static string[] StandingsStatValues(StandingView r, bool isGamesMode)
    {
        var v = new List<string>
        {
            r.SeasonSeed.ToString(),
            (isGamesMode ? r.GamesPlayed : r.MatchesPlayed).ToString(),
            r.Wins.ToString()
        };
        if (!isGamesMode) v.Add(r.Ties.ToString());
        v.Add(r.Losses.ToString()); v.Add(r.Forfeits.ToString());
        v.Add(r.StandingsPoints.ToString()); v.Add(PmStr(r.PlusMinus));
        return v.ToArray();
    }

    // divisionId narrows to a single division's table. fromWeek/toWeek default to the full
    // season; pass a narrower range (e.g. from the Standings panel's week-range slider) to get
    // a historical "as of" snapshot instead of the latest standings.
    public static string BuildStandingsHtml(int seasonId, int? divisionId = null, int? fromWeek = null, int? toWeek = null)
    {
        using var db = new BocceDbContext();
        var season = db.Seasons.Include(s => s.League).First(s => s.Id == seasonId);
        bool isGamesMode = season.ScoringMode == "games_mode";

        int maxWeek = GetMaxWeek(db, seasonId);
        int effFrom = fromWeek ?? 1;
        int effTo   = toWeek ?? maxWeek;

        var allRows = StandingsAggregationService.GetStandings(db, seasonId, isGamesMode, season.FirstPlaceGuaranteed, effFrom, effTo, maxWeek);
        if (divisionId.HasValue) allRows = allRows.Where(r => r.DivisionId == divisionId.Value).ToList();
        allRows = allRows.OrderBy(r => r.DivisionId).ThenBy(r => r.DivisionRank).ThenBy(r => r.DivisionSeed).ToList();
        if (allRows.Count == 0) return "<p>No standings data found for the current season.</p>";

        var divIds = allRows.Select(r => r.DivisionId).Distinct().ToList();
        var divisions = db.Divisions
            .Include(d => d.DaySlot).Include(d => d.TimeSlot)
            .Where(d => divIds.Contains(d.Id))
            .ToList();

        var timeCols = divisions
            .Where(d => d.TimeSlotId.HasValue && d.TimeSlot != null)
            .Select(d => (Id: d.TimeSlotId!.Value, Label: d.TimeSlot!.Timeslot12h, Sort: d.TimeSlot!.SortOrder ?? 999))
            .Distinct().OrderBy(t => t.Sort).ToList();

        var clubName = AppParameterService.GetAppParameter(db, "ClubName") ?? "Bocce League";
        var weekEndDate = GetWeekEndDate(db, effTo, divIds);
        string docHeader = $"{clubName}  —  {season.League.Name}  —  {season.Name}  —  Standings{WeekRangeSuffix(effFrom, effTo, maxWeek, weekEndDate)}";

        string[] statHeaders = StandingsStatHeaders(isGamesMode);

        var sb = new StringBuilder();
        sb.Append(Band(docHeader, AppTheme.NavHeader, Color.White));

        var divisionHeaderColor = Color.FromArgb(100, 130, 170);
        var zebraColor = Color.FromArgb(235, 240, 248);

        foreach (var tc in timeCols)
        {
            sb.Append(Band(tc.Label, AppTheme.GridHeaderBackground, Color.White, fontPx: 12, padY: 5));

            var divsInSlot = divisions
                .Where(d => d.TimeSlotId == tc.Id && d.DaySlotId.HasValue && d.DaySlot != null)
                .OrderBy(d => d.DaySlot!.DayNbr)
                .ToList();

            foreach (var div in divsInSlot)
            {
                var rows = allRows.Where(r => r.DivisionId == div.Id).ToList();
                if (rows.Count == 0) continue;

                sb.Append(Band(div.Name, divisionHeaderColor, Color.White, fontPx: 11, padY: 4));
                sb.Append(TableStart());

                (string Text, string Align)[] headerCells =
                    [("Team", "left"), .. statHeaders.Select(h => (h, "center"))];
                sb.Append(HeaderRow(headerCells, AppTheme.GridHeaderBackground, Color.White));

                for (int i = 0; i < rows.Count; i++)
                {
                    var r = rows[i];
                    string teamDisplay = season.TeamsInPlayoffs > 0 && r.SeasonSeed > 0 && r.SeasonSeed <= season.TeamsInPlayoffs
                        ? $"✓ {r.TeamName}" : r.TeamName;
                    (string Text, string Align)[] cells =
                        [(teamDisplay, "left"), .. StandingsStatValues(r, isGamesMode).Select(v => (v, "center"))];
                    sb.Append(DataRow(cells, AppTheme.GridLines, alt: i % 2 == 1, altBg: zebraColor, bold: r.DivisionRank == 1));
                }
                sb.Append("</table>");
            }
        }
        return sb.ToString();
    }

    // ── Season Seed (overall playoff seeding order across all divisions) ─────────

    public static string BuildSeasonSeedHtml(int seasonId, int? fromWeek = null, int? toWeek = null)
    {
        using var db = new BocceDbContext();
        var season = db.Seasons.Include(s => s.League).First(s => s.Id == seasonId);
        bool isGamesMode = season.ScoringMode == "games_mode";

        int maxWeek = GetMaxWeek(db, seasonId);
        int effFrom = fromWeek ?? 1;
        int effTo   = toWeek ?? maxWeek;

        var allRows = StandingsAggregationService.GetStandings(db, seasonId, isGamesMode, season.FirstPlaceGuaranteed, effFrom, effTo, maxWeek)
            .OrderBy(r => r.SeasonSeed)
            .ToList();
        if (allRows.Count == 0) return "<p>No standings data found for the current season.</p>";

        var divIds = allRows.Select(r => r.DivisionId).Distinct().ToList();
        var divisions = db.Divisions
            .Include(d => d.DaySlot).Include(d => d.TimeSlot)
            .Where(d => divIds.Contains(d.Id))
            .ToList();
        var shortNames = divisions.ToDictionary(d => d.Id,
            d => d.DaySlot != null && d.TimeSlot != null
                ? $"{ToTitle(d.DaySlot.DayAbbr)} {d.TimeSlot.Timeslot12h}"
                : d.Name);

        var clubName = AppParameterService.GetAppParameter(db, "ClubName") ?? "Bocce League";
        var weekEndDate = GetWeekEndDate(db, effTo, divIds);
        string docHeader = $"{clubName}  —  {season.League.Name}  —  {season.Name}  —  Season Seed{WeekRangeSuffix(effFrom, effTo, maxWeek, weekEndDate)}";

        var headers = new List<string> { "Seed", "Team", "Division", "Pts", "+/-", "W" };
        if (!isGamesMode) headers.Add("T");
        headers.AddRange(["L", "F"]);
        if (season.TeamsInPlayoffs > 0) headers.Add("✓");

        string Align(string h) => h is "Team" or "Division" ? "left" : "center";

        var sb = new StringBuilder();
        sb.Append(Band(docHeader, AppTheme.NavHeader, Color.White));
        sb.Append(TableStart());
        sb.Append(HeaderRow(headers.Select(h => (h, Align(h))).ToArray(), AppTheme.GridHeaderBackground, Color.White));

        for (int i = 0; i < allRows.Count; i++)
        {
            var r = allRows[i];
            var vals = new List<string>
            {
                r.SeasonSeed.ToString(), r.TeamName,
                shortNames.TryGetValue(r.DivisionId, out var sn) ? sn : "?",
                r.StandingsPoints.ToString(), PmStr(r.PlusMinus), r.Wins.ToString()
            };
            if (!isGamesMode) vals.Add(r.Ties.ToString());
            vals.Add(r.Losses.ToString()); vals.Add(r.Forfeits.ToString());

            bool qualifies = season.TeamsInPlayoffs > 0 && r.SeasonSeed <= season.TeamsInPlayoffs;
            if (season.TeamsInPlayoffs > 0) vals.Add(qualifies ? "✓" : "");

            var cells = vals.Select((v, idx) => (v, Align(headers[idx]))).ToArray();
            sb.Append(DataRow(cells, AppTheme.GridLines, alt: i % 2 == 1, altBg: AppTheme.GridAlternateRow, bold: qualifies));
        }
        sb.Append("</table>");
        return sb.ToString();
    }

    // ── Playoff Schedule (game list; bracket ladder diagram is not represented) ──

    private record PlayoffRow(string Round, int Game, string Top, string Bottom, string Court, string Date, string Time, string Score, string Status);

    public static string BuildPlayoffScheduleHtml(int seasonId)
    {
        using var db = new BocceDbContext();

        bool generated = db.PlayoffConfigs.Any(c => c.SeasonId == seasonId && c.IsGenerated);
        if (!generated)
            throw new InvalidOperationException("The playoff bracket hasn't been generated yet for this season — use Playoff Setup first.");

        var season = db.Seasons.Include(s => s.League).First(s => s.Id == seasonId);
        var clubName = AppParameterService.GetAppParameter(db, "ClubName") ?? "Bocce League";
        string docHeader = string.Join("  —  ", new[] { clubName, season.League.Name, season.Name, "Playoff Schedule" }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

        var matches = db.PlayoffMatches
            .Include(m => m.PlayoffRound)
            .Include(m => m.Team1)
            .Include(m => m.Team2)
            .Include(m => m.Court)
            .Where(m => m.SeasonId == seasonId)
            .OrderBy(m => m.PlayoffRound!.RoundNumber)
            .ThenBy(m => m.BracketSlot)
            .ToList();

        var gameScores = db.PlayoffGames
            .Where(g => matches.Select(m => m.Id).Contains(g.PlayoffMatchId))
            .GroupBy(g => g.PlayoffMatchId)
            .ToDictionary(g => g.Key, g => (T1: g.Sum(x => x.Team1Score), T2: g.Sum(x => x.Team2Score)));

        var rows = new List<PlayoffRow>();
        int gameNum = 1;
        foreach (var m in matches)
        {
            gameScores.TryGetValue(m.Id, out var sc);
            string score = sc.T1 > 0 || sc.T2 > 0 ? $"{sc.T1} – {sc.T2}" : "";
            string court = m.Court != null ? $"Court {m.Court.CourtNumber}" : "";
            string date = m.ScheduledDate?.ToString("ddd MMM d") ?? "";
            string time = m.ScheduledTime?.ToString("HHmm") ?? "";

            rows.Add(new PlayoffRow(
                m.PlayoffRound?.RoundName ?? $"Round {m.PlayoffRound?.RoundNumber}",
                gameNum++,
                m.Team1?.EffectiveDisplayName ?? "TBD",
                m.Team2?.EffectiveDisplayName ?? "TBD",
                court, date, time, score,
                m.Status == "completed" ? "Done" : "Pending"));
        }

        if (rows.Count == 0) return "<p>No playoff matches found for the current season.</p>";

        var sb = new StringBuilder();
        sb.Append(Band(docHeader, AppTheme.NavHeader, Color.White));
        sb.Append(TableStart());
        sb.Append(HeaderRow(
            [("Round", "left"), ("Game #", "center"), ("Top Team", "left"), ("Bot Team", "left"),
             ("Court", "left"), ("Date", "left"), ("Time", "left"), ("Score", "center"), ("Status", "center")],
            AppTheme.GridHeaderBackground, Color.White));

        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            sb.Append(DataRow(
                [(r.Round, "left"), (r.Game.ToString(), "center"), (r.Top, "left"), (r.Bottom, "left"),
                 (r.Court, "left"), (r.Date, "left"), (r.Time, "left"), (r.Score, "center"), (r.Status, "center")],
                AppTheme.GridLines, alt: i % 2 == 1, altBg: AppTheme.GridAlternateRow));
        }
        sb.Append("</table>");
        return sb.ToString();
    }
}

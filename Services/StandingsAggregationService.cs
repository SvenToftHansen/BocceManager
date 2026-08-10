using BocceManager.Data;
using BocceManager.Data.Entities;

namespace BocceManager.Services;

// Shared "full season" vs "as of week range" standings aggregation — originally lived only in
// StandingsPanel (for its week-range slider), extracted so report/email code can produce
// standings "as of" the same partial week cutoff the panel is currently showing, not always
// the latest full-season snapshot.
public static class StandingsAggregationService
{
    public static List<StandingView> GetStandings(BocceDbContext db, int seasonId,
        bool isGamesMode, bool firstPlaceGuaranteed, int fromWeek, int toWeek, int maxWeek)
    {
        bool isAll = fromWeek <= 1 && toWeek >= maxWeek;
        if (isAll)
        {
            // Use the pre-aggregated (and H2H-resolved) Standings view
            return db.Standings
                .Where(s => s.SeasonId == seasonId)
                .OrderBy(s => s.DivisionId).ThenBy(s => s.DivisionRank).ThenBy(s => s.DivisionSeed)
                .ToList();
        }

        return AggregateForWeekRange(db, seasonId, isGamesMode, firstPlaceGuaranteed, fromWeek, toWeek);
    }

    // ── Custom week-range aggregation (bypasses Standings view) ───────────────
    // Cross-division H2H tiebreakers aren't computed for partial-range slices (H2HPlusMinus/
    // H2HWins left at 0) — only the full-season Standings view resolves those.

    private static List<StandingView> AggregateForWeekRange(
        BocceDbContext db, int seasonId,
        bool isGamesMode, bool firstPlaceGuaranteed,
        int fromWeek, int toWeek)
    {
        var rows = db.Scoring
            .Where(s => s.SeasonId == seasonId && s.WeekId >= fromWeek && s.WeekId <= toWeek)
            .ToList();

        if (rows.Count == 0) return [];

        // Aggregate per (TeamId, DivisionId)
        var agg = rows
            .GroupBy(s => new { s.TeamId, s.DivisionId, s.TeamName, s.SeasonId, s.LeagueId })
            .Select(g => new StandingView
            {
                TeamId   = g.Key.TeamId,
                TeamName = g.Key.TeamName,
                DivisionId  = g.Key.DivisionId,
                SeasonId    = g.Key.SeasonId,
                LeagueId    = g.Key.LeagueId,
                GamesPlayed    = isGamesMode ? g.Count() : 0,
                MatchesPlayed  = isGamesMode ? 0 : g.Select(s => s.ScheduleDivisionsId).Distinct().Count(),
                Wins       = g.Count(s => s.IsWin),
                Ties       = g.Count(s => s.IsTie),
                Losses     = g.Count(s => s.IsLoss && !s.IsForfeit),
                Forfeits   = g.Count(s => s.IsForfeit),
                StandingsPoints = g.Sum(s => s.WinPTS + s.TiePTS + s.LossPTS + s.ForfeitPoints),
                PlusMinus  = g.Sum(s => s.PlusMinus),
                PointsFor  = g.Sum(s => s.PointsFor),
                PointsAgainst = g.Sum(s => s.PointsAgainst),
                H2HPlusMinus = 0,  // cross-div H2H not computed for date-range slices
                H2HWins      = 0
            })
            .ToList();

        // Division rank (DENSE_RANK per division)
        foreach (var dg in agg.GroupBy(r => r.DivisionId))
        {
            int seed = 1;
            int prevPts = int.MaxValue, prevPm = int.MaxValue, prevW = int.MaxValue;
            foreach (var r in dg.OrderByDescending(r => r.StandingsPoints)
                                 .ThenByDescending(r => r.PlusMinus)
                                 .ThenByDescending(r => r.Wins))
            {
                if (r.StandingsPoints != prevPts || r.PlusMinus != prevPm || r.Wins != prevW)
                {
                    prevPts = r.StandingsPoints; prevPm = r.PlusMinus; prevW = r.Wins;
                }
                // Assign the rank at the first position for this stats group
                r.DivisionRank = seed;
                r.DivisionSeed = seed;
                seed++;
            }
            // Correct DivisionRank to DENSE_RANK (tied teams share the same rank)
            var sorted2 = dg.OrderByDescending(r => r.StandingsPoints)
                            .ThenByDescending(r => r.PlusMinus)
                            .ThenByDescending(r => r.Wins).ToList();
            int rank = 1;
            for (int i = 0; i < sorted2.Count; i++)
            {
                if (i > 0 && (sorted2[i].StandingsPoints != sorted2[i-1].StandingsPoints
                           || sorted2[i].PlusMinus        != sorted2[i-1].PlusMinus
                           || sorted2[i].Wins             != sorted2[i-1].Wins))
                    rank = i + 1;
                sorted2[i].DivisionRank = rank;
            }
        }

        // Season seed (mirrors FixSeasonSeedOrdering logic)
        int sseed = 1;
        foreach (var r in agg
            .OrderBy(r => firstPlaceGuaranteed && r.DivisionRank == 1 ? 0 : 1)
            .ThenByDescending(r => r.StandingsPoints)
            .ThenByDescending(r => r.PlusMinus)
            .ThenByDescending(r => r.Wins)
            .ThenBy(r => r.DivisionRank))
        {
            r.SeasonSeed = sseed++;
        }

        return agg.OrderBy(r => r.DivisionId).ThenBy(r => r.DivisionRank).ToList();
    }
}

using BocceManager.Data;
using BocceManager.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Services;

public static class PlayoffService
{
    // ── Bracket math ──────────────────────────────────────────────────────────

    public static int GetRoundCount(int teamCount)
    {
        int bracket = NextPowerOfTwo(teamCount);
        return (int)Math.Log2(bracket);
    }

    public static int GetByeCount(int teamCount) =>
        NextPowerOfTwo(teamCount) - teamCount;

    public static int GetGamesInRound(int teamCount, int roundNumber)
    {
        // Round 1 = first round played (bye teams sit out).
        // roundNumber is 1-based.
        int bracket  = NextPowerOfTwo(teamCount);
        int byeCount = bracket - teamCount;
        // R1 has (teamCount - byeCount) / 2 games
        // Each subsequent round halves the field.
        int r1Games = (teamCount - byeCount) / 2;
        if (roundNumber == 1) return r1Games > 0 ? r1Games : bracket / 2;
        return bracket / (int)Math.Pow(2, roundNumber);
    }

    // Round names for display
    public static string RoundName(int teamCount, int roundNumber)
    {
        int totalRounds = GetRoundCount(teamCount);
        int fromEnd = totalRounds - roundNumber;
        return fromEnd switch
        {
            0 => "Final",
            1 => "Semi-Finals",
            2 => "Quarter-Finals",
            _ => $"Round {roundNumber}"
        };
    }

    // ── Schedule computation ──────────────────────────────────────────────────

    public record RoundSchedule(int Round, string Name, DateOnly Date, TimeOnly StartTime);

    /// <summary>
    /// Assigns rounds to days/times based on day parameters.
    /// MatchLengthMins per day is the total slot per game (includes gap — no separate gap).
    /// </summary>
    public static List<RoundSchedule> ComputeRoundSchedule(
        int teamCount,
        int matchDurationMins,      // kept for caller compatibility; now ignored in favour of per-day MatchLengthMins
        IReadOnlyList<PlayoffDayParams> days,
        int courtCount)
    {
        if (courtCount <= 0) courtCount = 1;

        int totalRounds = GetRoundCount(teamCount);
        var result      = new List<RoundSchedule>();
        int round       = 1;

        foreach (var day in days.OrderBy(d => d.DayNumber))
        {
            var current    = day.StartTime;
            int matchSlot  = day.MatchLengthMins > 0 ? day.MatchLengthMins : matchDurationMins;

            while (round <= totalRounds)
            {
                int games    = GetGamesInRound(teamCount, round);
                int waves    = (int)Math.Ceiling((double)games / courtCount);
                int roundMins = waves * matchSlot;
                var roundEnd  = current.AddMinutes(roundMins);

                // Would this round finish after the day's end time? Stop.
                if (roundEnd > day.EndTime) break;

                result.Add(new RoundSchedule(
                    round,
                    RoundName(teamCount, round),
                    day.GameDate,
                    current));

                round++;
                if (round > totalRounds) break;

                // Gap is built into MatchLengthMins — next round starts immediately after
                current = current.AddMinutes(roundMins);
            }

            if (round > totalRounds) break;
        }

        // Any rounds that didn't fit get no date/time — surface them as unscheduled.
        while (round <= totalRounds)
        {
            result.Add(new RoundSchedule(round, RoundName(teamCount, round),
                DateOnly.MinValue, TimeOnly.MinValue));
            round++;
        }

        return result;
    }

    // ── Bracket generation ────────────────────────────────────────────────────

    /// <summary>
    /// Wipes existing rounds/matches for the season and regenerates the full bracket.
    /// </summary>
    public static void GenerateBracket(BocceDbContext db, int seasonId)
    {
        var season = db.Seasons.Find(seasonId)
            ?? throw new InvalidOperationException("Season not found.");

        var config = db.PlayoffConfigs
            .Include(c => c.DayParams)
            .FirstOrDefault(c => c.SeasonId == seasonId)
            ?? throw new InvalidOperationException("Playoff config not found. Save config first.");

        var seedings = db.PlayoffSeedings
            .Where(s => s.SeasonId == seasonId)
            .OrderBy(s => s.Seed)
            .ToList();

        int teamCount = season.TeamsInPlayoffs;
        if (teamCount < 2) throw new InvalidOperationException("Need at least 2 playoff teams.");

        var courts = db.SeasonCourts
            .Where(sc => sc.SeasonId == seasonId)
            .Include(sc => sc.Court)
            .Select(sc => sc.Court)
            .OrderBy(c => c.SortOrder)
            .ToList();

        var schedule = ComputeRoundSchedule(
            teamCount,
            config.MatchDurationMins,
            config.DayParams.ToList(),
            courts.Count);

        // Clear old data
        var oldRounds = db.PlayoffRounds.Where(r => r.SeasonId == seasonId).ToList();
        var oldMatchIds = db.PlayoffMatches.Where(m => m.SeasonId == seasonId).Select(m => m.Id).ToList();
        if (oldMatchIds.Count > 0)
        {
            db.PlayoffGames.Where(g => oldMatchIds.Contains(g.PlayoffMatchId)).ExecuteDelete();
            db.PlayoffMatches.Where(m => m.SeasonId == seasonId).ExecuteDelete();
        }
        db.PlayoffRounds.RemoveRange(oldRounds);
        db.SaveChanges();

        int totalRounds = GetRoundCount(teamCount);
        int byeCount    = GetByeCount(teamCount);

        // Create round rows
        var rounds = new Dictionary<int, PlayoffRound>();
        foreach (var rs in schedule)
        {
            var pr = new PlayoffRound
            {
                SeasonId    = seasonId,
                RoundNumber = rs.Round,
                RoundName   = rs.Name,
                MatchDate   = rs.Date == DateOnly.MinValue ? null : rs.Date,
                StartTime   = rs.StartTime == TimeOnly.MinValue ? null : rs.StartTime,
            };
            db.PlayoffRounds.Add(pr);
            rounds[rs.Round] = pr;
        }
        db.SaveChanges();

        // Build match slots bottom-up (Final → R1) so NextMatchId can be set.
        // We'll store them in a 2D array: matchSlots[round][slot]
        var matchSlots = new PlayoffMatch[totalRounds + 1][];
        for (int r = 1; r <= totalRounds; r++)
        {
            int games = GetGamesInRound(teamCount, r);
            // Round 1 might be 0 if all teams get byes (teamCount = power of 2 → no R1 games)
            if (r == 1 && byeCount == teamCount)
                games = teamCount / 2;
            matchSlots[r] = new PlayoffMatch[games > 0 ? games : 1];
        }

        // Final (last round) has 1 match, slot 0
        var finalRound = rounds.ContainsKey(totalRounds) ? rounds[totalRounds] : null;
        var finalMatch = new PlayoffMatch
        {
            SeasonId       = seasonId,
            PlayoffRoundId = finalRound?.Id,
            BracketSlot    = 0,
            Seed1          = 0,
            Status         = "scheduled",
            ScheduledDate  = finalRound?.MatchDate,
            ScheduledTime  = AssignTime(schedule, totalRounds, courts, 0, config.MatchDurationMins, config.DayParams.ToList()),
            CourtId        = AssignCourt(courts, 0),
        };
        db.PlayoffMatches.Add(finalMatch);
        matchSlots[totalRounds][0] = finalMatch;
        db.SaveChanges();

        // Build each round from second-to-last back to first
        for (int r = totalRounds - 1; r >= 1; r--)
        {
            int parentCount = matchSlots[r + 1].Length;
            int childCount  = parentCount * 2;
            matchSlots[r]   = new PlayoffMatch[childCount];

            var roundRow = rounds.ContainsKey(r) ? rounds[r] : null;

            for (int slot = 0; slot < childCount; slot++)
            {
                int parentSlot    = slot / 2;
                bool isTopOfParent = slot % 2 == 0;
                var parent        = matchSlots[r + 1][parentSlot];

                var match = new PlayoffMatch
                {
                    SeasonId       = seasonId,
                    PlayoffRoundId = roundRow?.Id,
                    BracketSlot    = slot,
                    Seed1          = 0,
                    Status         = "scheduled",
                    NextMatchId    = parent.Id,
                    NextMatchIsTop = isTopOfParent,
                    ScheduledDate  = roundRow?.MatchDate,
                    ScheduledTime  = AssignTime(schedule, r, courts, slot, config.MatchDurationMins, config.DayParams.ToList()),
                    CourtId        = AssignCourt(courts, slot),
                };
                db.PlayoffMatches.Add(match);
                matchSlots[r][slot] = match;
            }
            db.SaveChanges();
        }

        // Assign seeds to slots using ladder bye-ordering: 1, 4, 3, 2 ... pattern
        AssignSeeds(db, seasonId, teamCount, byeCount, matchSlots, seedings, totalRounds, schedule, courts, config.MatchDurationMins);

        config.IsGenerated = true;
        db.SaveChanges();
    }

    // ── Seed assignment ───────────────────────────────────────────────────────

    private static void AssignSeeds(
        BocceDbContext db,
        int seasonId,
        int teamCount,
        int byeCount,
        PlayoffMatch[][] matchSlots,
        List<PlayoffSeeding> seedings,
        int totalRounds,
        List<RoundSchedule> schedule,
        List<Court> courts,
        int matchDurationMins)
    {
        // The bye seeds fill the top-team slot of their R2 match.
        // The bye ordering from top to bottom of bracket is: 1, 4, 3, 2 (for 4 byes)
        // Generalised: build the ordered bye sequence for N byes.
        var byeOrder = BuildByeOrder(byeCount);

        int r2 = byeCount > 0 ? 2 : 1; // round where byes enter (or R1 if no byes)

        if (byeCount > 0 && matchSlots.Length > r2 && matchSlots[r2] != null)
        {
            // R2 slots: pairs are (slot 0, slot 1), (slot 2, slot 3) ...
            // bye teams occupy top position (Team1) of each pair's combined output match
            // Actually each R2 match's top team = bye, bottom = R1 winner
            for (int i = 0; i < byeOrder.Count && i < matchSlots[r2].Length; i++)
            {
                int byeSeed = byeOrder[i];
                var seeding = seedings.FirstOrDefault(s => s.Seed == byeSeed);
                var match   = matchSlots[r2][i];

                match.Seed1   = byeSeed;
                match.Team1Id = seeding?.TeamId;
                match.IsBye   = false; // bye team is a real participant; their R1 game is the bye
            }

            // R2 non-bye slots connect to R1 matches
            // Non-bye seeds paired: 5v(N), 6v(N-1) ... matching bye order
            var nonByeSeeds = BuildNonByePairs(teamCount, byeCount, byeOrder);
            for (int i = 0; i < matchSlots[1].Length && i < nonByeSeeds.Count; i++)
            {
                var (topSeed, botSeed) = nonByeSeeds[i];
                var r1Match = matchSlots[1][i];
                var topSeed_ = seedings.FirstOrDefault(s => s.Seed == topSeed);
                var botSeed_ = seedings.FirstOrDefault(s => s.Seed == botSeed);

                r1Match.Seed1   = topSeed;
                r1Match.Seed2   = botSeed;
                r1Match.Team1Id = topSeed_?.TeamId;
                r1Match.Team2Id = botSeed_?.TeamId;
            }
        }
        else
        {
            // No byes — assign all R1 matches directly (1v16, 2v15, ...)
            int n = teamCount;
            for (int slot = 0; slot < matchSlots[1].Length; slot++)
            {
                int topSeed = slot + 1;
                int botSeed = n - slot;
                var ts = seedings.FirstOrDefault(s => s.Seed == topSeed);
                var bs = seedings.FirstOrDefault(s => s.Seed == botSeed);
                var m  = matchSlots[1][slot];
                m.Seed1   = topSeed;
                m.Seed2   = botSeed;
                m.Team1Id = ts?.TeamId;
                m.Team2Id = bs?.TeamId;
            }
        }

        db.SaveChanges();
    }

    // Bye ordering for N byes: produces the top-to-bottom sequence of bye seeds.
    // For 4 byes: [1, 4, 3, 2]
    // This ensures: #1 and #2 are in opposite halves; #1 meets #4 in SF worst case.
    private static List<int> BuildByeOrder(int byeCount)
    {
        if (byeCount == 0) return [];
        // Build recursively: seed 1 top, seed 2 bottom, fill inward
        var order = new int[byeCount];
        int lo = 0, hi = byeCount - 1;
        int seed = 1;
        bool top = true;
        while (seed <= byeCount)
        {
            if (top) order[lo++] = seed;
            else     order[hi--] = seed;
            seed++;
            top = !top;
        }
        return [.. order];
    }

    // Pair R1 seeds to match each bye opponent correctly.
    // Bye 1 (weakest opp): R1 match is (byeCount+half) v N, e.g. #8v#9 for 12 teams
    // Bye 4 (strongest opp): R1 match is (byeCount+1) v (byeCount+half+1)
    private static List<(int top, int bot)> BuildNonByePairs(int teamCount, int byeCount, List<int> byeOrder)
    {
        // Non-bye seeds: byeCount+1 .. teamCount
        // Pair them top vs bottom within the non-bye group
        // The pairing matches the bye order so each bye faces the right opponent
        int lo = byeCount + 1;
        int hi = teamCount;
        int n  = (hi - lo + 1) / 2; // number of R1 games
        var pairs = new List<(int, int)>();
        for (int i = 0; i < n; i++)
            pairs.Add((lo + i, hi - i));

        // Reorder pairs to align with byeOrder:
        // bye order gives: bye 1 → weakest R1 winner → pair with seeds closest to middle
        // For 12 teams: pairs = (5,12),(6,11),(7,10),(8,9); bye order = 1,4,3,2
        // Bye 1 wants weakest → pair (8,9); Bye 4 wants strongest → pair (5,12)
        // So reverse the pairs list for correct alignment with byeOrder
        pairs.Reverse();
        return pairs;
    }

    // ── Winner advancement ────────────────────────────────────────────────────

    /// <summary>
    /// Records scores for a match, determines winner, advances them to next round.
    /// </summary>
    /// <summary>
    /// Saves scores for a 2-game playoff match plus optional tiebreaker.
    /// tiebreakerWinner: 1 = team1, 2 = team2, null = no tiebreaker played.
    /// Aggregate shown on bracket = sum of all game scores per team (tiebreaker adds 0 or 1).
    /// </summary>
    public static void SaveMatchScore(BocceDbContext db, int matchId,
        int t1g1, int t2g1,
        int t1g2, int t2g2,
        int? tiebreakerWinner)
    {
        var match = db.PlayoffMatches
            .Include(m => m.Team1)
            .Include(m => m.Team2)
            .FirstOrDefault(m => m.Id == matchId)
            ?? throw new InvalidOperationException("Match not found.");

        // Determine winner from games won (each game won by higher scorer)
        int t1wins = (t1g1 > t2g1 ? 1 : 0) + (t1g2 > t2g2 ? 1 : 0);
        int t2wins = (t2g1 > t1g1 ? 1 : 0) + (t2g2 > t1g2 ? 1 : 0);

        int? winnerId;
        if      (t1wins > t2wins)         winnerId = match.Team1Id;
        else if (t2wins > t1wins)         winnerId = match.Team2Id;
        else if (tiebreakerWinner == 1)   winnerId = match.Team1Id;
        else if (tiebreakerWinner == 2)   winnerId = match.Team2Id;
        else                              winnerId = null; // tied, no tiebreaker yet

        match.WinnerId  = winnerId;
        match.Status    = winnerId.HasValue ? "completed" : "scheduled";
        match.EnteredAt = DateTime.UtcNow;

        // Replace game rows
        db.PlayoffGames.Where(g => g.PlayoffMatchId == matchId).ExecuteDelete();

        db.PlayoffGames.Add(new PlayoffGame { PlayoffMatchId = matchId, GameNumber = 1,
            Team1Score = t1g1, Team2Score = t2g1, EnteredAt = DateTime.UtcNow });
        db.PlayoffGames.Add(new PlayoffGame { PlayoffMatchId = matchId, GameNumber = 2,
            Team1Score = t1g2, Team2Score = t2g2, EnteredAt = DateTime.UtcNow });

        // Tiebreaker: stored as GameNumber 3 with 1/0 to indicate winner
        if (tiebreakerWinner.HasValue)
            db.PlayoffGames.Add(new PlayoffGame { PlayoffMatchId = matchId, GameNumber = 3,
                Team1Score = tiebreakerWinner == 1 ? 1 : 0,
                Team2Score = tiebreakerWinner == 2 ? 1 : 0,
                EnteredAt  = DateTime.UtcNow });

        // Advance winner to next match
        if (winnerId.HasValue && match.NextMatchId.HasValue)
        {
            var next = db.PlayoffMatches.Find(match.NextMatchId.Value);
            if (next != null)
            {
                if (match.NextMatchIsTop) next.Team1Id = winnerId;
                else                     next.Team2Id = winnerId;
            }
        }

        db.SaveChanges();
    }

    // ── Court/time helpers ────────────────────────────────────────────────────

    private static int? AssignCourt(List<Court> courts, int slot) =>
        courts.Count > 0 ? courts[slot % courts.Count].Id : null;

    private static TimeOnly? AssignTime(
        List<RoundSchedule> schedule, int round, List<Court> courts, int slot,
        int fallbackMatchMins, IReadOnlyList<PlayoffDayParams> dayParams)
    {
        var rs = schedule.FirstOrDefault(s => s.Round == round);
        if (rs == null || rs.StartTime == TimeOnly.MinValue) return null;

        var day        = dayParams.FirstOrDefault(d => d.GameDate == rs.Date);
        int matchSlot  = day?.MatchLengthMins > 0 ? day.MatchLengthMins : fallbackMatchMins;
        int courtCount = Math.Max(1, courts.Count);
        int wave       = slot / courtCount;
        return rs.StartTime.AddMinutes(wave * matchSlot);
    }

    // ── Utilities ─────────────────────────────────────────────────────────────

    private static int NextPowerOfTwo(int n)
    {
        if (n <= 1) return 1;
        int p = 1;
        while (p < n) p <<= 1;
        return p;
    }
}

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

        // Validate clean bracket size: must be a power of 2 (no byes) or 3× a power of 2
        // (exactly one bye per bye-seed in every R2 match).
        // 20 or 28 teams cause some R2 games to be bye-vs-bye — unfair double advantage.
        if (!IsCleanBracketSize(teamCount))
            throw new InvalidOperationException(
                $"{teamCount} teams produces an unbalanced bracket where some teams receive " +
                $"a double-bye advantage. Valid playoff sizes: 4, 8, 12, 16, 24, 32, 48, 64. " +
                $"Please adjust Season → Teams in Playoffs.");

        // Playoffs always use the season's court selection and priority order.
        var courts = db.SeasonCourts
            .Where(sc => sc.SeasonId == seasonId && sc.Court.IsActive)
            .Include(sc => sc.Court)
            .OrderBy(sc => sc.SortOrder).ThenBy(sc => sc.Court.CourtNumber)
            .Select(sc => sc.Court)
            .ToList();

        if (courts.Count == 0)
            throw new InvalidOperationException(
                "No courts selected for this season. Please choose courts on the Season screen before generating the bracket.");

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
            int childCount  = GetGamesInRound(teamCount, r);
            if (childCount <= 0) childCount = 1;
            int parentCount = matchSlots[r + 1].Length;
            matchSlots[r]   = new PlayoffMatch[childCount];

            var roundRow = rounds.ContainsKey(r) ? rounds[r] : null;

            for (int slot = 0; slot < childCount; slot++)
            {
                int  parentSlot;
                bool isTopOfParent;

                if (childCount == parentCount)
                {
                    // Bye-entry transition (e.g. 12-team R1→R2): same number of games.
                    // Each R2 match already has a bye in the top slot; R1 winner fills bottom.
                    parentSlot    = slot;
                    isTopOfParent = false;
                }
                else
                {
                    // Normal doubling: 2 children per parent match.
                    parentSlot    = slot / 2;
                    isTopOfParent = slot % 2 == 0;
                }

                var parent = matchSlots[r + 1][parentSlot];

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

    // Bye ordering — top-to-bottom bracket slot assignment for N bye seeds.
    // Uses the standard seeded-bracket recursive doubling rule:
    //   Start [1,2]. Double: even-index pairs expand as (seed, complement);
    //   odd-index pairs expand as (complement, seed). Repeat until size≥byeCount.
    //
    // Results:
    //   4 byes → [1, 4, 3, 2]        (old code gave [1,3,4,2] — WRONG)
    //   8 byes → [1, 8, 5, 4, 3, 6, 7, 2]
    //
    // Guarantees: seeds 1 & 2 in opposite halves (Final only);
    //             seeds 1 & 4 in same half (SF worst-case).
    private static List<int> BuildByeOrder(int byeCount)
    {
        if (byeCount <= 0) return [];

        var slots = new List<int> { 1, 2 };
        int size  = 2;

        while (size < byeCount)
        {
            size *= 2;
            var expanded = new List<int>(size);
            for (int i = 0; i < slots.Count; i++)
            {
                int s          = slots[i];
                int complement = size + 1 - s;
                if (i % 2 == 0)          // even index: keep seed first
                { expanded.Add(s); expanded.Add(complement); }
                else                     // odd index: complement first
                { expanded.Add(complement); expanded.Add(s); }
            }
            slots = expanded;
        }

        return slots.Take(byeCount).ToList();
    }

    // Pair R1 seeds to match each bye opponent correctly.
    // Works for all team counts — previous formula (pairs[byeCount-s]) only
    // worked when pairs.Count == byeCount (e.g. 12 or 24 teams); crashed for
    // 20 teams (byeCount=12, pairs.Count=4) or any count where teamCount ≠ 3×byeCount.
    private static List<(int top, int bot)> BuildNonByePairs(int teamCount, int byeCount, List<int> byeOrder)
    {
        int lo = byeCount + 1;
        int hi = teamCount;
        int n  = (hi - lo + 1) / 2;   // number of R1 games
        if (n <= 0) return [];

        // pairs[0] = strongest (lowest seed numbers), pairs[n-1] = weakest
        var pairs = new List<(int, int)>(n);
        for (int i = 0; i < n; i++)
            pairs.Add((lo + i, hi - i));

        // Only the first n byes in byeOrder get R1 opponents.
        // Within those n byes, stronger bye (lower seed) → weaker pair; weaker bye → stronger pair.
        var byesWithR1 = byeOrder.Take(n).ToList();
        var sortedAsc  = byesWithR1.OrderBy(s => s).ToList();  // seed 1 = index 0 = strongest
        var pairMap    = new Dictionary<int, (int, int)>(n);
        for (int rank = 0; rank < n; rank++)
            pairMap[sortedAsc[rank]] = pairs[n - 1 - rank];   // rank 0 → weakest pair

        return byesWithR1.Select(s => pairMap[s]).ToList();
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

        // Winner = higher total points across both games (e.g. 12+5=17 vs 6+12=18 → Team2 wins).
        // Tie (both totals equal) requires a tiebreaker.
        int t1total = t1g1 + t1g2;
        int t2total = t2g1 + t2g2;

        int? winnerId;
        if      (t1total > t2total)       winnerId = match.Team1Id;
        else if (t2total > t1total)       winnerId = match.Team2Id;
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

    /// <summary>
    /// Clears a match's score/winner (e.g. entered in the wrong slot). If the winner had
    /// already advanced into the next match's team slot, that slot is cleared too — and if
    /// the next match had itself already been played using that team, it's reset first
    /// (cascading forward through the bracket) so no match is left referencing a result
    /// that no longer exists.
    /// </summary>
    public static void ResetMatchScore(BocceDbContext db, int matchId)
    {
        var match = db.PlayoffMatches.Find(matchId)
            ?? throw new InvalidOperationException("Match not found.");

        int? winnerId = match.WinnerId;

        db.PlayoffGames.Where(g => g.PlayoffMatchId == matchId).ExecuteDelete();

        match.WinnerId  = null;
        match.Status    = "scheduled";
        match.EnteredAt = null;

        if (winnerId.HasValue && match.NextMatchId.HasValue)
        {
            var next = db.PlayoffMatches.Find(match.NextMatchId.Value);
            if (next != null)
            {
                bool wasTop     = match.NextMatchIsTop;
                int? assignedId = wasTop ? next.Team1Id : next.Team2Id;

                if (assignedId == winnerId)
                {
                    if (next.Status == "completed")
                        ResetMatchScore(db, next.Id);

                    if (wasTop) next.Team1Id = null;
                    else        next.Team2Id = null;
                }
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

    // A "clean" bracket size produces exactly one bye per bye-seed in every R2 match:
    //   power of 2  (no byes)         → 4, 8, 16, 32, 64
    //   3 × power of 2  (clean byes)  → 12, 24, 48, 96
    // 20, 28, 40, etc. produce R2 games where two bye-seeds play each other (unfair).
    public static bool IsCleanBracketSize(int n)
    {
        if (n <= 0) return false;
        // Is n a power of 2?
        if ((n & (n - 1)) == 0) return true;
        // Is n/3 a power of 2?
        if (n % 3 == 0)
        {
            int m = n / 3;
            return (m & (m - 1)) == 0;
        }
        return false;
    }
}

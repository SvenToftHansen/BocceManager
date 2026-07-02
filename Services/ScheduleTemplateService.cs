using BocceManager.Data;
using BocceManager.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Services;

public static class ScheduleTemplateService
{
    public static readonly int[] ValidTeamCounts = [4, 6, 8, 10, 12, 14, 16, 18, 20];

    public static bool IsValidTeamCount(int teamCount) => teamCount >= 4 && teamCount % 2 == 0;

    /// <summary>
    /// Generates (or replaces) a schedule template for the given season and team count.
    /// Uses the circle-method round-robin and greedy court balancing.
    /// For 4-team templates, enforces a fixed pattern for weeks 1-3 for backward compatibility.
    /// </summary>
    public static ScheduleTemplate Generate(BocceDbContext db, int seasonId, int teamCount)
    {
        if (!IsValidTeamCount(teamCount))
            throw new ArgumentException($"Team count must be even and >= 4 (got {teamCount}).");

        var season = db.Seasons.Find(seasonId)
            ?? throw new InvalidOperationException("Season not found.");

        int weekCount = season.WeeksInSeason;
        if (weekCount <= 0)
            throw new InvalidOperationException(
                "Season must have Weeks in Season set before generating a template.");

        var courtIds = db.SeasonCourts
            .Where(sc => sc.SeasonId == seasonId && sc.Court.IsActive)
            .OrderBy(sc => sc.SortOrder).ThenBy(sc => sc.Court.CourtNumber)
            .Select(sc => sc.CourtId)
            .ToList();

        int courtsNeeded = teamCount / 2;
        if (courtIds.Count < courtsNeeded)
            throw new InvalidOperationException(
                $"Season needs at least {courtsNeeded} courts for a {teamCount}-team template " +
                $"(currently has {courtIds.Count} selected on the Season screen).");

        courtIds = courtIds.Take(courtsNeeded).ToList();

        // Remove existing template (cascade deletes weeks + matches)
        var existing = db.ScheduleTemplates
            .FirstOrDefault(t => t.SeasonId == seasonId && t.TeamCount == teamCount);
        if (existing != null)
            db.ScheduleTemplates.Remove(existing);

        var rounds = BuildRounds(teamCount);         // n-1 rounds per full cycle
        int cycleLen = teamCount - 1;

        var template = new ScheduleTemplate
        {
            SeasonId    = seasonId,
            TeamCount   = teamCount,
            WeekCount   = weekCount,
            GeneratedAt = DateTime.UtcNow,
            Weeks       = []
        };

        // court usage: how many times slot X has played on court Y
        var usage = new Dictionary<(int CourtId, char Slot), int>();

        // Special case for 4-team: fixed pattern for first 3 weeks for backward compatibility
        List<List<(char Slot1, char Slot2)>> initialRounds = [];
        if (teamCount == 4)
        {
            initialRounds = new List<List<(char Slot1, char Slot2)>>
            {
                new() { (Slot1: 'A', Slot2: 'B'), (Slot1: 'C', Slot2: 'D') },  // Week 1
                new() { (Slot1: 'A', Slot2: 'C'), (Slot1: 'B', Slot2: 'D') },  // Week 2
                new() { (Slot1: 'A', Slot2: 'D'), (Slot1: 'B', Slot2: 'C') }   // Week 3
            };
        }

        for (int week = 1; week <= weekCount; week++)
        {
            List<(char Slot1, char Slot2)> round;

            if (teamCount == 4 && week <= 3)
            {
                round = initialRounds[week - 1];
            }
            else if (teamCount == 4)
            {
                // Continue from round 0 (standard rotation) for weeks 4+
                round = rounds[(week - 4) % cycleLen];
            }
            else
            {
                round = rounds[(week - 1) % cycleLen];
            }

            var courts = AssignCourts(round, courtIds, usage);

            var templateWeek = new ScheduleTemplateWeek
            {
                WeekNumber = week,
                Matches    = []
            };

            for (int m = 0; m < round.Count; m++)
            {
                templateWeek.Matches.Add(new ScheduleTemplateMatch
                {
                    Slot1    = round[m].Slot1.ToString(),
                    Slot2    = round[m].Slot2.ToString(),
                    CourtId  = courts[m]
                });
            }

            template.Weeks.Add(templateWeek);
        }

        db.ScheduleTemplates.Add(template);
        db.SaveChanges();

        return template;
    }

    // ── Circle-method round-robin ──────────────────────────────────────────────

    private static List<List<(char Slot1, char Slot2)>> BuildRounds(int n)
    {
        // Seed: idx[k] = 2k, idx[n-1-k] = 2k+1  →  Round 1 = A vs B, C vs D, E vs F, G vs H
        int[] idx = new int[n];
        for (int k = 0; k < n / 2; k++)
        {
            idx[k]         = 2 * k;
            idx[n - 1 - k] = 2 * k + 1;
        }

        var rounds = new List<List<(char, char)>>();

        for (int r = 0; r < n - 1; r++)
        {
            var matches = new List<(char, char)>();
            for (int i = 0; i < n / 2; i++)
                matches.Add(((char)('A' + idx[i]), (char)('A' + idx[n - 1 - i])));
            rounds.Add(matches);

            // Rotate idx[1..n-1] right: last element → position 1
            int last = idx[n - 1];
            for (int j = n - 1; j > 1; j--)
                idx[j] = idx[j - 1];
            idx[1] = last;
        }

        return rounds;
    }

    // ── Greedy court balancing ─────────────────────────────────────────────────

    private static List<int> AssignCourts(
        List<(char Slot1, char Slot2)> matches,
        List<int> courtIds,
        Dictionary<(int CourtId, char Slot), int> usage)
    {
        var usedThisWeek = new HashSet<int>();
        var assignments  = new List<int>();

        foreach (var (s1, s2) in matches)
        {
            int bestCourt = -1;
            int bestScore = int.MaxValue;

            foreach (int courtId in courtIds)
            {
                if (usedThisWeek.Contains(courtId)) continue;
                int score = usage.GetValueOrDefault((courtId, s1))
                          + usage.GetValueOrDefault((courtId, s2));
                if (score < bestScore)
                {
                    bestScore = score;
                    bestCourt = courtId;
                }
            }

            if (bestCourt == -1)
                throw new InvalidOperationException("Not enough courts available for this round.");

            assignments.Add(bestCourt);
            usedThisWeek.Add(bestCourt);
            usage[(bestCourt, s1)] = usage.GetValueOrDefault((bestCourt, s1)) + 1;
            usage[(bestCourt, s2)] = usage.GetValueOrDefault((bestCourt, s2)) + 1;
        }

        return assignments;
    }

    // ── Load helpers for the UI ────────────────────────────────────────────────

    public static List<ScheduleTemplate> GetTemplatesForSeason(BocceDbContext db, int seasonId)
    {
        return db.ScheduleTemplates
            .Where(t => t.SeasonId == seasonId)
            .OrderBy(t => t.TeamCount)
            .ToList();
    }

    public static List<TemplateRow> GetTemplateRows(BocceDbContext db, int templateId)
    {
        return db.ScheduleTemplateWeeks
            .Where(w => w.TemplateId == templateId)
            .OrderBy(w => w.WeekNumber)
            .SelectMany(w => w.Matches
                .Select(m => new TemplateRow(
                    w.Id,
                    w.WeekNumber,
                    m.Id,
                    m.Slot1,
                    m.Slot2,
                    m.CourtId)))
            .ToList();
    }

    public static void UpdateMatchSlots(BocceDbContext db, int matchId, string slot1, string slot2)
    {
        var match = db.ScheduleTemplateMatches.Find(matchId)
            ?? throw new InvalidOperationException("Match not found.");
        match.Slot1 = slot1;
        match.Slot2 = slot2;
        db.SaveChanges();
    }

    public static void UpdateMatchCourt(BocceDbContext db, int matchId, int courtId)
    {
        var match = db.ScheduleTemplateMatches.Find(matchId)
            ?? throw new InvalidOperationException("Match not found.");
        match.CourtId = courtId;
        db.SaveChanges();
    }

    public static void DeleteTemplate(BocceDbContext db, int templateId)
    {
        var template = db.ScheduleTemplates.Find(templateId)
            ?? throw new InvalidOperationException("Template not found.");

        // Delete all division schedules that reference this template
        var divisionSchedules = db.ScheduleDivisions.Where(s => s.TemplateId == templateId).ToList();
        db.ScheduleDivisions.RemoveRange(divisionSchedules);

        db.ScheduleTemplates.Remove(template);
        db.SaveChanges();
    }

    public record TemplateRow(
        int  WeekId,
        int  WeekNumber,
        int  MatchId,
        string Slot1,
        string Slot2,
        int  CourtId);
}

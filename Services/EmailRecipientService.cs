using BocceManager.Data;
using BocceManager.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Services;

public enum EmailRecipientScope
{
    AllActivePlayers, PlayersOnTeams, Division, Team, SpareList, UnpaidPlayers, EmailList, Individual
}

public record EmailRecipient(int? PlayerId, string FirstName, string LastName, string Email, string? TeamName);

// Builds the recipient set for each of Compose's scopes, for merge-field personalization.
public static class EmailRecipientService
{
    // uniqueEmailOnly collapses two player rows sharing one email (e.g. spouses/partners entered
    // separately) down to a single send — but only when they're also associated with the same
    // team, since the same person can legitimately belong to more than one team and needs a
    // separate, team-specific send for each. Defaults on; callers can offer it as a toggle.
    public static List<EmailRecipient> GetRecipients(BocceDbContext db, EmailRecipientScope scope,
        IReadOnlyCollection<int> scopeIds, int? leagueId, int? seasonId, bool uniqueEmailOnly = true)
    {
        var unpaidIds = scope == EmailRecipientScope.UnpaidPlayers ? GetUnpaidPlayerIds(db, seasonId) : null;

        IQueryable<Player> players = scope switch
        {
            EmailRecipientScope.AllActivePlayers =>
                db.Players.Where(p => p.IsActive),
            EmailRecipientScope.PlayersOnTeams =>
                db.TeamPlayers.Where(tp => tp.IsActive && seasonId.HasValue && tp.Team.Division.SeasonId == seasonId.Value).Select(tp => tp.Player),
            EmailRecipientScope.Division =>
                db.TeamPlayers.Where(tp => tp.IsActive && scopeIds.Contains(tp.Team.DivisionId)).Select(tp => tp.Player),
            EmailRecipientScope.Team =>
                db.TeamPlayers.Where(tp => tp.IsActive && scopeIds.Contains(tp.TeamId)).Select(tp => tp.Player),
            EmailRecipientScope.SpareList =>
                db.SpareLists.Where(s => s.IsActive && leagueId.HasValue && s.LeagueId == leagueId.Value).Select(s => s.Player),
            EmailRecipientScope.UnpaidPlayers =>
                db.Players.Where(p => unpaidIds!.Contains(p.Id)),
            EmailRecipientScope.EmailList =>
                db.EmailListMembers.Where(m => m.IsActive && scopeIds.Contains(m.EmailListId)).Select(m => m.Player),
            EmailRecipientScope.Individual =>
                db.Players.Where(p => scopeIds.Contains(p.Id)),
            _ => Enumerable.Empty<Player>().AsQueryable()
        };

        var distinctPlayers = players
            .Where(p => p.IsActive && p.Email != null && p.Email != "")
            .ToList()
            .GroupBy(p => p.Id)
            .Select(g => g.First())
            .ToList();

        var playerIds = distinctPlayers.Select(p => p.Id).ToList();

        // Best-effort team name for {TeamName} merge field — first active team in the current season.
        var teamNames = seasonId.HasValue
            ? db.TeamPlayers
                .Where(tp => tp.IsActive && tp.Team.Division.SeasonId == seasonId.Value && playerIds.Contains(tp.PlayerId))
                .Select(tp => new { tp.PlayerId, TeamName = tp.Team.DisplayName ?? tp.Team.SystemName })
                .ToList()
                .GroupBy(x => x.PlayerId)
                .ToDictionary(g => g.Key, g => g.First().TeamName)
            : [];

        var recipients = distinctPlayers
            .Select(p => new EmailRecipient(p.Id, p.FirstName, p.LastName, p.Email!, teamNames.GetValueOrDefault(p.Id)))
            .ToList();

        if (!uniqueEmailOnly) return recipients;

        return recipients
            .GroupBy(r => (Email: r.Email.Trim().ToLowerInvariant(), Team: r.TeamName ?? ""))
            .Select(g =>
            {
                var first = g.First();
                // Two people sharing one email/team (e.g. spouses/partners) collapse to a single
                // send — combine their first names ("John & Jane") instead of silently keeping
                // only whichever player row happened to come first.
                return g.Count() == 1 ? first : first with { FirstName = CombineFirstNames(g.Select(r => r.FirstName)) };
            })
            .ToList();
    }

    private static string CombineFirstNames(IEnumerable<string> firstNames)
    {
        var names = firstNames.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return names.Count switch
        {
            0 => "",
            1 => names[0],
            _ => string.Join(", ", names.Take(names.Count - 1)) + " & " + names[^1]
        };
    }

    // Same "unpaid" definition UnpaidFeesReportService uses: an initiation fee owing (league-wide)
    // or a current-season fee owing, expressed as a player-ID set instead of print rows.
    private static HashSet<int> GetUnpaidPlayerIds(BocceDbContext db, int? seasonId)
    {
        var unpaidInitiation = db.InitiationFees
            .Where(f => f.AmountPaid < f.AmountOwing)
            .Select(f => f.PlayerId);

        var unpaidSeason = seasonId.HasValue
            ? db.SeasonFees.Where(f => f.SeasonId == seasonId.Value && f.AmountPaid < f.AmountOwing).Select(f => f.PlayerId)
            : Enumerable.Empty<int>().AsQueryable();

        return unpaidInitiation.Concat(unpaidSeason).ToHashSet();
    }
}

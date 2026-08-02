using BocceManager.Data;
using BocceManager.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Services;

public enum EmailRecipientScope { Individual, Team, Division, EmailList, AllMembers }

public record EmailRecipient(int PlayerId, string FirstName, string LastName, string Email, string? TeamName);

// Builds the recipient set for each of Compose's scopes (individuals, teams, divisions,
// email lists, or all members of the current season), for merge-field personalization.
public static class EmailRecipientService
{
    public static List<EmailRecipient> GetRecipients(BocceDbContext db, EmailRecipientScope scope, int? scopeId, int seasonId)
    {
        IQueryable<Player> players = scope switch
        {
            EmailRecipientScope.Individual =>
                db.Players.Where(p => p.Id == scopeId),
            EmailRecipientScope.Team =>
                db.TeamPlayers.Where(tp => tp.TeamId == scopeId && tp.IsActive).Select(tp => tp.Player),
            EmailRecipientScope.Division =>
                db.TeamPlayers.Where(tp => tp.Team.DivisionId == scopeId && tp.IsActive).Select(tp => tp.Player),
            EmailRecipientScope.EmailList =>
                db.EmailListMembers.Where(m => m.EmailListId == scopeId && m.IsActive).Select(m => m.Player),
            EmailRecipientScope.AllMembers =>
                db.TeamPlayers.Where(tp => tp.IsActive && tp.Team.Division.SeasonId == seasonId).Select(tp => tp.Player),
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
        var teamNames = db.TeamPlayers
            .Where(tp => tp.IsActive && tp.Team.Division.SeasonId == seasonId && playerIds.Contains(tp.PlayerId))
            .Select(tp => new { tp.PlayerId, TeamName = tp.Team.DisplayName ?? tp.Team.SystemName })
            .ToList()
            .GroupBy(x => x.PlayerId)
            .ToDictionary(g => g.Key, g => g.First().TeamName);

        return distinctPlayers
            .Select(p => new EmailRecipient(p.Id, p.FirstName, p.LastName, p.Email!, teamNames.GetValueOrDefault(p.Id)))
            .ToList();
    }
}

using BocceManager.Data;

namespace BocceManager.Services;

// Keeps captain designation in sync with team roster order.
// Player #1 (lowest PlayerNumber) on a team is always its captain.
public static class TeamRosterService
{
    public static void SyncCaptain(BocceDbContext db, int teamId)
    {
        var team = db.Teams.Find(teamId);
        if (team == null) return;

        var tps = db.TeamPlayers.Where(tp => tp.TeamId == teamId).OrderBy(tp => tp.PlayerNumber).ToList();
        int? captainId = tps.Count > 0 ? tps[0].PlayerId : null;

        foreach (var tp in tps)
            tp.Role = tp.PlayerId == captainId ? "captain" : "player";

        if (captainId.HasValue)
        {
            team.CaptainPlayerId = captainId;
            var captain = db.Players.Find(captainId.Value);
            if (captain != null)
                team.DisplayName = $"{team.TeamLetter}-{captain.LastName}";
        }
        else
        {
            team.CaptainPlayerId = null;
            team.DisplayName     = null;
        }
        db.SaveChanges();
    }
}

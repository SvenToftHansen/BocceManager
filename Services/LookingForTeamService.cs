using BocceManager.Data;

namespace BocceManager.Services;

public static class LookingForTeamService
{
    // Deletes assigned (inactive) LFT records in bulk, optionally scoped.
    public static int DeleteAssigned(BocceDbContext db, int? leagueId = null, int? seasonId = null)
    {
        var query = db.LookingForTeams.Where(l => l.TeamId != null);

        if (leagueId.HasValue)
            query = query.Where(l => l.LeagueId == leagueId.Value);

        if (seasonId.HasValue)
            query = query.Where(l => l.Team != null && l.Team.Division.SeasonId == seasonId.Value);

        var rows = query.ToList();
        if (rows.Count == 0) return 0;

        db.LookingForTeams.RemoveRange(rows);
        db.SaveChanges();
        return rows.Count;
    }

    // Deletes user-selected LFT records by primary key.
    public static int DeleteByIds(BocceDbContext db, IEnumerable<int> ids)
    {
        var idSet = ids.Distinct().ToList();
        if (idSet.Count == 0) return 0;

        var rows = db.LookingForTeams.Where(l => idSet.Contains(l.Id)).ToList();
        if (rows.Count == 0) return 0;

        db.LookingForTeams.RemoveRange(rows);
        db.SaveChanges();
        return rows.Count;
    }
}

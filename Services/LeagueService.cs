using BocceManager.Data;
using BocceManager.Data.Entities;

namespace BocceManager.Services;

public sealed record SeasonImpact(string Name, int Divisions, int Teams);

public sealed record CascadeImpact(
    string LeagueName,
    List<SeasonImpact> Seasons,
    int TotalDivisions,
    int TotalTeams,
    int SpareListCount,
    int OfficialCount,
    int AnnouncementCount,
    int EmailListCount,
    int ParameterCount);

public static class LeagueService
{
    /// <summary>
    /// Returns a summary of every record that would be deleted if this league is deleted.
    /// Used to populate the confirmation dialog before executing the cascade.
    /// </summary>
    public static CascadeImpact ComputeDeleteImpact(BocceDbContext db, int leagueId)
    {
        var league = db.Leagues.Find(leagueId)
            ?? throw new InvalidOperationException($"League {leagueId} not found.");

        var seasons = db.Seasons.Where(s => s.LeagueId == leagueId).ToList();

        var seasonImpacts = seasons.Select(s =>
        {
            int divs  = db.Divisions.Count(d => d.SeasonId == s.Id);
            int teams = db.Teams.Count(t => t.Division.SeasonId == s.Id);
            return new SeasonImpact(s.Name, divs, teams);
        }).ToList();

        return new CascadeImpact(
            LeagueName:        league.Name,
            Seasons:           seasonImpacts,
            TotalDivisions:    seasonImpacts.Sum(s => s.Divisions),
            TotalTeams:        seasonImpacts.Sum(s => s.Teams),
            SpareListCount:    db.SpareLists.Count(sl => sl.LeagueId == leagueId),
            OfficialCount:     db.LeagueOfficials.Count(o => o.LeagueId == leagueId),
            AnnouncementCount: db.Announcements.Count(a => a.LeagueId == leagueId),
            EmailListCount:    db.EmailLists.Count(e => e.LeagueId == leagueId),
            ParameterCount:    db.LeagueParameters.Count(p => p.LeagueId == leagueId));
    }

    /// <summary>
    /// Deletes the league and every record that belongs to it, in FK-safe order.
    /// Players are never deleted — they are app-level, not league-level.
    /// Caller is responsible for confirming with the user beforehand.
    /// </summary>
    public static void ExecuteCascadeDelete(BocceDbContext db, int leagueId)
    {
        var seasonIds = db.Seasons.Where(s => s.LeagueId == leagueId)
                                  .Select(s => s.Id).ToList();

        foreach (var seasonId in seasonIds)
        {
            var divisionIds = db.Divisions.Where(d => d.SeasonId == seasonId)
                                          .Select(d => d.Id).ToList();

            foreach (var divisionId in divisionIds)
            {
                var teamIds = db.Teams.Where(t => t.DivisionId == divisionId)
                                      .Select(t => t.Id).ToList();

                // Clear captain FK before deleting teams — it is configured as Restrict
                if (teamIds.Count > 0)
                {
                    foreach (var t in db.Teams.Where(t => t.DivisionId == divisionId))
                        t.CaptainPlayerId = null;
                    db.SaveChanges();
                }

                // Schedule data: games → match results → matches → weeks
                var weekIds = db.ScheduleWeeks.Where(w => w.DivisionId == divisionId)
                                              .Select(w => w.Id).ToList();
                foreach (var weekId in weekIds)
                {
                    var matchIds = db.Matches.Where(m => m.ScheduleWeekId == weekId)
                                             .Select(m => m.Id).ToList();
                    foreach (var matchId in matchIds)
                    {
                        db.Games.RemoveRange(db.Games.Where(g => g.MatchId == matchId));
                        db.MatchTeamResults.RemoveRange(
                            db.MatchTeamResults.Where(r => r.MatchId == matchId));
                    }
                    db.Matches.RemoveRange(db.Matches.Where(m => m.ScheduleWeekId == weekId));
                }
                db.ScheduleWeeks.RemoveRange(
                    db.ScheduleWeeks.Where(w => w.DivisionId == divisionId));

                foreach (var teamId in teamIds)
                {
                    db.TeamPlayers.RemoveRange(db.TeamPlayers.Where(tp => tp.TeamId == teamId));
                    db.TeamParameters.RemoveRange(db.TeamParameters.Where(tp => tp.TeamId == teamId));
                    db.TeamStandings.RemoveRange(db.TeamStandings.Where(ts => ts.TeamId == teamId));
                }
                db.Teams.RemoveRange(db.Teams.Where(t => t.DivisionId == divisionId));
                db.DivisionParameters.RemoveRange(
                    db.DivisionParameters.Where(p => p.DivisionId == divisionId));
            }
            db.Divisions.RemoveRange(db.Divisions.Where(d => d.SeasonId == seasonId));

            // Playoff data
            var playoffMatchIds = db.PlayoffMatches.Where(pm => pm.SeasonId == seasonId)
                                                   .Select(pm => pm.Id).ToList();
            foreach (var pmId in playoffMatchIds)
                db.PlayoffGames.RemoveRange(
                    db.PlayoffGames.Where(pg => pg.PlayoffMatchId == pmId));
            db.PlayoffMatches.RemoveRange(db.PlayoffMatches.Where(pm => pm.SeasonId == seasonId));
            db.PlayoffRounds.RemoveRange(db.PlayoffRounds.Where(pr => pr.SeasonId == seasonId));

            db.SeasonCourts.RemoveRange(db.SeasonCourts.Where(sc => sc.SeasonId == seasonId));
            db.SeasonParameters.RemoveRange(
                db.SeasonParameters.Where(p => p.SeasonId == seasonId));
        }
        db.Seasons.RemoveRange(db.Seasons.Where(s => s.LeagueId == leagueId));

        // Spare requests via spare lists owned by this league
        var spareListIds = db.SpareLists.Where(sl => sl.LeagueId == leagueId)
                                        .Select(sl => sl.Id).ToList();
        foreach (var slId in spareListIds)
        {
            db.SpareRequests.RemoveRange(db.SpareRequests.Where(sr => sr.SpareListId == slId));
            db.SpareListPlayers.RemoveRange(
                db.SpareListPlayers.Where(slp => slp.SpareListId == slId));
        }
        db.SpareLists.RemoveRange(db.SpareLists.Where(sl => sl.LeagueId == leagueId));

        db.LeagueOfficials.RemoveRange(db.LeagueOfficials.Where(o => o.LeagueId == leagueId));
        db.Announcements.RemoveRange(db.Announcements.Where(a => a.LeagueId == leagueId));

        var emailListIds = db.EmailLists.Where(el => el.LeagueId == leagueId)
                                        .Select(el => el.Id).ToList();
        foreach (var elId in emailListIds)
            db.EmailListMembers.RemoveRange(
                db.EmailListMembers.Where(elm => elm.EmailListId == elId));
        db.EmailLists.RemoveRange(db.EmailLists.Where(el => el.LeagueId == leagueId));

        // Email logs have optional LeagueId — nullify rather than delete
        foreach (var log in db.EmailLogs.Where(el => el.LeagueId == leagueId))
            log.LeagueId = null;

        db.LeagueParameters.RemoveRange(db.LeagueParameters.Where(p => p.LeagueId == leagueId));
        db.Leagues.Remove(db.Leagues.Find(leagueId)!);
        db.SaveChanges();
    }
}

using BocceManager.Data;
using BocceManager.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Services;

public static class TeamApplicantService
{
    public record LftPoolEntry(
        int LftId,
        int PlayerId,
        string FullName,
        string? Phone,
        string? Email,
        string? Notes,
        DateOnly? RegisteredDate);

    public static List<TeamApplicant> GetPending(BocceDbContext db, int leagueId, int seasonId)
        => db.TeamApplicants
            .Include(a => a.Members)
            .Include(a => a.PreferredDivision)
            .Where(a => a.LeagueId == leagueId && a.SeasonId == seasonId && a.Status == "Pending")
            .OrderBy(a => a.GroupName)
            .ToList();

    public static List<TeamApplicant> GetAll(BocceDbContext db, int leagueId, int seasonId)
        => db.TeamApplicants
            .Include(a => a.Members)
            .Include(a => a.PreferredDivision)
            .Include(a => a.PlacedTeam)
            .Where(a => a.LeagueId == leagueId && a.SeasonId == seasonId)
            .OrderBy(a => a.Status).ThenBy(a => a.GroupName)
            .ToList();

    public static List<LftPoolEntry> GetLftPool(BocceDbContext db, int leagueId, int seasonId)
        => db.LookingForTeams
            .Include(l => l.Player)
            .Where(l => l.LeagueId == leagueId && l.SeasonId == seasonId && l.TeamId == null)
            .OrderBy(l => l.Player.LastName).ThenBy(l => l.Player.FirstName)
            .AsEnumerable()
            .Select(l => new LftPoolEntry(
                l.Id,
                l.PlayerId,
                $"{l.Player.LastName}, {l.Player.FirstName}".Trim().TrimStart(',').Trim(),
                l.Player.Phone,
                l.Player.Email,
                l.Notes,
                l.RegisteredDate))
            .ToList();

    // Places a pending applicant group into a division as a new team.
    // Creates Player records for any new members, creates the Team, adds all players.
    // Returns (success, message, team). All done in a single transaction.
    public static (bool Success, string Message, Team? Team) PlaceGroup(
        BocceDbContext db, int teamApplicantId, int divisionId)
    {
        try
        {
            var applicant = db.TeamApplicants
                .Include(a => a.Members)
                .Include(a => a.Season)
                .FirstOrDefault(a => a.Id == teamApplicantId);

            if (applicant == null)
                return (false, "Applicant group not found.", null);
            if (applicant.Status != "Pending")
                return (false, $"Group is already {applicant.Status}.", null);

            var div = db.Divisions.Find(divisionId);
            if (div == null)
                return (false, "Division not found.", null);

            // Assign next available team letter
            var existing = db.Teams.Where(t => t.DivisionId == divisionId).OrderBy(t => t.TeamLetter).ToList();
            char nextLetter = existing.Count > 0
                ? (char)(existing.Max(t => string.IsNullOrEmpty(t.TeamLetter) ? 'A' - 1 : t.TeamLetter[0]) + 1)
                : 'A';

            var systemName = $"{nextLetter}-{div.ShortName}";
            var team = new Team
            {
                DivisionId  = divisionId,
                TeamLetter  = nextLetter.ToString(),
                SystemName  = systemName,
                DisplayName = applicant.GroupName,
                SortOrder   = $"{div.SortName}-{nextLetter}",
                IsActive    = true
            };
            db.Teams.Add(team);
            db.SaveChanges(); // get team.Id

            foreach (var member in applicant.Members)
            {
                int playerId;

                if (member.PlayerId.HasValue)
                {
                    playerId = member.PlayerId.Value;
                }
                else
                {
                    // Create new Player record
                    var newPlayer = new Player
                    {
                        FirstName = member.FirstName,
                        LastName  = member.LastName,
                        Email     = member.Email,
                        Phone     = member.Phone,
                        IsActive  = true
                    };
                    db.Players.Add(newPlayer);
                    db.SaveChanges();
                    member.CreatedPlayerId = newPlayer.Id;
                    playerId = newPlayer.Id;
                }

                db.TeamPlayers.Add(new TeamPlayer
                {
                    TeamId     = team.Id,
                    PlayerId   = playerId,
                    Role       = "player",
                    JoinedDate = DateOnly.FromDateTime(DateTime.Today)
                });

                // Clear any LFT entry for this player in this season
                var lft = db.LookingForTeams.FirstOrDefault(l =>
                    l.PlayerId == playerId &&
                    l.LeagueId == applicant.LeagueId &&
                    l.SeasonId == applicant.SeasonId);
                if (lft != null)
                    lft.TeamId = team.Id;

                // Ensure season fee
                FeeService.EnsureSeasonFee(db, playerId, applicant.SeasonId);
            }

            applicant.Status      = "Placed";
            applicant.PlacedTeamId = team.Id;

            db.SaveChanges();

            AppLogger.Info("Placed applicant group {GroupName} as team {SystemName} in division {DivisionId}",
                applicant.GroupName, team.SystemName, divisionId);

            return (true, $"Group placed as Team {nextLetter} ({team.DisplayName}).", team);
        }
        catch (Exception ex)
        {
            AppLogger.Error(ex, "Failed to place applicant group {Id}", teamApplicantId);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public static void WithdrawGroup(BocceDbContext db, int teamApplicantId)
    {
        var applicant = db.TeamApplicants.Find(teamApplicantId);
        if (applicant == null) return;
        applicant.Status = "Withdrawn";
        db.SaveChanges();
        AppLogger.Info("Withdrew applicant group {Id} ({GroupName})", teamApplicantId, applicant.GroupName);
    }

    // Places a single LFT player onto an existing team.
    public static (bool Success, string Message) PlaceLftPlayer(
        BocceDbContext db, int lftId, int teamId)
    {
        try
        {
            var lft = db.LookingForTeams.Include(l => l.Player).FirstOrDefault(l => l.Id == lftId);
            if (lft == null) return (false, "LFT entry not found.");
            if (lft.TeamId.HasValue) return (false, "Player has already been placed.");

            var team = db.Teams.Include(t => t.Division).ThenInclude(d => d.Season).FirstOrDefault(t => t.Id == teamId);
            if (team == null) return (false, "Team not found.");

            bool alreadyOnTeam = db.TeamPlayers.Any(tp => tp.TeamId == teamId && tp.PlayerId == lft.PlayerId);
            if (alreadyOnTeam) return (false, "Player is already on that team.");

            db.TeamPlayers.Add(new TeamPlayer
            {
                TeamId     = teamId,
                PlayerId   = lft.PlayerId,
                Role       = "player",
                JoinedDate = DateOnly.FromDateTime(DateTime.Today)
            });

            lft.TeamId = teamId;

            FeeService.EnsureSeasonFee(db, lft.PlayerId, team.Division.SeasonId);

            db.SaveChanges();

            AppLogger.Info("Placed LFT player {PlayerId} onto team {TeamId}", lft.PlayerId, teamId);
            return (true, $"{lft.Player.FullName} added to {team.SystemName}.");
        }
        catch (Exception ex)
        {
            AppLogger.Error(ex, "Failed to place LFT player {LftId}", lftId);
            return (false, $"Error: {ex.Message}");
        }
    }
}

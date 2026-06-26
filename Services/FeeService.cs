using BocceManager.Data;
using BocceManager.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Services;

public static class FeeService
{
    // ── Amount lookups ──────────────────────────────────────────────────────

    public static decimal GetInitiationFeeAmount(BocceDbContext db)
    {
        var val = db.AppParameters.FirstOrDefault(p => p.Key == "InitiationFeeAmount")?.Value;
        return decimal.TryParse(val, out var amount) ? amount : 0m;
    }

    public static decimal GetSeasonFeeAmount(BocceDbContext db, int seasonId)
    {
        var val = db.SeasonParameters
            .FirstOrDefault(p => p.SeasonId == seasonId && p.Key == "SeasonFeeAmount")?.Value;
        return decimal.TryParse(val, out var amount) ? amount : 0m;
    }

    // ── Current season ─────────────────────────────────────────────────────

    /// <summary>
    /// Returns the current season for a league, or null if none is marked current.
    /// </summary>
    public static Season? GetCurrentSeason(BocceDbContext db, int leagueId)
        => db.Seasons.FirstOrDefault(s => s.LeagueId == leagueId && s.IsCurrent);

    // ── Initiation fee ─────────────────────────────────────────────────────

    /// <summary>
    /// Creates an InitiationFee for a player if one does not already exist.
    /// Amount is read from the AppParameter "InitiationFeeAmount".
    /// </summary>
    public static void EnsureInitiationFee(BocceDbContext db, int playerId)
    {
        if (db.InitiationFees.Any(f => f.PlayerId == playerId)) return;

        db.InitiationFees.Add(new InitiationFee
        {
            PlayerId   = playerId,
            AmountOwing = GetInitiationFeeAmount(db),
            CreatedAt  = DateTime.UtcNow
        });
        db.SaveChanges();
    }

    // ── Seasonal fee ───────────────────────────────────────────────────────

    /// <summary>
    /// Creates a SeasonFee for a player/season if one does not already exist (rule g).
    /// Amount is read from the SeasonParameter "SeasonFeeAmount" for that season.
    /// </summary>
    public static void EnsureSeasonFee(BocceDbContext db, int playerId, int seasonId)
    {
        if (db.SeasonFees.Any(f => f.PlayerId == playerId && f.SeasonId == seasonId)) return;

        db.SeasonFees.Add(new SeasonFee
        {
            PlayerId    = playerId,
            SeasonId    = seasonId,
            AmountOwing = GetSeasonFeeAmount(db, seasonId),
            CreatedAt   = DateTime.UtcNow
        });
        db.SaveChanges();
    }

    /// <summary>
    /// Rescinds all unpaid seasonal fees for a player in a league.
    /// Only rescinded if the player is off BOTH the spare list AND all teams in the league (rules d, f, h).
    /// </summary>
    public static void RescindUnpaidSeasonFees(BocceDbContext db, int playerId, int leagueId)
    {
        var onSpareList = db.SpareLists.Any(s => s.LeagueId == leagueId && s.PlayerId == playerId);
        if (onSpareList) return;

        var onAnyTeam = db.TeamPlayers.Any(tp =>
            tp.PlayerId == playerId &&
            tp.Team.Division.Season.LeagueId == leagueId);
        if (onAnyTeam) return;

        var seasonIds = db.Seasons
            .Where(s => s.LeagueId == leagueId)
            .Select(s => s.Id)
            .ToList();

        var unpaidFees = db.SeasonFees
            .Where(f => f.PlayerId == playerId && seasonIds.Contains(f.SeasonId) && f.AmountPaid == 0)
            .ToList();

        if (unpaidFees.Count == 0) return;

        db.SeasonFees.RemoveRange(unpaidFees);
        db.SaveChanges();
    }

    // ── Clone season helpers ───────────────────────────────────────────────

    /// <summary>
    /// Copies the SeasonFeeAmount parameter from the source season to the new season.
    /// Called when Clone Season completes.
    /// </summary>
    public static void CopySeasonFeeParameter(BocceDbContext db, int sourceSeasonId, int newSeasonId)
    {
        var source = db.SeasonParameters
            .FirstOrDefault(p => p.SeasonId == sourceSeasonId && p.Key == "SeasonFeeAmount");

        string value = source?.Value ?? "10.00";

        var existing = db.SeasonParameters
            .FirstOrDefault(p => p.SeasonId == newSeasonId && p.Key == "SeasonFeeAmount");

        if (existing == null)
        {
            db.SeasonParameters.Add(new SeasonParameter
            {
                SeasonId    = newSeasonId,
                Key         = "SeasonFeeAmount",
                Value       = value,
                Description = "Seasonal play fee for this season",
                IsActive    = true
            });
        }
        else
        {
            existing.Value = value;
        }

        db.SaveChanges();
    }

    /// <summary>
    /// Assigns a seasonal fee to every active spare list player for the given league/season.
    /// Called when Clone Season completes.
    /// </summary>
    public static void AssignSeasonFeesForSpareList(BocceDbContext db, int newSeasonId, int leagueId)
    {
        var sparePlayers = db.SpareLists
            .Where(s => s.LeagueId == leagueId && s.IsActive)
            .Select(s => s.PlayerId)
            .ToList();

        foreach (var playerId in sparePlayers)
            EnsureSeasonFee(db, playerId, newSeasonId);
    }

    /// <summary>
    /// Assigns a seasonal fee to every active team player in the new season.
    /// Called when Clone Season completes.
    /// </summary>
    public static void AssignSeasonFeesForTeamPlayers(BocceDbContext db, int newSeasonId)
    {
        var teamPlayerIds = db.TeamPlayers
            .Where(tp => tp.Team.Division.SeasonId == newSeasonId && tp.IsActive)
            .Select(tp => tp.PlayerId)
            .Distinct()
            .ToList();

        foreach (var playerId in teamPlayerIds)
            EnsureSeasonFee(db, playerId, newSeasonId);
    }
}

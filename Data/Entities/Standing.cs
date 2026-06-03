namespace BocceManager.Data.Entities;

// Sort: standings_points DESC, then plus_minus DESC
public class TeamStanding
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public int DivisionId { get; set; }
    public int Wins { get; set; } = 0;
    public int Losses { get; set; } = 0;
    public int Ties { get; set; } = 0;
    public int NoShows { get; set; } = 0;
    public int StandingsPoints { get; set; } = 0;
    public int PointsFor { get; set; } = 0;
    public int PointsAgainst { get; set; } = 0;
    public int PlusMinus { get; set; } = 0;
    public int? DivisionRank { get; set; }
    public int? PlayoffSeed { get; set; }

    public Team Team { get; set; } = null!;
    public Division Division { get; set; } = null!;
}

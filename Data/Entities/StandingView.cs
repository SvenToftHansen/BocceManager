namespace BocceManager.Data.Entities;

// Read-only view entity — aggregated from the Scoring view.
// Sort key: StandingsPoints DESC → PlusMinus DESC → Wins DESC.
// DivisionRank: DENSE_RANK within division (tied teams share a rank — for display).
// DivisionSeed: ROW_NUMBER within division (always unique — for bracket assignment).
// SeasonSeed:   rank across all divisions; respects Season.FirstPlaceGuaranteed.
public class StandingView
{
    public int DivisionRank { get; set; }
    public int DivisionSeed { get; set; }
    public int SeasonSeed { get; set; }
    public int TeamId { get; set; }
    public string TeamName { get; set; } = "";
    public int DivisionId { get; set; }
    public int SeasonId { get; set; }
    public int LeagueId { get; set; }
    public int GamesPlayed { get; set; }    // rows from Scoring (games in games_mode, matches in match_score_mode)
    public int MatchesPlayed { get; set; }  // distinct ScheduleDivisionsId
    public int Wins { get; set; }
    public int Ties { get; set; }
    public int Losses { get; set; }         // non-forfeit losses only
    public int Forfeits { get; set; }       // forfeit losses
    public int StandingsPoints { get; set; }
    public int PlusMinus { get; set; }
    public int PointsFor { get; set; }
    public int PointsAgainst { get; set; }
    // Head-to-head stats against tied opponents only (used as tiebreaker after PlusMinus/Wins)
    public int H2HPlusMinus { get; set; }
    public int H2HWins { get; set; }
}

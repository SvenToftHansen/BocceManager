namespace BocceManager.Data.Entities;

public class Scoring
{
    public long Id { get; set; }
    public int ScheduleDivisionsId { get; set; }
    public int SeasonId { get; set; }
    public int LeagueId { get; set; }
    public int DivisionId { get; set; }
    public int WeekId { get; set; }
    public int TeamId { get; set; }
    public string TeamName { get; set; } = "";
    public int OpposingTeamId { get; set; }
    public string OpposingTeamName { get; set; } = "";
    public int? GameNumber { get; set; }   // 1 or 2 in games_mode; null in match_score_mode
    public int PointsFor { get; set; }
    public int PointsAgainst { get; set; }
    public int PlusMinus { get; set; }
    public bool IsWin { get; set; }
    public bool IsTie { get; set; }
    public bool IsLoss { get; set; }
    public bool IsForfeit { get; set; }    // this team forfeited
    public bool ByForfeit { get; set; }    // this team won because opponent forfeited

    // Standings points earned for this row (apply season PointsFor* params)
    public int WinPTS { get; set; }
    public int TiePTS { get; set; }
    public int LossPTS { get; set; }
    public int ForfeitPoints { get; set; }
}

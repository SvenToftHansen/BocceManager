namespace BocceManager.Data.Entities;

public class ScheduleWeek
{
    public int Id { get; set; }
    public int DivisionId { get; set; }
    public int WeekNumber { get; set; }
    public DateOnly? MatchDate { get; set; }

    public Division Division { get; set; } = null!;
    public ICollection<BocceMatch> Matches { get; set; } = [];
}

// Uses Team1/Team2 — no home/away distinction
public class BocceMatch
{
    public int Id { get; set; }
    public int ScheduleWeekId { get; set; }
    public int Team1Id { get; set; }
    public int Team2Id { get; set; }
    public int? CourtId { get; set; }
    public DateOnly? ScheduledDate { get; set; }
    public TimeOnly? ScheduledTime { get; set; }
    public int GamesPlayed { get; set; } = 0;
    // scheduled | completed | no_show_team1 | no_show_team2 | postponed | bye
    public string Status { get; set; } = "scheduled";
    public string? EnteredBy { get; set; }
    public DateTime? EnteredAt { get; set; }

    public ScheduleWeek ScheduleWeek { get; set; } = null!;
    public Team Team1 { get; set; } = null!;
    public Team Team2 { get; set; } = null!;
    public Court? Court { get; set; }
    public ICollection<Game> Games { get; set; } = [];
    public ICollection<MatchTeamResult> Results { get; set; } = [];
}

public class MatchTeamResult
{
    public int Id { get; set; }
    public int MatchId { get; set; }
    public int TeamId { get; set; }
    public int Wins { get; set; } = 0;
    public int Losses { get; set; } = 0;
    public int Ties { get; set; } = 0;
    public int NoShows { get; set; } = 0;
    public int StandingsPoints { get; set; } = 0;
    public int PointsFor { get; set; } = 0;
    public int PointsAgainst { get; set; } = 0;
    public int PlusMinus { get; set; } = 0;

    public BocceMatch Match { get; set; } = null!;
    public Team Team { get; set; } = null!;
}

// Individual game scores within a match (final totals only, not end-by-end)
public class Game
{
    public int Id { get; set; }
    public int MatchId { get; set; }
    public int GameNumber { get; set; }
    public int Team1Score { get; set; } = 0;
    public int Team2Score { get; set; } = 0;
    // TRUE when auto-generated for a no-show forfeit
    public bool IsForfeit { get; set; } = false;
    public string? EnteredBy { get; set; }
    public DateTime? EnteredAt { get; set; }

    public BocceMatch Match { get; set; } = null!;
}

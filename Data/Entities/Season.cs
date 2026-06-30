namespace BocceManager.Data.Entities;

public class Season
{
    public int Id { get; set; }
    public int LeagueId { get; set; }
    public string Name { get; set; } = "";
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public int GamesPerSeason { get; set; }

    public int? PlayersPerTeamMinimum { get; set; }
    public int? PlayersPerTeamMaximum { get; set; }

    public int PointsForWin { get; set; } = 2;
    public int PointsForTie { get; set; } = 1;
    public int PointsForLoss { get; set; } = 0;
    public int PointsForNoShow { get; set; } = -1;

    public int PointsToWinGame { get; set; } = 12;
    public int GamesPerMatch { get; set; } = 2;

    // games_mode | match_score_mode | match_play
    public string ScoringMode { get; set; } = "games_mode";

    // Plus/Minus applied to a team that forfeits (one-sided or double forfeit)
    public int ForfeitPlusMinus { get; set; } = -6;
    // Plus/Minus applied to the opponent of a one-sided forfeit
    public int ForfeitOpponentPlusMinus { get; set; } = 1;

    public int TeamsInPlayoffs { get; set; } = 0;
    public bool FirstPlaceGuaranteed { get; set; } = true;
    // ladder | round_robin
    public string PlayoffType { get; set; } = "ladder";
    public int PlayoffGamesPerMatch { get; set; } = 2;
    // match_play (always)
    public string PlayoffScoringMode { get; set; } = "match_play";
    // none | 1b1p | 1b4p | 2b1p | 2b4p
    public string PlayoffTiebreakerFormat { get; set; } = "none";

    public bool IsCurrent { get; set; } = false;
    public int WeeksInSeason { get; set; } = 0;
    public int  MaxTeamsInDivision    { get; set; } = 0;
    // Setup | League Play | Playoff Play | Completed
    public string Status { get; set; } = "Setup";
    public bool IsLocked { get; set; } = false;
    public DateOnly? PlayoffStartDate { get; set; }
    public DateOnly? PlayoffEndDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public League League { get; set; } = null!;
    public ICollection<Division> Divisions { get; set; } = [];
}

// Courts activated for this season (drawn from the park's court list)
public class SeasonCourt
{
    public int Id { get; set; }
    public int SeasonId { get; set; }
    public int CourtId { get; set; }

    public Season Season { get; set; } = null!;
    public Court Court { get; set; } = null!;
}

// Default day slots configured for a season — used to filter Division Editor dropdowns
public class SeasonDaySlot
{
    public int Id { get; set; }
    public int SeasonId { get; set; }
    public int DaySlotId { get; set; }

    public Season Season { get; set; } = null!;
    public DaySlot DaySlot { get; set; } = null!;
}

// Default time slots configured for a season — used to filter Division Editor dropdowns
public class SeasonTimeSlot
{
    public int Id { get; set; }
    public int SeasonId { get; set; }
    public int TimeSlotId { get; set; }

    public Season Season { get; set; } = null!;
    public TimeSlot TimeSlot { get; set; } = null!;
}

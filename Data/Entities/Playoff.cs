namespace BocceManager.Data.Entities;

public class PlayoffRound
{
    public int Id { get; set; }
    public int SeasonId { get; set; }
    public int RoundNumber { get; set; }
    public string? RoundName { get; set; }
    public DateOnly? MatchDate { get; set; }

    public Season Season { get; set; } = null!;
    public ICollection<PlayoffMatch> Matches { get; set; } = [];
}

public class PlayoffMatch
{
    public int Id { get; set; }
    public int SeasonId { get; set; }
    // NULL for round-robin (no rounds)
    public int? PlayoffRoundId { get; set; }
    public int Seed1 { get; set; }
    public int? Seed2 { get; set; }
    public int? Team1Id { get; set; }
    public int? Team2Id { get; set; }
    public int? CourtId { get; set; }
    public DateOnly? ScheduledDate { get; set; }
    public TimeOnly? ScheduledTime { get; set; }
    // scheduled | completed | bye
    public string Status { get; set; } = "scheduled";
    public int? WinnerId { get; set; }
    public string? EnteredBy { get; set; }
    public DateTime? EnteredAt { get; set; }

    public Season Season { get; set; } = null!;
    public PlayoffRound? PlayoffRound { get; set; }
    public Team? Team1 { get; set; }
    public Team? Team2 { get; set; }
    public Court? Court { get; set; }
    public Team? Winner { get; set; }
    public ICollection<PlayoffGame> Games { get; set; } = [];
}

public class PlayoffGame
{
    public int Id { get; set; }
    public int PlayoffMatchId { get; set; }
    public int GameNumber { get; set; }
    public int Team1Score { get; set; } = 0;
    public int Team2Score { get; set; } = 0;
    public bool IsForfeit { get; set; } = false;
    public string? EnteredBy { get; set; }
    public DateTime? EnteredAt { get; set; }

    public PlayoffMatch PlayoffMatch { get; set; } = null!;
}

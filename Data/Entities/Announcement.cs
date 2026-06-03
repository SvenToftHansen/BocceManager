namespace BocceManager.Data.Entities;

public class Announcement
{
    public int Id { get; set; }
    public int? LeagueId { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public DateTime? PublishedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public League? League { get; set; }
}

// Log of spare requests submitted through the public site
public class SpareRequest
{
    public int Id { get; set; }
    public int LeagueId { get; set; }
    public int SpareListId { get; set; }
    public int RequestingPlayerId { get; set; }
    public int SparePlayerId { get; set; }
    public int? MatchId { get; set; }
    public DateOnly? GameDate { get; set; }
    public string? Message { get; set; }
    // pending | accepted | declined | cancelled
    public string Status { get; set; } = "pending";
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }

    public League League { get; set; } = null!;
    public SpareList SpareList { get; set; } = null!;
    public Player RequestingPlayer { get; set; } = null!;
    public Player SparePlayer { get; set; } = null!;
    public BocceMatch? Match { get; set; }
}

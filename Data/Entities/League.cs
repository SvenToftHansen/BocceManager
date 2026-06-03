namespace BocceManager.Data.Entities;

public class League
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? RulesText { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? PlayersPerTeamMinimum { get; set; }
    public int? PlayersPerTeamMaximum { get; set; }

    public ICollection<Season> Seasons { get; set; } = [];
}

public class SpareList
{
    public int Id { get; set; }
    public int LeagueId { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public League League { get; set; } = null!;
    public ICollection<SpareListPlayer> SpareListPlayers { get; set; } = [];
}

public class SpareListPlayer
{
    public int Id { get; set; }
    public int SpareListId { get; set; }
    public int PlayerId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateOnly AddedDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public SpareList SpareList { get; set; } = null!;
    public Player Player { get; set; } = null!;
}

public class LeagueOfficial
{
    public int Id { get; set; }
    public int LeagueId { get; set; }
    public int PlayerId { get; set; }
    public string RoleTitle { get; set; } = "";
    public bool ReceivesSpareRequests { get; set; } = false;
    public bool ReceivesRegistrations { get; set; } = false;
    public bool IsActive { get; set; } = true;

    public League League { get; set; } = null!;
    public Player Player { get; set; } = null!;
}

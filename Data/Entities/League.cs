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
    public int  MaxTeamsInDivision    { get; set; } = 0;

    public ICollection<Season> Seasons { get; set; } = [];
}

// One row per player who is on the spare list for a league
public class SpareList
{
    public int Id { get; set; }
    public int LeagueId { get; set; }
    public int PlayerId { get; set; }
    public bool IsActive { get; set; } = true;

    public League League { get; set; } = null!;
    public Player Player { get; set; } = null!;
}

// One row per player who is looking for a team in a league.
// TeamId is set when they are placed on a team; they stay in the list until explicitly removed.
public class LookingForTeam
{
    public int Id { get; set; }
    public int LeagueId { get; set; }
    public int PlayerId { get; set; }
    public int? TeamId { get; set; }

    public League League { get; set; } = null!;
    public Player Player { get; set; } = null!;
    public Team? Team { get; set; }
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

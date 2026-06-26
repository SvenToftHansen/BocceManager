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
    public string? Notes { get; set; }

    public League League { get; set; } = null!;
    public Player Player { get; set; } = null!;
}

// One row per player who is looking for a team in a league/season.
// TeamId is set when they are placed on a team (historical tracking of placement).
// SeasonId binds the player to a specific season for team search purposes.
public class LookingForTeam
{
    public int Id { get; set; }
    public int LeagueId { get; set; }
    public int PlayerId { get; set; }
    public int? SeasonId { get; set; }
    public int? TeamId { get; set; }                // set at placement
    public int? LookingForTeamGroupId { get; set; } // link to buddy group
    public int? PreferredTeamId { get; set; }       // specific team they want (informational)
    public string? Notes { get; set; }
    public DateOnly? RegisteredDate { get; set; }

    public League League { get; set; } = null!;
    public Player Player { get; set; } = null!;
    public Season? Season { get; set; }
    public Team? Team { get; set; }
    public Team? PreferredTeam { get; set; }
    public LookingForTeamGroup? Group { get; set; }
    public ICollection<LookingForTeamDivision> PreferredDivisions { get; set; } = [];
    public ICollection<LookingForTeamPreferredDay> PreferredDays { get; set; } = [];
    public ICollection<LookingForTeamPreferredTime> PreferredTimes { get; set; } = [];
}

// A set of players who want to be placed on the same team.
public class LookingForTeamGroup
{
    public int Id { get; set; }
    public int LeagueId { get; set; }
    public int SeasonId { get; set; }
    public string? Name { get; set; }
    public int? GroupLeaderId { get; set; }  // FK to LookingForTeam: the group leader
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public League League { get; set; } = null!;
    public Season Season { get; set; } = null!;
    public LookingForTeam? Leader { get; set; }
    public ICollection<LookingForTeam> Members { get; set; } = [];
}

// Junction: which divisions a particular LFT player prefers (multi-select).
public class LookingForTeamDivision
{
    public int Id { get; set; }
    public int LookingForTeamId { get; set; }
    public int DivisionId { get; set; }

    public LookingForTeam LookingForTeam { get; set; } = null!;
    public Division Division { get; set; } = null!;
}

// Junction: which days a particular LFT player prefers (multi-select).
public class LookingForTeamPreferredDay
{
    public int Id { get; set; }
    public int LookingForTeamId { get; set; }
    public int DaySlotId { get; set; }

    public LookingForTeam LookingForTeam { get; set; } = null!;
    public DaySlot DaySlot { get; set; } = null!;
}

// Junction: which times a particular LFT player prefers (multi-select).
public class LookingForTeamPreferredTime
{
    public int Id { get; set; }
    public int LookingForTeamId { get; set; }
    public int TimeSlotId { get; set; }

    public LookingForTeam LookingForTeam { get; set; } = null!;
    public TimeSlot TimeSlot { get; set; } = null!;
}


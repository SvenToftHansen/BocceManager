namespace BocceManager.Data.Entities;

public class AppParameter
{
    public int Id { get; set; }
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class LeagueParameter
{
    public int Id { get; set; }
    public int LeagueId { get; set; }
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public League League { get; set; } = null!;
}

public class SeasonParameter
{
    public int Id { get; set; }
    public int SeasonId { get; set; }
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public Season Season { get; set; } = null!;
}

public class DivisionParameter
{
    public int Id { get; set; }
    public int DivisionId { get; set; }
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public Division Division { get; set; } = null!;
}

public class TeamParameter
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public Team Team { get; set; } = null!;
}

public class PlayerParameter
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public Player Player { get; set; } = null!;
}

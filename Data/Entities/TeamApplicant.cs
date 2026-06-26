namespace BocceManager.Data.Entities;

// A group of players applying to join a season as a complete team.
public class TeamApplicant
{
    public int Id { get; set; }
    public int LeagueId { get; set; }
    public int SeasonId { get; set; }
    public string GroupName { get; set; } = "";
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public int? PreferredDivisionId { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = "Pending"; // Pending | Placed | Withdrawn
    public int? PlacedTeamId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public League League { get; set; } = null!;
    public Season Season { get; set; } = null!;
    public Division? PreferredDivision { get; set; }
    public Team? PlacedTeam { get; set; }

    public ICollection<TeamApplicantMember> Members { get; set; } = [];
}

// One row per person in a TeamApplicant group.
// PlayerId is null when the person doesn't yet exist in the Players table.
public class TeamApplicantMember
{
    public int Id { get; set; }
    public int TeamApplicantId { get; set; }
    public int? PlayerId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Notes { get; set; }
    public int? CreatedPlayerId { get; set; }

    public TeamApplicant TeamApplicant { get; set; } = null!;
    public Player? Player { get; set; }
    public Player? CreatedPlayer { get; set; }

    public string DisplayName => PlayerId.HasValue
        ? $"{LastName}, {FirstName}".Trim().TrimStart(',').Trim()
        : $"{LastName}, {FirstName} (new)".Trim().TrimStart(',').Trim();

    public string FullName => string.IsNullOrWhiteSpace(FirstName)
        ? LastName.Trim()
        : string.IsNullOrWhiteSpace(LastName)
            ? FirstName.Trim()
            : $"{FirstName} {LastName}".Trim();
}

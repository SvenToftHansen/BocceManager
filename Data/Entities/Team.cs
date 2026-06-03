namespace BocceManager.Data.Entities;

public class Team
{
    public int Id { get; set; }
    public int DivisionId { get; set; }
    // Unique within division: A, B, C, ...
    public string TeamLetter { get; set; } = "";
    // e.g. "A-MON-0900"
    public string SystemName { get; set; } = "";
    // Defaults to system_name; auto-updated to [Letter][CaptainLastName] when captain assigned
    public string? DisplayName { get; set; }
    public int? CaptainPlayerId { get; set; }
    public bool IsActive { get; set; } = true;
    // TRUE only when division has odd active teams despite even teams_in_division
    public bool IsByeTeam { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Division Division { get; set; } = null!;
    public Player? Captain { get; set; }
    public ICollection<TeamPlayer> TeamPlayers { get; set; } = [];

    public string EffectiveDisplayName => DisplayName ?? SystemName;
}

public class TeamPlayer
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public int PlayerId { get; set; }
    // captain | player
    public string Role { get; set; } = "player";
    public bool IsActive { get; set; } = true;
    public DateOnly JoinedDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public Team Team { get; set; } = null!;
    public Player Player { get; set; } = null!;
}

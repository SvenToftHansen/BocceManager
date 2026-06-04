namespace BocceManager.Data.Entities;

// A group of players applying to be placed into a division for a season.
public class TeamApplicant
{
    public int Id { get; set; }
    public int SeasonId { get; set; }
    public string ContactName { get; set; } = "";
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = "pending"; // pending | placed | declined
    public int? AssignedDivisionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Season Season { get; set; } = null!;
    public Division? AssignedDivision { get; set; }
    public ICollection<TeamApplicantPlayer>   Players         { get; set; } = [];
    public ICollection<TeamApplicantDaySlot>  DayPreferences  { get; set; } = [];
    public ICollection<TeamApplicantTimeSlot> TimePreferences { get; set; } = [];
}

// Players on the applying team. Role: "captain" | "player"
public class TeamApplicantPlayer
{
    public int Id { get; set; }
    public int TeamApplicantId { get; set; }
    public int PlayerId { get; set; }
    public string Role { get; set; } = "player";

    public TeamApplicant TeamApplicant { get; set; } = null!;
    public Player Player { get; set; } = null!;
}

// Preferred play days for the applying team
public class TeamApplicantDaySlot
{
    public int Id { get; set; }
    public int TeamApplicantId { get; set; }
    public int DaySlotId { get; set; }

    public TeamApplicant TeamApplicant { get; set; } = null!;
    public DaySlot DaySlot { get; set; } = null!;
}

// Preferred time slots for the applying team
public class TeamApplicantTimeSlot
{
    public int Id { get; set; }
    public int TeamApplicantId { get; set; }
    public int TimeSlotId { get; set; }

    public TeamApplicant TeamApplicant { get; set; } = null!;
    public TimeSlot TimeSlot { get; set; } = null!;
}

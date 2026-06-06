namespace BocceManager.Data.Entities;

public class Player
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? LotNumber { get; set; }
    public int? PartnerPlayerId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool LookingForTeam { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string FullName => $"{FirstName} {LastName}";
}

public class PendingPlayer
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public string? LotNumber { get; set; }
    // looking_for_team | spare_list | both | neither
    public string Preference { get; set; } = "neither";
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    // pending | approved | rejected
    public string Status { get; set; } = "pending";
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? Notes { get; set; }
}

public class InitiationFee
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public decimal AmountOwing { get; set; } = 0;
    public decimal AmountPaid { get; set; } = 0;
    public DateOnly? PaidDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Player Player { get; set; } = null!;
}

public class SeasonFee
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public int SeasonId { get; set; }
    public decimal AmountOwing { get; set; } = 0;
    public decimal AmountPaid { get; set; } = 0;
    public DateOnly? PaidDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Player Player { get; set; } = null!;
    public Season Season { get; set; } = null!;
}

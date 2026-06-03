namespace BocceManager.Data.Entities;

public class EmailList
{
    public int Id { get; set; }
    public int LeagueId { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public League League { get; set; } = null!;
    public ICollection<EmailListMember> Members { get; set; } = [];
}

public class EmailListMember
{
    public int Id { get; set; }
    public int EmailListId { get; set; }
    public int PlayerId { get; set; }
    public bool IsActive { get; set; } = true;

    public EmailList EmailList { get; set; } = null!;
    public Player Player { get; set; } = null!;
}

public class EmailLog
{
    public int Id { get; set; }
    public string? SentBy { get; set; }
    public int? LeagueId { get; set; }
    public string Subject { get; set; } = "";
    public string? Body { get; set; }
    public int? RecipientCount { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public League? League { get; set; }
}

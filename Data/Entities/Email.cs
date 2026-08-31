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
    public ICollection<EmailLogRecipient> Recipients { get; set; } = [];
}

public class EmailLogRecipient
{
    public int Id { get; set; }
    public int EmailLogId { get; set; }
    public int? PlayerId { get; set; }
    public string Email { get; set; } = "";
    public string Status { get; set; } = "Sent"; // Sent | Failed
    public string? ErrorMessage { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public EmailLog EmailLog { get; set; } = null!;
    public Player? Player { get; set; }
}

// A saved-in-progress Compose Email screen so the admin can leave and pick it back up later.
// Recipient selection is stored by its raw scope index/target rather than resolved emails, since
// the underlying roster (team assignments, spare lists, etc.) may change between save and reload.
public class EmailDraft
{
    public int Id { get; set; }
    public string Subject { get; set; } = "";
    public string BodyHtml { get; set; } = "";
    public int ScopeIndex { get; set; }
    public int? TargetId { get; set; }
    public string? ManualEmails { get; set; }
    public bool UniqueEmailOnly { get; set; } = true;
    public string? SelectedIndividualIds { get; set; } // comma-separated player ids, scope 7 only
    public string? SavedBy { get; set; }
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;
}

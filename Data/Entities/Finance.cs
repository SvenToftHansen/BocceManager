namespace BocceManager.Data.Entities;

// asset | income | expense | liability | equity
public class GlAccount
{
    public int Id { get; set; }
    public string Code { get; set; } = "";       // e.g. "4100"
    public string Name { get; set; } = "";
    public string AccountType { get; set; } = ""; // asset | income | expense | liability | equity
    public decimal OpeningBalance { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<JournalEntry> DebitEntries  { get; set; } = [];
    public ICollection<JournalEntry> CreditEntries { get; set; } = [];
}

// Double-entry: every entry debits one account and credits another
public class JournalEntry
{
    public int Id { get; set; }
    public DateOnly EntryDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public decimal Amount { get; set; } = 0;
    public string Description { get; set; } = "";
    public int DebitAccountId { get; set; }
    public int CreditAccountId { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public string? EnteredBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public GlAccount DebitAccount  { get; set; } = null!;
    public GlAccount CreditAccount { get; set; } = null!;
}

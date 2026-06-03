namespace BocceManager.Data.Entities;

public class ClubDocument
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string FileName { get; set; } = "";   // stored name in Documents folder; empty for Google Docs
    public string DocType { get; set; } = "";    // "pdf", "docx", "googledocs"
    public string? GoogleDocsUrl { get; set; }
    public int? LeagueId { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.Now;
    public string? Notes { get; set; }
}

namespace BocceManager.Data.Entities;

public class ScheduleDivision
{
    public int Id { get; set; }
    public int DivisionId { get; set; }
    public int TemplateId { get; set; }
    public int TemplateWeekNumber { get; set; }
    public DateOnly MatchDate { get; set; }
    public int Team1Id { get; set; }
    public int Team2Id { get; set; }
    public int? CourtId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public Division Division { get; set; } = null!;
    public ScheduleTemplate Template { get; set; } = null!;
    public Team Team1 { get; set; } = null!;
    public Team Team2 { get; set; } = null!;
    public Court? Court { get; set; }
}

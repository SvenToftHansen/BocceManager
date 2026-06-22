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

    // Game scores (2 games per match)
    public int Team1Score1 { get; set; } = 0;
    public int Team2Score1 { get; set; } = 0;
    public int Team1Score2 { get; set; } = 0;
    public int Team2Score2 { get; set; } = 0;

    // Calculated totals
    public int Team1Total => Team1Score1 + Team1Score2;
    public int Team2Total => Team2Score1 + Team2Score2;

    public Division Division { get; set; } = null!;
    public ScheduleTemplate Template { get; set; } = null!;
    public Team Team1 { get; set; } = null!;
    public Team Team2 { get; set; } = null!;
    public Court? Court { get; set; }
}

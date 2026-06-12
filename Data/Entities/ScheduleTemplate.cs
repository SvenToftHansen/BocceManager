namespace BocceManager.Data.Entities;

public class ScheduleTemplate
{
    public int Id { get; set; }
    public int SeasonId { get; set; }
    public int TeamCount { get; set; }   // 4, 6, or 8
    public int WeekCount { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public Season Season { get; set; } = null!;
    public ICollection<ScheduleTemplateWeek> Weeks { get; set; } = [];
}

public class ScheduleTemplateWeek
{
    public int Id { get; set; }
    public int TemplateId { get; set; }
    public int WeekNumber { get; set; }

    public ScheduleTemplate Template { get; set; } = null!;
    public ICollection<ScheduleTemplateMatch> Matches { get; set; } = [];
}

public class ScheduleTemplateMatch
{
    public int Id { get; set; }
    public int TemplateWeekId { get; set; }
    public string Slot1 { get; set; } = "";   // "A", "B", etc.
    public string Slot2 { get; set; } = "";
    public int CourtId { get; set; }

    public ScheduleTemplateWeek TemplateWeek { get; set; } = null!;
    public Court Court { get; set; } = null!;
}

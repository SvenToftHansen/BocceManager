namespace BocceManager.Data.Entities;

public class Stats
{
    public int Id { get; set; }
    public int ScheduleDivisionsId { get; set; }
    public int DivisionId { get; set; }
    public int WeekNumber { get; set; }
    public int TeamId { get; set; }
    public int PlusMinus { get; set; }
    public int Wins { get; set; }
    public int Ties { get; set; }
    public int Losses { get; set; }
    public int Forfeits { get; set; }
    public int Points { get; set; }
}

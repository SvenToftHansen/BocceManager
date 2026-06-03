namespace BocceManager.Data.Entities;

public class Division
{
    public int Id { get; set; }
    public int SeasonId { get; set; }
    public string Name { get; set; } = "";
    public string ShortName { get; set; } = "";
    public string SortName { get; set; } = "";
    public int? PlayersPerTeamMinimum { get; set; }
    public int? PlayersPerTeamMaximum { get; set; }
    // Even numbers preferred (2-16); odd numbers supported (scheduler assigns one bye per round)
    public int TeamsInDivision { get; set; }
    // NULL when season.game_interval = 'schedule_determined'
    public int? DaySlotId { get; set; }
    public int? TimeSlotId { get; set; }
    public bool IsActive { get; set; } = true;

    public Season Season { get; set; } = null!;
    public DaySlot? DaySlot { get; set; }
    public TimeSlot? TimeSlot { get; set; }
    public ICollection<Team> Teams { get; set; } = [];
}

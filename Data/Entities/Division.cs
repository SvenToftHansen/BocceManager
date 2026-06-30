using System.ComponentModel.DataAnnotations.Schema;

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
    // Actual count of teams in this division; auto-updated from Teams tab; drives scheduling
    [Column("TeamsInDivision")]
    public int TeamCount { get; set; }
    public int? DaySlotId { get; set; }
    public int? TimeSlotId { get; set; }
    public bool IsActive { get; set; } = true;

    public Season Season { get; set; } = null!;
    public DaySlot? DaySlot { get; set; }
    public TimeSlot? TimeSlot { get; set; }
    public ICollection<Team> Teams { get; set; } = [];
}

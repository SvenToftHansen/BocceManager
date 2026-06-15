namespace BocceManager.Data.Entities;

public class Court
{
    public int Id { get; set; }
    public int CourtNumber { get; set; }
    public string CourtLetter { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
}

public class TimeSlot
{
    public int Id { get; set; }
    public string Timeslot12h { get; set; } = "";
    public string Timeslot24h { get; set; } = "";
    public int? SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class DaySlot
{
    public int Id { get; set; }
    public string DayName { get; set; } = "";
    public string DayAbbr { get; set; } = "";
    public int DayNbr { get; set; }
    public bool IsActive { get; set; } = true;
}

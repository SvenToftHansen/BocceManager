namespace BocceManager.Data.Entities;

public class NewIdea
{
    public int Id { get; set; }
    public string Idea { get; set; } = "";
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime? DateCollected { get; set; }
}

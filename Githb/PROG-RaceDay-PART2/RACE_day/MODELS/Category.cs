namespace RaceDay.API.Models;

public class Category
{
    public int CategoryID { get; set; }
    public int EventID { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? AgeGroupMin { get; set; }
    public int? AgeGroupMax { get; set; }
    public char? Gender { get; set; } // 'M', 'F', or null
    public decimal EntryFee { get; set; }

    // Navigation properties
    public Event Event { get; set; } = null!;
    public ICollection<EventEnrolment> EventEnrolments { get; set; } = new List<EventEnrolment>();
}
namespace RaceDay.API.Models;

public class Event
{
    public int EventID { get; set; }
    public int OrganiserID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal RouteDistanceKM { get; set; }
    public DateTime RegistrationDeadline { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Organiser Organiser { get; set; } = null!;
    public ICollection<Category> Categories { get; set; } = new List<Category>();
}
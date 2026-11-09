namespace RaceDay.API.DTOs.Events;

public class EventDto
{
    public int EventID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal RouteDistanceKM { get; set; }
    public DateTime RegistrationDeadline { get; set; }
    public bool IsActive { get; set; }
    public string OrganiserName { get; set; } = string.Empty;
    public int CategoryCount { get; set; }
}
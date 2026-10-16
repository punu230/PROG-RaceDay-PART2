using Microsoft.Extensions.Logging;

namespace RaceDay.API.Models;

public class Organiser
{
    public int OrganiserID { get; set; }
    public int UserID { get; set; }
    public string OrganisationName { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string? Website { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<Event> Events { get; set; } = new List<Event>();
}
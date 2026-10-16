namespace RaceDay.API.Models;

public class Participant
{
    public int ParticipantID { get; set; }
    public int UserID { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? MedicalConditions { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<EventEnrolment> EventEnrolments { get; set; } = new List<EventEnrolment>();
}
namespace RaceDay.API.Models;

public class EventEnrolment
{
    public int EnrolmentID { get; set; }
    public int ParticipantID { get; set; }
    public int CategoryID { get; set; }
    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;
    public bool IsPaid { get; set; } = false;

    // Navigation properties
    public Participant Participant { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public Result? Result { get; set; }
}
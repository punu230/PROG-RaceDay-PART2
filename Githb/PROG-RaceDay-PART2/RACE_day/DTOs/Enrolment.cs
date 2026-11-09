namespace RaceDay.API.DTOs.Enrolments;

public class EnrolmentDto
{
    public int EnrolmentID { get; set; }
    public int ParticipantID { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int EventID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public DateTime EnrolmentDate { get; set; }
    public bool IsPaid { get; set; }
    public decimal EntryFee { get; set; }
}
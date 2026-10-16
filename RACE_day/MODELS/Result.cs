namespace RaceDay.API.Models;

public class Result
{
    public int ResultID { get; set; }
    public int EnrolmentID { get; set; }
    public TimeSpan FinishTime { get; set; }
    public int Position { get; set; }

    // Navigation property
    public EventEnrolment EventEnrolment { get; set; } = null!;
}
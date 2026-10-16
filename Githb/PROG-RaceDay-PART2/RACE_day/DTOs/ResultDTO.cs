namespace RaceDay.API.DTOs.Results;

public class ResultDto
{
    public int ResultID { get; set; }
    public int EnrolmentID { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public TimeSpan FinishTime { get; set; }
    public int Position { get; set; }
}
using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs.Results;

public class CreateResultDto
{
    [Required]
    public TimeSpan FinishTime { get; set; }

    [Required]
    [Range(1, 10000)]
    public int Position { get; set; }
}
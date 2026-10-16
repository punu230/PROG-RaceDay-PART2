using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs.Events;

public class CreateEventDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public DateTime EventDate { get; set; }

    [Required]
    [MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    [Required]
    [Range(0.1, 1000)]
    public decimal RouteDistanceKM { get; set; }

    [Required]
    public DateTime RegistrationDeadline { get; set; }
}
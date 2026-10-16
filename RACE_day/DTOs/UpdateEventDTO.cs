using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs.Events;

public class UpdateEventDto
{
    [MaxLength(200)]
    public string? Name { get; set; }

    public string? Description { get; set; }

    public DateTime? EventDate { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    [Range(0.1, 1000)]
    public decimal? RouteDistanceKM { get; set; }

    public DateTime? RegistrationDeadline { get; set; }

    public bool? IsActive { get; set; }
}
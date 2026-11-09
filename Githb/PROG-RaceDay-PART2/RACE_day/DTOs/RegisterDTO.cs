using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs.Auth;

public class RegisterDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    public DateTime? DateOfBirth { get; set; }

    [Required]
    public string RoleName { get; set; } = string.Empty; // "Organiser" or "Participant"

    // Organiser-specific fields
    public string? OrganisationName { get; set; }
    public string? ContactPhone { get; set; }
    public string? Website { get; set; }

    // Participant-specific fields
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? MedicalConditions { get; set; }
}
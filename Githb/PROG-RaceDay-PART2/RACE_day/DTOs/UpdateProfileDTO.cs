using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs.Users;

public class UpdateProfileDto
{
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    public DateTime? DateOfBirth { get; set; }

    // Organiser fields
    [MaxLength(200)]
    public string? OrganisationName { get; set; }

    [MaxLength(20)]
    public string? ContactPhone { get; set; }

    [MaxLength(200)]
    public string? Website { get; set; }

    // Participant fields
    [MaxLength(200)]
    public string? EmergencyContactName { get; set; }

    [MaxLength(20)]
    public string? EmergencyContactPhone { get; set; }

    public string? MedicalConditions { get; set; }
}
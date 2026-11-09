namespace RaceDay.API.DTOs.Users;

public class UserProfileDto
{
    public int UserID { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string RoleName { get; set; } = string.Empty;

    // Organiser fields
    public string? OrganisationName { get; set; }
    public string? ContactPhone { get; set; }
    public string? Website { get; set; }

    // Participant fields
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? MedicalConditions { get; set; }
}
namespace RaceDay.API.Models;

public class User
{
    public int UserID { get; set; }
    public int RoleID { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }

    // Navigation properties
    public Role Role { get; set; } = null!;
    public Organiser? Organiser { get; set; }
    public Participant? Participant { get; set; }
}
namespace RaceDay.API.DTOs.Auth;

public class AuthResponseDto
{
    public int UserID { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime TokenExpiry { get; set; }
}
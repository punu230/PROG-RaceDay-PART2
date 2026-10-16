using RaceDay.API.Models;

// Interface for JWT token generation.
namespace RaceDay.API.Services;

public interface ITokenService
{
    string GenerateToken(User user, string roleName);
    DateTime GetTokenExpiry();
}
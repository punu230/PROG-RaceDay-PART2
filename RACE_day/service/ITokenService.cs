using RaceDay.API.Models;

namespace RaceDay.API.Services;

public interface ITokenService
{
    string GenerateToken(User user, string roleName);
    DateTime GetTokenExpiry();
}
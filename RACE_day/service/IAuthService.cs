using RaceDay.API.DTOs.Auth;

// Interface for the authentication service.
namespace RaceDay.API.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
}
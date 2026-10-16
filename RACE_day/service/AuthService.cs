using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs.Auth;
using RaceDay.API.Models;

namespace RaceDay.API.Services;

public class AuthService : IAuthService
{
    private readonly RaceDayDbContext _context;
    private readonly ITokenService _tokenService;

    public AuthService(RaceDayDbContext context, ITokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        // Check if email already exists
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
        {
            throw new InvalidOperationException("Email already exists.");
        }

        // Get the role
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == dto.RoleName);
        if (role == null)
        {
            throw new InvalidOperationException($"Role '{dto.RoleName}' does not exist.");
        }

        // Create the user
        var user = new User
        {
            RoleID = role.RoleID,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            DateOfBirth = dto.DateOfBirth
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Create role-specific profile
        if (dto.RoleName == "Organiser")
        {
            var organiser = new Organiser
            {
                UserID = user.UserID,
                OrganisationName = dto.OrganisationName ?? $"{dto.FirstName} {dto.LastName}",
                ContactPhone = dto.ContactPhone,
                Website = dto.Website
            };
            _context.Organisers.Add(organiser);
        }
        else if (dto.RoleName == "Participant")
        {
            var participant = new Participant
            {
                UserID = user.UserID,
                EmergencyContactName = dto.EmergencyContactName,
                EmergencyContactPhone = dto.EmergencyContactPhone,
                MedicalConditions = dto.MedicalConditions
            };
            _context.Participants.Add(participant);
        }

        await _context.SaveChangesAsync();

        // Generate token
        var token = _tokenService.GenerateToken(user, role.RoleName);

        return new AuthResponseDto
        {
            UserID = user.UserID,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            RoleName = role.RoleName,
            Token = token,
            TokenExpiry = _tokenService.GetTokenExpiry()
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        // Find the user
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        // Generate token
        var token = _tokenService.GenerateToken(user, user.Role.RoleName);

        return new AuthResponseDto
        {
            UserID = user.UserID,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            RoleName = user.Role.RoleName,
            Token = token,
            TokenExpiry = _tokenService.GetTokenExpiry()
        };
    }
}
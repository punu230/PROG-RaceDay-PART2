using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs.Users;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/users")]  // Was: api/[controller]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public UsersController(RaceDayDbContext context)
    {
        _context = context;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim ?? "0");
    }

    /// <summary>
    /// Gets the current user's profile
    /// </summary>
    [HttpGet("profile")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetCurrentUserId();

        var user = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.Organiser)
            .Include(u => u.Participant)
            .FirstOrDefaultAsync(u => u.UserID == userId);

        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        var profile = new UserProfileDto
        {
            UserID = user.UserID,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DateOfBirth = user.DateOfBirth,
            RoleName = user.Role.RoleName,
            OrganisationName = user.Organiser?.OrganisationName,
            ContactPhone = user.Organiser?.ContactPhone,
            Website = user.Organiser?.Website,
            EmergencyContactName = user.Participant?.EmergencyContactName,
            EmergencyContactPhone = user.Participant?.EmergencyContactPhone,
            MedicalConditions = user.Participant?.MedicalConditions
        };

        return Ok(profile);
    }

    /// <summary>
    /// Updates the current user's profile
    /// </summary>
    [HttpPut("profile")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetCurrentUserId();

        var user = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.Organiser)
            .Include(u => u.Participant)
            .FirstOrDefaultAsync(u => u.UserID == userId);

        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        // Update common fields
        if (!string.IsNullOrEmpty(dto.FirstName)) user.FirstName = dto.FirstName;
        if (!string.IsNullOrEmpty(dto.LastName)) user.LastName = dto.LastName;
        if (dto.DateOfBirth.HasValue) user.DateOfBirth = dto.DateOfBirth;

        // Update role-specific fields
        if (user.Role.RoleName == "Organiser" && user.Organiser != null)
        {
            if (!string.IsNullOrEmpty(dto.OrganisationName))
                user.Organiser.OrganisationName = dto.OrganisationName;
            if (dto.ContactPhone != null)
                user.Organiser.ContactPhone = dto.ContactPhone;
            if (dto.Website != null)
                user.Organiser.Website = dto.Website;
        }
        else if (user.Role.RoleName == "Participant" && user.Participant != null)
        {
            if (dto.EmergencyContactName != null)
                user.Participant.EmergencyContactName = dto.EmergencyContactName;
            if (dto.EmergencyContactPhone != null)
                user.Participant.EmergencyContactPhone = dto.EmergencyContactPhone;
            if (dto.MedicalConditions != null)
                user.Participant.MedicalConditions = dto.MedicalConditions;
        }

        await _context.SaveChangesAsync();

        var profile = new UserProfileDto
        {
            UserID = user.UserID,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DateOfBirth = user.DateOfBirth,
            RoleName = user.Role.RoleName,
            OrganisationName = user.Organiser?.OrganisationName,
            ContactPhone = user.Organiser?.ContactPhone,
            Website = user.Organiser?.Website,
            EmergencyContactName = user.Participant?.EmergencyContactName,
            EmergencyContactPhone = user.Participant?.EmergencyContactPhone,
            MedicalConditions = user.Participant?.MedicalConditions
        };

        return Ok(profile);
    }
}
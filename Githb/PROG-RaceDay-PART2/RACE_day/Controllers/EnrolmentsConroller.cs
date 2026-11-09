using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs.Enrolments;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public EnrolmentsController(RaceDayDbContext context)
    {
        _context = context;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim ?? "0");
    }

    /// <summary>
    /// Gets all enrolments for the current participant
    /// </summary>
    /// <returns>List of enrolments</returns>
    /// <response code="200">Enrolments retrieved successfully</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not a Participant</response>
    [HttpGet("users/enrolments")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyEnrolments()
    {
        var userId = GetCurrentUserId();

        var participant = await _context.Participants
            .FirstOrDefaultAsync(p => p.UserID == userId);

        if (participant == null)
        {
            return Forbid();
        }

        var enrolments = await _context.EventEnrolments
            .Include(e => e.Participant)
                .ThenInclude(p => p.User)
            .Include(e => e.Category)
                .ThenInclude(c => c.Event)
            .Where(e => e.ParticipantID == participant.ParticipantID)
            .Select(e => new EnrolmentDto
            {
                EnrolmentID = e.EnrolmentID,
                ParticipantID = e.ParticipantID,
                ParticipantName = e.Participant.User.FirstName + " " + e.Participant.User.LastName,
                CategoryID = e.CategoryID,
                CategoryName = e.Category.Name,
                EventID = e.Category.EventID,
                EventName = e.Category.Event.Name,
                EventDate = e.Category.Event.EventDate,
                EnrolmentDate = e.EnrolmentDate,
                IsPaid = e.IsPaid,
                EntryFee = e.Category.EntryFee
            })
            .ToListAsync();

        return Ok(enrolments);
    }

    /// <summary>
    /// Gets all enrolments for a specific category (Organiser only)
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="categoryId">Category ID</param>
    /// <returns>List of enrolments</returns>
    /// <response code="200">Enrolments retrieved successfully</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not the event owner</response>
    /// <response code="404">Event or Category not found</response>
    [HttpGet("events/{eventId}/categories/{categoryId}/enrolments")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryEnrolments(int eventId, int categoryId)
    {
        var userId = GetCurrentUserId();

        var organiser = await _context.Organisers
            .FirstOrDefaultAsync(o => o.UserID == userId);

        if (organiser == null)
        {
            return Forbid();
        }

        var eventEntity = await _context.Events
            .FirstOrDefaultAsync(e => e.EventID == eventId);

        if (eventEntity == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        if (eventEntity.OrganiserID != organiser.OrganiserID)
        {
            return Forbid();
        }

        var enrolments = await _context.EventEnrolments
            .Include(e => e.Participant)
                .ThenInclude(p => p.User)
            .Include(e => e.Category)
            .Where(e => e.CategoryID == categoryId && e.Category.EventID == eventId)
            .Select(e => new EnrolmentDto
            {
                EnrolmentID = e.EnrolmentID,
                ParticipantID = e.ParticipantID,
                ParticipantName = e.Participant.User.FirstName + " " + e.Participant.User.LastName,
                CategoryID = e.CategoryID,
                CategoryName = e.Category.Name,
                EventID = e.Category.EventID,
                EventName = eventEntity.Name,
                EventDate = eventEntity.EventDate,
                EnrolmentDate = e.EnrolmentDate,
                IsPaid = e.IsPaid,
                EntryFee = e.Category.EntryFee
            })
            .ToListAsync();

        return Ok(enrolments);
    }

    /// <summary>
    /// Enrols the current participant in a category
    /// </summary>
    /// <param name="categoryId">Category ID</param>
    /// <returns>Created enrolment</returns>
    /// <response code="201">Enrolment created successfully</response>
    /// <response code="400">Already enrolled or deadline passed</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not a Participant</response>
    /// <response code="404">Category not found</response>
    [HttpPost("categories/{categoryId}/enrol")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Enrol(int categoryId)
    {
        var userId = GetCurrentUserId();

        var participant = await _context.Participants
            .FirstOrDefaultAsync(p => p.UserID == userId);

        if (participant == null)
        {
            return Forbid();
        }

        var category = await _context.Categories
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryID == categoryId);

        if (category == null)
        {
            return NotFound(new { message = "Category not found." });
        }

        // Check if registration deadline has passed
        if (category.Event.RegistrationDeadline < DateTime.UtcNow.Date)
        {
            return BadRequest(new { message = "Registration deadline has passed." });
        }

        // Check if already enrolled
        var existingEnrolment = await _context.EventEnrolments
            .AnyAsync(e => e.ParticipantID == participant.ParticipantID
                        && e.CategoryID == categoryId);

        if (existingEnrolment)
        {
            return BadRequest(new { message = "You are already enrolled in this category." });
        }

        var enrolment = new EventEnrolment
        {
            ParticipantID = participant.ParticipantID,
            CategoryID = categoryId,
            EnrolmentDate = DateTime.UtcNow,
            IsPaid = false
        };

        _context.EventEnrolments.Add(enrolment);
        await _context.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new EnrolmentDto
        {
            EnrolmentID = enrolment.EnrolmentID,
            ParticipantID = participant.ParticipantID,
            ParticipantName = $"{participant.User?.FirstName} {participant.User?.LastName}",
            CategoryID = categoryId,
            CategoryName = category.Name,
            EventID = category.EventID,
            EventName = category.Event.Name,
            EventDate = category.Event.EventDate,
            EnrolmentDate = enrolment.EnrolmentDate,
            IsPaid = enrolment.IsPaid,
            EntryFee = category.EntryFee
        });
    }

    /// <summary>
    /// Withdraws from an enrolment (Participant only)
    /// </summary>
    /// <param name="enrolmentId">Enrolment ID</param>
    /// <returns>No content</returns>
    /// <response code="204">Withdrawn successfully</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not the participant</response>
    /// <response code="404">Enrolment not found</response>
    [HttpDelete("enrolments/{enrolmentId}")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Withdraw(int enrolmentId)
    {
        var userId = GetCurrentUserId();

        var participant = await _context.Participants
            .FirstOrDefaultAsync(p => p.UserID == userId);

        if (participant == null)
        {
            return Forbid();
        }

        var enrolment = await _context.EventEnrolments
            .FirstOrDefaultAsync(e => e.EnrolmentID == enrolmentId);

        if (enrolment == null)
        {
            return NotFound(new { message = "Enrolment not found." });
        }

        if (enrolment.ParticipantID != participant.ParticipantID)
        {
            return Forbid();
        }

        _context.EventEnrolments.Remove(enrolment);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
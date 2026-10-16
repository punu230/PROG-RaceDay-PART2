using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs.Results;
using RaceDay.API.Models;

// Handles race result recording and retrieval.
namespace RaceDay.API.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class ResultController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public ResultController(RaceDayDbContext context)
    {
        _context = context;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim ?? "0");
    }

    // ============================================================
    // GET: /api/users/results
    // PARTICIPANT ONLY — gets the current participant's results
    // ============================================================
    [HttpGet("users/results")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(IEnumerable<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyResults()
    {
        var userId = GetCurrentUserId();

        var participant = await _context.Participants
            .FirstOrDefaultAsync(p => p.UserID == userId);

        if (participant == null)
        {
            return Forbid();
        }

        var results = await _context.Results
            .Include(r => r.EventEnrolment)
                .ThenInclude(e => e.Participant)
                    .ThenInclude(p => p.User)
            .Include(r => r.EventEnrolment)
                .ThenInclude(e => e.Category)
                    .ThenInclude(c => c.Event)
            .Where(r => r.EventEnrolment.ParticipantID == participant.ParticipantID)
            .Select(r => new ResultDto
            {
                ResultID = r.ResultID,
                EnrolmentID = r.EnrolmentID,
                ParticipantName = r.EventEnrolment.Participant.User.FirstName + " " + r.EventEnrolment.Participant.User.LastName,
                EventName = r.EventEnrolment.Category.Event.Name,
                CategoryName = r.EventEnrolment.Category.Name,
                FinishTime = r.FinishTime,
                Position = r.Position
            })
            .ToListAsync();

        return Ok(results);
    }

    // ============================================================
    // GET: /api/events/{eventId}/categories/{categoryId}/results
    // PUBLIC — anyone can view results for a category
    // ============================================================
    [HttpGet("events/{eventId}/categories/{categoryId}/results")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryResults(int eventId, int categoryId)
    {
        var categoryExists = await _context.Categories
            .AnyAsync(c => c.CategoryID == categoryId && c.EventID == eventId);

        if (!categoryExists)
        {
            return NotFound(new { message = "Category not found." });
        }

        var results = await _context.Results
            .Include(r => r.EventEnrolment)
                .ThenInclude(e => e.Participant)
                    .ThenInclude(p => p.User)
            .Include(r => r.EventEnrolment)
                .ThenInclude(e => e.Category)
                    .ThenInclude(c => c.Event)
            .Where(r => r.EventEnrolment.CategoryID == categoryId)
            .OrderBy(r => r.Position)
            .Select(r => new ResultDto
            {
                ResultID = r.ResultID,
                EnrolmentID = r.EnrolmentID,
                ParticipantName = r.EventEnrolment.Participant.User.FirstName + " " + r.EventEnrolment.Participant.User.LastName,
                EventName = r.EventEnrolment.Category.Event.Name,
                CategoryName = r.EventEnrolment.Category.Name,
                FinishTime = r.FinishTime,
                Position = r.Position
            })
            .ToListAsync();

        return Ok(results);
    }

    // ============================================================
    // POST: /api/enrolments/{enrolmentId}/results
    // ORGANISER ONLY (must own the event)
    // ============================================================
    [HttpPost("enrolments/{enrolmentId}/results")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateResult(int enrolmentId, [FromBody] CreateResultDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetCurrentUserId();

        var organiser = await _context.Organisers
            .FirstOrDefaultAsync(o => o.UserID == userId);

        if (organiser == null)
        {
            return Forbid();
        }

        var enrolment = await _context.EventEnrolments
            .Include(e => e.Category)
                .ThenInclude(c => c.Event)
            .Include(e => e.Participant)
                .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(e => e.EnrolmentID == enrolmentId);

        if (enrolment == null)
        {
            return NotFound(new { message = "Enrolment not found." });
        }

        if (enrolment.Category.Event.OrganiserID != organiser.OrganiserID)
        {
            return Forbid();
        }

        var existingResult = await _context.Results
            .AnyAsync(r => r.EnrolmentID == enrolmentId);

        if (existingResult)
        {
            return BadRequest(new { message = "A result already exists for this enrolment." });
        }

        var result = new Result
        {
            EnrolmentID = enrolmentId,
            FinishTime = dto.FinishTime,
            Position = dto.Position
        };

        _context.Results.Add(result);
        await _context.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new ResultDto
        {
            ResultID = result.ResultID,
            EnrolmentID = result.EnrolmentID,
            ParticipantName = $"{enrolment.Participant.User.FirstName} {enrolment.Participant.User.LastName}",
            EventName = enrolment.Category.Event.Name,
            CategoryName = enrolment.Category.Name,
            FinishTime = result.FinishTime,
            Position = result.Position
        });
    }
}
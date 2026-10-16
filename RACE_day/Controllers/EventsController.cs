using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs.Events;
using RaceDay.API.Models;

// Handles event management including creation, retrieval, and updates.
namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public EventsController(RaceDayDbContext context)
    {
        _context = context;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim ?? "0");
    }

    // ============================================================
    // GET: /api/events
    // PUBLIC — anyone can browse events
    // ============================================================
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<EventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllEvents([FromQuery] bool? activeOnly = true)
    {
        var query = _context.Events
            .Include(e => e.Organiser)
            .Include(e => e.Categories)
            .AsQueryable();

        if (activeOnly == true)
        {
            query = query.Where(e => e.IsActive);
        }

        var events = await query
            .OrderBy(e => e.EventDate)
            .Select(e => new EventDto
            {
                EventID = e.EventID,
                Name = e.Name,
                Description = e.Description,
                EventDate = e.EventDate,
                Location = e.Location,
                RouteDistanceKM = e.RouteDistanceKM,
                RegistrationDeadline = e.RegistrationDeadline,
                IsActive = e.IsActive,
                OrganiserName = e.Organiser.OrganisationName,
                CategoryCount = e.Categories.Count
            })
            .ToListAsync();

        return Ok(events);
    }

    // ============================================================
    // GET: /api/events/{id}
    // PUBLIC — anyone can view a single event
    // ============================================================
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEvent(int id)
    {
        var eventEntity = await _context.Events
            .Include(e => e.Organiser)
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventID == id);

        if (eventEntity == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        return Ok(new
        {
            eventEntity.EventID,
            eventEntity.Name,
            eventEntity.Description,
            eventEntity.EventDate,
            eventEntity.Location,
            eventEntity.RouteDistanceKM,
            eventEntity.RegistrationDeadline,
            eventEntity.IsActive,
            OrganiserName = eventEntity.Organiser.OrganisationName,
            Categories = eventEntity.Categories.Select(c => new
            {
                c.CategoryID,
                c.Name,
                c.AgeGroupMin,
                c.AgeGroupMax,
                c.Gender,
                c.EntryFee
            })
        });
    }

    // ============================================================
    // POST: /api/events
    // ORGANISER ONLY
    // ============================================================
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (dto.RegistrationDeadline >= dto.EventDate)
        {
            return BadRequest(new { message = "Registration deadline must be before the event date." });
        }

        var userId = GetCurrentUserId();

        var organiser = await _context.Organisers
            .FirstOrDefaultAsync(o => o.UserID == userId);

        if (organiser == null)
        {
            return Forbid();
        }

        var eventEntity = new Event
        {
            OrganiserID = organiser.OrganiserID,
            Name = dto.Name,
            Description = dto.Description,
            EventDate = dto.EventDate,
            Location = dto.Location,
            RouteDistanceKM = dto.RouteDistanceKM,
            RegistrationDeadline = dto.RegistrationDeadline,
            IsActive = true
        };

        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEvent), new { id = eventEntity.EventID }, new
        {
            eventEntity.EventID,
            eventEntity.Name,
            eventEntity.Description,
            eventEntity.EventDate,
            eventEntity.Location,
            eventEntity.RouteDistanceKM,
            eventEntity.RegistrationDeadline,
            eventEntity.IsActive
        });
    }

    // ============================================================
    // PUT: /api/events/{id}
    // ORGANISER ONLY (must own the event)
    // ============================================================
    [HttpPut("{id}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvent(int id, [FromBody] UpdateEventDto dto)
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

        var eventEntity = await _context.Events
            .FirstOrDefaultAsync(e => e.EventID == id);

        if (eventEntity == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        if (eventEntity.OrganiserID != organiser.OrganiserID)
        {
            return Forbid();
        }

        if (!string.IsNullOrEmpty(dto.Name)) eventEntity.Name = dto.Name;
        if (dto.Description != null) eventEntity.Description = dto.Description;
        if (dto.EventDate.HasValue) eventEntity.EventDate = dto.EventDate.Value;
        if (!string.IsNullOrEmpty(dto.Location)) eventEntity.Location = dto.Location;
        if (dto.RouteDistanceKM.HasValue) eventEntity.RouteDistanceKM = dto.RouteDistanceKM.Value;
        if (dto.RegistrationDeadline.HasValue) eventEntity.RegistrationDeadline = dto.RegistrationDeadline.Value;
        if (dto.IsActive.HasValue) eventEntity.IsActive = dto.IsActive.Value;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            eventEntity.EventID,
            eventEntity.Name,
            eventEntity.Description,
            eventEntity.EventDate,
            eventEntity.Location,
            eventEntity.RouteDistanceKM,
            eventEntity.RegistrationDeadline,
            eventEntity.IsActive
        });
    }

    // ============================================================
    // DELETE: /api/events/{id}
    // ORGANISER ONLY (must own the event) — soft delete
    // ============================================================
    [HttpDelete("{id}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var userId = GetCurrentUserId();

        var organiser = await _context.Organisers
            .FirstOrDefaultAsync(o => o.UserID == userId);

        if (organiser == null)
        {
            return Forbid();
        }

        var eventEntity = await _context.Events
            .FirstOrDefaultAsync(e => e.EventID == id);

        if (eventEntity == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        if (eventEntity.OrganiserID != organiser.OrganiserID)
        {
            return Forbid();
        }

        // Soft delete
        eventEntity.IsActive = false;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
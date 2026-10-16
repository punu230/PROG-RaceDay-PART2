using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs.Categories;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api")]
public class CategoryController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public CategoryController(RaceDayDbContext context)
    {
        _context = context;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim ?? "0");
    }

    // ============================================================
    // GET: /api/events/{eventId}/categories
    // PUBLIC — anyone can view categories for an event
    // ============================================================
    [HttpGet("events/{eventId}/categories")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategories(int eventId)
    {
        var eventExists = await _context.Events.AnyAsync(e => e.EventID == eventId);
        if (!eventExists)
        {
            return NotFound(new { message = "Event not found." });
        }

        var categories = await _context.Categories
            .Where(c => c.EventID == eventId)
            .Select(c => new CategoryDto
            {
                CategoryID = c.CategoryID,
                EventID = c.EventID,
                Name = c.Name,
                AgeGroupMin = c.AgeGroupMin,
                AgeGroupMax = c.AgeGroupMax,
                Gender = c.Gender,
                EntryFee = c.EntryFee
            })
            .ToListAsync();

        return Ok(categories);
    }

    // ============================================================
    // POST: /api/events/{eventId}/categories
    // ORGANISER ONLY (must own the event)
    // ============================================================
    [HttpPost("events/{eventId}/categories")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateCategory(int eventId, [FromBody] CreateCategoryDto dto)
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
            .FirstOrDefaultAsync(e => e.EventID == eventId);

        if (eventEntity == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        if (eventEntity.OrganiserID != organiser.OrganiserID)
        {
            return Forbid();
        }

        if (dto.AgeGroupMin.HasValue && dto.AgeGroupMax.HasValue &&
            dto.AgeGroupMin > dto.AgeGroupMax)
        {
            return BadRequest(new { message = "Minimum age cannot be greater than maximum age." });
        }

        var category = new Category
        {
            EventID = eventId,
            Name = dto.Name,
            AgeGroupMin = dto.AgeGroupMin,
            AgeGroupMax = dto.AgeGroupMax,
            Gender = dto.Gender,
            EntryFee = dto.EntryFee
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new CategoryDto
        {
            CategoryID = category.CategoryID,
            EventID = category.EventID,
            Name = category.Name,
            AgeGroupMin = category.AgeGroupMin,
            AgeGroupMax = category.AgeGroupMax,
            Gender = category.Gender,
            EntryFee = category.EntryFee
        });
    }

    // ============================================================
    // PUT: /api/categories/{categoryId}
    // ORGANISER ONLY (must own the event)
    // ============================================================
    [HttpPut("categories/{categoryId}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCategory(int categoryId, [FromBody] UpdateCategoryDto dto)
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

        var category = await _context.Categories
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryID == categoryId);

        if (category == null)
        {
            return NotFound(new { message = "Category not found." });
        }

        if (category.Event.OrganiserID != organiser.OrganiserID)
        {
            return Forbid();
        }

        if (!string.IsNullOrEmpty(dto.Name)) category.Name = dto.Name;
        if (dto.AgeGroupMin.HasValue) category.AgeGroupMin = dto.AgeGroupMin;
        if (dto.AgeGroupMax.HasValue) category.AgeGroupMax = dto.AgeGroupMax;
        if (dto.Gender.HasValue) category.Gender = dto.Gender;
        if (dto.EntryFee.HasValue) category.EntryFee = dto.EntryFee.Value;

        await _context.SaveChangesAsync();

        return Ok(new CategoryDto
        {
            CategoryID = category.CategoryID,
            EventID = category.EventID,
            Name = category.Name,
            AgeGroupMin = category.AgeGroupMin,
            AgeGroupMax = category.AgeGroupMax,
            Gender = category.Gender,
            EntryFee = category.EntryFee
        });
    }

    // ============================================================
    // DELETE: /api/categories/{categoryId}
    // ORGANISER ONLY (must own the event)
    // ============================================================
    [HttpDelete("categories/{categoryId}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCategory(int categoryId)
    {
        var userId = GetCurrentUserId();

        var organiser = await _context.Organisers
            .FirstOrDefaultAsync(o => o.UserID == userId);

        if (organiser == null)
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

        if (category.Event.OrganiserID != organiser.OrganiserID)
        {
            return Forbid();
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
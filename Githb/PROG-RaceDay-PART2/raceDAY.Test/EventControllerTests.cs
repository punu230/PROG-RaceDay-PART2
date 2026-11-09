using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Controllers;
using RaceDay.API.Data;
using RaceDay.API.DTOs.Events;
using RaceDay.API.Models;
using Xunit;

namespace raceDAY.Test;

public class EventControllerTests
{
    private RaceDayDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<RaceDayDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new RaceDayDbContext(options);
    }

    // Helper: Seed a full scenario with an organiser and events
    private async Task<(RaceDayDbContext context, int userId)> SeedOrganiserWithEventsAsync()
    {
        var context = GetInMemoryDbContext();

        context.Roles.Add(new Role { RoleID = 1, RoleName = "Organiser" });
        context.Roles.Add(new Role { RoleID = 2, RoleName = "Participant" });

        var user = new User
        {
            UserID = 1,
            RoleID = 1,
            Email = "org@test.com",
            PasswordHash = "hashed",
            FirstName = "Test",
            LastName = "Organiser"
        };
        context.Users.Add(user);

        var organiser = new Organiser
        {
            OrganiserID = 1,
            UserID = 1,
            OrganisationName = "Test Organisation"
        };
        context.Organisers.Add(organiser);

        context.Events.AddRange(
            new Event
            {
                EventID = 1,
                OrganiserID = 1,
                Name = "Active Event",
                Description = "An active event",
                EventDate = DateTime.UtcNow.AddDays(30),
                Location = "Cape Town",
                RouteDistanceKM = 42.2m,
                RegistrationDeadline = DateTime.UtcNow.AddDays(15),
                IsActive = true
            },
            new Event
            {
                EventID = 2,
                OrganiserID = 1,
                Name = "Inactive Event",
                Description = "An inactive event",
                EventDate = DateTime.UtcNow.AddDays(60),
                Location = "Durban",
                RouteDistanceKM = 21.1m,
                RegistrationDeadline = DateTime.UtcNow.AddDays(30),
                IsActive = false
            }
        );

        await context.SaveChangesAsync();
        return (context, user.UserID);
    }

    // Helper: Attach a fake authenticated user to the controller
    private void SetControllerUser(ControllerBase controller, int userId, string role)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    // ---------- GET ALL EVENTS ----------

    [Fact]
    public async Task GetAllEvents_WithActiveOnlyTrue_ReturnsOnlyActiveEvents()
    {
        // Arrange
        var (context, _) = await SeedOrganiserWithEventsAsync();
        var controller = new EventsController(context);

        // Act
        var result = await controller.GetAllEvents(true);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var events = Assert.IsAssignableFrom<IEnumerable<EventDto>>(okResult.Value);
        Assert.Single(events);
        Assert.Equal("Active Event", events.First().Name);
    }

    [Fact]
    public async Task GetAllEvents_WithActiveOnlyFalse_ReturnsAllEvents()
    {
        // Arrange
        var (context, _) = await SeedOrganiserWithEventsAsync();
        var controller = new EventsController(context);

        // Act
        var result = await controller.GetAllEvents(false);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var events = Assert.IsAssignableFrom<IEnumerable<EventDto>>(okResult.Value);
        Assert.Equal(2, events.Count());
    }

    // ---------- GET SINGLE EVENT ----------

    [Fact]
    public async Task GetEvent_WithValidId_ReturnsOkWithEvent()
    {
        // Arrange
        var (context, _) = await SeedOrganiserWithEventsAsync();
        var controller = new EventsController(context);

        // Act
        var result = await controller.GetEvent(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetEvent_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var (context, _) = await SeedOrganiserWithEventsAsync();
        var controller = new EventsController(context);

        // Act
        var result = await controller.GetEvent(9999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ---------- CREATE EVENT ----------

    [Fact]
    public async Task CreateEvent_AsOrganiser_WithValidData_ReturnsCreated()
    {
        // Arrange
        var (context, userId) = await SeedOrganiserWithEventsAsync();
        var controller = new EventsController(context);
        SetControllerUser(controller, userId, "Organiser");

        var dto = new CreateEventDto
        {
            Name = "New Marathon",
            Description = "A new test marathon",
            EventDate = DateTime.UtcNow.AddDays(90),
            Location = "Johannesburg",
            RouteDistanceKM = 42.2m,
            RegistrationDeadline = DateTime.UtcNow.AddDays(60)
        };

        // Act
        var result = await controller.CreateEvent(dto);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result);
        var eventCount = await context.Events.CountAsync();
        Assert.Equal(3, eventCount); // Was 2, now 3
    }

    [Fact]
    public async Task CreateEvent_WithRegistrationDeadlineAfterEventDate_ReturnsBadRequest()
    {
        // Arrange
        var (context, userId) = await SeedOrganiserWithEventsAsync();
        var controller = new EventsController(context);
        SetControllerUser(controller, userId, "Organiser");

        var dto = new CreateEventDto
        {
            Name = "Invalid Event",
            EventDate = DateTime.UtcNow.AddDays(30),
            Location = "Test",
            RouteDistanceKM = 10,
            RegistrationDeadline = DateTime.UtcNow.AddDays(60) // After event date!
        };

        // Act
        var result = await controller.CreateEvent(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ---------- UPDATE EVENT ----------

    [Fact]
    public async Task UpdateEvent_AsOwnerOrganiser_ReturnsOk()
    {
        // Arrange
        var (context, userId) = await SeedOrganiserWithEventsAsync();
        var controller = new EventsController(context);
        SetControllerUser(controller, userId, "Organiser");

        var dto = new UpdateEventDto
        {
            Name = "Updated Event Name",
            Location = "Updated Location"
        };

        // Act
        var result = await controller.UpdateEvent(1, dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var updated = await context.Events.FindAsync(1);
        Assert.Equal("Updated Event Name", updated!.Name);
    }

    [Fact]
    public async Task UpdateEvent_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var (context, userId) = await SeedOrganiserWithEventsAsync();
        var controller = new EventsController(context);
        SetControllerUser(controller, userId, "Organiser");

        // Act
        var result = await controller.UpdateEvent(9999, new UpdateEventDto { Name = "X" });

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ---------- DELETE EVENT ----------

    [Fact]
    public async Task DeleteEvent_AsOwnerOrganiser_ReturnsNoContent()
    {
        // Arrange
        var (context, userId) = await SeedOrganiserWithEventsAsync();
        var controller = new EventsController(context);
        SetControllerUser(controller, userId, "Organiser");

        // Act
        var result = await controller.DeleteEvent(1);

        // Assert
        Assert.IsType<NoContentResult>(result);
        var deleted = await context.Events.FindAsync(1);
        Assert.False(deleted!.IsActive); // Soft deleted
    }

    [Fact]
    public async Task DeleteEvent_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var (context, userId) = await SeedOrganiserWithEventsAsync();
        var controller = new EventsController(context);
        SetControllerUser(controller, userId, "Organiser");

        // Act
        var result = await controller.DeleteEvent(9999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }
}

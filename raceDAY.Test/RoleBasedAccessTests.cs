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

public class RoleBasedAccessTests
{
    private RaceDayDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<RaceDayDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new RaceDayDbContext(options);
    }

    private async Task<RaceDayDbContext> SeedFullScenarioAsync()
    {
        var context = GetInMemoryDbContext();

        context.Roles.AddRange(
            new Role { RoleID = 1, RoleName = "Organiser" },
            new Role { RoleID = 2, RoleName = "Participant" }
        );

        // Organiser user
        context.Users.Add(new User
        {
            UserID = 1,
            RoleID = 1,
            Email = "organiser@test.com",
            PasswordHash = "hashed",
            FirstName = "Org",
            LastName = "User"
        });
        context.Organisers.Add(new Organiser
        {
            OrganiserID = 1,
            UserID = 1,
            OrganisationName = "Test Org"
        });

        // Participant user
        context.Users.Add(new User
        {
            UserID = 2,
            RoleID = 2,
            Email = "participant@test.com",
            PasswordHash = "hashed",
            FirstName = "Part",
            LastName = "User"
        });
        context.Participants.Add(new Participant
        {
            ParticipantID = 1,
            UserID = 2
        });

        // An event owned by the organiser
        context.Events.Add(new Event
        {
            EventID = 1,
            OrganiserID = 1,
            Name = "Test Event",
            EventDate = DateTime.UtcNow.AddDays(30),
            Location = "Test Location",
            RouteDistanceKM = 42.2m,
            RegistrationDeadline = DateTime.UtcNow.AddDays(15),
            IsActive = true
        });

        await context.SaveChangesAsync();
        return context;
    }

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

    // ---------- PARTICIPANT CANNOT CREATE EVENTS ----------

    [Fact]
    public async Task CreateEvent_AsParticipant_ReturnsForbidden()
    {
        // Arrange
        var context = await SeedFullScenarioAsync();
        var controller = new EventsController(context);

        // Set user as PARTICIPANT (userID 2)
        SetControllerUser(controller, 2, "Participant");

        var dto = new CreateEventDto
        {
            Name = "Should Not Be Created",
            EventDate = DateTime.UtcNow.AddDays(60),
            Location = "Nowhere",
            RouteDistanceKM = 10,
            RegistrationDeadline = DateTime.UtcNow.AddDays(30)
        };

        // Act
        var result = await controller.CreateEvent(dto);

        // Assert
        Assert.IsType<ForbidResult>(result);

        // Verify no new event was created
        var eventCount = await context.Events.CountAsync();
        Assert.Equal(1, eventCount); // Still only the seeded event
    }

    [Fact]
    public async Task UpdateEvent_AsParticipant_ReturnsForbidden()
    {
        // Arrange
        var context = await SeedFullScenarioAsync();
        var controller = new EventsController(context);
        SetControllerUser(controller, 2, "Participant");

        // Act
        var result = await controller.UpdateEvent(1, new UpdateEventDto { Name = "Hacked" });

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeleteEvent_AsParticipant_ReturnsForbidden()
    {
        // Arrange
        var context = await SeedFullScenarioAsync();
        var controller = new EventsController(context);
        SetControllerUser(controller, 2, "Participant");

        // Act
        var result = await controller.DeleteEvent(1);

        // Assert
        Assert.IsType<ForbidResult>(result);

        // Verify event is still active
        var eventEntity = await context.Events.FindAsync(1);
        Assert.True(eventEntity!.IsActive);
    }

    // ---------- ORGANISER CANNOT UPDATE OTHERS' EVENTS ----------

    [Fact]
    public async Task UpdateEvent_ByDifferentOrganiser_ReturnsForbidden()
    {
        // Arrange
        var context = await SeedFullScenarioAsync();

        // Create a second organiser
        context.Users.Add(new User
        {
            UserID = 3,
            RoleID = 1,
            Email = "organiser2@test.com",
            PasswordHash = "hashed",
            FirstName = "Second",
            LastName = "Org"
        });
        context.Organisers.Add(new Organiser
        {
            OrganiserID = 2,
            UserID = 3,
            OrganisationName = "Other Org"
        });
        await context.SaveChangesAsync();

        var controller = new EventsController(context);
        SetControllerUser(controller, 3, "Organiser"); // Different organiser

        // Act — try to update event ID 1 (owned by organiser 1)
        var result = await controller.UpdateEvent(1, new UpdateEventDto { Name = "Stolen" });

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeleteEvent_ByDifferentOrganiser_ReturnsForbidden()
    {
        // Arrange
        var context = await SeedFullScenarioAsync();

        context.Users.Add(new User
        {
            UserID = 3,
            RoleID = 1,
            Email = "organiser2@test.com",
            PasswordHash = "hashed",
            FirstName = "Second",
            LastName = "Org"
        });
        context.Organisers.Add(new Organiser
        {
            OrganiserID = 2,
            UserID = 3,
            OrganisationName = "Other Org"
        });
        await context.SaveChangesAsync();

        var controller = new EventsController(context);
        SetControllerUser(controller, 3, "Organiser");

        // Act
        var result = await controller.DeleteEvent(1);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    // ---------- ORGANISER CANNOT CREATE EVENTS IF NOT REGISTERED AS ORGANISER ----------

    [Fact]
    public async Task CreateEvent_WithOrganiserRoleButNoOrganiserProfile_ReturnsForbidden()
    {
        // Arrange — user has Organiser role but no Organiser profile in the DB
        var context = GetInMemoryDbContext();
        context.Roles.AddRange(
            new Role { RoleID = 1, RoleName = "Organiser" },
            new Role { RoleID = 2, RoleName = "Participant" }
        );
        context.Users.Add(new User
        {
            UserID = 99,
            RoleID = 1,
            Email = "orphan@test.com",
            PasswordHash = "hashed",
            FirstName = "Orphan",
            LastName = "User"
        });
        await context.SaveChangesAsync();

        var controller = new EventsController(context);
        SetControllerUser(controller, 99, "Organiser");

        var dto = new CreateEventDto
        {
            Name = "Test",
            EventDate = DateTime.UtcNow.AddDays(30),
            Location = "X",
            RouteDistanceKM = 10,
            RegistrationDeadline = DateTime.UtcNow.AddDays(20)
        };

        // Act
        var result = await controller.CreateEvent(dto);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    // ---------- PUBLIC ENDPOINTS DON'T REQUIRE AUTH ----------

    [Fact]
    public async Task GetAllEvents_AsAnonymous_ReturnsOk()
    {
        // Arrange
        var context = await SeedFullScenarioAsync();
        var controller = new EventsController(context);
        // Do NOT set a user — simulate anonymous

        // Act
        var result = await controller.GetAllEvents(true);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetEvent_AsAnonymous_ReturnsOk()
    {
        // Arrange
        var context = await SeedFullScenarioAsync();
        var controller = new EventsController(context);

        // Act
        var result = await controller.GetEvent(1);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }
}
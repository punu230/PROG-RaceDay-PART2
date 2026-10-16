using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RaceDay.API.Data;
using RaceDay.API.DTOs.Auth;
using RaceDay.API.Models;
using RaceDay.API.Services;
using Xunit;

// Unit tests for the AuthService.
namespace raceDAY.Test;

public class AuthServiceTests
{
    // Helper: Creates a fresh in-memory database for each test
    private RaceDayDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<RaceDayDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new RaceDayDbContext(options);

        // Seed required roles
        context.Roles.AddRange(
            new Role { RoleID = 1, RoleName = "Organiser" },
            new Role { RoleID = 2, RoleName = "Participant" }
        );
        context.SaveChanges();

        return context;
    }

    // Helper: Creates a real TokenService with test config
    private ITokenService GetTokenService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "Jwt:Key", "ThisIsAVerySecretKeyForRaceDaySystem2026MakeItLongEnoughForSecurity!" },
                { "Jwt:Issuer", "RaceDayAPI" },
                { "Jwt:Audience", "RaceDayClient" }
            })
            .Build();

        return new TokenService(config);
    }

    // ---------- REGISTER TESTS ----------

    [Fact]
    public async Task RegisterAsync_WithValidOrganiserData_ReturnsAuthResponse()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var authService = new AuthService(context, GetTokenService());

        var dto = new RegisterDto
        {
            Email = "organiser@test.com",
            Password = "Password123!",
            FirstName = "John",
            LastName = "Doe",
            RoleName = "Organiser",
            OrganisationName = "Test Events Co"
        };

        // Act
        var result = await authService.RegisterAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("organiser@test.com", result.Email);
        Assert.Equal("Organiser", result.RoleName);
        Assert.NotEmpty(result.Token);

        // Verify organiser profile was created
        var organiser = await context.Organisers.FirstOrDefaultAsync();
        Assert.NotNull(organiser);
        Assert.Equal("Test Events Co", organiser.OrganisationName);
    }

    [Fact]
    public async Task RegisterAsync_WithValidParticipantData_ReturnsAuthResponse()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var authService = new AuthService(context, GetTokenService());

        var dto = new RegisterDto
        {
            Email = "participant@test.com",
            Password = "Password123!",
            FirstName = "Jane",
            LastName = "Smith",
            RoleName = "Participant",
            EmergencyContactName = "John Smith",
            EmergencyContactPhone = "0821234567"
        };

        // Act
        var result = await authService.RegisterAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Participant", result.RoleName);

        var participant = await context.Participants.FirstOrDefaultAsync();
        Assert.NotNull(participant);
        Assert.Equal("John Smith", participant.EmergencyContactName);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var authService = new AuthService(context, GetTokenService());

        var dto = new RegisterDto
        {
            Email = "duplicate@test.com",
            Password = "Password123!",
            FirstName = "First",
            LastName = "User",
            RoleName = "Participant"
        };

        await authService.RegisterAsync(dto);

        // Act & Assert
        var duplicate = new RegisterDto
        {
            Email = "duplicate@test.com",
            Password = "Password456!",
            FirstName = "Second",
            LastName = "User",
            RoleName = "Participant"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => authService.RegisterAsync(duplicate));
    }

    [Fact]
    public async Task RegisterAsync_WithInvalidRole_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var authService = new AuthService(context, GetTokenService());

        var dto = new RegisterDto
        {
            Email = "invalid@test.com",
            Password = "Password123!",
            FirstName = "Test",
            LastName = "User",
            RoleName = "SuperAdmin"  // This role doesn't exist
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => authService.RegisterAsync(dto));
    }

    [Fact]
    public async Task RegisterAsync_HashesPassword_NotStoredInPlainText()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var authService = new AuthService(context, GetTokenService());

        var dto = new RegisterDto
        {
            Email = "hashtest@test.com",
            Password = "MyPassword123!",
            FirstName = "Hash",
            LastName = "Test",
            RoleName = "Participant"
        };

        // Act
        await authService.RegisterAsync(dto);

        // Assert
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == "hashtest@test.com");
        Assert.NotNull(user);
        Assert.NotEqual("MyPassword123!", user.PasswordHash);
        Assert.True(user.PasswordHash.Length > 20); // BCrypt hashes are long
    }

    // ---------- LOGIN TESTS ----------

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var authService = new AuthService(context, GetTokenService());

        await authService.RegisterAsync(new RegisterDto
        {
            Email = "login@test.com",
            Password = "Password123!",
            FirstName = "Login",
            LastName = "Test",
            RoleName = "Participant"
        });

        var loginDto = new LoginDto
        {
            Email = "login@test.com",
            Password = "Password123!"
        };

        // Act
        var result = await authService.LoginAsync(loginDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("login@test.com", result.Email);
        Assert.NotEmpty(result.Token);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var authService = new AuthService(context, GetTokenService());

        await authService.RegisterAsync(new RegisterDto
        {
            Email = "wrongpass@test.com",
            Password = "CorrectPassword123!",
            FirstName = "Test",
            LastName = "User",
            RoleName = "Participant"
        });

        var loginDto = new LoginDto
        {
            Email = "wrongpass@test.com",
            Password = "WrongPassword!"
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => authService.LoginAsync(loginDto));
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentEmail_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var authService = new AuthService(context, GetTokenService());

        var loginDto = new LoginDto
        {
            Email = "nobody@test.com",
            Password = "Password123!"
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => authService.LoginAsync(loginDto));
    }

    [Fact]
    public async Task LoginAsync_ReturnsCorrectRole()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var authService = new AuthService(context, GetTokenService());

        await authService.RegisterAsync(new RegisterDto
        {
            Email = "org@test.com",
            Password = "Password123!",
            FirstName = "Org",
            LastName = "User",
            RoleName = "Organiser",
            OrganisationName = "Test Org"
        });

        // Act
        var result = await authService.LoginAsync(new LoginDto
        {
            Email = "org@test.com",
            Password = "Password123!"
        });

        // Assert
        Assert.Equal("Organiser", result.RoleName);
    }
}

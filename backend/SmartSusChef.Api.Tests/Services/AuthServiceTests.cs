using Xunit;
using Microsoft.EntityFrameworkCore;
using SmartSusChef.Api.Data;
using SmartSusChef.Api.Services;
using SmartSusChef.Api.Models;
using SmartSusChef.Api.DTOs;
using Moq;
using Microsoft.Extensions.Configuration;

namespace SmartSusChef.Api.Tests.Services;

public class AuthServiceTests
{
    private ApplicationDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private IConfiguration GetConfiguration()
    {
        var inMemorySettings = new Dictionary<string, string> {
            {"Jwt:Key", "SuperSecretKeyForTestingPurposesOnly123!"},
            {"Jwt:Issuer", "TestIssuer"},
            {"Jwt:Audience", "TestAudience"},
            {"Jwt:ExpiryMinutes", "60"}
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    [Fact]
    public async Task RegisterManagerAsync_ShouldHashPassword()
    {
        // 1. Arrange
        var context = GetDbContext();
        var config = GetConfiguration();
        var service = new AuthService(context, config);

        var request = new RegisterManagerRequest("testuser", "password123", "Test User", "test@example.com");

        // 2. Act
        var result = await service.RegisterManagerAsync(request);

        // 3. Assert
        Assert.NotNull(result);
        Assert.Equal(RegisterErrorType.None, result.ErrorType);
        Assert.NotNull(result.Response);
        Assert.Equal("testuser", result.Response.User.Username);

        var user = await context.Users.FirstOrDefaultAsync(u => u.Username == "testuser");
        Assert.NotNull(user);
        Assert.NotEqual("password123", user.PasswordHash); // Password should be hashed
        Assert.True(BCrypt.Net.BCrypt.Verify("password123", user.PasswordHash)); // Verify hash works
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnToken_ForValidCredentials()
    {
        // 1. Arrange
        var context = GetDbContext();
        var config = GetConfiguration();
        var service = new AuthService(context, config);

        // Create a user with a known password hash
        var password = "password123";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
        
        // A store must exist for the user
        var store = new Store { Id = 1, StoreName = "Test Store" };
        context.Store.Add(store);
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            PasswordHash = passwordHash,
            Name = "Test User",
            Role = UserRole.Employee,
            StoreId = store.Id
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var request = new LoginRequest("testuser", password);

        // 2. Act
        var result = await service.LoginAsync(request);

        // 3. Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Token);
        Assert.NotEmpty(result.Token);
        Assert.Equal("testuser", result.User.Username);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnNull_ForInvalidPassword()
    {
        // 1. Arrange
        var context = GetDbContext();
        var config = GetConfiguration();
        var service = new AuthService(context, config);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Name = "Test User",
            Role = UserRole.Employee,
            StoreId = 1
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var request = new LoginRequest("testuser", "wrongpassword");

        // 2. Act
        var result = await service.LoginAsync(request);

        // 3. Assert
        Assert.Null(result);
    }
}

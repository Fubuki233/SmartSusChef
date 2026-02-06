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
        var inMemorySettings = new Dictionary<string, string?> {
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

    [Fact]
    public async Task CreateUserAsync_ShouldCreateUser()
    {
        // Arrange
        var context = GetDbContext();
        var config = GetConfiguration();
        var service = new AuthService(context, config);
        var request = new CreateUserRequest("newuser", "password", "New User", "new@example.com", "Employee");

        // Act
        var result = await service.CreateUserAsync(request, 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("newuser", result.Username);
        var userInDb = await context.Users.FirstOrDefaultAsync(u => u.Username == "newuser");
        Assert.NotNull(userInDb);
    }

    [Fact]
    public async Task GetAllUsersAsync_ShouldReturnAllUsersForStore()
    {
        // Arrange
        var context = GetDbContext();
        var config = GetConfiguration();
        var service = new AuthService(context, config);
        var storeId = 1;
        context.Users.Add(new User { Id = Guid.NewGuid(), Username = "user1", StoreId = storeId });
        context.Users.Add(new User { Id = Guid.NewGuid(), Username = "user2", StoreId = storeId });
        context.Users.Add(new User { Id = Guid.NewGuid(), Username = "user3", StoreId = 2 });
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAllUsersAsync(storeId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldUpdateUser()
    {
        // Arrange
        var context = GetDbContext();
        var config = GetConfiguration();
        var service = new AuthService(context, config);
        var userId = Guid.NewGuid();
        context.Users.Add(new User { Id = userId, Username = "olduser", StoreId = 1 });
        await context.SaveChangesAsync();
        var request = new UpdateUserRequest("newuser", null, "New Name", null, null, null);

        // Act
        var result = await service.UpdateUserAsync(userId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("newuser", result.Username);
        Assert.Equal("New Name", result.Name);
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldDeleteUser()
    {
        // Arrange
        var context = GetDbContext();
        var config = GetConfiguration();
        var service = new AuthService(context, config);
        var userId = Guid.NewGuid();
        context.Users.Add(new User { Id = userId, Username = "todelete", StoreId = 1 });
        await context.SaveChangesAsync();

        // Act
        var result = await service.DeleteUserAsync(userId);

        // Assert
        Assert.True(result);
        var userInDb = await context.Users.FindAsync(userId);
        Assert.Null(userInDb);
    }

    [Fact]
    public async Task UpdateProfileAsync_ShouldUpdateNameAndEmail()
    {
        // Arrange
        var context = GetDbContext();
        var config = GetConfiguration();
        var service = new AuthService(context, config);
        var userId = Guid.NewGuid();
        context.Users.Add(new User { Id = userId, Name = "Old Name", Email = "old@example.com", StoreId = 1 });
        await context.SaveChangesAsync();
        var request = new UpdateProfileRequest("New Name", "new@example.com");

        // Act
        var result = await service.UpdateProfileAsync(userId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Name", result.Name);
        Assert.Equal("new@example.com", result.Email);
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldChangePassword_WhenCurrentPasswordIsCorrect()
    {
        // Arrange
        var context = GetDbContext();
        var config = GetConfiguration();
        var service = new AuthService(context, config);
        var userId = Guid.NewGuid();
        var password = "password123";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
        context.Users.Add(new User { Id = userId, PasswordHash = passwordHash, StoreId = 1 });
        await context.SaveChangesAsync();

        // Act
        var result = await service.ChangePasswordAsync(userId, password, "newPassword");

        // Assert
        Assert.True(result);
        var userInDb = await context.Users.FindAsync(userId);
        Assert.NotNull(userInDb);
        Assert.True(BCrypt.Net.BCrypt.Verify("newPassword", userInDb.PasswordHash));
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldResetPasswordAndReturnTempPassword()
    {
        // Arrange
        var context = GetDbContext();
        var config = GetConfiguration();
        var service = new AuthService(context, config);
        var user = new User { Id = Guid.NewGuid(), Username = "testuser", Email = "test@example.com", StoreId = 1 };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var result = await service.ResetPasswordAsync(user.Username);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task IsStoreSetupRequiredAsync_ShouldReturnTrue_WhenStoreNameIsEmpty()
    {
        // Arrange
        var context = GetDbContext();
        var config = GetConfiguration();
        var service = new AuthService(context, config);
        var userId = Guid.NewGuid();
        context.Users.Add(new User { Id = userId, StoreId = 1 });
        context.Store.Add(new Store { Id = 1 });
        await context.SaveChangesAsync();

        // Act
        var result = await service.IsStoreSetupRequiredAsync(userId);

        // Assert
        Assert.True(result);
    }
}

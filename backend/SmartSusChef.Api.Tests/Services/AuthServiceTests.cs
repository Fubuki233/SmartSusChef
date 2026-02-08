using Xunit;
using Microsoft.EntityFrameworkCore;
using SmartSusChef.Api.Data;
using SmartSusChef.Api.Services;
using SmartSusChef.Api.Models;
using SmartSusChef.Api.DTOs;
using Moq;
using Microsoft.Extensions.Configuration;
using System.Reflection;

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
    public async Task LoginAsync_ShouldSetStoreSetupRequired_WhenStoreNameIsEmpty()
    {
        // Arrange
        var context = GetDbContext();
        var config = GetConfiguration();
        var service = new AuthService(context, config);

        var password = "password123";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        var store = new Store { Id = 10, StoreName = "" };
        context.Store.Add(store);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "setupuser",
            PasswordHash = passwordHash,
            Name = "Setup User",
            Role = UserRole.Manager,
            StoreId = store.Id
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var result = await service.LoginAsync(new LoginRequest("setupuser", password));

        // Assert
        Assert.NotNull(result);
        Assert.True(result.StoreSetupRequired);
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
    public async Task CreateUserAsync_ShouldReturnNull_WhenUsernameExists()
    {
        // Arrange
        var context = GetDbContext();
        var config = GetConfiguration();
        var service = new AuthService(context, config);
        var storeId = 1;
        context.Users.Add(new User { Id = Guid.NewGuid(), Username = "existinguser", StoreId = storeId });
        await context.SaveChangesAsync();

        var request = new CreateUserRequest("existinguser", "password", "New User", "new@example.com", "Employee");

        // Act
        var result = await service.CreateUserAsync(request, storeId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldReturnNull_ForCaseInsensitiveUsername()
    {
        // Arrange
        var context = GetDbContext();
        var config = GetConfiguration();
        var service = new AuthService(context, config);
        var storeId = 1;
        context.Users.Add(new User { Id = Guid.NewGuid(), Username = "ExistingUser", StoreId = storeId });
        await context.SaveChangesAsync();

        var request = new CreateUserRequest("existinguser", "password", "New User", "new@example.com", "Employee");

        // Act
        var result = await service.CreateUserAsync(request, storeId);

        // Assert
        Assert.Null(result);
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
    public async Task GetUserByIdAsync_ShouldReturnNull_WhenMissing()
    {
        // Arrange
        var context = GetDbContext();
        var config = GetConfiguration();
        var service = new AuthService(context, config);

        // Act
        var result = await service.GetUserByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldReturnUserDto_WhenFound()
    {
        // Arrange
        var context = GetDbContext();
        var config = GetConfiguration();
        var service = new AuthService(context, config);
        var userId = Guid.NewGuid();
        context.Users.Add(new User
        {
            Id = userId,
            Username = "u1",
            Name = "User One",
            Email = "u1@example.com",
            Role = UserRole.Employee,
            StoreId = 1
        });
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetUserByIdAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("u1", result.Username);
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
    public async Task UpdateUserAsync_ShouldReturnNull_WhenUserMissing()
    {
        // Arrange
        var context = GetDbContext();
        var config = GetConfiguration();
        var service = new AuthService(context, config);

        // Act
        var result = await service.UpdateUserAsync(Guid.NewGuid(), new UpdateUserRequest("x", null, null, null, null, null));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldReturnNull_WhenUsernameTaken()
    {
        // Arrange
        var context = GetDbContext();
        var config = GetConfiguration();
        var service = new AuthService(context, config);
        var targetId = Guid.NewGuid();
        context.Users.Add(new User { Id = targetId, Username = "user1", StoreId = 1 });
        context.Users.Add(new User { Id = Guid.NewGuid(), Username = "user2", StoreId = 1 });
        await context.SaveChangesAsync();

        // Act
        var result = await service.UpdateUserAsync(targetId, new UpdateUserRequest("user2", null, null, null, null, null));

        // Assert
        Assert.Null(result);
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
    public async Task DeleteUserAsync_ShouldReturnFalse_WhenMissing()
    {
        // Arrange
        var context = GetDbContext();
        var config = GetConfiguration();
        var service = new AuthService(context, config);

        // Act
        var result = await service.DeleteUserAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
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
    public async Task UpdateProfileAsync_ShouldReturnNull_WhenUserMissing()
    {
        // Arrange
        var context = GetDbContext();
        var config = GetConfiguration();
        var service = new AuthService(context, config);

        // Act
        var result = await service.UpdateProfileAsync(Guid.NewGuid(), new UpdateProfileRequest("New", "new@example.com"));

        // Assert
        Assert.Null(result);
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
    public async Task ChangePasswordAsync_ShouldReturnFalse_WhenUserMissing()
    {
        // Arrange
        var context = GetDbContext();
        var config = GetConfiguration();
        var service = new AuthService(context, config);

        // Act
        var result = await service.ChangePasswordAsync(Guid.NewGuid(), "old", "new");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldReturnFalse_WhenPasswordIncorrect()
    {
        // Arrange
        var context = GetDbContext();
        var config = GetConfiguration();
        var service = new AuthService(context, config);
        var userId = Guid.NewGuid();
        context.Users.Add(new User { Id = userId, PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct"), StoreId = 1 });
        await context.SaveChangesAsync();

        // Act
        var result = await service.ChangePasswordAsync(userId, "wrong", "new");

        // Assert
        Assert.False(result);
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
    public async Task ResetPasswordAsync_ShouldReturnNull_WhenUserMissing()
    {
        // Arrange
        var context = GetDbContext();
        var config = GetConfiguration();
        var service = new AuthService(context, config);

        // Act
        var result = await service.ResetPasswordAsync("missing@example.com");

        // Assert
        Assert.Null(result);
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

    [Fact]
    public async Task IsStoreSetupRequiredAsync_ShouldReturnTrue_WhenUserMissing()
    {
        // Arrange
        var context = GetDbContext();
        var config = GetConfiguration();
        var service = new AuthService(context, config);

        // Act
        var result = await service.IsStoreSetupRequiredAsync(Guid.NewGuid());

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsStoreSetupRequiredAsync_ShouldReturnFalse_WhenStoreNameIsSet()
    {
        // Arrange
        var context = GetDbContext();
        var config = GetConfiguration();
        var service = new AuthService(context, config);
        var userId = Guid.NewGuid();
        context.Store.Add(new Store { Id = 2, StoreName = "Main" });
        context.Users.Add(new User { Id = userId, StoreId = 2 });
        await context.SaveChangesAsync();

        // Act
        var result = await service.IsStoreSetupRequiredAsync(userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RegisterManagerAsync_ShouldReturnUsernameExists_WhenDuplicate()
    {
        // Arrange
        var context = GetDbContext();
        var config = GetConfiguration();
        var service = new AuthService(context, config);
        context.Users.Add(new User { Id = Guid.NewGuid(), Username = "dupuser", StoreId = 1 });
        await context.SaveChangesAsync();

        // Act
        var result = await service.RegisterManagerAsync(new RegisterManagerRequest("dupuser", "pw", "Dup", "dup@example.com"));

        // Assert
        Assert.Equal(RegisterErrorType.UsernameExists, result.ErrorType);
        Assert.Null(result.Response);
    }

    [Fact]
    public void GenerateUniqueStoreId_ShouldReturnPositiveInteger()
    {
        // Arrange
        // Use reflection to invoke private static method
        var methodInfo = typeof(AuthService).GetMethod("GenerateUniqueStoreId", BindingFlags.NonPublic | BindingFlags.Static);

        // Act
        var result = (int)methodInfo?.Invoke(null, null)!;

        // Assert
        Assert.True(result > 0);
    }
}

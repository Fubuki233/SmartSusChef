using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using SmartSusChef.Api.Controllers;
using SmartSusChef.Api.DTOs;
using SmartSusChef.Api.Services;
using System;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SmartSusChef.Api.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly Mock<ILogger<AuthController>> _mockLogger;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        _mockLogger = new Mock<ILogger<AuthController>>();
        _controller = new AuthController(_mockAuthService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Register_ShouldReturnCreatedAtAction_WhenRegistrationIsSuccessful()
    {
        // Arrange
        var request = new RegisterManagerRequest("testuser", "password", "Test User", "test@example.com");
        var userDto = new UserDto(Guid.NewGuid().ToString(), "testuser", "Test User", "test@example.com", "Manager", "Active", DateTime.UtcNow, DateTime.UtcNow);
        var registerResponse = new RegisterResponse("token", userDto, false);
        var registerResult = new RegisterResult(registerResponse, RegisterErrorType.None);

        _mockAuthService.Setup(s => s.RegisterManagerAsync(request)).ReturnsAsync(registerResult);

        // Act
        var result = await _controller.Register(request);

        // Assert
        var actionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal("GetCurrentUser", actionResult.ActionName);
        var value = Assert.IsType<RegisterResponse>(actionResult.Value);
        Assert.Equal(userDto.Username, value.User.Username);
    }

    [Fact]
    public async Task Register_ShouldReturnConflict_WhenUsernameExists()
    {
        // Arrange
        var request = new RegisterManagerRequest("testuser", "password", "Test User", "test@example.com");
        var registerResult = new RegisterResult(null, RegisterErrorType.UsernameExists);

        _mockAuthService.Setup(s => s.RegisterManagerAsync(request)).ReturnsAsync(registerResult);

        // Act
        var result = await _controller.Register(request);

        // Assert
        var actionResult = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.NotNull(actionResult.Value);
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenRegistrationFailsForOtherReasons()
    {
        // Arrange
        var request = new RegisterManagerRequest("testuser", "password", "Test User", "test@example.com");
        var registerResult = new RegisterResult(null, RegisterErrorType.ManagerAlreadyExists);

        _mockAuthService.Setup(s => s.RegisterManagerAsync(request)).ReturnsAsync(registerResult);

        // Act
        var result = await _controller.Register(request);

        // Assert
        var actionResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(actionResult.Value);
    }

    [Fact]
    public async Task Register_ShouldReturn500_WhenExceptionIsThrown()
    {
        // Arrange
        var request = new RegisterManagerRequest("testuser", "password", "Test User", "test@example.com");
        _mockAuthService.Setup(s => s.RegisterManagerAsync(request)).ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.Register(request);

        // Assert
        var actionResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, actionResult.StatusCode);
    }

    [Fact]
    public async Task Login_ShouldReturnOk_WhenLoginIsSuccessful()
    {
        // Arrange
        var request = new LoginRequest("testuser", "password");
        var userDto = new UserDto(Guid.NewGuid().ToString(), "testuser", "Test User", "test@example.com", "Manager", "Active", DateTime.UtcNow, DateTime.UtcNow);
        var loginResponse = new LoginResponse("token", userDto, false);

        _mockAuthService.Setup(s => s.LoginAsync(request)).ReturnsAsync(loginResponse);

        // Act
        var result = await _controller.Login(request);

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsType<LoginResponse>(actionResult.Value);
        Assert.Equal(userDto.Username, value.User.Username);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenLoginFails()
    {
        // Arrange
        var request = new LoginRequest("testuser", "wrongpassword");
        _mockAuthService.Setup(s => s.LoginAsync(request)).ReturnsAsync((LoginResponse?)null);

        // Act
        var result = await _controller.Login(request);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task ForgotPassword_ShouldReturnOk_WithGenericMessage_WhenUserExists()
    {
        // Arrange
        var request = new ForgotPasswordRequest("test@example.com");
        var tempPassword = "tempPassword123";
        _mockAuthService.Setup(s => s.ResetPasswordAsync(request.EmailOrUsername)).ReturnsAsync(tempPassword);

        // Act
        var result = await _controller.ForgotPassword(request);

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsType<ForgotPasswordResponse>(actionResult.Value);
        Assert.NotNull(value.Message);
        // Should NOT contain the actual temp password (security fix)
        Assert.DoesNotContain(tempPassword, value.Message);
    }

    [Fact]
    public async Task ForgotPassword_ShouldReturnOk_WithSameMessage_WhenUserNotFound()
    {
        // Arrange - user does not exist
        var request = new ForgotPasswordRequest("unknown@example.com");
        _mockAuthService.Setup(s => s.ResetPasswordAsync(request.EmailOrUsername)).ReturnsAsync((string?)null);

        // Act
        var result = await _controller.ForgotPassword(request);

        // Assert - should still return Ok (prevents user enumeration)
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsType<ForgotPasswordResponse>(actionResult.Value);
        Assert.NotNull(value.Message);
    }

    [Fact]
    public async Task ForgotPassword_ShouldReturnBadRequest_WhenRequestIsInvalid()
    {
        // Arrange
        var request = new ForgotPasswordRequest(" ");

        // Act
        var result = await _controller.ForgotPassword(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    private void SetupUserClaims(Guid userId)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task GetCurrentUser_ShouldReturnUserDto_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userDto = new UserDto(userId.ToString(), "testuser", "Test User", "test@example.com", "Manager", "Active", DateTime.UtcNow, DateTime.UtcNow);
        SetupUserClaims(userId);
        _mockAuthService.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync(userDto);

        // Act
        var result = await _controller.GetCurrentUser();

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsType<UserDto>(actionResult.Value);
        Assert.Equal(userId.ToString(), value.Id);
    }

    [Fact]
    public async Task GetCurrentUser_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupUserClaims(userId);
        _mockAuthService.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync((UserDto?)null);

        // Act
        var result = await _controller.GetCurrentUser();

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CheckStoreSetupRequired_ShouldReturnOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupUserClaims(userId);
        _mockAuthService.Setup(s => s.IsStoreSetupRequiredAsync(userId)).ReturnsAsync(true);

        // Act
        var result = await _controller.CheckStoreSetupRequired();

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateProfile_ShouldReturnOk_WithUpdatedUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new UpdateProfileRequest("New Name", "new@example.com");
        var updatedUser = new UserDto(userId.ToString(), "testuser", "New Name", "new@example.com", "Manager", "Active", DateTime.UtcNow, DateTime.UtcNow);
        SetupUserClaims(userId);
        _mockAuthService.Setup(s => s.UpdateProfileAsync(userId, request)).ReturnsAsync(updatedUser);

        // Act
        var result = await _controller.UpdateProfile(request);

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsType<UserDto>(actionResult.Value);
        Assert.Equal("New Name", value.Name);
    }

    [Fact]
    public async Task ChangePassword_ShouldReturnNoContent_WhenSuccessful()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new ChangePasswordRequest("currentPassword", "newPassword");
        SetupUserClaims(userId);
        _mockAuthService.Setup(s => s.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword)).ReturnsAsync(true);

        // Act
        var result = await _controller.ChangePassword(request);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task ChangePassword_ShouldReturnBadRequest_WhenPasswordIsIncorrect()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new ChangePasswordRequest("wrongPassword", "newPassword");
        SetupUserClaims(userId);
        _mockAuthService.Setup(s => s.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword)).ReturnsAsync(false);

        // Act
        var result = await _controller.ChangePassword(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }
}

using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using SmartSusChef.Api.Controllers;
using SmartSusChef.Api.DTOs;
using SmartSusChef.Api.Services;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace SmartSusChef.Api.Tests.Controllers;

public class UsersControllerTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        _controller = new UsersController(_mockAuthService.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim("StoreId", "1"),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        }, "mock"));

        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = user }
        };
    }

    [Fact]
    public async Task GetAllUsers_ShouldReturnOk_WithListOfUsers()
    {
        // Arrange
        var users = new List<UserListDto> { new UserListDto(Guid.NewGuid().ToString(), "test", "test", "test", "test", "test", DateTime.UtcNow, DateTime.UtcNow) };
        _mockAuthService.Setup(s => s.GetAllUsersAsync(1)).ReturnsAsync(users);

        // Act
        var result = await _controller.GetAllUsers();

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsAssignableFrom<List<UserListDto>>(actionResult.Value);
        Assert.Single(value);
    }
    
    [Fact]
    public async Task CreateUser_ShouldReturnCreatedAtAction_WhenSuccessful()
    {
        // Arrange
        var request = new CreateUserRequest("test", "password", "test", "test", "test");
        var user = new UserListDto(Guid.NewGuid().ToString(), "test", "test", "test", "test", "test", DateTime.UtcNow, DateTime.UtcNow);
        _mockAuthService.Setup(s => s.CreateUserAsync(request, 1)).ReturnsAsync(user);

        // Act
        var result = await _controller.CreateUser(request);

        // Assert
        var actionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal("GetAllUsers", actionResult.ActionName);
    }

    [Fact]
    public async Task CreateUser_ShouldReturnBadRequest_WhenPasswordIsMissing()
    {
        // Arrange
        var request = new CreateUserRequest("test", "", "test", "test", "test");

        // Act
        var result = await _controller.CreateUser(request);

        // Assert
        var actionResult = Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateUser_ShouldReturnBadRequest_WhenPasswordIsTooShort()
    {
        // Arrange
        var request = new CreateUserRequest("test", "123", "test", "test", "test");

        // Act
        var result = await _controller.CreateUser(request);

        // Assert
        var actionResult = Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateUser_ShouldReturnConflict_WhenUsernameExists()
    {
        // Arrange
        var request = new CreateUserRequest("test", "password", "test", "test", "test");
        _mockAuthService.Setup(s => s.CreateUserAsync(request, 1)).ReturnsAsync((UserListDto?)null);

        // Act
        var result = await _controller.CreateUser(request);

        // Assert
        var actionResult = Assert.IsType<ConflictObjectResult>(result.Result);
    }
    
    [Fact]
    public async Task UpdateUser_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new UpdateUserRequest("test", "password", "test", "test", "test", "test");
        var user = new UserListDto(userId.ToString(), "test", "test", "test", "test", "test", DateTime.UtcNow, DateTime.UtcNow);
        _mockAuthService.Setup(s => s.UpdateUserAsync(userId, request)).ReturnsAsync(user);

        // Act
        var result = await _controller.UpdateUser(userId.ToString(), request);

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsType<UserListDto>(actionResult.Value);
        Assert.Equal(userId.ToString(), value.Id);
    }

    [Fact]
    public async Task UpdateUser_ShouldReturnBadRequest_WhenIdIsInvalid()
    {
        // Act
        var result = await _controller.UpdateUser("invalid-id", new UpdateUserRequest("test", "password", "test", "test", "test", "test"));

        // Assert
        var actionResult = Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateUser_ShouldReturnNotFound_WhenUserNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new UpdateUserRequest("test", "password", "test", "test", "test", "test");
        _mockAuthService.Setup(s => s.UpdateUserAsync(userId, request)).ReturnsAsync((UserListDto?)null);

        // Act
        var result = await _controller.UpdateUser(userId.ToString(), request);

        // Assert
        var actionResult = Assert.IsType<NotFoundObjectResult>(result.Result);
    }
    
    [Fact]
    public async Task DeleteUser_ShouldReturnNoContent_WhenSuccessful()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockAuthService.Setup(s => s.DeleteUserAsync(userId)).ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteUser(userId.ToString());

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteUser_ShouldReturnBadRequest_WhenIdIsInvalid()
    {
        // Act
        var result = await _controller.DeleteUser("invalid-id");

        // Assert
        var actionResult = Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DeleteUser_ShouldReturnBadRequest_WhenDeletingSelf()
    {
        // Arrange
        var userId = Guid.Parse(_controller.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // Act
        var result = await _controller.DeleteUser(userId.ToString());

        // Assert
        var actionResult = Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DeleteUser_ShouldReturnNotFound_WhenUserNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockAuthService.Setup(s => s.DeleteUserAsync(userId)).ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteUser(userId.ToString());

        // Assert
        var actionResult = Assert.IsType<NotFoundObjectResult>(result);
    }
}

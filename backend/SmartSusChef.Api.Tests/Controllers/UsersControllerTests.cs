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
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim("StoreId", "1")
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
        var users = new List<UserListDto> { new UserListDto(Guid.NewGuid().ToString(), "test", "Test", "test@test.com", "test", "test", DateTime.UtcNow, DateTime.UtcNow) };
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
        var request = new CreateUserRequest("newuser", "password", "newuser", "newuser@test.com", "Employee");
        var user = new UserListDto(Guid.NewGuid().ToString(), "newuser", "newuser", "newuser@test.com", "Employee", "Active", DateTime.UtcNow, DateTime.UtcNow);
        _mockAuthService.Setup(s => s.CreateUserAsync(request, 1)).ReturnsAsync(user);

        // Act
        var result = await _controller.CreateUser(request);

        // Assert
        var actionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal("GetAllUsers", actionResult.ActionName);
    }
    
    [Fact]
    public async Task UpdateUser_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new UpdateUserRequest("updateduser", null, "updateduser", null, "Manager", null);
        var user = new UserListDto(userId.ToString(), "updateduser", "updateduser", "updateduser@test.com", "Manager", "Active", DateTime.UtcNow, DateTime.UtcNow);
        _mockAuthService.Setup(s => s.UpdateUserAsync(userId, request)).ReturnsAsync(user);

        // Act
        var result = await _controller.UpdateUser(userId.ToString(), request);

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsType<UserListDto>(actionResult.Value);
        Assert.Equal("updateduser", value.Username);
    }
    
    [Fact]
    public async Task DeleteUser_ShouldReturnNoContent_WhenSuccessful()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockAuthService.Setup(s => s.DeleteUserAsync(userId)).ReturnsAsync(true);
        
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim("StoreId", "1")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = user }
        };


        // Act
        var result = await _controller.DeleteUser(userId.ToString());

        // Assert
        Assert.IsType<NoContentResult>(result);
    }
}

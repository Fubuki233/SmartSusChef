using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using SmartSusChef.Api.Controllers;
using SmartSusChef.Api.DTOs;
using SmartSusChef.Api.Services;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace SmartSusChef.Api.Tests.Controllers;

public class StoreControllerTests
{
    private readonly Mock<IStoreService> _mockStoreService;
    private readonly StoreController _controller;

    public StoreControllerTests()
    {
        _mockStoreService = new Mock<IStoreService>();
        _controller = new StoreController(_mockStoreService.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim("StoreId", "1")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = user }
        };
    }

    [Fact]
    public async Task GetStore_ShouldReturnOk_WithStoreDto()
    {
        // Arrange
        var store = new StoreDto(1, "test", "test", "Test Store", "test", "test", DateTime.UtcNow, 1, 1, "US", "test", true, DateTime.UtcNow, DateTime.UtcNow);
        _mockStoreService.Setup(s => s.GetStoreByIdAsync(1)).ReturnsAsync(store);

        // Act
        var result = await _controller.GetStore();

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsType<StoreDto>(actionResult.Value);
        Assert.Equal(1, value.Id);
    }
    
    [Fact]
    public async Task GetStoreStatus_ShouldReturnOk()
    {
        // Arrange
        _mockStoreService.Setup(s => s.IsStoreSetupCompleteAsync(1)).ReturnsAsync(true);

        // Act
        var result = await _controller.GetStoreStatus();

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(actionResult.Value);
    }
    
    [Fact]
    public async Task SetupStore_ShouldReturnOk_WithStoreDto()
    {
        // Arrange
        var request = new UpdateStoreRequest(null, null, "Test Store", null, null, null, null, null, null, null, null);
        var store = new StoreDto(1, "test", "test", "Test Store", "test", "test", DateTime.UtcNow, 1, 1, "US", "test", true, DateTime.UtcNow, DateTime.UtcNow);
        _mockStoreService.Setup(s => s.UpdateStoreByIdAsync(1, request)).ReturnsAsync(store);

        // Act
        var result = await _controller.SetupStore(request);

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsType<StoreDto>(actionResult.Value);
        Assert.Equal(1, value.Id);
    }
    
    [Fact]
    public async Task UpdateStore_ShouldReturnOk_WithStoreDto()
    {
        // Arrange
        var request = new UpdateStoreRequest(null, null, "Updated Store", null, null, null, null, null, null, null, null);
        var store = new StoreDto(1, "test", "test", "Updated Store", "test", "test", DateTime.UtcNow, 1, 1, "US", "test", true, DateTime.UtcNow, DateTime.UtcNow);
        _mockStoreService.Setup(s => s.UpdateStoreByIdAsync(1, request)).ReturnsAsync(store);

        // Act
        var result = await _controller.UpdateStore(request);

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsType<StoreDto>(actionResult.Value);
        Assert.Equal("Updated Store", value.StoreName);
    }
}

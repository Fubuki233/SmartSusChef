using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using SmartSusChef.Api.Controllers;
using SmartSusChef.Api.DTOs;
using SmartSusChef.Api.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartSusChef.Api.Tests.Controllers;

public class WastageControllerTests
{
    private readonly Mock<IWastageService> _mockWastageService;
    private readonly WastageController _controller;

    public WastageControllerTests()
    {
        _mockWastageService = new Mock<IWastageService>();
        _controller = new WastageController(_mockWastageService.Object);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOk_WithListOfWastageData()
    {
        // Arrange
        var wastageData = new List<WastageDataDto> { new WastageDataDto(Guid.NewGuid().ToString(), DateTime.UtcNow.ToString(), Guid.NewGuid().ToString(), null, "test", "kg", 1, 1, DateTime.UtcNow, DateTime.UtcNow) };
        _mockWastageService.Setup(s => s.GetAllAsync(null, null)).ReturnsAsync(wastageData);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsAssignableFrom<List<WastageDataDto>>(actionResult.Value);
        Assert.Single(value);
    }
    
    [Fact]
    public async Task GetById_ShouldReturnOk_WhenWastageDataExists()
    {
        // Arrange
        var wastageId = Guid.NewGuid();
        var wastageData = new WastageDataDto(wastageId.ToString(), DateTime.UtcNow.ToString(), Guid.NewGuid().ToString(), null, "test", "kg", 1, 1, DateTime.UtcNow, DateTime.UtcNow);
        _mockWastageService.Setup(s => s.GetByIdAsync(wastageId)).ReturnsAsync(wastageData);

        // Act
        var result = await _controller.GetById(wastageId);

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsType<WastageDataDto>(actionResult.Value);
        Assert.Equal(wastageId.ToString(), value.Id);
    }
    
    [Fact]
    public async Task GetTrend_ShouldReturnOk_WithListOfWastageTrend()
    {
        // Arrange
        var trend = new List<WastageTrendDto> { new WastageTrendDto(DateTime.UtcNow.ToString(), 1, 1, new List<ItemWastageDto>()) };
        _mockWastageService.Setup(s => s.GetTrendAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(trend);

        // Act
        var result = await _controller.GetTrend(DateTime.UtcNow, DateTime.UtcNow);

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsAssignableFrom<List<WastageTrendDto>>(actionResult.Value);
        Assert.Single(value);
    }
    
    [Fact]
    public async Task Create_ShouldReturnCreatedAtAction_WhenSuccessful()
    {
        // Arrange
        var request = new CreateWastageDataRequest(DateTime.UtcNow.ToString(), Guid.NewGuid().ToString(), null, 1);
        var wastageData = new WastageDataDto(Guid.NewGuid().ToString(), DateTime.UtcNow.ToString(), request.IngredientId, null, "test", "kg", 1, 1, DateTime.UtcNow, DateTime.UtcNow);
        _mockWastageService.Setup(s => s.CreateAsync(request)).ReturnsAsync(wastageData);

        // Act
        var result = await _controller.Create(request);

        // Assert
        var actionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal("GetById", actionResult.ActionName);
    }
    
    [Fact]
    public async Task Update_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var wastageId = Guid.NewGuid();
        var request = new UpdateWastageDataRequest(DateTime.UtcNow.ToString(), Guid.NewGuid().ToString(), null, 1);
        var wastageData = new WastageDataDto(wastageId.ToString(), DateTime.UtcNow.ToString(), request.IngredientId, null, "test", "kg", 1, 1, DateTime.UtcNow, DateTime.UtcNow);
        _mockWastageService.Setup(s => s.UpdateAsync(wastageId, request)).ReturnsAsync(wastageData);

        // Act
        var result = await _controller.Update(wastageId, request);

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsType<WastageDataDto>(actionResult.Value);
        Assert.Equal(wastageId.ToString(), value.Id);
    }
    
    [Fact]
    public async Task Delete_ShouldReturnNoContent_WhenSuccessful()
    {
        // Arrange
        var wastageId = Guid.NewGuid();
        _mockWastageService.Setup(s => s.DeleteAsync(wastageId)).ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(wastageId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }
}

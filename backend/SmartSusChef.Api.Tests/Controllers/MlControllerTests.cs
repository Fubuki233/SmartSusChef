using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using SmartSusChef.Api.Controllers;
using SmartSusChef.Api.DTOs;
using SmartSusChef.Api.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartSusChef.Api.Data;
using Microsoft.EntityFrameworkCore;
using SmartSusChef.Api.Models;

namespace SmartSusChef.Api.Tests.Controllers;

public class MlControllerTests
{
    private readonly Mock<IMlPredictionService> _mockMlService;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IStoreService> _mockStoreService;
    private readonly Mock<ILogger<MlController>> _mockLogger;
    private readonly ApplicationDbContext _dbContext;
    private readonly MlController _controller;

    public MlControllerTests()
    {
        _mockMlService = new Mock<IMlPredictionService>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockStoreService = new Mock<IStoreService>();
        _mockLogger = new Mock<ILogger<MlController>>();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);

        _controller = new MlController(
            _mockMlService.Object,
            _mockCurrentUserService.Object,
            _mockStoreService.Object,
            _dbContext,
            _mockLogger.Object
        );

        _mockCurrentUserService.Setup(s => s.StoreId).Returns(1);
    }

    [Fact]
    public async Task GetStatus_ShouldReturnOk_WhenServiceIsAvailableAndModelsAreReady()
    {
        // Arrange
        var status = new MlStoreStatusDto(
            StoreId: 1,
            HasModels: true,
            IsTraining: false,
            Dishes: new List<string> { "Pizza" },
            DaysAvailable: 150,
            ServiceAvailable: true
        );
        _mockMlService.Setup(s => s.GetStoreStatusAsync(1)).ReturnsAsync(status);

        // Act
        var result = await _controller.GetStatus();

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsType<MlStatusResponseDto>(actionResult.Value);
        Assert.Equal("ready", value.Status);
        Assert.True(value.HasModels);
    }

    [Fact]
    public async Task GetStatus_ShouldReturnOk_WhenServiceIsUnavailable()
    {
        // Arrange
        var status = new MlStoreStatusDto(1, false, false, null, 0, null, false);
        _mockMlService.Setup(s => s.GetStoreStatusAsync(1)).ReturnsAsync(status);
        
        await _dbContext.SalesData.AddAsync(new SalesData { StoreId = 1, Date = DateTime.Now.AddDays(-1) });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetStatus();

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsType<MlStatusResponseDto>(actionResult.Value);
        Assert.False(value.HasModels);
        Assert.Equal(1, value.DaysAvailable);
    }
    
    [Fact]
    public async Task GetStatus_ShouldReturnOk_WhenServiceThrowsException()
    {
        // Arrange
        _mockMlService.Setup(s => s.GetStoreStatusAsync(1)).ThrowsAsync(new Exception());
        
        await _dbContext.SalesData.AddAsync(new SalesData { StoreId = 1, Date = DateTime.Now.AddDays(-1) });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetStatus();

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsType<MlStatusResponseDto>(actionResult.Value);
        Assert.False(value.HasModels);
        Assert.Equal(1, value.DaysAvailable);
    }

    [Fact]
    public async Task TriggerTraining_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var response = new MlTrainResponseDto("OK", 1, "Training started");
        _mockMlService.Setup(s => s.TriggerTrainingAsync(1)).ReturnsAsync(response);

        // Act
        var result = await _controller.TriggerTraining();

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsType<MlTrainResponseDto>(actionResult.Value);
        Assert.Equal("OK", value.Status);
    }

    [Fact]
    public async Task TriggerTraining_ShouldReturnStatusCode503_WhenServiceThrowsException()
    {
        // Arrange
        _mockMlService.Setup(s => s.TriggerTrainingAsync(1)).ThrowsAsync(new Exception());

        // Act
        var result = await _controller.TriggerTraining();

        // Assert
        var actionResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(503, actionResult.StatusCode);
    }

    [Fact]
    public async Task Predict_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var store = new StoreDto(1, "Company", "UEN", "Test Store", "Location", "12345", DateTime.UtcNow, 1.0m, 1.0m, "US", "Address", true, DateTime.UtcNow, DateTime.UtcNow);
        var response = new MlStorePredictResponseDto(1, "OK", null, 7, new Dictionary<string, MlDishPredictionDto>());
        _mockStoreService.Setup(s => s.GetStoreAsync()).ReturnsAsync(store);
        _mockMlService.Setup(s => s.GetStorePredictionsAsync(1, 7, 1.0m, 1.0m, "US")).ReturnsAsync(response);

        // Act
        var result = await _controller.Predict();

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsType<MlStorePredictResponseDto>(actionResult.Value);
        Assert.Equal(1, value.StoreId);
    }
    
    [Fact]
    public async Task Predict_ShouldReturnStatusCode503_WhenServiceThrowsException()
    {
        // Arrange
        var store = new StoreDto(1, "Company", "UEN", "Test Store", "Location", "12345", DateTime.UtcNow, 1.0m, 1.0m, "US", "Address", true, DateTime.UtcNow, DateTime.UtcNow);
         _mockStoreService.Setup(s => s.GetStoreAsync()).ReturnsAsync(store);
        _mockMlService.Setup(s => s.GetStorePredictionsAsync(1, 7, 1.0m, 1.0m, "US")).ThrowsAsync(new Exception());

        // Act
        var result = await _controller.Predict();

        // Assert
        var actionResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(503, actionResult.StatusCode);
    }
}

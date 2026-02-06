using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using SmartSusChef.Api.Controllers;
using Microsoft.Extensions.Logging;
using SmartSusChef.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace SmartSusChef.Api.Tests.Controllers;

public class HealthControllerTests
{
    private readonly Mock<ApplicationDbContext> _mockContext;
    private readonly Mock<ILogger<HealthController>> _mockLogger;
    private readonly HealthController _controller;
    private readonly Mock<DatabaseFacade> _mockDatabase;

    public HealthControllerTests()
    {
        // Mock DbContext
        var options = new DbContextOptions<ApplicationDbContext>();
        _mockContext = new Mock<ApplicationDbContext>(options);

        // Mock Logger
        _mockLogger = new Mock<ILogger<HealthController>>();

        // Mock DatabaseFacade
        _mockDatabase = new Mock<DatabaseFacade>(_mockContext.Object);
        _mockContext.Setup(c => c.Database).Returns(_mockDatabase.Object);


        _controller = new HealthController(_mockContext.Object, _mockLogger.Object);
    }

    [Fact]
    public void Get_ShouldReturnOk()
    {
        // Act
        var result = _controller.Get();

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(actionResult.Value);
    }

    [Fact]
    public void Live_ShouldReturnOk()
    {
        // Act
        var result = _controller.Live();

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(actionResult.Value);
    }

    [Fact]
    public async Task Ready_ShouldReturnOk_WhenDbCanConnect()
    {
        // Arrange
        _mockDatabase.Setup(d => d.CanConnectAsync(default)).ReturnsAsync(true);

        // Act
        var result = await _controller.Ready();

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(actionResult.Value);
    }

    [Fact]
    public async Task Ready_ShouldReturn503_WhenDbCannotConnect()
    {
        // Arrange
        _mockDatabase.Setup(d => d.CanConnectAsync(default)).ReturnsAsync(false);

        // Act
        var result = await _controller.Ready();

        // Assert
        var actionResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(503, actionResult.StatusCode);
    }

    [Fact]
    public async Task Ready_ShouldReturn503_WhenExceptionOccurs()
    {
        // Arrange
        _mockDatabase.Setup(d => d.CanConnectAsync(default)).ThrowsAsync(new Exception("DB Error"));

        // Act
        var result = await _controller.Ready();

        // Assert
        var actionResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(503, actionResult.StatusCode);
    }

    [Fact]
    public async Task GetDetailed_ShouldReturnOk_WhenEverythingIsHealthy()
    {
        // Arrange
        _mockDatabase.Setup(d => d.CanConnectAsync(default)).ReturnsAsync(true);

        // Act
        var result = await _controller.GetDetailed();

        // Assert
        var actionResult = Assert.IsType<ObjectResult>(result);
        var healthResult = Assert.IsType<HealthCheckResult>(actionResult.Value);
        Assert.Equal("healthy", healthResult.Status);
        Assert.Equal(200, actionResult.StatusCode);
    }
    
    [Fact]
    public async Task GetDetailed_ShouldReturn503_WhenDbIsUnhealthy()
    {
        // Arrange
        _mockDatabase.Setup(d => d.CanConnectAsync(default)).ReturnsAsync(false);

        // Act
        var result = await _controller.GetDetailed();

        // Assert
        var actionResult = Assert.IsType<ObjectResult>(result);
        var healthResult = Assert.IsType<HealthCheckResult>(actionResult.Value);
        Assert.Equal("unhealthy", healthResult.Status);
        Assert.Equal(503, actionResult.StatusCode);
    }

    [Fact]
    public async Task GetDetailed_ShouldReturn503_WhenExceptionOccurs()
    {
        // Arrange
        _mockDatabase.Setup(d => d.CanConnectAsync(default)).ThrowsAsync(new Exception("DB Error"));

        // Act
        var result = await _controller.GetDetailed();

        // Assert
        var actionResult = Assert.IsType<ObjectResult>(result);
        var healthResult = Assert.IsType<HealthCheckResult>(actionResult.Value);
        Assert.Equal("unhealthy", healthResult.Status);
        Assert.Equal(503, actionResult.StatusCode);
        Assert.Contains("DB Error", healthResult.Checks["database"].Message);
    }
}

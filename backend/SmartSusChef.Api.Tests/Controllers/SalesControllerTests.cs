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

public class SalesControllerTests
{
    private readonly Mock<ISalesService> _mockSalesService;
    private readonly SalesController _controller;

    public SalesControllerTests()
    {
        _mockSalesService = new Mock<ISalesService>();
        _controller = new SalesController(_mockSalesService.Object);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOk_WithListOfSalesData()
    {
        // Arrange
        var salesData = new List<SalesDataDto> { new(Guid.NewGuid().ToString(), DateTime.UtcNow.ToString(), Guid.NewGuid().ToString(), "Pizza", 10, DateTime.UtcNow, DateTime.UtcNow) };
        _mockSalesService.Setup(s => s.GetAllAsync(null, null)).ReturnsAsync(salesData);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsAssignableFrom<List<SalesDataDto>>(actionResult.Value);
        Assert.Single(value);
    }

    [Fact]
    public async Task GetTrend_ShouldReturnOk_WithSalesTrend()
    {
        // Arrange
        var trend = new List<SalesWithSignalsDto> { new(DateTime.UtcNow.ToString("yyyy-MM-dd"), 100, false, string.Empty, 0, "Sunny", new List<RecipeSalesDto>()) };
        _mockSalesService.Setup(s => s.GetTrendAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(trend);

        // Act
        var result = await _controller.GetTrend(DateTime.UtcNow, DateTime.UtcNow);

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsAssignableFrom<List<SalesWithSignalsDto>>(actionResult.Value);
        Assert.Single(value);
    }

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var salesData = new SalesDataDto(id.ToString(), DateTime.UtcNow.ToString(), Guid.NewGuid().ToString(), "Pizza", 10, DateTime.UtcNow, DateTime.UtcNow);
        _mockSalesService.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(salesData);

        // Act
        var result = await _controller.GetById(id);

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsType<SalesDataDto>(actionResult.Value);
        Assert.Equal(id.ToString(), value.Id);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenMissing()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockSalesService.Setup(s => s.GetByIdAsync(id)).ReturnsAsync((SalesDataDto?)null);

        // Act
        var result = await _controller.GetById(id);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_ShouldReturnCreatedAtAction_WhenSuccessful()
    {
        // Arrange
        var request = new CreateSalesDataRequest(DateTime.UtcNow.ToString(), Guid.NewGuid().ToString(), 10);
        var salesData = new SalesDataDto(Guid.NewGuid().ToString(), DateTime.UtcNow.ToString(), Guid.NewGuid().ToString(), "Pizza", 10, DateTime.UtcNow, DateTime.UtcNow);
        _mockSalesService.Setup(s => s.CreateAsync(request)).ReturnsAsync(salesData);

        // Act
        var result = await _controller.Create(request);

        // Assert
        var actionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal("GetById", actionResult.ActionName);
    }

    [Fact]
    public async Task Import_ShouldReturnOk()
    {
        // Arrange
        var request = new ImportSalesDataRequest(new List<CreateSalesDataRequest>());
        _mockSalesService.Setup(s => s.ImportAsync(request.SalesData)).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Import(request);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Update_ShouldReturnOk_WhenFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateSalesDataRequest(15);
        var salesData = new SalesDataDto(id.ToString(), DateTime.UtcNow.ToString(), Guid.NewGuid().ToString(), "Pizza", 15, DateTime.UtcNow, DateTime.UtcNow);
        _mockSalesService.Setup(s => s.UpdateAsync(id, request)).ReturnsAsync(salesData);

        // Act
        var result = await _controller.Update(id, request);

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsType<SalesDataDto>(actionResult.Value);
        Assert.Equal(15, value.Quantity);
    }

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenMissing()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateSalesDataRequest(15);
        _mockSalesService.Setup(s => s.UpdateAsync(id, request)).ReturnsAsync((SalesDataDto?)null);

        // Act
        var result = await _controller.Update(id, request);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent_WhenDeleted()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockSalesService.Setup(s => s.DeleteAsync(id)).ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(id);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenMissing()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockSalesService.Setup(s => s.DeleteAsync(id)).ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(id);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}

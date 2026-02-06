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

public class ExportControllerTests
{
    private readonly Mock<ISalesService> _mockSalesService;
    private readonly Mock<IWastageService> _mockWastageService;
    private readonly Mock<IForecastService> _mockForecastService;
    private readonly ExportController _controller;

    public ExportControllerTests()
    {
        _mockSalesService = new Mock<ISalesService>();
        _mockWastageService = new Mock<IWastageService>();
        _mockForecastService = new Mock<IForecastService>();
        _controller = new ExportController(
            _mockSalesService.Object,
            _mockWastageService.Object,
            _mockForecastService.Object);
    }

    [Fact]
    public async Task ExportSalesCsv_ShouldReturnFileResult()
    {
        // Arrange
        var sales = new List<SalesDataDto> { new SalesDataDto(Guid.NewGuid().ToString(), DateTime.UtcNow.ToString(), Guid.NewGuid().ToString(), "test", 1, DateTime.UtcNow, DateTime.UtcNow) };
        _mockSalesService.Setup(s => s.GetAllAsync(null, null)).ReturnsAsync(sales);

        // Act
        var result = await _controller.ExportSalesCsv(null, null);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", fileResult.ContentType);
    }
    
    [Fact]
    public async Task ExportWastageCsv_ShouldReturnFileResult()
    {
        // Arrange
        var wastage = new List<WastageDataDto> { new WastageDataDto(Guid.NewGuid().ToString(), DateTime.UtcNow.ToString(), Guid.NewGuid().ToString(), null, "test", "kg", 1, 1, DateTime.UtcNow, DateTime.UtcNow) };
        _mockWastageService.Setup(s => s.GetAllAsync(null, null)).ReturnsAsync(wastage);

        // Act
        var result = await _controller.ExportWastageCsv(null, null);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", fileResult.ContentType);
    }
    
    [Fact]
    public async Task ExportForecastCsv_ShouldReturnFileResult()
    {
        // Arrange
        var forecast = new List<ForecastDto> { new ForecastDto(DateTime.UtcNow.ToString(), Guid.NewGuid().ToString(), "test", 1, new List<ForecastIngredientDto>()) };
        _mockForecastService.Setup(s => s.GetForecastAsync(7)).ReturnsAsync(forecast);

        // Act
        var result = await _controller.ExportForecastCsv(7);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", fileResult.ContentType);
    }
    
    [Fact]
    public void ExportSalesPdf_ShouldReturnFileResult()
    {
        // Act
        var result = _controller.ExportSalesPdf(DateTime.UtcNow, DateTime.UtcNow);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", fileResult.ContentType);
    }
}

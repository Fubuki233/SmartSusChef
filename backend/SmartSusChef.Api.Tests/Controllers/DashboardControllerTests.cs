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

public class DashboardControllerTests
{
    private readonly Mock<ISalesService> _mockSalesService;
    private readonly Mock<IWastageService> _mockWastageService;
    private readonly Mock<IHolidayService> _mockHolidayService;
    private readonly Mock<IWeatherService> _mockWeatherService;
    private readonly DashboardController _controller;

    public DashboardControllerTests()
    {
        _mockSalesService = new Mock<ISalesService>();
        _mockWastageService = new Mock<IWastageService>();
        _mockHolidayService = new Mock<IHolidayService>();
        _mockWeatherService = new Mock<IWeatherService>();
        _controller = new DashboardController(
            _mockSalesService.Object,
            _mockWastageService.Object,
            _mockHolidayService.Object,
            _mockWeatherService.Object);
    }

    [Fact]
    public async Task GetDashboardSummary_ShouldReturnOk_WithSummaryData()
    {
        // Arrange
        var salesTrend = new List<SalesWithSignalsDto> { new SalesWithSignalsDto(DateTime.UtcNow.ToString(), 1, true, "Test Holiday", 0, "test", new List<RecipeSalesDto>()) };
        _mockSalesService.Setup(s => s.GetTrendAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(salesTrend);
        _mockWastageService.Setup(s => s.GetTotalWastageImpactAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(10.5m);
        _mockHolidayService.Setup(s => s.IsHolidayAsync(It.IsAny<DateTime>())).ReturnsAsync(true);
        _mockWeatherService.Setup(s => s.GetCurrentWeatherAsync()).ReturnsAsync(new WeatherDto(1, "test", 1, "test"));

        // Act
        var result = await _controller.GetDashboardSummary();

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(actionResult.Value);
    }
}

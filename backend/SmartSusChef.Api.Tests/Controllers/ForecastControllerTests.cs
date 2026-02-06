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

public class ForecastControllerTests
{
    private readonly Mock<IForecastService> _mockForecastService;
    private readonly Mock<IWeatherService> _mockWeatherService;
    private readonly Mock<IHolidayService> _mockHolidayService;
    private readonly Mock<IStoreService> _mockStoreService;
    private readonly ForecastController _controller;

    public ForecastControllerTests()
    {
        _mockForecastService = new Mock<IForecastService>();
        _mockWeatherService = new Mock<IWeatherService>();
        _mockHolidayService = new Mock<IHolidayService>();
        _mockStoreService = new Mock<IStoreService>();
        _controller = new ForecastController(
            _mockForecastService.Object,
            _mockWeatherService.Object,
            _mockHolidayService.Object,
            _mockStoreService.Object);
    }

    [Fact]
    public async Task GetForecast_ShouldReturnOk_WithListOfForecasts()
    {
        // Arrange
        var forecast = new List<ForecastDto> { new ForecastDto(DateTime.UtcNow.ToString(), Guid.NewGuid().ToString(), "test", 1, new List<ForecastIngredientDto>()) };
        // Explicitly provide all arguments to avoid "expression tree cannot contain optional arguments" error
        _mockForecastService.Setup(s => s.GetForecastAsync(7, 0)).ReturnsAsync(forecast);

        // Act
        var result = await _controller.GetForecast(7);

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsAssignableFrom<List<ForecastDto>>(actionResult.Value);
        Assert.Single(value);
    }
    
    [Fact]
    public async Task GetForecast_ShouldReturnBadRequest_WhenDaysIsInvalid()
    {
        // Act
        var result = await _controller.GetForecast(0);

        // Assert
        var actionResult = Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetForecast_ShouldReturnBadRequest_WhenIncludePastDaysIsInvalid()
    {
        // Act
        var result = await _controller.GetForecast(7, -1);

        // Assert
        var actionResult = Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetForecastSummary_ShouldReturnOk_WithListOfForecastSummaries()
    {
        // Arrange
        var summary = new List<ForecastSummaryDto> { new ForecastSummaryDto(DateTime.UtcNow.ToString(), 1, 0.1m) };
        // Explicitly provide all arguments to avoid "expression tree cannot contain optional arguments" error
        _mockForecastService.Setup(s => s.GetForecastSummaryAsync(7, 0)).ReturnsAsync(summary);

        // Act
        var result = await _controller.GetForecastSummary(7);

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsAssignableFrom<List<ForecastSummaryDto>>(actionResult.Value);
        Assert.Single(value);
    }
    
    [Fact]
    public async Task GetForecastSummary_ShouldReturnBadRequest_WhenDaysIsInvalid()
    {
        // Act
        var result = await _controller.GetForecastSummary(31);

        // Assert
        var actionResult = Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetWeather_ShouldReturnOk_WithWeatherDto()
    {
        // Arrange
        var weather = new WeatherDto(1, "test", 1, "test");
        _mockWeatherService.Setup(s => s.GetCurrentWeatherAsync()).ReturnsAsync(weather);

        // Act
        var result = await _controller.GetWeather();

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.IsType<WeatherDto>(actionResult.Value);
    }
    
    [Fact]
    public async Task GetWeather_ShouldReturnNoContent_WhenWeatherIsNull()
    {
        // Arrange
        _mockWeatherService.Setup(s => s.GetCurrentWeatherAsync()).ReturnsAsync((WeatherDto?)null);

        // Act
        var result = await _controller.GetWeather();

        // Assert
        Assert.IsType<NoContentResult>(result.Result);
    }

    [Fact]
    public async Task GetHolidays_ShouldReturnOk_WithListOfHolidays()
    {
        // Arrange
        var holidays = new List<HolidayDto> { new HolidayDto(DateTime.UtcNow.ToString(), "test") };
        _mockStoreService.Setup(s => s.GetStoreAsync()).ReturnsAsync(new StoreDto(1, "test", "test", "test", "test", "test", DateTime.UtcNow, 1, 1, "US", "test", true, DateTime.UtcNow, DateTime.UtcNow));
        _mockHolidayService.Setup(s => s.GetHolidaysAsync(2024, "US")).ReturnsAsync(holidays);

        // Act
        var result = await _controller.GetHolidays(2024);

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsAssignableFrom<List<HolidayDto>>(actionResult.Value);
        Assert.Single(value);
    }
    
    [Fact]
    public async Task GetHolidays_ShouldReturnBadRequest_WhenYearIsInvalid()
    {
        // Act
        var result = await _controller.GetHolidays(2019);

        // Assert
        var actionResult = Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetTomorrowForecast_ShouldReturnOk()
    {
        // Arrange
        _mockStoreService.Setup(s => s.GetStoreAsync()).ReturnsAsync(new StoreDto(1, "test", "test", "test", "test", "test", DateTime.UtcNow, 1, 1, "US", "test", true, DateTime.UtcNow, DateTime.UtcNow));
        _mockHolidayService.Setup(s => s.IsHolidayAsync(It.IsAny<DateTime>(), "US")).ReturnsAsync((false, null));
        _mockWeatherService.Setup(s => s.GetWeatherForecastAsync(It.IsAny<DateTime>(), 1, 1)).ReturnsAsync(new WeatherForecastDto("2024-01-01", 30, 25, 0, 0, "Sunny"));

        // Act
        var result = await _controller.GetTomorrowForecast();

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.IsType<TomorrowForecastDto>(actionResult.Value);
    }

    [Fact]
    public async Task GetCalendarDay_ShouldReturnOk()
    {
        // Arrange
        var date = "2024-01-01";
        _mockStoreService.Setup(s => s.GetStoreAsync()).ReturnsAsync(new StoreDto(1, "test", "test", "test", "test", "test", DateTime.UtcNow, 1, 1, "US", "test", true, DateTime.UtcNow, DateTime.UtcNow));
        _mockHolidayService.Setup(s => s.IsHolidayAsync(It.IsAny<DateTime>(), "US")).ReturnsAsync((false, null));

        // Act
        var result = await _controller.GetCalendarDay(date);

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.IsType<CalendarDayDto>(actionResult.Value);
    }

    [Fact]
    public async Task GetCalendarDay_ShouldReturnBadRequest_WhenDateIsInvalid()
    {
        // Act
        var result = await _controller.GetCalendarDay("invalid-date");

        // Assert
        var actionResult = Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetCalendarRange_ShouldReturnOk()
    {
        // Arrange
        var startDate = "2024-01-01";
        var endDate = "2024-01-05";
        _mockStoreService.Setup(s => s.GetStoreAsync()).ReturnsAsync(new StoreDto(1, "test", "test", "test", "test", "test", DateTime.UtcNow, 1, 1, "US", "test", true, DateTime.UtcNow, DateTime.UtcNow));
        _mockHolidayService.Setup(s => s.IsHolidayAsync(It.IsAny<DateTime>(), "US")).ReturnsAsync((false, null));

        // Act
        var result = await _controller.GetCalendarRange(startDate, endDate);

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsAssignableFrom<List<CalendarDayDto>>(actionResult.Value);
        Assert.Equal(5, value.Count);
    }

    [Fact]
    public async Task GetCalendarRange_ShouldReturnBadRequest_WhenStartDateIsInvalid()
    {
        // Act
        var result = await _controller.GetCalendarRange("invalid", "2024-01-05");

        // Assert
        var actionResult = Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetCalendarRange_ShouldReturnBadRequest_WhenEndDateIsInvalid()
    {
        // Act
        var result = await _controller.GetCalendarRange("2024-01-01", "invalid");

        // Assert
        var actionResult = Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetCalendarRange_ShouldReturnBadRequest_WhenEndDateIsBeforeStartDate()
    {
        // Act
        var result = await _controller.GetCalendarRange("2024-01-05", "2024-01-01");

        // Assert
        var actionResult = Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetCalendarRange_ShouldReturnBadRequest_WhenRangeExceeds30Days()
    {
        // Act
        var result = await _controller.GetCalendarRange("2024-01-01", "2024-02-05");

        // Assert
        var actionResult = Assert.IsType<BadRequestObjectResult>(result.Result);
    }
}

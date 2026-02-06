using Xunit;
using Microsoft.EntityFrameworkCore;
using SmartSusChef.Api.Data;
using SmartSusChef.Api.Services;
using SmartSusChef.Api.DTOs;
using Moq;
using Microsoft.Extensions.Configuration;
using System.Net;
using Moq.Protected;
using SmartSusChef.Api.Models;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace SmartSusChef.Api.Tests.Services;

public class WeatherServiceTests
{
    private ApplicationDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private Mock<HttpMessageHandler> CreateMockHttpMessageHandler(string jsonResponse)
    {
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });
        return mockHttpMessageHandler;
    }

    [Fact]
    public async Task SyncWeatherForDateAsync_ShouldStoreRainfallWithHighPrecision()
    {
        // 1. Arrange
        var context = GetDbContext();
        var date = new DateTime(2024, 5, 20);

        // Mock JSON response with high-precision rainfall
        var jsonResponse = @"{
            ""daily"": {
                ""time"": [""2024-05-20""],
                ""precipitation_sum"": [12.5],
                ""weathercode"": [63]
            }
        }";
        var mockHttpMessageHandler = CreateMockHttpMessageHandler(jsonResponse);
        var httpClient = new HttpClient(mockHttpMessageHandler.Object);

        var mockStoreService = new Mock<IStoreService>();
        mockStoreService.Setup(s => s.GetStoreAsync()).ReturnsAsync(new StoreDto(1, "Company", "UEN", "Test Store", "Location", "123", DateTime.UtcNow, 1.35m, 103.81m, "SG", "Address", true, DateTime.UtcNow, DateTime.UtcNow));

        var mockConfiguration = new Mock<IConfiguration>();
        // Setup configuration to return a dummy URL to prevent ArgumentNullException
        mockConfiguration.Setup(c => c["ExternalApis:WeatherApiUrl"]).Returns("https://api.open-meteo.com/v1/forecast");

        var service = new WeatherService(httpClient, mockConfiguration.Object, mockStoreService.Object, context);

        // 2. Act
        await service.SyncWeatherForDateAsync(date);

        // 3. Assert
        var signal = await context.GlobalCalendarSignals.FindAsync(date.Date);
        Assert.NotNull(signal);
        Assert.Equal(12.5m, signal.RainMm);
        Assert.Equal("Moderate Rain", signal.WeatherDesc);
    }

    [Fact]
    public async Task SyncWeatherForDateAsync_ShouldHandleEmptyWeatherDescription()
    {
        // 1. Arrange
        var context = GetDbContext();
        var date = new DateTime(2024, 5, 20);

        // Mock JSON response with a null or unknown weathercode
        var jsonResponse = @"{
            ""daily"": {
                ""time"": [""2024-05-20""],
                ""precipitation_sum"": [0],
                ""weathercode"": [null]
            }
        }";
        var mockHttpMessageHandler = CreateMockHttpMessageHandler(jsonResponse);
        var httpClient = new HttpClient(mockHttpMessageHandler.Object);

        var mockStoreService = new Mock<IStoreService>();
        mockStoreService.Setup(s => s.GetStoreAsync()).ReturnsAsync(new StoreDto(1, "Company", "UEN", "Test Store", "Location", "123", DateTime.UtcNow, 1.35m, 103.81m, "SG", "Address", true, DateTime.UtcNow, DateTime.UtcNow));

        var mockConfiguration = new Mock<IConfiguration>();
        mockConfiguration.Setup(c => c["ExternalApis:WeatherApiUrl"]).Returns("https://api.open-meteo.com/v1/forecast");

        var service = new WeatherService(httpClient, mockConfiguration.Object, mockStoreService.Object, context);

        // 2. Act
        await service.SyncWeatherForDateAsync(date);

        // 3. Assert
        var signal = await context.GlobalCalendarSignals.FindAsync(date.Date);
        Assert.NotNull(signal);
        Assert.Equal("Unknown", signal.WeatherDesc); // Should default to "Unknown"
    }

    [Theory]
    [InlineData(0, "Sunny")]
    [InlineData(1, "Mainly Clear")]
    [InlineData(2, "Partly Cloudy")]
    [InlineData(3, "Overcast")]
    [InlineData(45, "Foggy")]
    [InlineData(48, "Depositing Rime Fog")]
    [InlineData(51, "Light Drizzle")]
    [InlineData(53, "Moderate Drizzle")]
    [InlineData(55, "Dense Drizzle")]
    [InlineData(61, "Slight Rain")]
    [InlineData(63, "Moderate Rain")]
    [InlineData(65, "Heavy Rain")]
    [InlineData(71, "Slight Snow")]
    [InlineData(73, "Moderate Snow")]
    [InlineData(75, "Heavy Snow")]
    [InlineData(95, "Thunderstorm")]
    [InlineData(999, "Unknown")] // Test unknown code
    public void GetWeatherDescription_ShouldReturnCorrectDescription(int code, string expectedDescription)
    {
        // Act
        var result = WeatherService.GetWeatherDescription(code);

        // Assert
        Assert.Equal(expectedDescription, result);
    }

    [Theory]
    [InlineData(0, "Clear", "Clear sky")]
    [InlineData(1, "Partly Cloudy", "Partly cloudy to cloudy")]
    [InlineData(45, "Foggy", "Foggy conditions")]
    [InlineData(51, "Drizzle", "Light to moderate drizzle")]
    [InlineData(61, "Rainy", "Light to heavy rain")]
    [InlineData(71, "Snowy", "Light to heavy snow")]
    [InlineData(80, "Rain Showers", "Rain showers")]
    [InlineData(95, "Thunderstorm", "Thunderstorm")]
    [InlineData(999, "Unknown", "Weather condition unknown")]
    public void MapWeatherCode_ShouldReturnCorrectConditionAndDescription(int code, string expectedCondition, string expectedDescription)
    {
        // Act
        var (condition, description) = WeatherService.MapWeatherCode(code);

        // Assert
        Assert.Equal(expectedCondition, condition);
        Assert.Equal(expectedDescription, description);
    }

    [Fact]
    public async Task GetWeatherForCoordinates_ShouldReturnWeather_WhenApiCallSucceeds()
    {
        // Arrange
        var context = GetDbContext();
        var jsonResponse = @"{
            ""current"": {
                ""temperature_2m"": 25.5,
                ""relative_humidity_2m"": 60,
                ""weather_code"": 0
            }
        }";
        var mockHttpMessageHandler = CreateMockHttpMessageHandler(jsonResponse);
        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var mockConfiguration = new Mock<IConfiguration>();
        mockConfiguration.Setup(c => c["ExternalApis:WeatherApiUrl"]).Returns("https://api.open-meteo.com/v1/forecast");

        var service = new WeatherService(httpClient, mockConfiguration.Object, Mock.Of<IStoreService>(), context);

        // Act
        var result = await service.GetWeatherForCoordinates(1.35, 103.81);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(25.5m, result.Temperature);
        Assert.Equal(60, result.Humidity);
        Assert.Equal("Clear", result.Condition);
    }

    [Fact]
    public async Task GetWeatherForCoordinates_ShouldHandleMissingHumidity()
    {
        // Arrange
        var context = GetDbContext();
        var jsonResponse = @"{
            ""current"": {
                ""temperature_2m"": 25.5,
                ""weather_code"": 0
            }
        }";
        var mockHttpMessageHandler = CreateMockHttpMessageHandler(jsonResponse);
        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var mockConfiguration = new Mock<IConfiguration>();
        mockConfiguration.Setup(c => c["ExternalApis:WeatherApiUrl"]).Returns("https://api.open-meteo.com/v1/forecast");

        var service = new WeatherService(httpClient, mockConfiguration.Object, Mock.Of<IStoreService>(), context);

        // Act
        var result = await service.GetWeatherForCoordinates(1.35, 103.81);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.Humidity);
        Assert.Contains("Humidity data unavailable", result.Description);
    }

    [Fact]
    public async Task GetWeatherForecastAsync_ShouldReturnForecast_WhenApiCallSucceeds()
    {
        // Arrange
        var context = GetDbContext();
        var jsonResponse = @"{
            ""daily"": {
                ""time"": [""2024-05-20"", ""2024-05-21""],
                ""temperature_2m_max"": [30.5, 31.0],
                ""temperature_2m_min"": [25.0, 25.5],
                ""precipitation_sum"": [5.0, 0.0],
                ""weathercode"": [61, 0]
            }
        }";
        var mockHttpMessageHandler = CreateMockHttpMessageHandler(jsonResponse);
        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var mockStoreService = new Mock<IStoreService>();
        mockStoreService.Setup(s => s.GetStoreAsync()).ReturnsAsync(new StoreDto(1, "Company", "UEN", "Test Store", "Location", "123", DateTime.UtcNow, 1.35m, 103.81m, "SG", "Address", true, DateTime.UtcNow, DateTime.UtcNow));
        var mockConfiguration = new Mock<IConfiguration>();
        mockConfiguration.Setup(c => c["ExternalApis:WeatherApiUrl"]).Returns("https://api.open-meteo.com/v1/forecast");

        var service = new WeatherService(httpClient, mockConfiguration.Object, mockStoreService.Object, context);

        // Act
        var result = await service.GetWeatherForecastAsync(new DateTime(2024, 5, 20), 1.35m, 103.81m);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("2024-05-20", result.Date);
        Assert.Equal(30.5m, result.TemperatureMax);
        Assert.Equal("Slight Rain", result.WeatherDescription);
    }

    [Fact]
    public async Task GetWeatherForecastAsync_ShouldReturnNull_WhenDateIsTooFarInFuture()
    {
        // Arrange
        var context = GetDbContext();
        var service = new WeatherService(new HttpClient(), Mock.Of<IConfiguration>(), Mock.Of<IStoreService>(), context);

        // Act
        var result = await service.GetWeatherForecastAsync(DateTime.UtcNow.AddDays(20), 1.35m, 103.81m);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetWeatherForecastAsync_ShouldUseArchiveApi_WhenDateIsPast()
    {
        // Arrange
        var context = GetDbContext();
        var jsonResponse = @"{
            ""daily"": {
                ""time"": [""2023-05-20""],
                ""temperature_2m_max"": [30.5],
                ""temperature_2m_min"": [25.0],
                ""precipitation_sum"": [5.0],
                ""weathercode"": [61]
            }
        }";
        var mockHttpMessageHandler = CreateMockHttpMessageHandler(jsonResponse);
        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var mockConfiguration = new Mock<IConfiguration>();
        mockConfiguration.Setup(c => c["ExternalApis:WeatherApiUrl"]).Returns("https://api.open-meteo.com/v1/forecast");

        var service = new WeatherService(httpClient, mockConfiguration.Object, Mock.Of<IStoreService>(), context);

        // Act
        var result = await service.GetWeatherForecastAsync(new DateTime(2023, 5, 20), 1.35m, 103.81m);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("2023-05-20", result.Date);
    }

    [Fact]
    public async Task GetWeatherForecastAsync_ShouldReturnNull_WhenApiCallFails()
    {
        // Arrange
        var context = GetDbContext();
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest
            });
        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var mockConfiguration = new Mock<IConfiguration>();
        mockConfiguration.Setup(c => c["ExternalApis:WeatherApiUrl"]).Returns("https://api.open-meteo.com/v1/forecast");

        var service = new WeatherService(httpClient, mockConfiguration.Object, Mock.Of<IStoreService>(), context);

        // Act
        var result = await service.GetWeatherForecastAsync(DateTime.UtcNow, 1.35m, 103.81m);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCurrentWeatherAsync_ShouldReturnCachedWeather_WhenAvailable()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var today = DateTime.UtcNow.Date;
        var cachedWeather = new WeatherDaily
        {
            StoreId = storeId,
            Date = today,
            Temperature = 28.5m,
            Condition = "Sunny",
            Humidity = 70,
            Description = "Clear sky",
            UpdatedAt = DateTime.UtcNow
        };
        context.WeatherDaily.Add(cachedWeather);
        await context.SaveChangesAsync();

        var mockStoreService = new Mock<IStoreService>();
        mockStoreService.Setup(s => s.GetStoreAsync()).ReturnsAsync(new StoreDto(storeId, "Company", "UEN", "Test Store", "Location", "123", DateTime.UtcNow, 1.35m, 103.81m, "SG", "Address", true, DateTime.UtcNow, DateTime.UtcNow));

        var service = new WeatherService(new HttpClient(), Mock.Of<IConfiguration>(), mockStoreService.Object, context);

        // Act
        var result = await service.GetCurrentWeatherAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(28.5m, result.Temperature);
        Assert.Equal("Sunny", result.Condition);
    }

    [Fact]
    public async Task GetCurrentWeatherAsync_ShouldReturnNull_WhenStoreNotFound()
    {
        // Arrange
        var context = GetDbContext();
        var mockStoreService = new Mock<IStoreService>();
        mockStoreService.Setup(s => s.GetStoreAsync()).ReturnsAsync((StoreDto?)null);

        var service = new WeatherService(new HttpClient(), Mock.Of<IConfiguration>(), mockStoreService.Object, context);

        // Act
        var result = await service.GetCurrentWeatherAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCurrentWeatherAsync_ShouldFetchAndCacheWeather_WhenNotCached()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var jsonResponse = @"{
            ""current"": {
                ""temperature_2m"": 26.0,
                ""relative_humidity_2m"": 65,
                ""weather_code"": 0
            }
        }";
        var mockHttpMessageHandler = CreateMockHttpMessageHandler(jsonResponse);
        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var mockConfiguration = new Mock<IConfiguration>();
        mockConfiguration.Setup(c => c["ExternalApis:WeatherApiUrl"]).Returns("https://api.open-meteo.com/v1/forecast");

        var mockStoreService = new Mock<IStoreService>();
        mockStoreService.Setup(s => s.GetStoreAsync()).ReturnsAsync(new StoreDto(storeId, "Company", "UEN", "Test Store", "Location", "123", DateTime.UtcNow, 1.35m, 103.81m, "SG", "Address", true, DateTime.UtcNow, DateTime.UtcNow));

        var service = new WeatherService(httpClient, mockConfiguration.Object, mockStoreService.Object, context);

        // Act
        var result = await service.GetCurrentWeatherAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(26.0m, result.Temperature);

        var cached = await context.WeatherDaily.FirstOrDefaultAsync(w => w.StoreId == storeId && w.Date == DateTime.UtcNow.Date);
        Assert.NotNull(cached);
        Assert.Equal(26.0m, cached.Temperature);
    }
}

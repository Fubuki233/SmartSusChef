using Xunit;
using Microsoft.EntityFrameworkCore;
using SmartSusChef.Api.Data;
using SmartSusChef.Api.Services;
using SmartSusChef.Api.Models;
using SmartSusChef.Api.DTOs;
using Moq;
using Microsoft.Extensions.Configuration;
using System.Net;
using Moq.Protected;

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
        mockStoreService.Setup(s => s.GetStoreAsync()).ReturnsAsync(new StoreDto(1, "Company", "UEN", "Test Store", "Location", "123", DateTime.UtcNow, 1.35m, 103.81m, "Address", true, DateTime.UtcNow, DateTime.UtcNow));

        var mockConfiguration = new Mock<IConfiguration>();

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
        mockStoreService.Setup(s => s.GetStoreAsync()).ReturnsAsync(new StoreDto(1, "Company", "UEN", "Test Store", "Location", "123", DateTime.UtcNow, 1.35m, 103.81m, "Address", true, DateTime.UtcNow, DateTime.UtcNow));

        var mockConfiguration = new Mock<IConfiguration>();

        var service = new WeatherService(httpClient, mockConfiguration.Object, mockStoreService.Object, context);

        // 2. Act
        await service.SyncWeatherForDateAsync(date);

        // 3. Assert
        var signal = await context.GlobalCalendarSignals.FindAsync(date.Date);
        Assert.NotNull(signal);
        Assert.Equal("Unknown", signal.WeatherDesc); // Should default to "Unknown"
    }
}

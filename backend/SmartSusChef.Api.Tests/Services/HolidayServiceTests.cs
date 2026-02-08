namespace SmartSusChef.Api.Tests.Services;
using Xunit;
using Microsoft.EntityFrameworkCore;
using SmartSusChef.Api.Data;
using SmartSusChef.Api.Services;
using SmartSusChef.Api.Models;
using Moq;
using Microsoft.Extensions.Configuration;
using System.Net;
using Moq.Protected;

public class HolidayServiceTests
{
    private ApplicationDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static void ClearHolidayCaches()
    {
        var type = typeof(HolidayService);
        var holidayCacheField = type.GetField("HolidayCache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var countryCacheField = type.GetField("CountryCodeCache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        if (holidayCacheField?.GetValue(null) is System.Collections.IDictionary holidayCache)
        {
            holidayCache.Clear();
        }
        if (countryCacheField?.GetValue(null) is System.Collections.IDictionary countryCache)
        {
            countryCache.Clear();
        }
    }

    [Fact]
    public async Task IsHolidayAsync_ShouldReturnTrue_ForKnownHolidayInDatabase()
    {
        // 1. Arrange
        var context = GetDbContext();
        
        var mockConfiguration = new Mock<IConfiguration>();
        var mockHttpClient = new HttpClient(); 

        var holidayDate = new DateTime(2026, 1, 1);
        
        context.GlobalCalendarSignals.Add(new GlobalCalendarSignals 
        { 
            Date = holidayDate, 
            IsHoliday = true, 
            HolidayName = "New Year's Day",
            WeatherDesc = "Sunny" 
        });
        await context.SaveChangesAsync();

        var service = new HolidayService(mockHttpClient, mockConfiguration.Object, context);

        // 2. Act
        bool result = await service.IsHolidayAsync(holidayDate);

        // 3. Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsHolidayAsync_ShouldReturnFalse_ForRegularDay()
    {
        // 1. Arrange
        var context = GetDbContext();
        var mockConfiguration = new Mock<IConfiguration>();
        var mockHttpClient = new HttpClient();
        var regularDate = new DateTime(2026, 1, 15);
        
        var service = new HolidayService(mockHttpClient, mockConfiguration.Object, context);

        // 2. Act
        bool result = await service.IsHolidayAsync(regularDate);

        // 3. Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SyncHolidaysAsync_ShouldParseHolidayNameCorrectly_ForChina()
    {
        // 1. Arrange
        var context = GetDbContext();
        var year = 2024;
        var countryCode = "CN";
        var holidayDate = new DateTime(year, 10, 1); // National Day

        // Mock JSON response from the holiday API
        var jsonResponse = @"
        [
            { ""date"": ""2024-10-01"", ""localName"": ""国庆节"", ""name"": ""National Day"" },
            { ""date"": ""2024-10-02"", ""localName"": ""国庆节"", ""name"": ""National Day"" }
        ]";

        // Mock HttpMessageHandler to intercept the request
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

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);

        // Mock IConfiguration to provide the API URL
        var mockConfiguration = new Mock<IConfiguration>();
        mockConfiguration.Setup(c => c["ExternalApis:HolidayApiUrl"]).Returns("https://date.nager.at/api/v3/PublicHolidays");

        var service = new HolidayService(httpClient, mockConfiguration.Object, context);

        // 2. Act
        await service.SyncHolidaysAsync(year, countryCode);

        // 3. Assert
        var signal = await context.GlobalCalendarSignals.FindAsync(holidayDate);
        Assert.NotNull(signal);
        Assert.True(signal.IsHoliday);
        Assert.Equal("国庆节", signal.HolidayName); // Verify the local name is parsed correctly
    }

    [Fact]
    public async Task GetCountryCodeFromCoordinatesAsync_ShouldReturnCode_WhenApiCallSucceeds()
    {
        // Arrange
        var context = GetDbContext();
        var jsonResponse = @"{
            ""address"": {
                ""country_code"": ""sg""
            }
        }";
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
        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var mockConfiguration = new Mock<IConfiguration>();
        
        var service = new HolidayService(httpClient, mockConfiguration.Object, context);

        // Act
        var result = await service.GetCountryCodeFromCoordinatesAsync(1.35m, 103.81m);

        // Assert
        Assert.Equal("SG", result);
    }

    [Fact]
    public async Task GetCountryCodeFromCoordinatesAsync_ShouldReturnNull_WhenApiCallFails()
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
        
        var service = new HolidayService(httpClient, mockConfiguration.Object, context);

        // Act
        // Use different coordinates to avoid cache hit from previous test
        var result = await service.GetCountryCodeFromCoordinatesAsync(51.50m, -0.12m);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetHolidaysAsync_ShouldReturnEmptyList_WhenApiCallFails()
    {
        // Arrange
        ClearHolidayCaches();
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
        mockConfiguration.Setup(c => c["ExternalApis:HolidayApiUrl"]).Returns("https://date.nager.at/api/v3/PublicHolidays");

        var service = new HolidayService(httpClient, mockConfiguration.Object, context);

        // Act
        var result = await service.GetHolidaysAsync(2024, "US");

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("2024-05-25", true)] // Start of Summer Holiday
    [InlineData("2024-06-23", true)] // End of Summer Holiday
    [InlineData("2024-06-01", true)] // Middle of Summer Holiday
    [InlineData("2024-01-02", false)] // Regular Tuesday
    public void IsSchoolHoliday_ShouldReturnCorrectStatus(string dateStr, bool expected)
    {
        // Arrange
        var date = DateTime.Parse(dateStr);

        // Act
        var result = HolidayService.IsSchoolHoliday(date);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetHolidaysAsync_ShouldThrow_WhenHolidayApiUrlMissing()
    {
        // Arrange
        ClearHolidayCaches();
        var context = GetDbContext();
        var httpClient = new HttpClient(new Mock<HttpMessageHandler>().Object);
        var mockConfiguration = new Mock<IConfiguration>();
        var service = new HolidayService(httpClient, mockConfiguration.Object, context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetHolidaysAsync(2024, "US"));
    }

    [Fact]
    public async Task GetHolidaysAsync_ShouldReturnEmptyList_WhenResponseIsInvalidJson()
    {
        // Arrange
        ClearHolidayCaches();
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
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("not-json")
            });
        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var mockConfiguration = new Mock<IConfiguration>();
        mockConfiguration.Setup(c => c["ExternalApis:HolidayApiUrl"]).Returns("https://date.nager.at/api/v3/PublicHolidays");
        var service = new HolidayService(httpClient, mockConfiguration.Object, context);

        // Act
        var result = await service.GetHolidaysAsync(2024, "US");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCountryCodeFromCoordinatesAsync_ShouldUseCache_OnSubsequentCalls()
    {
        // Arrange
        ClearHolidayCaches();
        var context = GetDbContext();
        var jsonResponse = @"{
            ""address"": {
                ""country_code"": ""us""
            }
        }";
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            })
            .ThrowsAsync(new HttpRequestException("should not be called"));
        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var mockConfiguration = new Mock<IConfiguration>();
        var service = new HolidayService(httpClient, mockConfiguration.Object, context);

        // Act
        var first = await service.GetCountryCodeFromCoordinatesAsync(11.11m, 22.22m);
        var second = await service.GetCountryCodeFromCoordinatesAsync(11.11m, 22.22m);

        // Assert
        Assert.Equal("US", first);
        Assert.Equal("US", second);
        mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task IsHolidayAsync_WithCountryCode_ShouldReturnName_WhenMatched()
    {
        // Arrange
        ClearHolidayCaches();
        var context = GetDbContext();
        var jsonResponse = @"[
            { ""date"": ""2024-12-25"", ""localName"": ""Christmas Day"" }
        ]";
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
        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var mockConfiguration = new Mock<IConfiguration>();
        mockConfiguration.Setup(c => c["ExternalApis:HolidayApiUrl"]).Returns("https://date.nager.at/api/v3/PublicHolidays");
        var service = new HolidayService(httpClient, mockConfiguration.Object, context);

        // Act
        var (isHoliday, name) = await service.IsHolidayAsync(new DateTime(2024, 12, 25), "US");

        // Assert
        Assert.True(isHoliday);
        Assert.Equal("Christmas Day", name);
    }

    [Fact]
    public void GetChineseNewYear_ShouldReturnDate_WhenKnownYear()
    {
        // Arrange
        var method = typeof(HolidayService).GetMethod("GetChineseNewYear", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        // Act
        var result = (DateTime?)method!.Invoke(null, new object[] { 2024 });

        // Assert
        Assert.NotNull(result);
        Assert.Equal(new DateTime(2024, 2, 10), result!.Value.Date);
    }

    [Fact]
    public void GetChineseNewYear_ShouldReturnNull_WhenUnknownYear()
    {
        // Arrange
        var method = typeof(HolidayService).GetMethod("GetChineseNewYear", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        // Act
        var result = (DateTime?)method!.Invoke(null, new object[] { 2019 });

        // Assert
        Assert.Null(result);
    }
}

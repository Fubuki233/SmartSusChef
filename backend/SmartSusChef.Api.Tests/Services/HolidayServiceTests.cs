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
            HolidayName = "New Year's Day"
        });
        await context.SaveChangesAsync();

        var service = new HolidayService(mockHttpClient, mockConfiguration.Object, context);

        // 2. Act
        var result = await service.IsHolidayAsync(holidayDate);

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
        var result = await service.IsHolidayAsync(regularDate);

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
}

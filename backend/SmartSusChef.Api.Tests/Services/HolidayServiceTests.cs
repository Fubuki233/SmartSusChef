namespace SmartSusChef.Api.Tests.Services;
using Xunit;
using Microsoft.EntityFrameworkCore;
using SmartSusChef.Api.Data;
using SmartSusChef.Api.Services;
using SmartSusChef.Api.Models;
using Moq;
using Microsoft.Extensions.Configuration;

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
        
        // Mocking the required parameters for HolidayService
        var mockConfiguration = new Mock<IConfiguration>();
        var mockHttpClient = new HttpClient(); // Using real HttpClient for now, or mock HttpMessageHandler if needed

        var holidayDate = new DateTime(2026, 1, 1); // New Year's Day
        
        // Using correct symbols: HolidayName and IsHoliday
        context.GlobalCalendarSignals.Add(new GlobalCalendarSignals 
        { 
            Date = holidayDate, 
            IsHoliday = true, 
            HolidayName = "New Year's Day",
            WeatherDesc = "Sunny" 
        });
        await context.SaveChangesAsync();

        // Satisfying the 3-parameter constructor: HttpClient, IConfiguration, ApplicationDbContext
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
}

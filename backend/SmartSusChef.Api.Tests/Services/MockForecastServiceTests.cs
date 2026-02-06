using Xunit;
using Microsoft.EntityFrameworkCore;
using SmartSusChef.Api.Data;
using SmartSusChef.Api.Services;
using SmartSusChef.Api.Models;
using Moq;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace SmartSusChef.Api.Tests.Services;

public class MockForecastServiceTests
{
    private ApplicationDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetForecastAsync_ShouldThrowArgumentException_WhenDaysIsInvalid()
    {
        // Arrange
        var context = GetDbContext();
        var service = new MockForecastService(context, Mock.Of<ICurrentUserService>());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetForecastAsync(0));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetForecastAsync(31));
    }

    [Fact]
    public async Task GetForecastAsync_ShouldThrowArgumentException_WhenIncludePastDaysIsInvalid()
    {
        // Arrange
        var context = GetDbContext();
        var service = new MockForecastService(context, Mock.Of<ICurrentUserService>());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetForecastAsync(7, -1));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetForecastAsync(7, 31));
    }

    [Fact]
    public async Task GetForecastAsync_ShouldReturnForecasts_WhenValid()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var recipeId = Guid.NewGuid();
        context.Recipes.Add(new Recipe { Id = recipeId, Name = "Test Recipe", StoreId = storeId });
        await context.SaveChangesAsync();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);
        var service = new MockForecastService(context, mockCurrentUserService.Object);

        // Act
        var result = await service.GetForecastAsync(7, 0);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains(result, f => f.RecipeId == recipeId.ToString());
    }

    [Fact]
    public async Task GetForecastAsync_ShouldUseDefaultAverage_WhenNoSalesHistory()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var recipeId = Guid.NewGuid();
        context.Recipes.Add(new Recipe { Id = recipeId, Name = "New Recipe", StoreId = storeId });
        await context.SaveChangesAsync();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);
        var service = new MockForecastService(context, mockCurrentUserService.Object);

        // Act
        var result = await service.GetForecastAsync(1, 0);

        // Assert
        // Default average is 10. Multiplier for weekdays is 0.8-1.5. Random factor 0.9-1.1.
        // So prediction should be roughly around 10.
        var forecast = result.First(f => f.RecipeId == recipeId.ToString());
        Assert.True(forecast.Quantity > 0); 
    }

    [Fact]
    public async Task GetForecastSummaryAsync_ShouldReturnSummary()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        context.Recipes.Add(new Recipe { Id = Guid.NewGuid(), Name = "Test Recipe", StoreId = storeId });
        await context.SaveChangesAsync();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);
        var service = new MockForecastService(context, mockCurrentUserService.Object);

        // Act
        var result = await service.GetForecastSummaryAsync(7, 0);

        // Assert
        Assert.NotEmpty(result);
    }
}

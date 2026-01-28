using Xunit;
using Microsoft.EntityFrameworkCore;
using SmartSusChef.Api.Data;
using SmartSusChef.Api.Services;
using SmartSusChef.Api.Models;

namespace SmartSusChef.Api.Tests.Services;

public class SalesServiceTests
{
    private ApplicationDbContext GetDbContext()
    {
        // Creates a fresh, unique in-memory database for every test run
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetTrendAsync_ShouldReturnExactly30DataPoints()
    {
        // 1. Arrange
        var context = GetDbContext();
        var service = new SalesService(context, context); // Pass context twice as per constructor
        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddDays(-29);

        // 2. Act
        var result = await service.GetTrendAsync(startDate, endDate);

        // 3. Assert
        Assert.Equal(30, result.Count);
    }

    [Fact]
    public async Task GetTrendAsync_ShouldOnlyReturnDataForCurrentStore()
    {
        // 1. Arrange
        var context = GetDbContext();
        
        // Seed a recipe to avoid null reference if SalesService tries to access Recipe.Name
        var recipeId = Guid.NewGuid();
        context.Recipes.Add(new Recipe { Id = recipeId, Name = "Test Recipe", StoreId = 1 });
        
        context.SalesData.Add(new SalesData { Id = Guid.NewGuid(), StoreId = 1, Date = DateTime.UtcNow, Quantity = 10, RecipeId = recipeId });
        context.SalesData.Add(new SalesData { Id = Guid.NewGuid(), StoreId = 2, Date = DateTime.UtcNow, Quantity = 50, RecipeId = recipeId }); // Should be ignored
        await context.SaveChangesAsync();

        var service = new SalesService(context, context);

        // 2. Act
        var result = await service.GetTrendAsync(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

        // 3. Assert
        // Verify that the quantities from Store 2 (50) are not included
        // The result will contain one entry for today with quantity 10
        var todayEntry = result.FirstOrDefault(x => x.Date == DateTime.UtcNow.ToString("yyyy-MM-dd"));
        Assert.NotNull(todayEntry);
        Assert.Equal(10, todayEntry.TotalQuantity);
    }
}

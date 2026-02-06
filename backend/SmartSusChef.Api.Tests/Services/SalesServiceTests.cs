using Xunit;
using Microsoft.EntityFrameworkCore;
using SmartSusChef.Api.Data;
using SmartSusChef.Api.Services;
using SmartSusChef.Api.Models;
using Moq;

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

    private async Task SeedSalesDataAsync(ApplicationDbContext context, int daysToSeed = 30)
    {
        var recipeId = Guid.NewGuid();
        context.Recipes.Add(new Recipe { Id = recipeId, Name = "Test Recipe", StoreId = 1 });

        var startDate = DateTime.UtcNow.Date.AddDays(-29);
        for (int i = 0; i < daysToSeed; i++)
        {
            var date = startDate.AddDays(i);
            context.SalesData.Add(new SalesData
            {
                Id = Guid.NewGuid(),
                StoreId = 1,
                Date = date,
                Quantity = 10 + i, // Varying quantity
                RecipeId = recipeId
            });
        }
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetTrendAsync_ShouldReturnExactly30DataPoints()
    {
        // 1. Arrange
        var context = GetDbContext();
        // Seed only 15 days of data to test date-filling logic
        await SeedSalesDataAsync(context, daysToSeed: 15); 
        
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(1);

        var service = new SalesService(context, mockCurrentUserService.Object); 
        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddDays(-29);

        // 2. Act
        var result = await service.GetTrendAsync(startDate, endDate);

        // 3. Assert
        Assert.Equal(30, result.Count);
        
        // Verify data integrity
        // First 15 days should have data (10 to 24)
        Assert.Equal(10, result.First().TotalQuantity);
        Assert.Equal(24, result[14].TotalQuantity);
        
        // Remaining 15 days should have 0 quantity (date filling)
        Assert.Equal(0, result[15].TotalQuantity);
        Assert.Equal(0, result.Last().TotalQuantity);
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

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(1);

        var service = new SalesService(context, mockCurrentUserService.Object);

        // 2. Act
        var result = await service.GetTrendAsync(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

        // 3. Assert
        // Verify that the quantities from Store 2 (50) are not included
        // The result will contain one entry for today with quantity 10
        var todayEntry = result.FirstOrDefault(x => x.Date == DateTime.UtcNow.ToString("yyyy-MM-dd"));
        Assert.NotNull(todayEntry);
        Assert.Equal(10, todayEntry.TotalQuantity);
    }

    [Fact]
    public async Task CreateAsync_ShouldSaveNewSalesEntry_WithValidRecipeId()
    {
        // 1. Arrange
        var context = GetDbContext();
        var recipeId = Guid.NewGuid();
        var storeId = 1;
        context.Recipes.Add(new Recipe { Id = recipeId, Name = "Test Recipe", StoreId = storeId });
        await context.SaveChangesAsync();
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);

        var service = new SalesService(context, mockCurrentUserService.Object);

        var request = new DTOs.CreateSalesDataRequest(
            Date: DateTime.UtcNow.ToString("o"),
            RecipeId: recipeId.ToString(),
            Quantity: 15
        );

        // 2. Act
        var resultDto = await service.CreateAsync(request);

        // 3. Assert
        Assert.NotNull(resultDto);
        Assert.Equal(15, resultDto.Quantity);

        var savedEntry = await context.SalesData.FirstOrDefaultAsync(s => s.Id.ToString() == resultDto.Id);
        Assert.NotNull(savedEntry);
        Assert.Equal(15, savedEntry.Quantity);
        Assert.Equal(recipeId, savedEntry.RecipeId);
        Assert.Equal(storeId, savedEntry.StoreId);
    }
}

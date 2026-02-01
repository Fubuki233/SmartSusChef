using Xunit;
using Microsoft.EntityFrameworkCore;
using SmartSusChef.Api.Data;
using SmartSusChef.Api.Services;
using SmartSusChef.Api.Models;
using Moq; // Ensure Moq is installed via NuGet

namespace SmartSusChef.Api.Tests.Services;

public class IngredientServiceTests
{
    private ApplicationDbContext GetDbContext()
    {
        // Requires Microsoft.EntityFrameworkCore.InMemory NuGet package
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetTotalCarbonImpactAsync_ShouldCalculateAccurateHighPrecisionSum()
    {
        // 1. Arrange
        var context = GetDbContext();
        
        // Mock the ICurrentUserService to return StoreId 1
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(1);

        var ingA = new Ingredient { 
            Id = Guid.NewGuid(), Name = "Beef", CarbonFootprint = 2.500m, StoreId = 1, Unit = "kg" 
        };
        var ingB = new Ingredient { 
            Id = Guid.NewGuid(), Name = "Tomato", CarbonFootprint = 0.125m, StoreId = 1, Unit = "kg" 
        };

        context.Ingredients.AddRange(ingA, ingB);
        await context.SaveChangesAsync();

        // Pass the Mock object as the second parameter to satisfy the new constructor
        var service = new IngredientService(context, mockCurrentUserService.Object); 

        var items = new List<(Guid Id, decimal Quantity)> 
        {
            (ingA.Id, 2.0m),
            (ingB.Id, 4.0m)
        };

        // 2. Act
        var totalImpact = await service.GetTotalCarbonImpactAsync(items);

        // 3. Assert
        // Verify math: (2 * 2.5) + (4 * 0.125) = 5.500
        Assert.Equal(5.500m, totalImpact);
    }
}

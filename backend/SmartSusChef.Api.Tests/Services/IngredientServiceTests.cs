using Xunit;
using Microsoft.EntityFrameworkCore;
using SmartSusChef.Api.Data;
using SmartSusChef.Api.Services;
using SmartSusChef.Api.Models;

namespace SmartSusChef.Api.Tests.Services;

public class IngredientServiceTests
{
    private ApplicationDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetTotalCarbonImpactAsync_ShouldCalculateAccurateHighPrecisionSum()
    {
        // 1. Arrange: Setup ingredients with specific carbon footprints
        var context = GetDbContext();
        
        // Ingredient A: 2.500 kg CO2e per unit
        var ingA = new Ingredient { 
            Id = Guid.NewGuid(), 
            Name = "Beef", 
            CarbonFootprint = 2.500m, 
            StoreId = 1,
            Unit = "kg" 
        };
        
        // Ingredient B: 0.125 kg CO2e per unit
        var ingB = new Ingredient { 
            Id = Guid.NewGuid(), 
            Name = "Tomato", 
            CarbonFootprint = 0.125m, 
            StoreId = 1,
            Unit = "kg" 
        };

        context.Ingredients.AddRange(ingA, ingB);
        await context.SaveChangesAsync();

        var service = new IngredientService(context);

        // Define quantities: 2kg of Beef and 4kg of Tomato
        // Expected Math: (2 * 2.500) + (4 * 0.125) = 5.000 + 0.500 = 5.500
        var items = new List<(Guid Id, decimal Quantity)> 
        {
            (ingA.Id, 2.0m),
            (ingB.Id, 4.0m)
        };

        // 2. Act
        var totalImpact = await service.GetTotalCarbonImpactAsync(items);

        // 3. Assert
        Assert.Equal(5.500m, totalImpact);
    }
}

using Xunit;
using Microsoft.EntityFrameworkCore;
using SmartSusChef.Api.Data;
using SmartSusChef.Api.Services;
using SmartSusChef.Api.Models;
using Moq;

namespace SmartSusChef.Api.Tests.Services;

public class WastageServiceTests
{
    private ApplicationDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetTotalWastageImpactAsync_ShouldSumIngredientsAndRecipesCorrectly()
    {
        // 1. Arrange
        var context = GetDbContext();
        var storeId = 1;

        // Ingredient: Beef (High Impact)
        var beef = new Ingredient { Id = Guid.NewGuid(), Name = "Beef", CarbonFootprint = 27.0m, StoreId = storeId, Unit = "kg" };
        context.Ingredients.Add(beef);

        // Recipe: Burger (Complex Item)
        // We will MOCK the recipe service to return a specific footprint for this burger
        // so we don't need to seed the entire recipe tree here.
        var burgerRecipe = new Recipe { Id = Guid.NewGuid(), Name = "Burger", StoreId = storeId };
        context.Recipes.Add(burgerRecipe);

        // Wastage Records
        // 1. Wasted 2kg of Beef -> 2 * 27 = 54 kg CO2e
        context.WastageData.Add(new WastageData 
        { 
            Id = Guid.NewGuid(), 
            StoreId = storeId, 
            Date = DateTime.UtcNow, 
            IngredientId = beef.Id, 
            Quantity = 2.0m 
        });

        // 2. Wasted 5 Burgers -> 5 * (Mocked 3.5) = 17.5 kg CO2e
        context.WastageData.Add(new WastageData 
        { 
            Id = Guid.NewGuid(), 
            StoreId = storeId, 
            Date = DateTime.UtcNow, 
            RecipeId = burgerRecipe.Id, 
            Quantity = 5.0m 
        });

        await context.SaveChangesAsync();

        // Mock RecipeService
        var mockRecipeService = new Mock<IRecipeService>();
        mockRecipeService
            .Setup(s => s.CalculateTotalCarbonFootprintAsync(burgerRecipe.Id))
            .ReturnsAsync(3.5m); // Assume each burger is 3.5 kg CO2e

        var service = new WastageService(context, mockRecipeService.Object);

        // 2. Act
        var totalImpact = await service.GetTotalWastageImpactAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));

        // 3. Assert
        // Expected: 54 + 17.5 = 71.5
        Assert.Equal(71.5m, totalImpact);
    }
}
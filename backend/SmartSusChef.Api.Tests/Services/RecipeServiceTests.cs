namespace SmartSusChef.Api.Tests.Services;

using Xunit;
using Microsoft.EntityFrameworkCore;
using SmartSusChef.Api.Data;
using SmartSusChef.Api.Services;
using SmartSusChef.Api.Models;

public class RecipeServiceTests
{
    private ApplicationDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CalculateTotalCarbonFootprintAsync_ShouldCalculateRecursiveNestedFootprints()
    {
        // 1. Arrange
        var context = GetDbContext();
        var storeId = 1;

        // Create a base Ingredient (Flour)
        var flour = new Ingredient 
        { 
            Id = Guid.NewGuid(), Name = "Flour", CarbonFootprint = 0.500m, StoreId = storeId, Unit = "kg" 
        };
        context.Ingredients.Add(flour);

        // Create a Sub-Recipe (Dough) that uses 2 units of Flour
        // Impact: 2 * 0.500 = 1.000
        var doughRecipe = new Recipe { Id = Guid.NewGuid(), Name = "Dough", StoreId = storeId, IsSubRecipe = true };
        context.Recipes.Add(doughRecipe);
        context.RecipeIngredients.Add(new RecipeIngredient { Id = Guid.NewGuid(), RecipeId = doughRecipe.Id, IngredientId = flour.Id, Quantity = 2.0m });

        // Create the Final Recipe (Pizza) that uses 1 unit of Dough
        // Impact: 1 * 1.000 = 1.000
        var pizzaRecipe = new Recipe { Id = Guid.NewGuid(), Name = "Pizza", StoreId = storeId, IsSellable = true };
        context.Recipes.Add(pizzaRecipe);
        context.RecipeIngredients.Add(new RecipeIngredient { Id = Guid.NewGuid(), RecipeId = pizzaRecipe.Id, ChildRecipeId = doughRecipe.Id, Quantity = 1.0m });

        await context.SaveChangesAsync();
        var service = new RecipeService(context);

        // 2. Act
        var totalFootprint = await service.CalculateTotalCarbonFootprintAsync(pizzaRecipe.Id);

        // 3. Assert
        // Verify the recursion correctly identified the Flour footprint through the Dough sub-recipe
        Assert.Equal(1.000m, totalFootprint);
    }
}

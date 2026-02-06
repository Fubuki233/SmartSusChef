namespace SmartSusChef.Api.Tests.Services;

using Xunit;
using Microsoft.EntityFrameworkCore;
using SmartSusChef.Api.Data;
using SmartSusChef.Api.Services;
using SmartSusChef.Api.Models;
using Moq;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

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

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);

        var service = new RecipeService(context, mockCurrentUserService.Object);

        // 2. Act
        var totalFootprint = await service.CalculateTotalCarbonFootprintAsync(pizzaRecipe.Id);

        // 3. Assert
        // Verify the recursion correctly identified the Flour footprint through the Dough sub-recipe
        Assert.Equal(1.000m, totalFootprint);
        Assert.Equal(1.000m, totalFootprint);
    }
    
    [Fact]
    public async Task CreateAsync_ShouldAddNewRecipe_WhenGivenValidData()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var ingredientId = Guid.NewGuid();
        context.Ingredients.Add(new Ingredient { Id = ingredientId, Name = "Cheese", StoreId = storeId, Unit = "kg", CarbonFootprint = 5.0m });
        await context.SaveChangesAsync();
        
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);
        var service = new RecipeService(context, mockCurrentUserService.Object);
        
        var request = new DTOs.CreateRecipeRequest(
            "Margherita Pizza",
            true,
            false,
            new List<DTOs.CreateRecipeIngredientRequest>
            {
                new(ingredientId.ToString(), null, 0.2m)
            }
        );

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Margherita Pizza", result.Name);
        var recipeInDb = await context.Recipes.Include(r => r.RecipeIngredients).FirstOrDefaultAsync(r => r.Id.ToString() == result.Id);
        Assert.NotNull(recipeInDb);
        Assert.Single(recipeInDb.RecipeIngredients);
        Assert.Equal(ingredientId, recipeInDb.RecipeIngredients.First().IngredientId);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowException_WhenNameIsDuplicate()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        context.Recipes.Add(new Recipe { Id = Guid.NewGuid(), Name = "Margherita Pizza", StoreId = storeId });
        await context.SaveChangesAsync();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);
        var service = new RecipeService(context, mockCurrentUserService.Object);

        var request = new DTOs.CreateRecipeRequest(
            "Margherita Pizza",
            true,
            false,
            new List<DTOs.CreateRecipeIngredientRequest>()
        );

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowException_WhenIngredientStructureIsInvalid()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);
        var service = new RecipeService(context, mockCurrentUserService.Object);

        var request = new DTOs.CreateRecipeRequest(
            "Invalid Recipe",
            true,
            false,
            new List<DTOs.CreateRecipeIngredientRequest>
            {
                new(null, null, 1.0m) // Both null
            }
        );

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnOnlyRecipesForCurrentStore()
    {
        // Arrange
        var context = GetDbContext();
        var storeId1 = 1;
        var storeId2 = 2;
        context.Recipes.Add(new Recipe { Id = Guid.NewGuid(), Name = "Recipe 1", StoreId = storeId1 });
        context.Recipes.Add(new Recipe { Id = Guid.NewGuid(), Name = "Recipe 2", StoreId = storeId2 });
        await context.SaveChangesAsync();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId1);
        var service = new RecipeService(context, mockCurrentUserService.Object);
        
        // Act
        var result = await service.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Recipe 1", result.First().Name);
    }
    
    [Fact]
    public async Task UpdateAsync_ShouldModifyExistingRecipe()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var recipeId = Guid.NewGuid();
        var initialIngredientId = Guid.NewGuid();
        var updatedIngredientId = Guid.NewGuid();
        context.Ingredients.Add(new Ingredient { Id = initialIngredientId, Name = "Dough", StoreId = storeId, Unit = "kg" });
        context.Ingredients.Add(new Ingredient { Id = updatedIngredientId, Name = "Tomato Sauce", StoreId = storeId, Unit = "kg" });
        var recipe = new Recipe { Id = recipeId, Name = "Old Pizza", StoreId = storeId };
        recipe.RecipeIngredients.Add(new RecipeIngredient { Id = Guid.NewGuid(), RecipeId = recipeId, IngredientId = initialIngredientId, Quantity = 1.0m });
        context.Recipes.Add(recipe);
        await context.SaveChangesAsync();
        
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);
        var service = new RecipeService(context, mockCurrentUserService.Object);
        
        var updateRequest = new DTOs.UpdateRecipeRequest(
            "New Pizza",
            true,
            false,
            new List<DTOs.CreateRecipeIngredientRequest>
            {
                new(updatedIngredientId.ToString(), null, 0.5m)
            }
        );

        // Act
        var result = await service.UpdateAsync(recipeId, updateRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Pizza", result.Name);
        Assert.Single(result.Ingredients);
        Assert.Equal(updatedIngredientId.ToString(), result.Ingredients.First().IngredientId);
        
        var recipeInDb = await context.Recipes.Include(r => r.RecipeIngredients).FirstOrDefaultAsync(r => r.Id == recipeId);
        Assert.NotNull(recipeInDb);
        Assert.Equal("New Pizza", recipeInDb.Name);
        Assert.Single(recipeInDb.RecipeIngredients);
        Assert.Equal(updatedIngredientId, recipeInDb.RecipeIngredients.First().IngredientId);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNull_WhenRecipeDoesNotExist()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);
        var service = new RecipeService(context, mockCurrentUserService.Object);
        var updateRequest = new DTOs.UpdateRecipeRequest("New Pizza", true, false, new List<DTOs.CreateRecipeIngredientRequest>());

        // Act
        var result = await service.UpdateAsync(Guid.NewGuid(), updateRequest);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveRecipe_WhenNotUsedAsSubRecipe()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var recipeId = Guid.NewGuid();
        context.Recipes.Add(new Recipe { Id = recipeId, Name = "Deletable Recipe", StoreId = storeId });
        await context.SaveChangesAsync();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);
        var service = new RecipeService(context, mockCurrentUserService.Object);

        // Act
        var result = await service.DeleteAsync(recipeId);

        // Assert
        Assert.True(result);
        var recipeInDb = await context.Recipes.FirstOrDefaultAsync(r => r.Id == recipeId);
        Assert.Null(recipeInDb);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenRecipeDoesNotExist()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);
        var service = new RecipeService(context, mockCurrentUserService.Object);

        // Act
        var result = await service.DeleteAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenRecipeDoesNotExist()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);
        var service = new RecipeService(context, mockCurrentUserService.Object);

        // Act
        var result = await service.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }
}

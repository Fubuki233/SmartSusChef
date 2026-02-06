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

        // Mock CurrentUserService
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);

        var service = new WastageService(context, mockRecipeService.Object, mockCurrentUserService.Object);

        // 2. Act
        var totalImpact = await service.GetTotalWastageImpactAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));

        // 3. Assert
        // Expected: 54 + 17.5 = 71.5
        Assert.Equal(71.5m, totalImpact);
    }

    [Fact]
    public async Task CreateAsync_ShouldSaveWastageEntry_WithValidIngredientId()
    {
        // 1. Arrange
        var context = GetDbContext();
        var storeId = 1;
        var ingredientId = Guid.NewGuid();

        // Seed a valid ingredient
        context.Ingredients.Add(new Ingredient { Id = ingredientId, Name = "Tomatoes", StoreId = storeId, Unit = "kg", CarbonFootprint = 1.1m});
        await context.SaveChangesAsync();
        
        // Mock dependencies
        var mockRecipeService = new Mock<IRecipeService>();
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);

        var service = new WastageService(context, mockRecipeService.Object, mockCurrentUserService.Object);

        var request = new DTOs.CreateWastageDataRequest(
            Date: DateTime.UtcNow.ToString("o"),
            IngredientId: ingredientId.ToString(),
            RecipeId: null,
            Quantity: 5.5m
        );
        
        // 2. Act
        var resultDto = await service.CreateAsync(request);

        // 3. Assert
        Assert.NotNull(resultDto);
        Assert.Equal(5.5m, resultDto.Quantity);
        Assert.Equal(ingredientId.ToString(), resultDto.IngredientId);

        var savedEntry = await context.WastageData.FirstOrDefaultAsync(w => w.Id.ToString() == resultDto.Id);
        Assert.NotNull(savedEntry);
        Assert.Equal(5.5m, savedEntry.Quantity);
        Assert.Equal(ingredientId, savedEntry.IngredientId);
        Assert.Null(savedEntry.RecipeId);
        Assert.Equal(storeId, savedEntry.StoreId);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowArgumentException_WhenQuantityIsNegative()
    {
        // Arrange
        var context = GetDbContext();
        var service = new WastageService(context, Mock.Of<IRecipeService>(), Mock.Of<ICurrentUserService>());
        var request = new DTOs.CreateWastageDataRequest(DateTime.UtcNow.ToString("o"), Guid.NewGuid().ToString(), null, -1.0m);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllWastageForStore()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);
        var service = new WastageService(context, Mock.Of<IRecipeService>(), mockCurrentUserService.Object);

        context.WastageData.Add(new WastageData { StoreId = storeId });
        context.WastageData.Add(new WastageData { StoreId = storeId });
        context.WastageData.Add(new WastageData { StoreId = 2 });
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetTrendAsync_ShouldReturnCorrectlyStructuredData()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);
        var mockRecipeService = new Mock<IRecipeService>();
        var service = new WastageService(context, mockRecipeService.Object, mockCurrentUserService.Object);
        var date = DateTime.UtcNow.Date;

        var ingredient = new Ingredient { Id = Guid.NewGuid(), Name = "Beef", CarbonFootprint = 27.0m, StoreId = storeId, Unit = "kg" };
        context.Ingredients.Add(ingredient);
        context.WastageData.Add(new WastageData { Date = date, StoreId = storeId, Ingredient = ingredient, Quantity = 2.0m });
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetTrendAsync(date, date);

        // Assert
        Assert.Single(result);
        var trendDay = result.First();
        Assert.Equal(date.ToString("yyyy-MM-dd"), trendDay.Date);
        Assert.Equal(2.0m, trendDay.TotalQuantity);
        Assert.Equal(54.0m, trendDay.TotalCarbonFootprint);
        Assert.Single(trendDay.ItemBreakdown);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateWastageData_WhenExists()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var wastageId = Guid.NewGuid();
        var ingredientId = Guid.NewGuid();
        context.Ingredients.Add(new Ingredient { Id = ingredientId, Name = "Test Ingredient", StoreId = storeId, Unit = "kg" });
        context.WastageData.Add(new WastageData { Id = wastageId, StoreId = storeId, IngredientId = ingredientId, Quantity = 10, Date = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);
        var service = new WastageService(context, Mock.Of<IRecipeService>(), mockCurrentUserService.Object);

        var request = new DTOs.UpdateWastageDataRequest(DateTime.UtcNow.ToString("o"), ingredientId.ToString(), null, 20);

        // Act
        var result = await service.UpdateAsync(wastageId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(20, result.Quantity);
        var dbEntry = await context.WastageData.FindAsync(wastageId);
        Assert.Equal(20, dbEntry.Quantity);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNull_WhenWastageDataDoesNotExist()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);
        var service = new WastageService(context, Mock.Of<IRecipeService>(), mockCurrentUserService.Object);

        var request = new DTOs.UpdateWastageDataRequest(DateTime.UtcNow.ToString("o"), Guid.NewGuid().ToString(), null, 20);

        // Act
        var result = await service.UpdateAsync(Guid.NewGuid(), request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveWastageData_WhenExists()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var wastageId = Guid.NewGuid();
        context.WastageData.Add(new WastageData { Id = wastageId, StoreId = storeId, Quantity = 10, Date = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);
        var service = new WastageService(context, Mock.Of<IRecipeService>(), mockCurrentUserService.Object);

        // Act
        var result = await service.DeleteAsync(wastageId);

        // Assert
        Assert.True(result);
        var dbEntry = await context.WastageData.FindAsync(wastageId);
        Assert.Null(dbEntry);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenWastageDataDoesNotExist()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);
        var service = new WastageService(context, Mock.Of<IRecipeService>(), mockCurrentUserService.Object);

        // Act
        var result = await service.DeleteAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenWastageDataDoesNotExist()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);
        var service = new WastageService(context, Mock.Of<IRecipeService>(), mockCurrentUserService.Object);

        // Act
        var result = await service.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }
}

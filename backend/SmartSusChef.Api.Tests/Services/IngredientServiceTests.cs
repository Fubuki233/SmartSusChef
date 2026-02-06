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
    public async Task CreateAsync_ShouldAddNewIngredient()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);
        var service = new IngredientService(context, mockCurrentUserService.Object);
        var request = new DTOs.CreateIngredientRequest("Flour", "kg", 0.5m);

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Flour", result.Name);
        var ingredientInDb = await context.Ingredients.FindAsync(Guid.Parse(result.Id));
        Assert.NotNull(ingredientInDb);
        Assert.Equal(storeId, ingredientInDb.StoreId);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnOnlyIngredientsForCurrentStore()
    {
        // Arrange
        var context = GetDbContext();
        var storeId1 = 1;
        var storeId2 = 2;
        context.Ingredients.Add(new Ingredient { Id = Guid.NewGuid(), Name = "Ingredient 1", StoreId = storeId1, Unit = "kg" });
        context.Ingredients.Add(new Ingredient { Id = Guid.NewGuid(), Name = "Ingredient 2", StoreId = storeId2, Unit = "kg" });
        await context.SaveChangesAsync();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId1);
        var service = new IngredientService(context, mockCurrentUserService.Object);

        // Act
        var result = await service.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Ingredient 1", result.First().Name);
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyExistingIngredient()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var ingredientId = Guid.NewGuid();
        context.Ingredients.Add(new Ingredient { Id = ingredientId, Name = "Old Name", StoreId = storeId, Unit = "g", CarbonFootprint = 1.0m });
        await context.SaveChangesAsync();
        
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);
        var service = new IngredientService(context, mockCurrentUserService.Object);

        var request = new DTOs.UpdateIngredientRequest("New Name", "kg", 2.0m);

        // Act
        var result = await service.UpdateAsync(ingredientId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Name", result.Name);
        Assert.Equal("kg", result.Unit);
        Assert.Equal(2.0m, result.CarbonFootprint);
        var ingredientInDb = await context.Ingredients.FindAsync(ingredientId);
        Assert.NotNull(ingredientInDb);
        Assert.Equal("New Name", ingredientInDb.Name);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveIngredient()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var ingredientId = Guid.NewGuid();
        context.Ingredients.Add(new Ingredient { Id = ingredientId, Name = "ToDelete", StoreId = storeId, Unit = "kg" });
        await context.SaveChangesAsync();
        
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);
        var service = new IngredientService(context, mockCurrentUserService.Object);

        // Act
        var result = await service.DeleteAsync(ingredientId);

        // Assert
        Assert.True(result);
        var ingredientInDb = await context.Ingredients.FindAsync(ingredientId);
        Assert.Null(ingredientInDb);
    }
}

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
using System.Reflection;
using SmartSusChef.Api.DTOs;

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

    [Fact]
    public async Task UpdateAsync_ShouldUpdateQuantity_WhenSalesDataExists()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var salesId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();
        context.Recipes.Add(new Recipe { Id = recipeId, Name = "Test Recipe", StoreId = storeId });
        context.SalesData.Add(new SalesData { Id = salesId, StoreId = storeId, Quantity = 10, RecipeId = recipeId, Date = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);
        var service = new SalesService(context, mockCurrentUserService.Object);

        var request = new DTOs.UpdateSalesDataRequest(20);

        // Act
        var result = await service.UpdateAsync(salesId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(20, result.Quantity);
        var dbEntry = await context.SalesData.FindAsync(salesId);
                Assert.NotNull(dbEntry);
        Assert.Equal(20, dbEntry.Quantity);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNull_WhenSalesDataDoesNotExist()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);
        var service = new SalesService(context, mockCurrentUserService.Object);

        var request = new DTOs.UpdateSalesDataRequest(20);

        // Act
        var result = await service.UpdateAsync(Guid.NewGuid(), request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveSalesData_WhenExists()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var salesId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();
        context.Recipes.Add(new Recipe { Id = recipeId, Name = "Test Recipe", StoreId = storeId });
        context.SalesData.Add(new SalesData { Id = salesId, StoreId = storeId, Quantity = 10, RecipeId = recipeId, Date = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);
        var service = new SalesService(context, mockCurrentUserService.Object);

        // Act
        var result = await service.DeleteAsync(salesId);

        // Assert
        Assert.True(result);
        var dbEntry = await context.SalesData.FindAsync(salesId);
        Assert.Null(dbEntry);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenSalesDataDoesNotExist()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);
        var service = new SalesService(context, mockCurrentUserService.Object);

        // Act
        var result = await service.DeleteAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByDateRange()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var recipeId = Guid.NewGuid();
        context.Recipes.Add(new Recipe { Id = recipeId, Name = "Test Recipe", StoreId = storeId });
        context.SalesData.Add(new SalesData { Id = Guid.NewGuid(), StoreId = storeId, Quantity = 10, RecipeId = recipeId, Date = new DateTime(2024, 1, 1) });
        context.SalesData.Add(new SalesData { Id = Guid.NewGuid(), StoreId = storeId, Quantity = 20, RecipeId = recipeId, Date = new DateTime(2024, 1, 10) });
        context.SalesData.Add(new SalesData { Id = Guid.NewGuid(), StoreId = storeId, Quantity = 30, RecipeId = recipeId, Date = new DateTime(2024, 1, 20) });
        await context.SaveChangesAsync();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);
        var service = new SalesService(context, mockCurrentUserService.Object);

        // Act
        var result = await service.GetAllAsync(new DateTime(2024, 1, 5), new DateTime(2024, 1, 15));

        // Assert
        Assert.Single(result);
        Assert.Equal(20, result[0].Quantity);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllSalesForStore()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var recipeId = Guid.NewGuid();
        context.Recipes.Add(new Recipe { Id = recipeId, Name = "Test Recipe", StoreId = storeId });
        context.SalesData.Add(new SalesData { Id = Guid.NewGuid(), StoreId = storeId, Quantity = 10, RecipeId = recipeId, Date = DateTime.UtcNow });
        context.SalesData.Add(new SalesData { Id = Guid.NewGuid(), StoreId = storeId, Quantity = 20, RecipeId = recipeId, Date = DateTime.UtcNow.AddDays(-1) });
        context.SalesData.Add(new SalesData { Id = Guid.NewGuid(), StoreId = 2, Quantity = 30, RecipeId = recipeId, Date = DateTime.UtcNow }); // Different store
        await context.SaveChangesAsync();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);
        var service = new SalesService(context, mockCurrentUserService.Object);

        // Act
        var result = await service.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnEmptyList_WhenNoSalesExist()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);
        var service = new SalesService(context, mockCurrentUserService.Object);

        // Act
        var result = await service.GetAllAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task CreateAsync_ShouldUpdateExistingRecord_WhenDuplicate()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var recipeId = Guid.NewGuid();
        var date = new DateTime(2024, 2, 1);
        context.Recipes.Add(new Recipe { Id = recipeId, Name = "Test Recipe", StoreId = storeId });
        var existing = new SalesData
        {
            Id = Guid.NewGuid(),
            StoreId = storeId,
            RecipeId = recipeId,
            Date = date,
            Quantity = 5,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        context.SalesData.Add(existing);
        await context.SaveChangesAsync();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);
        var service = new SalesService(context, mockCurrentUserService.Object);

        var request = new CreateSalesDataRequest(date.ToString("yyyy-MM-dd"), recipeId.ToString(), 12);

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        Assert.Equal(existing.Id.ToString(), result.Id);
        Assert.Equal(12, result.Quantity);
    }

    [Fact]
    public async Task ImportAsync_ShouldImportSalesData()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var recipeId = Guid.NewGuid();
        context.Recipes.Add(new Recipe { Id = recipeId, Name = "Test Recipe", StoreId = storeId });
        await context.SaveChangesAsync();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);
        var service = new SalesService(context, mockCurrentUserService.Object);

        var importData = new List<DTOs.CreateSalesDataRequest>
        {
            new(DateTime.UtcNow.ToString("o"), recipeId.ToString(), 10),
            new(DateTime.UtcNow.AddDays(-1).ToString("o"), recipeId.ToString(), 20)
        };

        // Act
        await service.ImportAsync(importData);

        // Assert
        var salesCount = await context.SalesData.CountAsync();
        Assert.Equal(2, salesCount);
    }

    [Fact]
    public async Task ImportAsync_ShouldNoOp_WhenInputEmpty()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);
        var service = new SalesService(context, mockCurrentUserService.Object);

        // Act
        await service.ImportAsync(new List<CreateSalesDataRequest>());

        // Assert
        Assert.Equal(0, await context.SalesData.CountAsync());
    }

    [Fact]
    public async Task ImportAsync_ShouldDeduplicateAndUpdateExisting()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var recipeId = Guid.NewGuid();
        context.Recipes.Add(new Recipe { Id = recipeId, Name = "Test Recipe", StoreId = storeId });
        var existing = new SalesData
        {
            Id = Guid.NewGuid(),
            StoreId = storeId,
            RecipeId = recipeId,
            Date = new DateTime(2024, 3, 1),
            Quantity = 1
        };
        context.SalesData.Add(existing);
        await context.SaveChangesAsync();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);
        var service = new SalesService(context, mockCurrentUserService.Object);

        var importData = new List<CreateSalesDataRequest>
        {
            new("2024-03-01", recipeId.ToString(), 5),
            new("2024-03-01", recipeId.ToString(), 9) // duplicate, should take last
        };

        // Act
        await service.ImportAsync(importData);

        // Assert
        var dbEntry = await context.SalesData.FindAsync(existing.Id);
        Assert.NotNull(dbEntry);
        Assert.Equal(9, dbEntry.Quantity);
    }

    [Fact]
    public async Task GetIngredientUsageByDateAsync_ShouldReturnCorrectUsage()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var recipeId = Guid.NewGuid();
        var ingredientId = Guid.NewGuid();
        var date = DateTime.UtcNow.Date;

        var ingredient = new Ingredient { Id = ingredientId, Name = "Flour", Unit = "kg", StoreId = storeId };
        context.Ingredients.Add(ingredient);

        var recipe = new Recipe { Id = recipeId, Name = "Bread", StoreId = storeId };
        recipe.RecipeIngredients.Add(new RecipeIngredient { IngredientId = ingredientId, Quantity = 0.5m });
        context.Recipes.Add(recipe);

        context.SalesData.Add(new SalesData { Id = Guid.NewGuid(), StoreId = storeId, RecipeId = recipeId, Quantity = 10, Date = date });
        await context.SaveChangesAsync();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);
        var service = new SalesService(context, mockCurrentUserService.Object);

        // Act
        var result = await service.GetIngredientUsageByDateAsync(date);

        // Assert
        Assert.Single(result);
        var usage = result.First();
        Assert.Equal("Flour", usage.IngredientName);
        Assert.Equal(5.0m, usage.Quantity); // 10 sales * 0.5kg
    }

    [Fact]
    public async Task GetRecipeSalesByDateAsync_ShouldReturnCorrectSales()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var recipeId = Guid.NewGuid();
        var date = DateTime.UtcNow.Date;

        context.Recipes.Add(new Recipe { Id = recipeId, Name = "Bread", StoreId = storeId });
        context.SalesData.Add(new SalesData { Id = Guid.NewGuid(), StoreId = storeId, RecipeId = recipeId, Quantity = 10, Date = date });
        context.SalesData.Add(new SalesData { Id = Guid.NewGuid(), StoreId = storeId, RecipeId = recipeId, Quantity = 5, Date = date });
        await context.SaveChangesAsync();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);
        var service = new SalesService(context, mockCurrentUserService.Object);

        // Act
        var result = await service.GetRecipeSalesByDateAsync(date);

        // Assert
        Assert.Single(result);
        var sales = result.First();
        Assert.Equal("Bread", sales.RecipeName);
        Assert.Equal(15, sales.Quantity); // 10 + 5
    }

    [Fact]
    public async Task GetIngredientUsageByDateAsync_ShouldSkipNullIngredientRefs()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var recipeId = Guid.NewGuid();
        var date = DateTime.UtcNow.Date;

        var recipe = new Recipe { Id = recipeId, Name = "Bread", StoreId = storeId };
        recipe.RecipeIngredients.Add(new RecipeIngredient { IngredientId = null, Quantity = 1m });
        context.Recipes.Add(recipe);

        context.SalesData.Add(new SalesData { Id = Guid.NewGuid(), StoreId = storeId, RecipeId = recipeId, Quantity = 10, Date = date });
        await context.SaveChangesAsync();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);
        var service = new SalesService(context, mockCurrentUserService.Object);

        // Act
        var result = await service.GetIngredientUsageByDateAsync(date);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnSalesData_WhenExists()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var salesId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();
        context.Recipes.Add(new Recipe { Id = recipeId, Name = "Test Recipe", StoreId = storeId });
        context.SalesData.Add(new SalesData { Id = salesId, StoreId = storeId, Quantity = 10, RecipeId = recipeId, Date = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);
        var service = new SalesService(context, mockCurrentUserService.Object);

        // Act
        var result = await service.GetByIdAsync(salesId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(salesId.ToString(), result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenSalesDataDoesNotExist()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);
        var service = new SalesService(context, mockCurrentUserService.Object);

        // Act
        var result = await service.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ImportByNameAsync_ShouldCreateMissingRecipes_AndImport()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        context.Recipes.Add(new Recipe { Id = Guid.NewGuid(), Name = "Existing Dish", StoreId = storeId });
        await context.SaveChangesAsync();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(storeId);
        var service = new SalesService(context, mockCurrentUserService.Object);

        var importItems = new List<ImportSalesByNameItem>
        {
            new("01/04/2024", "Existing Dish", 3),
            new("02/04/2024", "New Dish", 5)
        };

        // Act
        var result = await service.ImportByNameAsync(importItems, "dd/MM/yyyy");

        // Assert
        Assert.Equal(2, result.Imported);
        Assert.Equal(1, result.Created);
        Assert.Contains("New Dish", result.NewDishes);
        Assert.Equal(2, await context.SalesData.CountAsync());
        Assert.Equal(2, await context.Recipes.CountAsync());
    }

    [Fact]
    public async Task GetSalesTrendsWithSignalsAsync_ShouldDelegateToGetTrendAsync()
    {
        // Arrange
        var context = GetDbContext();
        var recipeId = Guid.NewGuid();
        context.Recipes.Add(new Recipe { Id = recipeId, Name = "Test Recipe", StoreId = 1 });
        context.SalesData.Add(new SalesData { Id = Guid.NewGuid(), StoreId = 1, Date = new DateTime(2024, 4, 1), Quantity = 7, RecipeId = recipeId });
        await context.SaveChangesAsync();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(1);
        var service = new SalesService(context, mockCurrentUserService.Object);

        var start = new DateTime(2024, 4, 1);
        var end = new DateTime(2024, 4, 1);

        // Act
        var result = await service.GetSalesTrendsWithSignalsAsync(start, end);

        // Assert
        Assert.Single(result);
        Assert.Equal(7, result[0].TotalQuantity);
    }

    [Fact]
    public void IsDuplicateKeyException_ShouldDetectDuplicateMessages()
    {
        // Arrange
        var inner = new Exception("Duplicate entry");
        var ex = new Microsoft.EntityFrameworkCore.DbUpdateException("dup", inner);
        var method = typeof(SalesService).GetMethod("IsDuplicateKeyException", BindingFlags.NonPublic | BindingFlags.Static);

        // Act
        var result = (bool)method!.Invoke(null, new object[] { ex })!;

        // Assert
        Assert.True(result);
    }
}

using Xunit;
using Microsoft.EntityFrameworkCore;
using SmartSusChef.Api.Data;
using SmartSusChef.Api.Services;
using SmartSusChef.Api.Models;
using Moq;
using System.Threading.Tasks;
using System;
using SmartSusChef.Api.DTOs;

namespace SmartSusChef.Api.Tests.Services;

public class StoreServiceTests
{
    private ApplicationDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }
    
    private Mock<ICurrentUserService> GetMockCurrentUserService(int storeId)
    {
        var mock = new Mock<ICurrentUserService>();
        mock.Setup(s => s.StoreId).Returns(storeId);
        return mock;
    }

    [Fact]
    public async Task UpdateStoreSettingsAsync_ShouldOnlyUpdateOwnedStore()
    {
        // 1. Arrange
        var context = GetDbContext();
        
        // Setup two different stores
        var store1 = new Store { Id = 1, StoreName = "Clementi Branch", OutletLocation = "West" };
        var store2 = new Store { Id = 2, StoreName = "Changi Branch", OutletLocation = "East" };
        context.Store.AddRange(store1, store2);
        await context.SaveChangesAsync();

        // Mock the user to be logged into Store 1
        var mockCurrentUserService = GetMockCurrentUserService(1);

        var service = new StoreService(context, mockCurrentUserService.Object);
        var updatedName = "Clementi Main Hub";

        // 2. Act
        // Attempt to update store settings
        var result = await service.UpdateStoreSettingsAsync(1, updatedName);

        // 3. Assert
        Assert.True(result); // Update should succeed for own store
        var dbStore = await context.Store.FindAsync(1);
        Assert.Equal(updatedName, dbStore?.StoreName);

        // Verify Store 2 was NOT changed
        var otherStore = await context.Store.FindAsync(2);
        Assert.Equal("Changi Branch", otherStore?.StoreName);
    }
    
    [Fact]
    public async Task GetStoreAsync_ShouldReturnStore_WhenStoreExists()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var store = new Store { Id = storeId, StoreName = "Test Store" };
        context.Store.Add(store);
        await context.SaveChangesAsync();
        var mockCurrentUserService = GetMockCurrentUserService(storeId);
        var service = new StoreService(context, mockCurrentUserService.Object);

        // Act
        var result = await service.GetStoreAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(storeId, result.Id);
    }

    [Fact]
    public async Task GetStoreAsync_ShouldReturnNull_WhenStoreDoesNotExist()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var mockCurrentUserService = GetMockCurrentUserService(storeId);
        var service = new StoreService(context, mockCurrentUserService.Object);

        // Act
        var result = await service.GetStoreAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task InitializeStoreAsync_ShouldUpdateExistingEmptyStore()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        context.Store.Add(new Store { Id = storeId });
        await context.SaveChangesAsync();
        
        var request = new CreateStoreRequest("Test Company", "UEN123", "Test Store", "Test Location", "+12345678", DateTime.UtcNow, 1.0m, 1.0m, "SG", "Test Address", true);
        var mockCurrentUserService = GetMockCurrentUserService(storeId);
        var service = new StoreService(context, mockCurrentUserService.Object);

        // Act
        var result = await service.InitializeStoreAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Store", result.StoreName);
    }

    [Fact]
    public async Task InitializeStoreAsync_ShouldThrowException_WhenStoreAlreadyInitialized()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        context.Store.Add(new Store { Id = storeId, StoreName = "Existing Store" });
        await context.SaveChangesAsync();
        
        var request = new CreateStoreRequest("Test Company", "UEN123", "Test Store", "Test Location", "+12345678", DateTime.UtcNow, 1.0m, 1.0m, "SG", "Test Address", true);
        var mockCurrentUserService = GetMockCurrentUserService(storeId);
        var service = new StoreService(context, mockCurrentUserService.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.InitializeStoreAsync(request));
    }

    [Fact]
    public async Task InitializeStoreAsync_ShouldCreateNewStore_WhenStoreDoesNotExist()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        // Do NOT add store to context
        
        var request = new CreateStoreRequest("Test Company", "UEN123", "Test Store", "Test Location", "+12345678", DateTime.UtcNow, 1.0m, 1.0m, "SG", "Test Address", true);
        var mockCurrentUserService = GetMockCurrentUserService(storeId);
        var service = new StoreService(context, mockCurrentUserService.Object);

        // Act
        var result = await service.InitializeStoreAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Store", result.StoreName);
        
        // Verify it was added to DB (although ID might be auto-generated differently depending on DB provider, 
        // but here we just check if a store exists)
        var count = await context.Store.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task UpdateStoreAsync_ShouldUpdateStore()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var store = new Store { Id = storeId, StoreName = "Old Name" };
        context.Store.Add(store);
        await context.SaveChangesAsync();
        
        var request = new UpdateStoreRequest(null, null, "New Name", null, null, null, null, null, null, null, null);
        var mockCurrentUserService = GetMockCurrentUserService(storeId);
        var service = new StoreService(context, mockCurrentUserService.Object);

        // Act
        var result = await service.UpdateStoreAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Name", result.StoreName);
    }

    [Fact]
    public async Task UpdateStoreAsync_ShouldReturnNull_WhenStoreDoesNotExist()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var mockCurrentUserService = GetMockCurrentUserService(storeId);
        var service = new StoreService(context, mockCurrentUserService.Object);
        var request = new UpdateStoreRequest(null, null, "New Name", null, null, null, null, null, null, null, null);

        // Act
        var result = await service.UpdateStoreAsync(request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task IsStoreInitializedAsync_ShouldReturnTrue_WhenStoreExists()
    {
        // Arrange
        var context = GetDbContext();
        context.Store.Add(new Store { Id = 1 });
        await context.SaveChangesAsync();
        var service = new StoreService(context, Mock.Of<ICurrentUserService>());

        // Act
        var result = await service.IsStoreInitializedAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsStoreSetupCompleteAsync_ShouldReturnTrue_WhenStoreNameIsNotEmpty()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        context.Store.Add(new Store { Id = storeId, StoreName = "Test Store" });
        await context.SaveChangesAsync();
        var service = new StoreService(context, Mock.Of<ICurrentUserService>());

        // Act
        var result = await service.IsStoreSetupCompleteAsync(storeId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsStoreSetupCompleteAsync_ShouldReturnFalse_WhenStoreDoesNotExist()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var service = new StoreService(context, Mock.Of<ICurrentUserService>());

        // Act
        var result = await service.IsStoreSetupCompleteAsync(storeId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetStoreByIdAsync_ShouldReturnStore_WhenIdMatchesCurrentStore()
    {
        // Arrange
        var context = GetDbContext();
        var storeId = 1;
        var store = new Store { Id = storeId, StoreName = "My Store" };
        context.Store.Add(store);
        await context.SaveChangesAsync();

        var mockCurrentUserService = GetMockCurrentUserService(storeId);
        var service = new StoreService(context, mockCurrentUserService.Object);

        // Act
        var result = await service.GetStoreByIdAsync(storeId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("My Store", result.StoreName);
    }

    [Fact]
    public async Task GetStoreByIdAsync_ShouldReturnNull_WhenIdDoesNotMatchCurrentStore()
    {
        // Arrange
        var context = GetDbContext();
        var myStoreId = 1;
        var otherStoreId = 2;
        context.Store.Add(new Store { Id = myStoreId, StoreName = "My Store" });
        context.Store.Add(new Store { Id = otherStoreId, StoreName = "Other Store" });
        await context.SaveChangesAsync();

        var mockCurrentUserService = GetMockCurrentUserService(myStoreId);
        var service = new StoreService(context, mockCurrentUserService.Object);

        // Act
        var result = await service.GetStoreByIdAsync(otherStoreId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateStoreByIdAsync_ShouldReturnNull_WhenIdDoesNotMatchCurrentStore()
    {
        // Arrange
        var context = GetDbContext();
        var myStoreId = 1;
        var otherStoreId = 2;
        context.Store.Add(new Store { Id = myStoreId, StoreName = "My Store" });
        context.Store.Add(new Store { Id = otherStoreId, StoreName = "Other Store" });
        await context.SaveChangesAsync();

        var mockCurrentUserService = GetMockCurrentUserService(myStoreId);
        var service = new StoreService(context, mockCurrentUserService.Object);
        var request = new UpdateStoreRequest(null, null, "Hacked Name", null, null, null, null, null, null, null, null);

        // Act
        var result = await service.UpdateStoreByIdAsync(otherStoreId, request);

        // Assert
        Assert.Null(result);
        var otherStore = await context.Store.FindAsync(otherStoreId);
                Assert.NotNull(otherStore);
        Assert.Equal("Other Store", otherStore.StoreName); // Should not be updated
    }

    [Fact]
    public async Task UpdateStoreByIdAsync_ShouldReturnNull_WhenStoreDoesNotExist()
    {
        // Arrange
        var context = GetDbContext();
        var myStoreId = 1;
        var mockCurrentUserService = GetMockCurrentUserService(myStoreId);
        var service = new StoreService(context, mockCurrentUserService.Object);
        var request = new UpdateStoreRequest(null, null, "New Name", null, null, null, null, null, null, null, null);

        // Act
        var result = await service.UpdateStoreByIdAsync(myStoreId, request);

        // Assert
        Assert.Null(result);
    }
}

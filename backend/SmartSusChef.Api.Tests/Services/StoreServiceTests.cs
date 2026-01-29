using Xunit;
using Microsoft.EntityFrameworkCore;
using SmartSusChef.Api.Data;
using SmartSusChef.Api.Services;
using SmartSusChef.Api.Models;
using Moq;

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
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.StoreId).Returns(1);

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
}

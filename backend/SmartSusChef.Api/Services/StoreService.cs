using Microsoft.EntityFrameworkCore;
using SmartSusChef.Api.Data;
using SmartSusChef.Api.DTOs;
using SmartSusChef.Api.Models;

namespace SmartSusChef.Api.Services;

public class StoreService : IStoreService
{
    private readonly ApplicationDbContext _context;

    public StoreService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<StoreDto?> GetStoreAsync()
    {
        var store = await _context.Store.FirstOrDefaultAsync();
        return store == null ? null : MapToDto(store);
    }

    public async Task<StoreDto> InitializeStoreAsync(CreateStoreRequest request)
    {
        // Check if store already exists
        var existingStore = await _context.Store.FirstOrDefaultAsync();
        if (existingStore != null)
        {
            throw new InvalidOperationException("Store has already been initialized. Use update endpoint to modify store information.");
        }

        var store = new Store
        {
            Id = 1, // Fixed ID for singleton
            CompanyName = request.CompanyName,
            UEN = request.UEN,
            StoreName = request.StoreName,
            OutletLocation = request.OutletLocation,
            ContactNumber = request.ContactNumber,
            OpeningDate = request.OpeningDate,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Address = request.Address,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Store.Add(store);
        await _context.SaveChangesAsync();

        return MapToDto(store);
    }

    public async Task<StoreDto?> UpdateStoreAsync(UpdateStoreRequest request)
    {
        var store = await _context.Store.FirstOrDefaultAsync();
        if (store == null)
        {
            return null;
        }

        // Update only provided fields
        if (request.CompanyName != null)
            store.CompanyName = request.CompanyName;
        if (request.UEN != null)
            store.UEN = request.UEN;
        if (request.StoreName != null)
            store.StoreName = request.StoreName;
        if (request.OutletLocation != null)
            store.OutletLocation = request.OutletLocation;
        if (request.ContactNumber != null)
            store.ContactNumber = request.ContactNumber;
        if (request.OpeningDate.HasValue)
            store.OpeningDate = request.OpeningDate.Value;
        if (request.Latitude.HasValue)
            store.Latitude = request.Latitude.Value;
        if (request.Longitude.HasValue)
            store.Longitude = request.Longitude.Value;
        if (request.Address != null)
            store.Address = request.Address;
        if (request.IsActive.HasValue)
            store.IsActive = request.IsActive.Value;

        store.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(store);
    }

    public async Task<bool> IsStoreInitializedAsync()
    {
        return await _context.Store.AnyAsync();
    }

    private static StoreDto MapToDto(Store store)
    {
        return new StoreDto(
            store.Id,
            store.CompanyName,
            store.UEN,
            store.StoreName,
            store.OutletLocation,
            store.ContactNumber,
            store.OpeningDate,
            store.Latitude,
            store.Longitude,
            store.Address,
            store.IsActive,
            store.CreatedAt,
            store.UpdatedAt
        );
    }
}

using Microsoft.EntityFrameworkCore;
using SmartSusChef.Api.Data;
using SmartSusChef.Api.DTOs;
using SmartSusChef.Api.Models;

namespace SmartSusChef.Api.Services;

public class StoreService : IStoreService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHolidayService _holidayService;

    private int CurrentStoreId => _currentUserService.StoreId;

    public StoreService(ApplicationDbContext context, ICurrentUserService currentUserService, IHolidayService holidayService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _holidayService = holidayService;
    }

    public async Task<StoreDto?> GetStoreAsync()
    {
        var store = await _context.Store.FindAsync(CurrentStoreId);
        return store == null ? null : MapToDto(store);
    }

    public async Task<StoreDto?> GetStoreByIdAsync(int storeId)
    {
        // Only allow access to own store
        if (storeId != CurrentStoreId)
            return null;

        var store = await _context.Store.FindAsync(storeId);
        return store == null ? null : MapToDto(store);
    }

    public async Task<StoreDto> InitializeStoreAsync(CreateStoreRequest request)
    {
        // Check if current user's store already has data
        var existingStore = await _context.Store.FindAsync(CurrentStoreId);
        if (existingStore != null && !string.IsNullOrEmpty(existingStore.StoreName))
        {
            throw new InvalidOperationException("Store has already been initialized. Use update endpoint to modify store information.");
        }

        if (existingStore != null)
        {
            // Update existing empty store
            existingStore.CompanyName = request.CompanyName;
            existingStore.UEN = request.UEN;
            existingStore.StoreName = request.StoreName;
            existingStore.OutletLocation = request.OutletLocation;
            existingStore.ContactNumber = request.ContactNumber;
            existingStore.OpeningDate = request.OpeningDate;
            existingStore.Latitude = request.Latitude;
            existingStore.Longitude = request.Longitude;
            existingStore.CountryCode = request.CountryCode;
            existingStore.Address = request.Address;
            existingStore.IsActive = request.IsActive;
            existingStore.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapToDto(existingStore);
        }

        // This shouldn't happen normally since store is created during registration
        var store = new Store
        {
            CompanyName = request.CompanyName,
            UEN = request.UEN,
            StoreName = request.StoreName,
            OutletLocation = request.OutletLocation,
            ContactNumber = request.ContactNumber,
            OpeningDate = request.OpeningDate,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            CountryCode = request.CountryCode,
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
        var store = await _context.Store.FindAsync(CurrentStoreId);
        if (store == null)
        {
            return null;
        }

        return await UpdateStoreInternal(store, request);
    }

    public async Task<StoreDto?> UpdateStoreByIdAsync(int storeId, UpdateStoreRequest request)
    {
        // Only allow updating own store
        if (storeId != CurrentStoreId)
            return null;

        var store = await _context.Store.FindAsync(storeId);
        if (store == null)
        {
            return null;
        }

        return await UpdateStoreInternal(store, request);
    }

    // Added method to satisfy test requirement
    public async Task<bool> UpdateStoreSettingsAsync(int storeId, string storeName)
    {
        var request = new UpdateStoreRequest(null, null, storeName, null, null, null, null, null, null, null);
        var result = await UpdateStoreByIdAsync(storeId, request);
        return result != null;
    }

    private async Task<StoreDto> UpdateStoreInternal(Store store, UpdateStoreRequest request)
    {
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

        // Auto-fetch country code from coordinates if latitude/longitude changed and no country code provided
        if ((request.Latitude.HasValue || request.Longitude.HasValue) && string.IsNullOrWhiteSpace(request.CountryCode))
        {
            if (store.Latitude != 0 && store.Longitude != 0)
            {
                var countryCode = await _holidayService.GetCountryCodeFromCoordinatesAsync(store.Latitude, store.Longitude);
                if (!string.IsNullOrWhiteSpace(countryCode))
                {
                    store.CountryCode = countryCode;
                }
            }
        }
        else if (request.CountryCode != null)
        {
            store.CountryCode = request.CountryCode;
        }

        store.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(store);
    }

    public async Task<bool> IsStoreInitializedAsync()
    {
        return await _context.Store.AnyAsync();
    }

    public async Task<bool> IsStoreSetupCompleteAsync(int storeId)
    {
        var store = await _context.Store.FindAsync(storeId);
        if (store == null) return false;

        // Store setup is complete if StoreName is not empty
        return !string.IsNullOrEmpty(store.StoreName);
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
            store.CountryCode,
            store.Address,
            store.IsActive,
            store.CreatedAt,
            store.UpdatedAt
        );
    }
}

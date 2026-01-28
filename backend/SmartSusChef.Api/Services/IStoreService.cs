using SmartSusChef.Api.DTOs;

namespace SmartSusChef.Api.Services;

public interface IStoreService
{
    Task<StoreDto?> GetStoreAsync();
    Task<StoreDto?> GetStoreByIdAsync(int storeId);
    Task<StoreDto> InitializeStoreAsync(CreateStoreRequest request);
    Task<StoreDto?> UpdateStoreAsync(UpdateStoreRequest request);
    Task<StoreDto?> UpdateStoreByIdAsync(int storeId, UpdateStoreRequest request);
    Task<bool> IsStoreInitializedAsync();
    Task<bool> IsStoreSetupCompleteAsync(int storeId);
}

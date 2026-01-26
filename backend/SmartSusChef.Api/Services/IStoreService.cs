using SmartSusChef.Api.DTOs;

namespace SmartSusChef.Api.Services;

public interface IStoreService
{
    Task<StoreDto?> GetStoreAsync();
    Task<StoreDto> InitializeStoreAsync(CreateStoreRequest request);
    Task<StoreDto?> UpdateStoreAsync(UpdateStoreRequest request);
    Task<bool> IsStoreInitializedAsync();
}

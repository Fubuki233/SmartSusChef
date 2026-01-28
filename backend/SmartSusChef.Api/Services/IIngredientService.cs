using SmartSusChef.Api.DTOs;

namespace SmartSusChef.Api.Services;

public interface IIngredientService
{
    Task<List<IngredientDto>> GetAllAsync();
    Task<IngredientDto?> GetByIdAsync(Guid id);
    Task<IngredientDto> CreateAsync(CreateIngredientRequest request);
    Task<IngredientDto?> UpdateAsync(Guid id, UpdateIngredientRequest request);
    Task<bool> DeleteAsync(Guid id);
    
    Task<decimal> GetTotalCarbonImpactAsync();
    
    /// <summary>
    /// Calculates the total carbon impact for a specific list of ingredient quantities.
    /// </summary>
    Task<decimal> GetTotalCarbonImpactAsync(List<(Guid Id, decimal Quantity)> items);
    
    /// Bulk update or import for ingredients, ensuring StoreId is assigned.
    Task ImportIngredientsAsync(List<CreateIngredientRequest> requests);
}

using SmartSusChef.Api.DTOs;

namespace SmartSusChef.Api.Services;

public interface IIngredientService
{
    Task<List<IngredientDto>> GetAllAsync();
    Task<IngredientDto?> GetByIdAsync(Guid id);
    Task<IngredientDto> CreateAsync(CreateIngredientRequest request);
    Task<IngredientDto?> UpdateAsync(Guid id, UpdateIngredientRequest request);
    Task<bool> DeleteAsync(Guid id);
}

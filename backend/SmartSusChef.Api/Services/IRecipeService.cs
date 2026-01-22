using SmartSusChef.Api.DTOs;

namespace SmartSusChef.Api.Services;

public interface IRecipeService
{
    Task<List<RecipeDto>> GetAllAsync();
    Task<RecipeDto?> GetByIdAsync(Guid id);
    Task<RecipeDto> CreateAsync(CreateRecipeRequest request);
    Task<RecipeDto?> UpdateAsync(Guid id, UpdateRecipeRequest request);
    Task<bool> DeleteAsync(Guid id);
}

using Microsoft.EntityFrameworkCore;
using SmartSusChef.Api.Data;
using SmartSusChef.Api.DTOs;
using SmartSusChef.Api.Models;

namespace SmartSusChef.Api.Services;

public class RecipeService : IRecipeService
{
    private readonly ApplicationDbContext _context;
    // Scoped to current store (simulating authenticated context)
    private readonly int _currentStoreId = 1;

    public RecipeService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<RecipeDto>> GetAllAsync()
    {
        var recipes = await _context.Recipes
            .Include(r => r.RecipeIngredients).ThenInclude(ri => ri.Ingredient)
            .Include(r => r.RecipeIngredients).ThenInclude(ri => ri.ChildRecipe)
            .Where(r => r.StoreId == _currentStoreId)
            .OrderBy(r => r.Name)
            .ToListAsync();
        
        return recipes.Select(MapToDto).ToList();
    }

    public async Task<RecipeDto?> GetByIdAsync(Guid id)
    {
        var recipe = await _context.Recipes
            .Include(r => r.RecipeIngredients).ThenInclude(ri => ri.Ingredient)
            .Include(r => r.RecipeIngredients).ThenInclude(ri => ri.ChildRecipe)
            .FirstOrDefaultAsync(r => r.Id == id && r.StoreId == _currentStoreId);

        return recipe == null ? null : MapToDto(recipe);
    }

    public async Task<RecipeDto> CreateAsync(CreateRecipeRequest request)
    {
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            StoreId = _currentStoreId,
            Name = request.Name,
            IsSellable = request.IsSellable,
            IsSubRecipe = request.IsSubRecipe,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        foreach (var riReq in request.Ingredients)
        {
            recipe.RecipeIngredients.Add(new RecipeIngredient
            {
                Id = Guid.NewGuid(),
                RecipeId = recipe.Id,
                // Fixed variable names from riRequest to riReq
                IngredientId = string.IsNullOrEmpty(riReq.IngredientId) ? null : Guid.Parse(riReq.IngredientId),
                ChildRecipeId = string.IsNullOrEmpty(riReq.ChildRecipeId) ? null : Guid.Parse(riReq.ChildRecipeId),
                Quantity = riReq.Quantity
            });
        }

        _context.Recipes.Add(recipe);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(recipe.Id) ?? throw new Exception("Failed to retrieve created recipe");
    }

    public async Task<RecipeDto?> UpdateAsync(Guid id, UpdateRecipeRequest request)
    {
        var recipe = await _context.Recipes
            .Include(r => r.RecipeIngredients)
            .FirstOrDefaultAsync(r => r.Id == id && r.StoreId == _currentStoreId);

        if (recipe == null) return null;

        recipe.Name = request.Name;
        recipe.IsSellable = request.IsSellable;
        recipe.IsSubRecipe = request.IsSubRecipe;
        recipe.UpdatedAt = DateTime.UtcNow;

        // Clear existing to prevent duplicates
        _context.RecipeIngredients.RemoveRange(recipe.RecipeIngredients);

        foreach (var riReq in request.Ingredients)
        {
            recipe.RecipeIngredients.Add(new RecipeIngredient
            {
                Id = Guid.NewGuid(),
                RecipeId = recipe.Id,
                IngredientId = string.IsNullOrEmpty(riReq.IngredientId) ? null : Guid.Parse(riReq.IngredientId),
                ChildRecipeId = string.IsNullOrEmpty(riReq.ChildRecipeId) ? null : Guid.Parse(riReq.ChildRecipeId),
                Quantity = riReq.Quantity
            });
        }

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<decimal> CalculateTotalCarbonFootprintAsync(Guid recipeId)
    {
        var recipe = await _context.Recipes
            .Include(r => r.RecipeIngredients).ThenInclude(ri => ri.Ingredient)
            .FirstOrDefaultAsync(r => r.Id == recipeId && r.StoreId == _currentStoreId);

        if (recipe == null) return 0;

        decimal totalFootprint = 0;

        foreach (var ri in recipe.RecipeIngredients)
        {
            if (ri.Ingredient != null)
            {
                totalFootprint += ri.Ingredient.CarbonFootprint * ri.Quantity;
            }
            else if (ri.ChildRecipeId.HasValue)
            {
                // Recursive call to handle sub-recipes
                totalFootprint += (await CalculateTotalCarbonFootprintAsync(ri.ChildRecipeId.Value)) * ri.Quantity;
            }
        }

        return totalFootprint;
    }

    public Task<List<IngredientUsageDto>> GetFlattenedIngredientsAsync(Guid recipeId)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var recipe = await _context.Recipes
            .FirstOrDefaultAsync(r => r.Id == id && r.StoreId == _currentStoreId);
            
        if (recipe == null) return false;

        _context.Recipes.Remove(recipe);
        await _context.SaveChangesAsync();
        return true;
    }

    private static RecipeDto MapToDto(Recipe recipe)
    {
        return new RecipeDto(
            recipe.Id.ToString(),
            recipe.Name,
            recipe.IsSellable,
            recipe.IsSubRecipe,
            recipe.RecipeIngredients.Select(ri => new RecipeIngredientDto(
                ri.IngredientId?.ToString(),
                ri.ChildRecipeId?.ToString(),
                ri.Ingredient?.Name ?? ri.ChildRecipe?.Name ?? "Unknown",
                ri.Ingredient?.Unit ?? "unit",
                ri.Quantity
            )).ToList()
        );
    }
}
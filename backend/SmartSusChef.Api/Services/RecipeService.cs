using Microsoft.EntityFrameworkCore;
using SmartSusChef.Api.Data;
using SmartSusChef.Api.DTOs;
using SmartSusChef.Api.Models;

namespace SmartSusChef.Api.Services;

public class RecipeService : IRecipeService
{
    private readonly ApplicationDbContext _context;

    public RecipeService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<RecipeDto>> GetAllAsync()
    {
        var recipes = await _context.Recipes
            .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
            .OrderBy(r => r.Name)
            .ToListAsync();

        return recipes.Select(MapToDto).ToList();
    }

    public async Task<RecipeDto?> GetByIdAsync(Guid id)
    {
        var recipe = await _context.Recipes
            .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
            .FirstOrDefaultAsync(r => r.Id == id);

        return recipe == null ? null : MapToDto(recipe);
    }

    public async Task<RecipeDto> CreateAsync(CreateRecipeRequest request)
    {
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        foreach (var ingredientRequest in request.Ingredients)
        {
            recipe.RecipeIngredients.Add(new RecipeIngredient
            {
                Id = Guid.NewGuid(),
                RecipeId = recipe.Id,
                IngredientId = Guid.Parse(ingredientRequest.IngredientId),
                Quantity = ingredientRequest.Quantity
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
            .FirstOrDefaultAsync(r => r.Id == id);

        if (recipe == null) return null;

        recipe.Name = request.Name;
        recipe.UpdatedAt = DateTime.UtcNow;

        // Remove existing ingredients
        _context.RecipeIngredients.RemoveRange(recipe.RecipeIngredients);

        // Add new ingredients
        foreach (var ingredientRequest in request.Ingredients)
        {
            recipe.RecipeIngredients.Add(new RecipeIngredient
            {
                Id = Guid.NewGuid(),
                RecipeId = recipe.Id,
                IngredientId = Guid.Parse(ingredientRequest.IngredientId),
                Quantity = ingredientRequest.Quantity
            });
        }

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var recipe = await _context.Recipes.FindAsync(id);
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
            recipe.RecipeIngredients.Select(ri => new RecipeIngredientDto(
                ri.IngredientId.ToString(),
                ri.Ingredient.Name,
                ri.Ingredient.Unit,
                ri.Quantity
            )).ToList()
        );
    }
}

using Microsoft.EntityFrameworkCore;
using SmartSusChef.Api.Data;
using SmartSusChef.Api.DTOs;
using SmartSusChef.Api.Models;

namespace SmartSusChef.Api.Services;

public class IngredientService : IIngredientService
{
    private readonly ApplicationDbContext _context;

    public IngredientService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<IngredientDto>> GetAllAsync()
    {
        var ingredients = await _context.Ingredients
            .OrderBy(i => i.Name)
            .ToListAsync();

        return ingredients.Select(MapToDto).ToList();
    }

    public async Task<IngredientDto?> GetByIdAsync(Guid id)
    {
        var ingredient = await _context.Ingredients.FindAsync(id);
        return ingredient == null ? null : MapToDto(ingredient);
    }

    public async Task<IngredientDto> CreateAsync(CreateIngredientRequest request)
    {
        var ingredient = new Ingredient
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Unit = request.Unit,
            CarbonFootprint = request.CarbonFootprint,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Ingredients.Add(ingredient);
        await _context.SaveChangesAsync();

        return MapToDto(ingredient);
    }

    public async Task<IngredientDto?> UpdateAsync(Guid id, UpdateIngredientRequest request)
    {
        var ingredient = await _context.Ingredients.FindAsync(id);
        if (ingredient == null) return null;

        ingredient.Name = request.Name;
        ingredient.Unit = request.Unit;
        ingredient.CarbonFootprint = request.CarbonFootprint;
        ingredient.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(ingredient);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var ingredient = await _context.Ingredients.FindAsync(id);
        if (ingredient == null) return false;

        _context.Ingredients.Remove(ingredient);
        await _context.SaveChangesAsync();

        return true;
    }

    private static IngredientDto MapToDto(Ingredient ingredient)
    {
        return new IngredientDto(
            ingredient.Id.ToString(),
            ingredient.Name,
            ingredient.Unit,
            ingredient.CarbonFootprint
        );
    }
}

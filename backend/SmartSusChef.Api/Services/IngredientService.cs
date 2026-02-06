using Microsoft.EntityFrameworkCore;
using SmartSusChef.Api.Data;
using SmartSusChef.Api.DTOs;
using SmartSusChef.Api.Models;

namespace SmartSusChef.Api.Services;

public partial class IngredientService : IIngredientService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    private int CurrentStoreId => _currentUserService.StoreId;

    public IngredientService(ApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<List<IngredientDto>> GetAllAsync()
    {
        // Filter by current user's StoreId for data isolation
        var ingredients = await _context.Ingredients
            .Where(i => i.StoreId == CurrentStoreId)
            .OrderBy(i => i.Name)
            .ToListAsync();

        return ingredients.Select(MapToDto).ToList();
    }


    public async Task<IngredientDto?> GetByIdAsync(Guid id)
    {
        // Use FirstOrDefaultAsync with StoreId filter instead of FindAsync
        var ingredient = await _context.Ingredients
            .FirstOrDefaultAsync(i => i.Id == id && i.StoreId == CurrentStoreId);

        return ingredient == null ? null : MapToDto(ingredient);
    }

    public async Task<IngredientDto> CreateAsync(CreateIngredientRequest request)
    {
        ValidateUnit(request.Unit);
        await ValidateNameIsUnique(request.Name);

        var ingredient = new Ingredient
        {
            Id = Guid.NewGuid(),
            StoreId = CurrentStoreId,
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
        var ingredient = await _context.Ingredients
            .FirstOrDefaultAsync(i => i.Id == id && i.StoreId == CurrentStoreId);

        if (ingredient == null) return null;

        ValidateUnit(request.Unit);

        if (request.Name.ToLower() != ingredient.Name.ToLower())
        {
            await ValidateNameIsUnique(request.Name, ingredient.Id);
        }

        ingredient.Name = request.Name;
        ingredient.Unit = request.Unit;
        ingredient.CarbonFootprint = request.CarbonFootprint;
        ingredient.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(ingredient);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        // Ensure delete only targets the current store's ingredients to prevent cross-store deletion
        var ingredient = await _context.Ingredients
            .FirstOrDefaultAsync(i => i.Id == id && i.StoreId == CurrentStoreId);

        if (ingredient == null) return false;

        var isUsedInRecipe = await _context.RecipeIngredients
            .AnyAsync(ri => ri.IngredientId == id);

        if (isUsedInRecipe)
        {
            throw new InvalidOperationException("This ingredient cannot be deleted because it is currently used in one or more recipes.");
        }

        _context.Ingredients.Remove(ingredient);
        await _context.SaveChangesAsync();

        return true;
    }

    private static void ValidateUnit(string unit)
    {
        var allowedUnits = new[] { "g", "ml", "kg", "L" };
        if (!allowedUnits.Contains(unit))
            throw new ArgumentException("Invalid unit. Must be g, ml, kg, or L.");
    }
    
    private async Task ValidateNameIsUnique(string name, Guid? existingId = null)
    {
        var query = _context.Ingredients
            .Where(i => i.StoreId == CurrentStoreId && i.Name.ToLower() == name.ToLower());

        if (existingId.HasValue)
        {
            query = query.Where(i => i.Id != existingId.Value);
        }

        var duplicate = await query.AnyAsync();
            
        if (duplicate)
        {
            throw new InvalidOperationException($"Ingredient '{name}' already exists.");
        }
    }

    private static IngredientDto MapToDto(Ingredient ingredient)
    {
        return new IngredientDto(
            ingredient.Id.ToString(),
            ingredient.Name,
            ingredient.Unit,
            ingredient.CarbonFootprint,
            ingredient.CreatedAt,
            ingredient.UpdatedAt
        );
    }
}

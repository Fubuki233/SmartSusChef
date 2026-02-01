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
        // Validation: Ensure Unit matches allowed values
        var allowedUnits = new[] { "g", "ml", "kg", "L" };
        if (!allowedUnits.Contains(request.Unit))
            throw new ArgumentException("Invalid unit. Must be g, ml, kg, or L.");

        // Refactor the duplicate check for high-reliability:
        // Use .ToLower() to prevent "Tomato" vs "tomato" duplicates
        var duplicate = await _context.Ingredients
            .AnyAsync(i => i.StoreId == CurrentStoreId && i.Name.ToLower() == request.Name.ToLower());
            
        if (duplicate)
        {
            throw new InvalidOperationException($"Ingredient '{request.Name}' already exists.");
        }

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

        // Optional: Re-validate unit during update
        var allowedUnits = new[] { "g", "ml", "kg", "L" };
        if (!allowedUnits.Contains(request.Unit))
            throw new ArgumentException("Invalid unit. Must be g, ml, kg, or L.");

        // Check for duplicates if name is changing
        if (request.Name.ToLower() != ingredient.Name.ToLower())
        {
            var duplicate = await _context.Ingredients
                .AnyAsync(i => i.StoreId == CurrentStoreId && i.Name.ToLower() == request.Name.ToLower());
                
            if (duplicate)
            {
                throw new InvalidOperationException($"Ingredient '{request.Name}' already exists.");
            }
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

        _context.Ingredients.Remove(ingredient);
        await _context.SaveChangesAsync();

        return true;
    }
    // Strategic Insight: Total environmental impact of current stock/usage
    public async Task<decimal> GetTotalCarbonImpactAsync()
    {
        return await _context.Ingredients
            .Where(i => i.StoreId == CurrentStoreId)
            .SumAsync(i => i.CarbonFootprint);
    }

    // Calculates total carbon impact for specific ingredients with quantities
    public async Task<decimal> GetTotalCarbonImpactAsync(List<(Guid Id, decimal Quantity)> items)
    {
        if (items == null || items.Count == 0)
            return 0;

        var ingredientIds = items.Select(x => x.Id).ToList();
        var ingredients = await _context.Ingredients
            .Where(i => i.StoreId == CurrentStoreId && ingredientIds.Contains(i.Id))
            .ToListAsync();

        decimal totalImpact = 0;
        foreach (var item in items)
        {
            var ingredient = ingredients.FirstOrDefault(i => i.Id == item.Id);
            if (ingredient != null)
            {
                // Carbon impact = carbon footprint per unit * quantity
                totalImpact += ingredient.CarbonFootprint * item.Quantity;
            }
        }

        return totalImpact;
    }

    // Bulk Import: Ensuring StoreId is enforced for all new records
    public async Task ImportIngredientsAsync(List<CreateIngredientRequest> requests)
    {
        var allowedUnits = new[] { "g", "ml", "kg", "L" };

        var newIngredients = requests.Select(req =>
        {
            if (!allowedUnits.Contains(req.Unit))
                throw new ArgumentException($"Invalid unit '{req.Unit}' for ingredient {req.Name}");

            return new Ingredient
            {
                Id = Guid.NewGuid(),
                StoreId = CurrentStoreId,
                Name = req.Name,
                Unit = req.Unit,
                CarbonFootprint = req.CarbonFootprint,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }).ToList();

        _context.Ingredients.AddRange(newIngredients);
        await _context.SaveChangesAsync();
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

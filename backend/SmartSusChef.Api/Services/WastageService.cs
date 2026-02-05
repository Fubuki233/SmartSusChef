using Microsoft.EntityFrameworkCore;
using SmartSusChef.Api.Data;
using SmartSusChef.Api.DTOs;
using SmartSusChef.Api.Models;

namespace SmartSusChef.Api.Services;

public class WastageService : IWastageService
{
    private readonly ApplicationDbContext _context;
    private readonly IRecipeService _recipeService;
    private readonly ICurrentUserService _currentUserService;

    private int CurrentStoreId => _currentUserService.StoreId;

    public WastageService(ApplicationDbContext context, IRecipeService recipeService, ICurrentUserService currentUserService)
    {
        _context = context;
        _recipeService = recipeService;
        _currentUserService = currentUserService;
    }

    public async Task<List<WastageDataDto>> GetAllAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.WastageData
            .AsNoTracking()
            .Include(w => w.Ingredient)
            .Include(w => w.Recipe)
            .Where(w => w.StoreId == CurrentStoreId)
            .AsQueryable();

        if (startDate.HasValue)
            query = query.Where(w => w.Date >= startDate.Value.Date);

        if (endDate.HasValue)
            query = query.Where(w => w.Date <= endDate.Value.Date);

        var wastageData = await query
            .OrderByDescending(w => w.Date)
            .ToListAsync();

        var dtos = new List<WastageDataDto>();
        foreach (var w in wastageData)
        {
            dtos.Add(await MapToDtoAsync(w));
        }
        return dtos;
    }

    public async Task<WastageDataDto?> GetByIdAsync(Guid id)
    {
        var wastageData = await _context.WastageData
            .AsNoTracking()
            .Include(w => w.Ingredient)
            .Include(w => w.Recipe)
            .FirstOrDefaultAsync(w => w.Id == id && w.StoreId == CurrentStoreId);

        return wastageData == null ? null : await MapToDtoAsync(wastageData);
    }

    public async Task<WastageDataDto> CreateAsync(CreateWastageDataRequest request)
    {
        if (request.Quantity < 0)
        {
            throw new ArgumentException("Quantity cannot be negative.");
        }

        if (!string.IsNullOrEmpty(request.IngredientId))
        {
            var ingredientExists = await _context.Ingredients
                .AnyAsync(i => i.Id == Guid.Parse(request.IngredientId) && i.StoreId == CurrentStoreId);
            if (!ingredientExists)
            {
                throw new DbUpdateException("Ingredient does not exist.");
            }
        }

        if (!string.IsNullOrEmpty(request.RecipeId))
        {
            var recipeExists = await _context.Recipes
                .AnyAsync(r => r.Id == Guid.Parse(request.RecipeId) && r.StoreId == CurrentStoreId);
            if (!recipeExists)
            {
                throw new DbUpdateException("Recipe does not exist.");
            }
        }

        var wastageData = new WastageData
        {
            Id = Guid.NewGuid(),
            StoreId = CurrentStoreId,
            Date = DateTime.Parse(request.Date).Date,
            IngredientId = string.IsNullOrEmpty(request.IngredientId) ? null : Guid.Parse(request.IngredientId),
            RecipeId = string.IsNullOrEmpty(request.RecipeId) ? null : Guid.Parse(request.RecipeId),
            Quantity = request.Quantity,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.WastageData.Add(wastageData);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(wastageData.Id) ?? throw new Exception("Failed to retrieve created wastage data");
    }

    public async Task<WastageDataDto?> UpdateAsync(Guid id, UpdateWastageDataRequest request)
    {
        var wastageData = await _context.WastageData
            .Include(w => w.Ingredient)
            .Include(w => w.Recipe)
            .FirstOrDefaultAsync(w => w.Id == id && w.StoreId == CurrentStoreId);

        if (wastageData == null) return null;

        if (request.Quantity < 0)
        {
            throw new ArgumentException("Quantity cannot be negative.");
        }

        if (!string.IsNullOrEmpty(request.IngredientId))
        {
            var ingredientExists = await _context.Ingredients
                .AnyAsync(i => i.Id == Guid.Parse(request.IngredientId) && i.StoreId == CurrentStoreId);
            if (!ingredientExists)
            {
                throw new DbUpdateException("Ingredient does not exist.");
            }
        }

        if (!string.IsNullOrEmpty(request.RecipeId))
        {
            var recipeExists = await _context.Recipes
                .AnyAsync(r => r.Id == Guid.Parse(request.RecipeId) && r.StoreId == CurrentStoreId);
            if (!recipeExists)
            {
                throw new DbUpdateException("Recipe does not exist.");
            }
        }

        wastageData.Date = DateTime.Parse(request.Date).Date;
        wastageData.IngredientId = string.IsNullOrEmpty(request.IngredientId) ? null : Guid.Parse(request.IngredientId);
        wastageData.RecipeId = string.IsNullOrEmpty(request.RecipeId) ? null : Guid.Parse(request.RecipeId);
        wastageData.Quantity = request.Quantity;
        wastageData.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await MapToDtoAsync(wastageData);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var wastageData = await _context.WastageData
            .FirstOrDefaultAsync(w => w.Id == id && w.StoreId == CurrentStoreId);

        if (wastageData == null) return false;

        _context.WastageData.Remove(wastageData);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<WastageTrendDto>> GetTrendAsync(DateTime startDate, DateTime endDate)
    {
        var wastageData = await _context.WastageData
            .AsNoTracking()
            .Include(w => w.Ingredient)
            .Include(w => w.Recipe)
            .Where(w => w.StoreId == CurrentStoreId && w.Date >= startDate.Date && w.Date <= endDate.Date)
            .ToListAsync();

        // We need to calculate impact for each item first because it's async
        var wastageWithImpact = new List<(WastageData Data, decimal Impact)>();
        foreach (var w in wastageData)
        {
            wastageWithImpact.Add((w, await GetTotalImpactAsync(w)));
        }

        var grouped = wastageWithImpact
            .GroupBy(x => x.Data.Date.Date)
            .OrderBy(g => g.Key)
            .Select(g => new WastageTrendDto(
                g.Key.ToString("yyyy-MM-dd"),
                g.Sum(x => x.Data.Quantity),
                g.Sum(x => x.Impact),
                g.GroupBy(x => new { x.Data.IngredientId, x.Data.RecipeId })
                    .Select(ig =>
                    {
                        var first = ig.First().Data;
                        return new ItemWastageDto(
                            first.IngredientId?.ToString(),
                            first.RecipeId?.ToString(),
                            first.Ingredient?.Name ?? first.Recipe?.Name ?? "Unknown",
                            first.Ingredient?.Unit ?? "unit",
                            ig.Sum(x => x.Data.Quantity),
                            ig.Sum(x => x.Impact)
                        );
                    })
                    .ToList()
            ))
            .ToList();

        return grouped;
    }

    public async Task<decimal> GetTotalWastageImpactAsync(DateTime startDate, DateTime endDate)
    {
        var wastageData = await _context.WastageData
            .AsNoTracking()
            .Include(w => w.Ingredient)
            .Include(w => w.Recipe)
            .Where(w => w.StoreId == CurrentStoreId && w.Date >= startDate.Date && w.Date <= endDate.Date)
            .ToListAsync();

        decimal totalImpact = 0;
        foreach (var w in wastageData)
        {
            totalImpact += await GetTotalImpactAsync(w);
        }
        return totalImpact;
    }

    private async Task<decimal> GetTotalImpactAsync(WastageData w)
    {
        if (w.Ingredient != null)
        {
            return w.Quantity * w.Ingredient.CarbonFootprint;
        }
        if (w.RecipeId.HasValue)
        {
            // Use the recursive service you just built!
            var footprintPerUnit = await _recipeService.CalculateTotalCarbonFootprintAsync(w.RecipeId.Value);
            return w.Quantity * footprintPerUnit;
        }
        return 0;
    }

    private async Task<WastageDataDto> MapToDtoAsync(WastageData wastageData)
    {
        return new WastageDataDto(
            wastageData.Id.ToString(),
            wastageData.Date.ToString("yyyy-MM-dd"),
            wastageData.IngredientId?.ToString(),
            wastageData.RecipeId?.ToString(),
            wastageData.Ingredient?.Name ?? wastageData.Recipe?.Name ?? "Unknown",
            wastageData.Ingredient?.Unit ?? "unit",
            wastageData.Quantity,
            await GetTotalImpactAsync(wastageData),
            wastageData.CreatedAt,
            wastageData.UpdatedAt
        );
    }
}

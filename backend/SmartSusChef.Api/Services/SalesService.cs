using Microsoft.EntityFrameworkCore;
using SmartSusChef.Api.Data;
using SmartSusChef.Api.DTOs;
using SmartSusChef.Api.Models;

namespace SmartSusChef.Api.Services;

public class SalesService : ISalesService
{
    private readonly ApplicationDbContext _context;

    public SalesService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<SalesDataDto>> GetAllAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.SalesData
            .Include(s => s.Recipe)
            .AsQueryable();

        if (startDate.HasValue)
            query = query.Where(s => s.Date >= startDate.Value.Date);

        if (endDate.HasValue)
            query = query.Where(s => s.Date <= endDate.Value.Date);

        var salesData = await query
            .OrderByDescending(s => s.Date)
            .ToListAsync();

        return salesData.Select(MapToDto).ToList();
    }

    public async Task<SalesDataDto?> GetByIdAsync(Guid id)
    {
        var salesData = await _context.SalesData
            .Include(s => s.Recipe)
            .FirstOrDefaultAsync(s => s.Id == id);

        return salesData == null ? null : MapToDto(salesData);
    }

    public async Task<SalesDataDto> CreateAsync(CreateSalesDataRequest request)
    {
        var salesData = new SalesData
        {
            Id = Guid.NewGuid(),
            Date = DateTime.Parse(request.Date).Date,
            RecipeId = Guid.Parse(request.RecipeId),
            Quantity = request.Quantity,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.SalesData.Add(salesData);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(salesData.Id) ?? throw new Exception("Failed to retrieve created sales data");
    }

    public async Task<SalesDataDto?> UpdateAsync(Guid id, UpdateSalesDataRequest request)
    {
        var salesData = await _context.SalesData
            .Include(s => s.Recipe)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (salesData == null) return null;

        salesData.Date = DateTime.Parse(request.Date).Date;
        salesData.RecipeId = Guid.Parse(request.RecipeId);
        salesData.Quantity = request.Quantity;
        salesData.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(salesData);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var salesData = await _context.SalesData.FindAsync(id);
        if (salesData == null) return false;

        _context.SalesData.Remove(salesData);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<SalesTrendDto>> GetTrendAsync(DateTime startDate, DateTime endDate)
    {
        var salesData = await _context.SalesData
            .Include(s => s.Recipe)
            .Where(s => s.Date >= startDate.Date && s.Date <= endDate.Date)
            .ToListAsync();

        var grouped = salesData
            .GroupBy(s => s.Date.Date)
            .OrderBy(g => g.Key)
            .Select(g => new SalesTrendDto(
                g.Key.ToString("yyyy-MM-dd"),
                g.Sum(s => s.Quantity),
                g.GroupBy(s => s.RecipeId)
                    .Select(rg => new RecipeSalesDto(
                        rg.Key.ToString(),
                        rg.First().Recipe.Name,
                        rg.Sum(s => s.Quantity)
                    ))
                    .ToList()
            ))
            .ToList();

        return grouped;
    }

    public async Task<List<IngredientUsageDto>> GetIngredientUsageByDateAsync(DateTime date)
    {
        var salesData = await _context.SalesData
            .Include(s => s.Recipe)
                .ThenInclude(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.Ingredient)
            .Where(s => s.Date.Date == date.Date)
            .ToListAsync();

        var ingredientUsage = new Dictionary<Guid, IngredientUsageDto>();

        foreach (var sale in salesData)
        {
            foreach (var recipeIngredient in sale.Recipe.RecipeIngredients)
            {
                if (recipeIngredient.IngredientId is null || recipeIngredient.Ingredient is null)
                {
                    continue;
                }

                var totalQuantity = recipeIngredient.Quantity * sale.Quantity;
                var ingredientId = recipeIngredient.IngredientId.Value;

                if (ingredientUsage.ContainsKey(ingredientId))
                {
                    var existing = ingredientUsage[ingredientId];
                    ingredientUsage[ingredientId] = existing with
                    {
                        Quantity = existing.Quantity + totalQuantity
                    };
                }
                else
                {
                    ingredientUsage[ingredientId] = new IngredientUsageDto(
                        ingredientId.ToString(),
                        recipeIngredient.Ingredient.Name,
                        recipeIngredient.Ingredient.Unit,
                        totalQuantity
                    );
                }
            }
        }

        return ingredientUsage.Values.OrderBy(i => i.IngredientName).ToList();
    }

    public async Task<List<RecipeSalesDto>> GetRecipeSalesByDateAsync(DateTime date)
    {
        var salesData = await _context.SalesData
            .Include(s => s.Recipe)
            .Where(s => s.Date.Date == date.Date)
            .ToListAsync();

        return salesData
            .Select(s => new RecipeSalesDto(
                s.RecipeId.ToString(),
                s.Recipe.Name,
                s.Quantity
            ))
            .ToList();
    }

    public async Task ImportAsync(List<CreateSalesDataRequest> salesData)
    {
        var entities = salesData.Select(s => new SalesData
        {
            Id = Guid.NewGuid(),
            Date = DateTime.Parse(s.Date).Date,
            RecipeId = Guid.Parse(s.RecipeId),
            Quantity = s.Quantity,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }).ToList();

        _context.SalesData.AddRange(entities);
        await _context.SaveChangesAsync();
    }

    private static SalesDataDto MapToDto(SalesData salesData)
    {
        return new SalesDataDto(
            salesData.Id.ToString(),
            salesData.Date.ToString("yyyy-MM-dd"),
            salesData.RecipeId.ToString(),
            salesData.Recipe.Name,
            salesData.Quantity
        );
    }
}

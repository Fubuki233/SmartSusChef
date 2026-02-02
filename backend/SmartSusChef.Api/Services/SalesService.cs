using Microsoft.EntityFrameworkCore;
using SmartSusChef.Api.Data;
using SmartSusChef.Api.DTOs;
using SmartSusChef.Api.Models;

namespace SmartSusChef.Api.Services;

public class SalesService : ISalesService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    private int CurrentStoreId => _currentUserService.StoreId;

    public SalesService(ApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<List<SalesDataDto>> GetAllAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.SalesData
            .AsNoTracking()
            .Include(s => s.Recipe)
            .Where(s => s.StoreId == CurrentStoreId) // Filter by Store
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
            .AsNoTracking()
            .Include(s => s.Recipe)
            .FirstOrDefaultAsync(s => s.Id == id && s.StoreId == CurrentStoreId);

        return salesData == null ? null : MapToDto(salesData);
    }

    public async Task<SalesDataDto> CreateAsync(CreateSalesDataRequest request)
    {
        var salesData = new SalesData
        {
            Id = Guid.NewGuid(),
            StoreId = CurrentStoreId, // Assign current store
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
            .FirstOrDefaultAsync(s => s.Id == id && s.StoreId == CurrentStoreId);

        if (salesData == null) return null;

        // Only allow quantity modification
        salesData.Quantity = request.Quantity;
        salesData.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(salesData);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var salesData = await _context.SalesData
            .FirstOrDefaultAsync(s => s.Id == id && s.StoreId == CurrentStoreId);
        if (salesData == null) return false;

        _context.SalesData.Remove(salesData);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<SalesWithSignalsDto>> GetTrendAsync(DateTime startDate, DateTime endDate)
    {
        // 1. Fetch data from DB filtered by Store
        var salesData = await _context.SalesData
            .AsNoTracking()
            .Include(s => s.Recipe)
            .Where(s => s.StoreId == CurrentStoreId && s.Date.Date >= startDate.Date && s.Date.Date <= endDate.Date)
            .ToListAsync();
        // 2. Fetch external signals (weather/holidays) for the same period
        var signals = await _context.GlobalCalendarSignals
            .AsNoTracking()
            .Where(sig => sig.Date.Date >= startDate.Date && sig.Date.Date <= endDate.Date)
            .ToDictionaryAsync(sig => sig.Date.Date);

        // 3. Generate the full range of dates to ensure exactly N data points for your unit test
        var allDates = Enumerable.Range(0, (endDate.Date - startDate.Date).Days + 1)
            .Select(d => startDate.AddDays(d).Date);

        // 4. Map everything to the new SalesWithSignalsDto
        var trend = allDates.Select(date =>
        {
            var daySales = salesData.Where(s => s.Date.Date == date).ToList();

            // Check if we have signals for this day, otherwise use defaults
            signals.TryGetValue(date, out var signal);

            return new SalesWithSignalsDto(
                date.ToString("yyyy-MM-dd"),
                daySales.Sum(s => s.Quantity),
                signal?.IsHoliday ?? false,
                signal?.HolidayName ?? "None",
                signal?.RainMm ?? 0m,
                signal?.WeatherDesc ?? "No Data",
                daySales.GroupBy(s => s.RecipeId)
                    .Select(rg => new RecipeSalesDto(
                        rg.Key.ToString(),
                        rg.First().Recipe.Name,
                        rg.Sum(s => s.Quantity)
                    )).ToList()
            );
        }).ToList();

        return trend;
    }

    public async Task<List<IngredientUsageDto>> GetIngredientUsageByDateAsync(DateTime date)
    {
        var salesData = await _context.SalesData
            .Include(s => s.Recipe)
                .ThenInclude(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.Ingredient)
            .Where(s => s.Date.Date == date.Date && s.StoreId == CurrentStoreId)
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
            .Where(s => s.Date.Date == date.Date && s.StoreId == CurrentStoreId)
            .ToListAsync();

        return salesData
            .Select(s => new RecipeSalesDto(
                s.RecipeId.ToString(),
                s.Recipe.Name,
                s.Quantity
            ))
            .ToList();
    }

    // Link the interface method to implementation
    public async Task<List<SalesWithSignalsDto>> GetSalesTrendsWithSignalsAsync(DateTime startDate, DateTime endDate)
    {
        return await GetTrendAsync(startDate, endDate);
    }


    public async Task ImportAsync(List<CreateSalesDataRequest> salesData)
    {
        // 1. Group incoming data to handle duplicates within the import file itself
        var groupedImport = salesData
            .GroupBy(s => new { Date = DateTime.Parse(s.Date).Date, RecipeId = Guid.Parse(s.RecipeId) })
            .Select(g => new { g.Key.Date, g.Key.RecipeId, Quantity = g.Sum(x => x.Quantity) }) // Sum quantities if multiple entries for same day/recipe
            .ToList();

        if (!groupedImport.Any()) return;

        // 2. Fetch existing records for the relevant dates and recipes to minimize DB queries
        var dates = groupedImport.Select(x => x.Date).Distinct().ToList();
        var recipeIds = groupedImport.Select(x => x.RecipeId).Distinct().ToList();

        var existingRecords = await _context.SalesData
            .Where(s => s.StoreId == CurrentStoreId && dates.Contains(s.Date) && recipeIds.Contains(s.RecipeId))
            .ToListAsync();

        // 3. Process Upsert Logic
        foreach (var item in groupedImport)
        {
            var existing = existingRecords
                .FirstOrDefault(s => s.Date == item.Date && s.RecipeId == item.RecipeId);

            if (existing != null)
            {
                // Update existing record
                existing.Quantity = item.Quantity; // Overwrite with new value (or could be += if additive logic preferred)
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Insert new record
                _context.SalesData.Add(new SalesData
                {
                    Id = Guid.NewGuid(),
                    StoreId = CurrentStoreId,
                    Date = item.Date,
                    RecipeId = item.RecipeId,
                    Quantity = item.Quantity,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();
    }

    private static SalesDataDto MapToDto(SalesData salesData)
    {
        return new SalesDataDto(
            salesData.Id.ToString(),
            salesData.Date.ToString("yyyy-MM-dd"),
            salesData.RecipeId.ToString(),
            salesData.Recipe.Name,
            salesData.Quantity,
            salesData.UpdatedAt,
            salesData.CreatedAt,
            salesData.UpdatedAt
        );
    }
}
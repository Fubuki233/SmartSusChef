using System.Globalization;
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

    /// <summary>
    /// Parse date string. If customFormat is provided, try it first; otherwise fall back to common formats.
    /// </summary>
    private static DateTime ParseDate(string dateStr, string? customFormat = null)
    {
        var allFormats = new List<string>();
        if (!string.IsNullOrWhiteSpace(customFormat))
            allFormats.Add(customFormat);
        allFormats.AddRange(new[] { "M/d/yy", "M/d/yyyy", "yyyy-MM-dd", "d/M/yy", "d/M/yyyy", "dd/MM/yyyy", "dd-MM-yyyy", "yyyy/MM/dd" });

        if (DateTime.TryParseExact(dateStr.Trim(), allFormats.ToArray(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
            return result.Date;
        // Fallback to general parse
        return DateTime.Parse(dateStr, CultureInfo.InvariantCulture).Date;
    }

    public async Task<SalesDataDto> CreateAsync(CreateSalesDataRequest request)
    {
        var date = ParseDate(request.Date);
        var recipeId = Guid.Parse(request.RecipeId);

        // Check if the same record already exists
        var existingRecord = await _context.SalesData
            .FirstOrDefaultAsync(s => s.StoreId == CurrentStoreId
                && s.Date == date
                && s.RecipeId == recipeId);

        if (existingRecord != null)
        {
            // Update existing records
            existingRecord.Quantity = request.Quantity;
            existingRecord.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GetByIdAsync(existingRecord.Id)
                ?? throw new Exception("Failed to retrieve updated sales data");
        }

        // Create a new record
        var salesData = new SalesData
        {
            Id = Guid.NewGuid(),
            StoreId = CurrentStoreId,
            Date = date,
            RecipeId = recipeId,
            Quantity = request.Quantity,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.SalesData.Add(salesData);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(salesData.Id)
            ?? throw new Exception("Failed to retrieve created sales data");
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
            .GroupBy(s => s.RecipeId)
            .Select(g => new RecipeSalesDto(
                g.Key.ToString(),
                g.First().Recipe.Name,
                g.Sum(s => s.Quantity)
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
        if (!salesData.Any()) return;

        // Group and process imported data
        var groupedImport = salesData
            .Select(s => new
            {
                Date = ParseDate(s.Date),
                RecipeId = Guid.Parse(s.RecipeId),
                Quantity = s.Quantity
            })
            .GroupBy(s => new { s.Date, s.RecipeId })
            .Select(g => new
            {
                g.Key.Date,
                g.Key.RecipeId,
                Quantity = g.Last().Quantity  // Take the last one in case of duplicates
            })
            .ToList();

        // Retrieve existing records of relevant dates and dishes
        var dates = groupedImport.Select(x => x.Date).Distinct().ToList();
        var recipeIds = groupedImport.Select(x => x.RecipeId).Distinct().ToList();

        var existingRecords = await _context.SalesData
            .Where(s => s.StoreId == CurrentStoreId
                && dates.Contains(s.Date)
                && recipeIds.Contains(s.RecipeId))
            .ToListAsync();

        // Check if we are running in memory (for tests)
        var isInMemory = _context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";

        if (isInMemory)
        {
            await ProcessImportAsync(groupedImport, existingRecords);
        }
        else
        {
            // Use transactions to ensure data consistency
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await ProcessImportAsync(groupedImport, existingRecords);
                await transaction.CommitAsync();
            }
            catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
            {
                await transaction.RollbackAsync();
                throw new InvalidOperationException(
                    "Duplicate records detected. This should not happen with proper validation.");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }

    private async Task ProcessImportAsync(IEnumerable<dynamic> groupedImport, List<SalesData> existingRecords)
    {
        foreach (var item in groupedImport)
        {
            var existing = existingRecords
                .FirstOrDefault(s => s.Date == item.Date && s.RecipeId == item.RecipeId);

            if (existing != null)
            {
                // Update existing records
                existing.Quantity = item.Quantity;
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

    private static bool IsDuplicateKeyException(DbUpdateException ex)
    {
        // Check if the uniqueness constraint is violated
        return ex.InnerException?.Message?.Contains("violates unique constraint") == true
            || ex.InnerException?.Message?.Contains("duplicate key") == true
            || ex.InnerException?.Message?.Contains("Duplicate entry") == true;
    }

    public async Task<ImportSalesByNameResponse> ImportByNameAsync(List<ImportSalesByNameItem> salesData, string? dateFormat = null)
    {
        if (!salesData.Any())
            return new ImportSalesByNameResponse(0, 0, new List<string>());

        // 1. Collect all unique dish names from import
        var dishNames = salesData
            .Select(s => s.DishName.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // 2. Look up existing recipes for this store (case-insensitive)
        var existingRecipes = await _context.Recipes
            .Where(r => r.StoreId == CurrentStoreId)
            .ToListAsync();

        var recipeMap = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in existingRecipes)
            recipeMap[r.Name] = r.Id;

        // 3. Auto-create missing recipes
        var newDishes = new List<string>();
        foreach (var name in dishNames)
        {
            if (!recipeMap.ContainsKey(name))
            {
                var newRecipe = new Recipe
                {
                    Id = Guid.NewGuid(),
                    StoreId = CurrentStoreId,
                    Name = name,
                    IsSubRecipe = false,
                    IsSellable = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Recipes.Add(newRecipe);
                recipeMap[name] = newRecipe.Id;
                newDishes.Add(name);
            }
        }

        if (newDishes.Any())
            await _context.SaveChangesAsync();

        // 4. Convert dates using the user-specified format, then delegate
        var standardData = salesData.Select(s => new CreateSalesDataRequest(
            Date: ParseDate(s.Date, dateFormat).ToString("yyyy-MM-dd"),
            RecipeId: recipeMap[s.DishName.Trim()].ToString(),
            Quantity: s.Quantity
        )).ToList();

        await ImportAsync(standardData);

        return new ImportSalesByNameResponse(
            Imported: standardData.Count,
            Created: newDishes.Count,
            NewDishes: newDishes
        );
    }

    private static SalesDataDto MapToDto(SalesData salesData)
    {
        return new SalesDataDto(
            salesData.Id.ToString(),
            salesData.Date.ToString("yyyy-MM-dd"),
            salesData.RecipeId.ToString(),
            salesData.Recipe.Name,
            salesData.Quantity,
            salesData.CreatedAt.ToUniversalTime(),
            salesData.UpdatedAt.ToUniversalTime()
        );
    }
}

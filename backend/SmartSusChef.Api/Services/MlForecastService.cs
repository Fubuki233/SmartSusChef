using Microsoft.EntityFrameworkCore;
using SmartSusChef.Api.Data;
using SmartSusChef.Api.DTOs;
using SmartSusChef.Api.Models;

namespace SmartSusChef.Api.Services;

/// <summary>
/// Real forecast service that calls the ML backend for predictions,
/// stores results in ForecastData, and falls back to mock when ML is unavailable.
/// </summary>
public class MlForecastService : IForecastService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMlPredictionService _mlService;
    private readonly IStoreService _storeService;
    private readonly ILogger<MlForecastService> _logger;

    private int CurrentStoreId => _currentUserService.StoreId;

    public MlForecastService(
        ApplicationDbContext context,
        ICurrentUserService currentUserService,
        IMlPredictionService mlService,
        IStoreService storeService,
        ILogger<MlForecastService> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _mlService = mlService;
        _storeService = storeService;
        _logger = logger;
    }

    public async Task<List<ForecastDto>> GetForecastAsync(int days = 7, int includePastDays = 0)
    {
        var storeId = CurrentStoreId;
        var today = DateTime.UtcNow.Date;

        // 1. Try to get ML predictions
        try
        {
            var store = await _storeService.GetStoreAsync();

            var mlResponse = await _mlService.GetStorePredictionsAsync(
                storeId,
                horizonDays: days + 2, // Extra buffer for timezone
                latitude: store?.Latitude,
                longitude: store?.Longitude,
                countryCode: store?.CountryCode
            );

            switch (mlResponse.Status)
            {
                case "ok" when mlResponse.Predictions != null:
                    // Save predictions to database and return
                    var forecasts = await SaveAndConvertPredictions(mlResponse, storeId, today, days, includePastDays);
                    _logger.LogInformation("Store {StoreId}: SaveAndConvertPredictions returned {Count} forecasts.", storeId, forecasts.Count);
                    if (forecasts.Any())
                        return forecasts;
                    _logger.LogWarning("Store {StoreId}: ML returned 'ok' but no forecasts matched date range. today={Today}", storeId, today.ToString("yyyy-MM-dd"));
                    break;

                case "training":
                    _logger.LogInformation("Store {StoreId}: ML models are being trained. Returning cached or mock data.", storeId);
                    break;

                case "insufficient_data":
                    _logger.LogInformation(
                        "Store {StoreId}: Insufficient data ({Days} days). Need 100+ days.",
                        storeId, mlResponse.DaysAvailable);
                    break;

                default:
                    _logger.LogWarning("Store {StoreId}: ML returned status '{Status}': {Message}",
                        storeId, mlResponse.Status, mlResponse.Message);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Store {StoreId}: ML service unavailable, falling back to cached/mock.", storeId);
        }

        // 2. Try cached ForecastData from database
        var cached = await GetCachedForecasts(storeId, today, days, includePastDays);
        if (cached.Any())
            return cached;

        // 3. No ML predictions and no cache — return empty list
        //    (Do NOT generate fake/mock data; leave it empty so the frontend shows "no data")
        _logger.LogInformation("Store {StoreId}: No ML predictions or cached forecasts available. Returning empty.", storeId);
        return new List<ForecastDto>();
    }

    public async Task<List<ForecastSummaryDto>> GetForecastSummaryAsync(int days = 7, int includePastDays = 0)
    {
        var forecasts = await GetForecastAsync(days, includePastDays);

        return forecasts
            .GroupBy(f => f.Date)
            .Select(g => new ForecastSummaryDto(
                g.Key,
                g.Sum(f => f.Quantity),
                0m // Change percentage would require historical comparison
            ))
            .ToList();
    }

    // -----------------------------------------------------------------
    // Save ML predictions to DB and convert to ForecastDto
    // -----------------------------------------------------------------
    private async Task<List<ForecastDto>> SaveAndConvertPredictions(
        MlStorePredictResponseDto mlResponse, int storeId, DateTime today, int days, int includePastDays)
    {
        var recipes = await _context.Recipes
            .Where(r => r.StoreId == storeId)
            .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
            .ToListAsync();

        var recipeByName = recipes.ToDictionary(r => r.Name, r => r, StringComparer.OrdinalIgnoreCase);
        var forecastsToSave = new List<ForecastData>();
        var result = new List<ForecastDto>();

        _logger.LogInformation(
            "Store {StoreId}: ML returned {DishCount} dishes, DB has {RecipeCount} recipes. ML dishes: [{MlDishes}], DB recipes: [{DbRecipes}]",
            storeId,
            mlResponse.Predictions!.Count,
            recipes.Count,
            string.Join(", ", mlResponse.Predictions!.Keys.Take(10)),
            string.Join(", ", recipes.Select(r => r.Name).Take(10))
        );

        foreach (var (dishName, prediction) in mlResponse.Predictions!)
        {
            if (prediction.Error != null || prediction.Predictions == null)
                continue;

            if (!recipeByName.TryGetValue(dishName, out var recipe))
            {
                _logger.LogWarning("ML dish '{DishName}' not found in store {StoreId} recipes, skipping.", dishName, storeId);
                continue;
            }

            foreach (var dayPred in prediction.Predictions)
            {
                if (!DateTime.TryParse(dayPred.Date, out var forecastDate))
                    continue;

                var predictedQty = (int)Math.Max(0, Math.Round(dayPred.Yhat));

                // Save to ForecastData
                forecastsToSave.Add(new ForecastData
                {
                    Id = Guid.NewGuid(),
                    StoreId = storeId,
                    RecipeId = recipe.Id,
                    ForecastDate = forecastDate,
                    PredictedQuantity = predictedQty,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                });

                // Check if this date should be included in the response
                var diffDays = (forecastDate.Date - today).Days;
                bool include = (diffDays >= -includePastDays && diffDays <= days + 2);

                if (include)
                {
                    var forecastIngredients = recipe.RecipeIngredients
                        .Where(ri => ri.Ingredient != null && ri.IngredientId.HasValue)
                        .Select(ri => new ForecastIngredientDto(
                            ri.IngredientId!.Value.ToString(),
                            ri.Ingredient!.Name,
                            ri.Ingredient.Unit,
                            ri.Quantity * predictedQty
                        )).ToList();

                    result.Add(new ForecastDto(
                        forecastDate.ToString("yyyy-MM-dd"),
                        recipe.Id.ToString(),
                        recipe.Name,
                        predictedQty,
                        forecastIngredients,
                        predictedQty > 50 ? "High" : (predictedQty > 20 ? "Medium" : "Low")
                    ));
                }
            }
        }

        // Upsert forecast data: remove old forecasts for the same date range, then add new
        if (forecastsToSave.Any())
        {
            _logger.LogInformation(
                "Store {StoreId}: Saving {SaveCount} forecast records, returning {ResultCount} to client. Today={Today}, days={Days}, pastDays={PastDays}",
                storeId, forecastsToSave.Count, result.Count, today.ToString("yyyy-MM-dd"), days, includePastDays
            );
            var minDate = forecastsToSave.Min(f => f.ForecastDate);
            var maxDate = forecastsToSave.Max(f => f.ForecastDate);

            var oldForecasts = await _context.ForecastData
                .Where(f => f.StoreId == storeId && f.ForecastDate >= minDate && f.ForecastDate <= maxDate)
                .ToListAsync();

            _context.ForecastData.RemoveRange(oldForecasts);
            _context.ForecastData.AddRange(forecastsToSave);

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Saved {Count} forecast records for store {StoreId}.", forecastsToSave.Count, storeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save forecast data for store {StoreId}.", storeId);
            }
        }

        return result;
    }

    // -----------------------------------------------------------------
    // Get cached forecasts from ForecastData table
    // -----------------------------------------------------------------
    private async Task<List<ForecastDto>> GetCachedForecasts(int storeId, DateTime today, int days, int includePastDays)
    {
        var startDate = today.AddDays(-includePastDays);
        var endDate = today.AddDays(days + 2);

        var cachedData = await _context.ForecastData
            .Where(f => f.StoreId == storeId && f.ForecastDate >= startDate && f.ForecastDate <= endDate)
            .Include(f => f.Recipe)
                .ThenInclude(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.Ingredient)
            .OrderBy(f => f.ForecastDate)
            .ToListAsync();

        if (!cachedData.Any())
            return new List<ForecastDto>();

        // Only return recent forecasts (less than 24h old)
        var recentCutoff = DateTime.UtcNow.AddHours(-24);
        cachedData = cachedData.Where(f => f.UpdatedAt >= recentCutoff).ToList();

        return cachedData.Select(f =>
        {
            var forecastIngredients = f.Recipe.RecipeIngredients
                .Where(ri => ri.Ingredient != null && ri.IngredientId.HasValue)
                .Select(ri => new ForecastIngredientDto(
                    ri.IngredientId!.Value.ToString(),
                    ri.Ingredient!.Name,
                    ri.Ingredient.Unit,
                    ri.Quantity * f.PredictedQuantity
                )).ToList();

            return new ForecastDto(
                f.ForecastDate.ToString("yyyy-MM-dd"),
                f.RecipeId.ToString(),
                f.Recipe.Name,
                f.PredictedQuantity,
                forecastIngredients,
                f.PredictedQuantity > 50 ? "High" : (f.PredictedQuantity > 20 ? "Medium" : "Low")
            );
        }).ToList();
    }

    // -----------------------------------------------------------------
    // Simple fallback (same logic as MockForecastService)
}

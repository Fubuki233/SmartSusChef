using Microsoft.EntityFrameworkCore;
using SmartSusChef.Api.Data;
using SmartSusChef.Api.DTOs;

namespace SmartSusChef.Api.Services;

/// <summary>
/// Mock forecast service that simulates ML predictions
/// Uses simple heuristics based on historical data with random variations
/// </summary>
public class MockForecastService : IForecastService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly Random _random = new();

    private int CurrentStoreId => _currentUserService.StoreId;

    public MockForecastService(ApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<List<ForecastDto>> GetForecastAsync(int days = 7)
    {
        if (days < 1 || days > 30)
        {
            throw new ArgumentException("Forecast days must be between 1 and 30.");
        }

        var recipes = await _context.Recipes
            .Where(r => r.StoreId == CurrentStoreId)
            .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
            .Include(r => r.SalesRecords)
            .ToListAsync();

        var forecasts = new List<ForecastDto>();
        var today = DateTime.UtcNow.Date;

        for (int i = 1; i <= days; i++)
        {
            var forecastDate = today.AddDays(i);

            foreach (var recipe in recipes)
            {
                // Calculate average sales from last 30 days
                var recentSales = recipe.SalesRecords
                    .Where(s => s.Date >= today.AddDays(-30))
                    .ToList();

                var avgQuantity = recentSales.Any()
                    ? (int)recentSales.Average(s => s.Quantity)
                    : 10; // Default if no history

                // Add some randomness and day-of-week pattern
                var dayOfWeek = forecastDate.DayOfWeek;
                var multiplier = dayOfWeek switch
                {
                    DayOfWeek.Friday => 1.3,
                    DayOfWeek.Saturday => 1.5,
                    DayOfWeek.Sunday => 1.2,
                    DayOfWeek.Monday => 0.8,
                    _ => 1.0
                };

                // Add random variation between -10% to +10%
                var randomFactor = 0.9 + (_random.NextDouble() * 0.2);
                
                // Ensure non-negative prediction
                var rawPrediction = avgQuantity * multiplier * randomFactor;
                var predictedQuantity = (int)Math.Max(0, rawPrediction);

                // Simulate ML confidence
                // In a real scenario, this would come from the model's probability score
                var confidence = predictedQuantity > 50 ? "High" : (predictedQuantity > 20 ? "Medium" : "Low");

                var forecastIngredients = recipe.RecipeIngredients
                    .Where(ri => ri.Ingredient != null && ri.IngredientId.HasValue) // Filter out null ingredients
                    .Select(ri =>
                    new ForecastIngredientDto(
                        ri.IngredientId!.Value.ToString(),
                        ri.Ingredient!.Name,
                        ri.Ingredient.Unit,
                        ri.Quantity * predictedQuantity
                    )).ToList();

                forecasts.Add(new ForecastDto(
                    forecastDate.ToString("yyyy-MM-dd"),
                    recipe.Id.ToString(),
                    recipe.Name,
                    predictedQuantity,
                    forecastIngredients,
                    confidence
                ));
            }
        }

        return forecasts;
    }

    public async Task<List<ForecastSummaryDto>> GetForecastSummaryAsync(int days = 7)
    {
        var forecasts = await GetForecastAsync(days);

        var summary = forecasts
            .GroupBy(f => f.Date)
            .Select(g =>
            {
                var totalQuantity = g.Sum(f => f.Quantity);

                // Calculate percentage change from last week (mock calculation)
                var changePercentage = -5 + (_random.NextDouble() * 20); // Random between -5% and +15%

                return new ForecastSummaryDto(
                    g.Key,
                    totalQuantity,
                    Math.Round((decimal)changePercentage, 1)
                );
            })
            .ToList();

        return summary;
    }
}

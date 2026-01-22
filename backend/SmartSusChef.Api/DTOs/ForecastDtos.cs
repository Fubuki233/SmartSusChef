namespace SmartSusChef.Api.DTOs;

public record ForecastDto(
    string Date,
    string RecipeId,
    string RecipeName,
    int Quantity,
    List<ForecastIngredientDto> Ingredients
);

public record ForecastIngredientDto(
    string IngredientId,
    string IngredientName,
    string Unit,
    decimal Quantity
);

public record ForecastSummaryDto(
    string Date,
    int TotalQuantity,
    decimal ChangePercentage
);

public record WeatherDto(
    decimal Temperature,
    string Condition,
    int Humidity,
    string Description
);

public record HolidayDto(
    string Date,
    string Name
);

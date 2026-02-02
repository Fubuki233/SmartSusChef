namespace SmartSusChef.Api.DTOs;

public record ForecastDto(
    string Date,
    string RecipeId,
    string RecipeName,
    int Quantity,
    List<ForecastIngredientDto> Ingredients,
    string Confidence = "Medium" // Added Confidence field
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

/// <summary>
/// Weather forecast for a specific date
/// </summary>
public record WeatherForecastDto(
    string Date,
    decimal? TemperatureMax,
    decimal? TemperatureMin,
    decimal RainMm,
    int WeatherCode,
    string WeatherDescription
);

/// <summary>
/// Calendar info for a specific date including weather and holiday status
/// </summary>
public record CalendarDayDto(
    string Date,
    bool IsHoliday,
    string? HolidayName,
    bool IsSchoolHoliday,
    bool IsWeekend,
    WeatherForecastDto? Weather
);

/// <summary>
/// Combined forecast and calendar response for tomorrow
/// </summary>
public record TomorrowForecastDto(
    string Date,
    CalendarDayDto Calendar,
    WeatherForecastDto? Weather
);

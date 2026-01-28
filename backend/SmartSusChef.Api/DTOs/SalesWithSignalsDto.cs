namespace SmartSusChef.Api.DTOs;

public record SalesWithSignalsDto(
    string Date,
    int TotalQuantity,
    bool IsHoliday,
    string HolidayName,
    decimal RainMm,
    string WeatherDesc,
    List<RecipeSalesDto> Recipes
);
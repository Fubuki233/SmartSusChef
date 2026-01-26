using SmartSusChef.Api.DTOs;

namespace SmartSusChef.Api.Services;

public interface IWeatherService
{
    Task<WeatherDto> GetCurrentWeatherAsync();

    /// <summary>
    /// Get weather forecast for a specific date using Open-Meteo API
    /// </summary>
    Task<WeatherForecastDto?> GetWeatherForecastAsync(DateTime date, decimal latitude, decimal longitude);
}

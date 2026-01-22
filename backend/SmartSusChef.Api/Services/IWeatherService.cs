using SmartSusChef.Api.DTOs;

namespace SmartSusChef.Api.Services;

public interface IWeatherService
{
    Task<WeatherDto> GetCurrentWeatherAsync();
}

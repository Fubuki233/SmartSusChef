using SmartSusChef.Api.DTOs;
using System.Text.Json;

namespace SmartSusChef.Api.Services;

public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public WeatherService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<WeatherDto> GetCurrentWeatherAsync()
    {
        try
        {
            // Singapore coordinates
            const double latitude = 1.3521;
            const double longitude = 103.8198;

            var baseUrl = _configuration["ExternalApis:WeatherApiUrl"];
            var url = $"{baseUrl}?latitude={latitude}&longitude={longitude}&current_weather=true";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);

            var currentWeather = doc.RootElement.GetProperty("current_weather");
            var temperature = currentWeather.GetProperty("temperature").GetDouble();
            var weatherCode = currentWeather.GetProperty("weathercode").GetInt32();

            var (condition, description) = MapWeatherCode(weatherCode);

            // Get humidity if available from hourly data
            var humidity = 70; // Default value

            return new WeatherDto(
                (decimal)temperature,
                condition,
                humidity,
                description
            );
        }
        catch (Exception)
        {
            // Return mock data if API fails
            return new WeatherDto(
                28.5m,
                "Partly Cloudy",
                75,
                "Warm and humid with partial cloud cover"
            );
        }
    }

    private static (string Condition, string Description) MapWeatherCode(int code)
    {
        return code switch
        {
            0 => ("Clear", "Clear sky"),
            1 or 2 or 3 => ("Partly Cloudy", "Partly cloudy to cloudy"),
            45 or 48 => ("Foggy", "Foggy conditions"),
            51 or 53 or 55 => ("Drizzle", "Light to moderate drizzle"),
            61 or 63 or 65 => ("Rainy", "Light to heavy rain"),
            71 or 73 or 75 => ("Snowy", "Light to heavy snow"),
            80 or 81 or 82 => ("Rain Showers", "Rain showers"),
            95 or 96 or 99 => ("Thunderstorm", "Thunderstorm"),
            _ => ("Unknown", "Weather condition unknown")
        };
    }
}

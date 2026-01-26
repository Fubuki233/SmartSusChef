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

    public async Task<WeatherForecastDto?> GetWeatherForecastAsync(DateTime date, decimal latitude, decimal longitude)
    {
        try
        {
            var dateStr = date.ToString("yyyy-MM-dd");
            var today = DateTime.UtcNow.Date;

            string url;

            // Use forecast API for future dates (up to 16 days), archive API for past dates
            if (date.Date >= today && date.Date <= today.AddDays(16))
            {
                // Use forecast API
                url = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}" +
                      $"&daily=temperature_2m_max,temperature_2m_min,precipitation_sum,weathercode" +
                      $"&timezone=auto&start_date={dateStr}&end_date={dateStr}";
            }
            else if (date.Date < today)
            {
                // Use archive API for historical data
                url = $"https://archive-api.open-meteo.com/v1/archive?latitude={latitude}&longitude={longitude}" +
                      $"&daily=temperature_2m_max,temperature_2m_min,precipitation_sum,weathercode" +
                      $"&timezone=auto&start_date={dateStr}&end_date={dateStr}";
            }
            else
            {
                // Date too far in the future
                return null;
            }

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);

            if (doc.RootElement.TryGetProperty("daily", out var daily))
            {
                var dates = daily.GetProperty("time");
                if (dates.GetArrayLength() > 0)
                {
                    decimal? tempMax = null;
                    decimal? tempMin = null;
                    decimal rainMm = 0;
                    int weatherCode = 0;

                    if (daily.TryGetProperty("temperature_2m_max", out var maxTemps) &&
                        maxTemps[0].ValueKind != JsonValueKind.Null)
                    {
                        tempMax = (decimal)maxTemps[0].GetDouble();
                    }

                    if (daily.TryGetProperty("temperature_2m_min", out var minTemps) &&
                        minTemps[0].ValueKind != JsonValueKind.Null)
                    {
                        tempMin = (decimal)minTemps[0].GetDouble();
                    }

                    if (daily.TryGetProperty("precipitation_sum", out var precip) &&
                        precip[0].ValueKind != JsonValueKind.Null)
                    {
                        rainMm = (decimal)precip[0].GetDouble();
                    }

                    if (daily.TryGetProperty("weathercode", out var codes) &&
                        codes[0].ValueKind != JsonValueKind.Null)
                    {
                        weatherCode = codes[0].GetInt32();
                    }

                    return new WeatherForecastDto(
                        dateStr,
                        tempMax,
                        tempMin,
                        Math.Round(rainMm, 2),
                        weatherCode,
                        GetWeatherDescription(weatherCode)
                    );
                }
            }

            return null;
        }
        catch (Exception)
        {
            return null;
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

    /// <summary>
    /// Convert WMO weather code to description (matching Python script logic)
    /// </summary>
    private static string GetWeatherDescription(int code)
    {
        return code switch
        {
            0 => "Sunny",
            1 => "Mainly Clear",
            2 => "Partly Cloudy",
            3 => "Overcast",
            45 => "Foggy",
            48 => "Depositing Rime Fog",
            51 => "Light Drizzle",
            53 => "Moderate Drizzle",
            55 => "Dense Drizzle",
            61 => "Slight Rain",
            63 => "Moderate Rain",
            65 => "Heavy Rain",
            71 => "Slight Snow",
            73 => "Moderate Snow",
            75 => "Heavy Snow",
            77 => "Snow Grains",
            80 => "Slight Rain Showers",
            81 => "Moderate Rain Showers",
            82 => "Violent Rain Showers",
            85 => "Slight Snow Showers",
            86 => "Heavy Snow Showers",
            95 => "Thunderstorm",
            96 => "Thunderstorm with Slight Hail",
            99 => "Thunderstorm with Heavy Hail",
            _ => "Unknown"
        };
    }
}

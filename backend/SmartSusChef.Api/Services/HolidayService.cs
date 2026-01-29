using Microsoft.EntityFrameworkCore;
using SmartSusChef.Api.Data;
using SmartSusChef.Api.DTOs;
using SmartSusChef.Api.Models;
using System.Text.Json;

namespace SmartSusChef.Api.Services;

public class HolidayService : IHolidayService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;

    // Cache for holidays to avoid repeated API calls
    private static readonly Dictionary<string, List<HolidayDto>> _holidayCache = new();

    // Cache for country codes based on coordinates
    private static readonly Dictionary<string, string> _countryCodeCache = new();

    public HolidayService(HttpClient httpClient, IConfiguration configuration, ApplicationDbContext context)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _context = context;
    }

    public async Task<string> GetCountryCodeFromCoordinatesAsync(decimal latitude, decimal longitude)
    {
        // Create a cache key based on rounded coordinates (to 1 decimal place for caching)
        var cacheKey = $"{Math.Round(latitude, 1)}_{Math.Round(longitude, 1)}";

        if (_countryCodeCache.TryGetValue(cacheKey, out var cachedCode))
        {
            return cachedCode;
        }

        try
        {
            // Use OpenStreetMap Nominatim for reverse geocoding (free, no API key required)
            var url = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={latitude}&lon={longitude}&zoom=3";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "SmartSusChef/1.0");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);

            if (doc.RootElement.TryGetProperty("address", out var address) &&
                address.TryGetProperty("country_code", out var countryCodeElement))
            {
                var countryCode = countryCodeElement.GetString()?.ToUpperInvariant()
                    ?? throw new InvalidOperationException("Failed to get country code from coordinates");
                _countryCodeCache[cacheKey] = countryCode;
                return countryCode;
            }
        }
        catch (HttpRequestException)
        {
            // Fall back to default if external service is unavailable
        }
        catch (TaskCanceledException)
        {
            // Timeout or cancellation: fall back to default
        }
        catch (JsonException)
        {
            // Malformed response: fall back to default
        }

        _countryCodeCache[cacheKey] = string.Empty;
        return string.Empty;
    }

    public async Task<List<HolidayDto>> GetHolidaysAsync(int year, string? countryCode = null)
    {
        var code = countryCode;
        if (string.IsNullOrWhiteSpace(code))
        {
            return new List<HolidayDto>();
        }
        var cacheKey = $"{code}_{year}";

        if (_holidayCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var stored = await _context.HolidayCalendars
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.CountryCode == code && h.Year == year);

        if (stored != null && !string.IsNullOrWhiteSpace(stored.HolidaysJson))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<HolidayDto>>(stored.HolidaysJson) ?? new List<HolidayDto>();
                var ordered = parsed.OrderBy(h => h.Date).ToList();
                _holidayCache[cacheKey] = ordered;
                return ordered;
            }
            catch (JsonException)
            {
                // Fall back to fetching from API if stored JSON is invalid
            }
        }

        try
        {
            var baseUrl = _configuration["ExternalApis:HolidayApiUrl"]
                ?? throw new InvalidOperationException("HolidayApiUrl is not configured");
            var url = $"{baseUrl}/{year}/{code}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);

            var holidays = new List<HolidayDto>();

            foreach (var holiday in doc.RootElement.EnumerateArray())
            {
                var date = holiday.GetProperty("date").GetString() ?? "";
                var name = holiday.GetProperty("localName").GetString() ?? "";

                holidays.Add(new HolidayDto(date, name));
            }

            var result = holidays.OrderBy(h => h.Date).ToList();
            _holidayCache[cacheKey] = result;

            var existing = await _context.HolidayCalendars
                .FirstOrDefaultAsync(h => h.CountryCode == code && h.Year == year);

            if (existing == null)
            {
                existing = new HolidayCalendar
                {
                    CountryCode = code,
                    Year = year,
                    HolidaysJson = JsonSerializer.Serialize(result),
                    UpdatedAt = DateTime.UtcNow
                };
                _context.HolidayCalendars.Add(existing);
            }
            else
            {
                existing.HolidaysJson = JsonSerializer.Serialize(result);
                existing.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            if (result.Count > 0)
            {
                var parsedDates = result
                    .Select(h => DateTime.TryParse(h.Date, out var d) ? d.Date : (DateTime?)null)
                    .Where(d => d.HasValue)
                    .Select(d => d!.Value)
                    .ToList();

                if (parsedDates.Count > 0)
                {
                    var existingSignals = await _context.GlobalCalendarSignals
                        .Where(s => parsedDates.Contains(s.Date))
                        .ToListAsync();

                    var signalMap = existingSignals.ToDictionary(s => s.Date, s => s);

                    foreach (var holiday in result)
                    {
                        if (!DateTime.TryParse(holiday.Date, out var holidayDate))
                        {
                            continue;
                        }

                        var dateOnly = holidayDate.Date;
                        if (!signalMap.TryGetValue(dateOnly, out var signal))
                        {
                            signal = new GlobalCalendarSignals
                            {
                                Date = dateOnly
                            };
                            _context.GlobalCalendarSignals.Add(signal);
                        }

                        signal.IsHoliday = true;
                        signal.HolidayName = holiday.Name;
                        signal.IsSchoolHoliday = IsSchoolHoliday(dateOnly);
                    }

                    await _context.SaveChangesAsync();
                }
            }
            return result;
        }
        catch (HttpRequestException)
        {
            // External service unavailable; return empty list to avoid crashing
        }
        catch (TaskCanceledException)
        {
            // Timeout; return empty list
        }
        catch (JsonException)
        {
            // Invalid JSON; return empty list
        }

        return new List<HolidayDto>();
    }

    public async Task<(bool IsHoliday, string? HolidayName)> IsHolidayAsync(DateTime date, string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            return (false, null);
        }

        var holidays = await GetHolidaysAsync(date.Year, countryCode);
        var dateStr = date.ToString("yyyy-MM-dd");

        var holiday = holidays.FirstOrDefault(h => h.Date == dateStr);
        return (holiday != null, holiday?.Name);
    }

    public bool IsSchoolHoliday(DateTime date)
    {
        var schoolHolidays = GetSchoolHolidays(date.Year);

        foreach (var (start, end) in schoolHolidays)
        {
            if (date.Date >= start.Date && date.Date <= end.Date)
            {
                return true;
            }
        }

        return false;
    }

    public DateTime? GetChineseNewYear(int year)
    {
        // Chinese New Year dates (approximate, based on lunar calendar)
        // These are pre-calculated for accuracy
        var cnyDates = new Dictionary<int, DateTime>
        {
            { 2020, new DateTime(2020, 1, 25) },
            { 2021, new DateTime(2021, 2, 12) },
            { 2022, new DateTime(2022, 2, 1) },
            { 2023, new DateTime(2023, 1, 22) },
            { 2024, new DateTime(2024, 2, 10) },
            { 2025, new DateTime(2025, 1, 29) },
            { 2026, new DateTime(2026, 2, 17) },
            { 2027, new DateTime(2027, 2, 6) },
            { 2028, new DateTime(2028, 1, 26) },
            { 2029, new DateTime(2029, 2, 13) },
            { 2030, new DateTime(2030, 2, 3) }
        };

        return cnyDates.TryGetValue(year, out var cny) ? cny : null;
    }

    /// <summary>
    /// Get school holiday periods for a given year
    /// Rules:
    /// 1. July and August are summer holidays
    /// 2. Two weeks before and after Chinese New Year are winter holidays
    /// </summary>
    private List<(DateTime Start, DateTime End)> GetSchoolHolidays(int year)
    {
        var holidays = new List<(DateTime, DateTime)>();

        // 1. Summer holidays: July 1 - August 31
        holidays.Add((new DateTime(year, 7, 1), new DateTime(year, 8, 31)));

        // 2. Winter holidays: 2 weeks before and after Chinese New Year
        var cny = GetChineseNewYear(year);
        if (cny.HasValue)
        {
            var startWinter = cny.Value.AddDays(-14);
            var endWinter = cny.Value.AddDays(14);
            holidays.Add((startWinter, endWinter));
        }

        // Check if CNY from previous year extends into this year
        var prevCny = GetChineseNewYear(year - 1);
        if (prevCny.HasValue)
        {
            var endWinter = prevCny.Value.AddDays(14);
            if (endWinter.Year == year)
            {
                var startWinter = prevCny.Value.AddDays(-14);
                holidays.Add((startWinter, endWinter));
            }
        }

        // Check if CNY from next year's winter holiday starts in this year
        var nextCny = GetChineseNewYear(year + 1);
        if (nextCny.HasValue)
        {
            var startWinter = nextCny.Value.AddDays(-14);
            if (startWinter.Year == year)
            {
                var endWinter = nextCny.Value.AddDays(14);
                holidays.Add((startWinter, endWinter));
            }
        }

        return holidays;
    }
}

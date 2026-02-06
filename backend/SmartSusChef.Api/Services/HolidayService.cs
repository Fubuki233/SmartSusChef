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
    private static readonly Dictionary<string, List<HolidayDto>> HolidayCache = new();
    private static readonly Dictionary<string, string> CountryCodeCache = new();

    public HolidayService(HttpClient httpClient, IConfiguration configuration, ApplicationDbContext context)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _context = context;
    }

    public async Task SyncHolidaysAsync(int year, string countryCode)
    {
        var holidays = await GetHolidaysAsync(year, countryCode);
        var schoolHolidays = GetSchoolHolidays(year);

        // Get all dates for the year
        var startDate = new DateTime(year, 1, 1);
        var endDate = new DateTime(year, 12, 31);
        
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var dateStr = date.ToString("yyyy-MM-dd");
            var holiday = holidays.FirstOrDefault(h => h.Date == dateStr);
            var isSchoolHoliday = IsDateInRanges(date, schoolHolidays);

            var signal = await _context.GlobalCalendarSignals.FindAsync(date);
            if (signal == null)
            {
                signal = new GlobalCalendarSignals
                {
                    Date = date,
                    IsHoliday = holiday != null,
                    HolidayName = holiday?.Name ?? string.Empty,
                    IsSchoolHoliday = isSchoolHoliday
                };
                _context.GlobalCalendarSignals.Add(signal);
            }
            else
            {
                signal.IsHoliday = holiday != null;
                signal.HolidayName = holiday?.Name ?? string.Empty;
                signal.IsSchoolHoliday = isSchoolHoliday;
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task<bool> IsHolidayAsync(DateTime date)
    {
        var signal = await _context.GlobalCalendarSignals.FindAsync(date.Date);
        return signal?.IsHoliday ?? false;
    }

    // Changed from private to internal to allow testing via InternalsVisibleTo
    internal async Task<string?> GetCountryCodeFromCoordinatesAsync(decimal latitude, decimal longitude)
    {
        var cacheKey = $"{Math.Round(latitude, 1)}_{Math.Round(longitude, 1)}";
        if (CountryCodeCache.TryGetValue(cacheKey, out var cachedCode))
        {
            return cachedCode;
        }

        var url = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={latitude}&lon={longitude}&zoom=3";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("User-Agent", "SmartSusChef/1.0");

        try
        {
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);

            if (doc.RootElement.TryGetProperty("address", out var address) &&
                address.TryGetProperty("country_code", out var countryCodeElement))
            {
                var countryCode = countryCodeElement.GetString()?.ToUpperInvariant();
                if (countryCode != null)
                {
                    CountryCodeCache[cacheKey] = countryCode;
                    return countryCode;
                }
            }
        }
        catch
        {
            // Ignore errors and return null
        }

        return null;
    }

    public async Task<List<HolidayDto>> GetHolidaysAsync(int year, string? countryCode = null)
    {
        var code = countryCode ?? "SG";
        var cacheKey = $"{code}_{year}";

        if (HolidayCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var baseUrl = _configuration["ExternalApis:HolidayApiUrl"]
            ?? throw new InvalidOperationException("HolidayApiUrl is not configured");
        var url = $"{baseUrl}/{year}/{code}";

        try
        {
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
            HolidayCache[cacheKey] = result;
            return result;
        }
        catch
        {
            return new List<HolidayDto>();
        }
    }

    public async Task<(bool IsHoliday, string? HolidayName)> IsHolidayAsync(DateTime date, string countryCode)
    {
        var holidays = await GetHolidaysAsync(date.Year, countryCode);
        var dateStr = date.ToString("yyyy-MM-dd");

        var holiday = holidays.FirstOrDefault(h => h.Date == dateStr);
        return (holiday != null, holiday?.Name);
    }

    // Changed from private to internal to allow testing via InternalsVisibleTo
    internal static bool IsSchoolHoliday(DateTime date)
    {
        var schoolHolidays = GetSchoolHolidays(date.Year);
        return IsDateInRanges(date, schoolHolidays);
    }

    private static bool IsDateInRanges(DateTime date, List<(DateTime Start, DateTime End)> ranges)
    {
        foreach (var (start, end) in ranges)
        {
            if (date.Date >= start.Date && date.Date <= end.Date)
            {
                return true;
            }
        }
        return false;
    }

    private static DateTime? GetChineseNewYear(int year)
    {
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

    private static List<(DateTime Start, DateTime End)> GetSchoolHolidays(int year)
    {
        var holidays = new List<(DateTime, DateTime)>();

        // Summer holiday (approximate)
        holidays.Add((new DateTime(year, 5, 25), new DateTime(year, 6, 23)));
        
        // End of year holiday
        holidays.Add((new DateTime(year, 11, 16), new DateTime(year, 12, 31)));

        // March holiday
        holidays.Add((new DateTime(year, 3, 9), new DateTime(year, 3, 17)));
        
        // September holiday
        holidays.Add((new DateTime(year, 9, 7), new DateTime(year, 9, 15)));

        return holidays;
    }
}

using SmartSusChef.Api.DTOs;
using System.Text.Json;

namespace SmartSusChef.Api.Services;

public class HolidayService : IHolidayService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    // Cache for holidays to avoid repeated API calls
    private static readonly Dictionary<string, List<HolidayDto>> _holidayCache = new();

    public HolidayService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<List<HolidayDto>> GetHolidaysAsync(int year)
    {
        var cacheKey = $"SG_{year}";
        if (_holidayCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        try
        {
            var baseUrl = _configuration["ExternalApis:HolidayApiUrl"];
            var url = $"{baseUrl}/{year}/SG"; // SG for Singapore

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
            return result;
        }
        catch (Exception)
        {
            // Return mock data if API fails
            var mockHolidays = GetMockHolidays(year);
            _holidayCache[cacheKey] = mockHolidays;
            return mockHolidays;
        }
    }

    public async Task<(bool IsHoliday, string? HolidayName)> IsHolidayAsync(DateTime date, string countryCode)
    {
        var holidays = await GetHolidaysAsync(date.Year);
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

    private static List<HolidayDto> GetMockHolidays(int year)
    {
        return new List<HolidayDto>
        {
            new HolidayDto($"{year}-01-01", "New Year's Day"),
            new HolidayDto($"{year}-02-10", "Chinese New Year"),
            new HolidayDto($"{year}-02-11", "Chinese New Year"),
            new HolidayDto($"{year}-04-10", "Hari Raya Puasa"),
            new HolidayDto($"{year}-05-01", "Labour Day"),
            new HolidayDto($"{year}-05-22", "Vesak Day"),
            new HolidayDto($"{year}-06-17", "Hari Raya Haji"),
            new HolidayDto($"{year}-08-09", "National Day"),
            new HolidayDto($"{year}-11-01", "Deepavali"),
            new HolidayDto($"{year}-12-25", "Christmas Day")
        };
    }
}

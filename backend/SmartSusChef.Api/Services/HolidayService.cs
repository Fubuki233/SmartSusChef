using SmartSusChef.Api.DTOs;
using System.Text.Json;

namespace SmartSusChef.Api.Services;

public class HolidayService : IHolidayService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public HolidayService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<List<HolidayDto>> GetHolidaysAsync(int year)
    {
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

            return holidays.OrderBy(h => h.Date).ToList();
        }
        catch (Exception)
        {
            // Return mock data if API fails
            return GetMockHolidays(year);
        }
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

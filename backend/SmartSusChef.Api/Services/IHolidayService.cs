using SmartSusChef.Api.DTOs;

namespace SmartSusChef.Api.Services;

public interface IHolidayService
{
    Task SyncHolidaysAsync(int year, string countryCode);
    Task<bool> IsHolidayAsync(DateTime date);
    
    // Methods required by ForecastController
    Task<string> GetCountryCodeFromCoordinatesAsync(decimal latitude, decimal longitude);
    Task<List<HolidayDto>> GetHolidaysAsync(int year, string? countryCode = null);
    Task<(bool IsHoliday, string? HolidayName)> IsHolidayAsync(DateTime date, string countryCode);
    bool IsSchoolHoliday(DateTime date);
}

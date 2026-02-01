using SmartSusChef.Api.DTOs;

namespace SmartSusChef.Api.Services;

public interface IHolidayService
{
    Task SyncHolidaysAsync(int year, string countryCode);
    Task<bool> IsHolidayAsync(DateTime date);

    // Methods required by ForecastController
    Task<string> GetCountryCodeFromCoordinatesAsync(decimal latitude, decimal longitude);
    Task<List<HolidayDto>> GetHolidaysAsync(int year, string? countryCode = null);

    /// <summary>
    /// Check if a specific date is a public holiday
    /// </summary>
    Task<(bool IsHoliday, string? HolidayName)> IsHolidayAsync(DateTime date, string countryCode);

    /// <summary>
    /// Check if a specific date is a school holiday
    /// Rules:
    /// 1. July and August are summer holidays
    /// 2. Two weeks before and after Chinese New Year are winter holidays
    /// </summary>
    bool IsSchoolHoliday(DateTime date);
}

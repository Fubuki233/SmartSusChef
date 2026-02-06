using SmartSusChef.Api.DTOs;

namespace SmartSusChef.Api.Services;

public interface IHolidayService
{
    Task SyncHolidaysAsync(int year, string countryCode);
    Task<bool> IsHolidayAsync(DateTime date);

    // Methods required by ForecastController
    Task<List<HolidayDto>> GetHolidaysAsync(int year, string? countryCode = null);

    /// <summary>
    /// Check if a specific date is a public holiday
    /// </summary>
    Task<(bool IsHoliday, string? HolidayName)> IsHolidayAsync(DateTime date, string countryCode);
}

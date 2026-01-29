using SmartSusChef.Api.DTOs;

namespace SmartSusChef.Api.Services;

public interface IHolidayService
{
    /// <summary>
    /// Get holidays for a specific year and country code
    /// </summary>
    Task<List<HolidayDto>> GetHolidaysAsync(int year, string? countryCode = null);

    /// <summary>
    /// Check if a specific date is a public holiday
    /// </summary>
    Task<(bool IsHoliday, string? HolidayName)> IsHolidayAsync(DateTime date, string? countryCode);

    /// <summary>
    /// Check if a specific date is a school holiday
    /// Rules:
    /// 1. July and August are summer holidays
    /// 2. Two weeks before and after Chinese New Year are winter holidays
    /// </summary>
    bool IsSchoolHoliday(DateTime date);

    /// <summary>
    /// Get Chinese New Year date for a specific year
    /// </summary>
    DateTime? GetChineseNewYear(int year);

    /// <summary>
    /// Get country code from latitude and longitude coordinates
    /// </summary>
    Task<string> GetCountryCodeFromCoordinatesAsync(decimal latitude, decimal longitude);
}

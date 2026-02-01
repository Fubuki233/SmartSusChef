using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSusChef.Api.DTOs;
using SmartSusChef.Api.Services;

namespace SmartSusChef.Api.Controllers;

/// <summary>
/// Controller for forecast, weather, and calendar information
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ForecastController : ControllerBase
{
    private readonly IForecastService _forecastService;
    private readonly IWeatherService _weatherService;
    private readonly IHolidayService _holidayService;
    private readonly IStoreService _storeService;

    public ForecastController(
        IForecastService forecastService,
        IWeatherService weatherService,
        IHolidayService holidayService,
        IStoreService storeService)
    {
        _forecastService = forecastService;
        _weatherService = weatherService;
        _holidayService = holidayService;
        _storeService = storeService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ForecastDto>>> GetForecast([FromQuery] int days = 7)
    {
        if (days < 1 || days > 30)
        {
            return BadRequest(new { message = "Days must be between 1 and 30" });
        }

        var forecast = await _forecastService.GetForecastAsync(days);
        return Ok(forecast);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<List<ForecastSummaryDto>>> GetForecastSummary([FromQuery] int days = 7)
    {
        if (days < 1 || days > 30)
        {
            return BadRequest(new { message = "Days must be between 1 and 30" });
        }

        var summary = await _forecastService.GetForecastSummaryAsync(days);
        return Ok(summary);
    }

    [HttpGet("weather")]
    public async Task<ActionResult<WeatherDto>> GetWeather()
    {
        var weather = await _weatherService.GetCurrentWeatherAsync();
        if (weather == null)
        {
            return NoContent();
        }
        return Ok(weather);
    }

    [HttpGet("holidays/{year}")]
    public async Task<ActionResult<List<HolidayDto>>> GetHolidays(int year)
    {
        if (year < 2020 || year > 2030)
        {
            return BadRequest(new { message = "Year must be between 2020 and 2030" });
        }

        // Get store coordinates to determine country
        var store = await _storeService.GetStoreAsync();
        string countryCode;

        if (store == null || (store.Latitude == 0 && store.Longitude == 0))
        {
            return Ok(new List<HolidayDto>());
        }

        if (!string.IsNullOrWhiteSpace(store.CountryCode))
        {
            countryCode = store.CountryCode;
        }
        else
        {
            countryCode = await _holidayService.GetCountryCodeFromCoordinatesAsync(store.Latitude, store.Longitude);
        }
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            return Ok(new List<HolidayDto>());
        }

        var holidays = await _holidayService.GetHolidaysAsync(year, countryCode);
        return Ok(holidays);
    }

    /// <summary>
    /// Get tomorrow's weather forecast and calendar information
    /// Uses store coordinates if available, otherwise defaults to Singapore
    /// </summary>
    [HttpGet("tomorrow")]
    public async Task<ActionResult<TomorrowForecastDto>> GetTomorrowForecast()
    {
        var tomorrow = DateTime.UtcNow.Date.AddDays(1);

        decimal? latitude = null;
        decimal? longitude = null;
        string? countryCode = null;

        var store = await _storeService.GetStoreAsync();
        if (store != null && !(store.Latitude == 0 && store.Longitude == 0))
        {
            latitude = store.Latitude;
            longitude = store.Longitude;
            countryCode = !string.IsNullOrWhiteSpace(store.CountryCode)
                ? store.CountryCode
                : await _holidayService.GetCountryCodeFromCoordinatesAsync(store.Latitude, store.Longitude);
        }

        WeatherForecastDto? weather = null;
        if (latitude.HasValue && longitude.HasValue)
        {
            weather = await _weatherService.GetWeatherForecastAsync(tomorrow, latitude.Value, longitude.Value);
        }

        var calendar = await GetCalendarDayAsync(tomorrow, countryCode, latitude, longitude);

        return Ok(new TomorrowForecastDto(
            tomorrow.ToString("yyyy-MM-dd"),
            calendar,
            weather
        ));
    }

    /// <summary>
    /// Get calendar and weather information for a specific date
    /// </summary>
    [HttpGet("calendar/{date}")]
    public async Task<ActionResult<CalendarDayDto>> GetCalendarDay(string date)
    {
        if (!DateTime.TryParse(date, out var targetDate))
        {
            return BadRequest(new { message = "Invalid date format. Use yyyy-MM-dd" });
        }

        decimal? latitude = null;
        decimal? longitude = null;
        string? countryCode = null;

        var store = await _storeService.GetStoreAsync();
        if (store != null && !(store.Latitude == 0 && store.Longitude == 0))
        {
            latitude = store.Latitude;
            longitude = store.Longitude;
            countryCode = !string.IsNullOrWhiteSpace(store.CountryCode)
                ? store.CountryCode
                : await _holidayService.GetCountryCodeFromCoordinatesAsync(store.Latitude, store.Longitude);
        }

        var calendar = await GetCalendarDayAsync(targetDate, countryCode, latitude, longitude);
        return Ok(calendar);
    }

    /// <summary>
    /// Get calendar and weather information for a date range
    /// </summary>
    [HttpGet("calendar")]
    public async Task<ActionResult<List<CalendarDayDto>>> GetCalendarRange(
        [FromQuery] string startDate,
        [FromQuery] string endDate)
    {
        if (!DateTime.TryParse(startDate, out var start))
        {
            return BadRequest(new { message = "Invalid start date format. Use yyyy-MM-dd" });
        }

        if (!DateTime.TryParse(endDate, out var end))
        {
            return BadRequest(new { message = "Invalid end date format. Use yyyy-MM-dd" });
        }

        if (end < start)
        {
            return BadRequest(new { message = "End date must be after start date" });
        }

        if ((end - start).Days > 30)
        {
            return BadRequest(new { message = "Date range cannot exceed 30 days" });
        }

        decimal? latitude = null;
        decimal? longitude = null;
        string? countryCode = null;

        var store = await _storeService.GetStoreAsync();
        if (store != null && !(store.Latitude == 0 && store.Longitude == 0))
        {
            latitude = store.Latitude;
            longitude = store.Longitude;
            countryCode = !string.IsNullOrWhiteSpace(store.CountryCode)
                ? store.CountryCode
                : await _holidayService.GetCountryCodeFromCoordinatesAsync(store.Latitude, store.Longitude);
        }

        var result = new List<CalendarDayDto>();
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            var calendar = await GetCalendarDayAsync(date, countryCode, latitude, longitude);
            result.Add(calendar);
        }

        return Ok(result);
    }

    /// <summary>
    /// Helper method to build calendar day information
    /// </summary>
    private async Task<CalendarDayDto> GetCalendarDayAsync(
        DateTime date,
        string? countryCode,
        decimal? latitude,
        decimal? longitude)
    {
        bool isHoliday = false;
        string? holidayName = null;
        if (!string.IsNullOrWhiteSpace(countryCode))
        {
            (isHoliday, holidayName) = await _holidayService.IsHolidayAsync(date, countryCode);
        }

        // Check if it's a school holiday
        var isSchoolHoliday = _holidayService.IsSchoolHoliday(date);

        // Check if it's a weekend
        var isWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;

        // Get weather forecast
        WeatherForecastDto? weather = null;
        if (latitude.HasValue && longitude.HasValue)
        {
            weather = await _weatherService.GetWeatherForecastAsync(date, latitude.Value, longitude.Value);
        }

        return new CalendarDayDto(
            date.ToString("yyyy-MM-dd"),
            isHoliday,
            holidayName,
            isSchoolHoliday,
            isWeekend,
            weather
        );
    }
}

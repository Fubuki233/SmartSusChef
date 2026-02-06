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

    // Private record to hold location info for cleaner code
    private record StoreLocationInfo(decimal? Latitude, decimal? Longitude, string? CountryCode);

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
    public async Task<ActionResult<List<ForecastDto>>> GetForecast([FromQuery] int days = 7, [FromQuery] int includePastDays = 0)
    {
        if (days < 1 || days > 30)
        {
            return BadRequest(new { message = "Days must be between 1 and 30" });
        }

        if (includePastDays < 0 || includePastDays > 30)
        {
            return BadRequest(new { message = "includePastDays must be between 0 and 30" });
        }

        var forecast = await _forecastService.GetForecastAsync(days, includePastDays);
        return Ok(forecast);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<List<ForecastSummaryDto>>> GetForecastSummary([FromQuery] int days = 7, [FromQuery] int includePastDays = 0)
    {
        if (days < 1 || days > 30)
        {
            return BadRequest(new { message = "Days must be between 1 and 30" });
        }

        if (includePastDays < 0 || includePastDays > 30)
        {
            return BadRequest(new { message = "includePastDays must be between 0 and 30" });
        }

        var summary = await _forecastService.GetForecastSummaryAsync(days, includePastDays);
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
        
        var locationInfo = await GetStoreLocationInfo();
        if (string.IsNullOrWhiteSpace(locationInfo?.CountryCode))
        {
            return Ok(new List<HolidayDto>());
        }

        var holidays = await _holidayService.GetHolidaysAsync(year, locationInfo.CountryCode);
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
        var locationInfo = await GetStoreLocationInfo();

        var calendar = await GetCalendarDayAsync(tomorrow, locationInfo);

        return Ok(new TomorrowForecastDto(
            tomorrow.ToString("yyyy-MM-dd"),
            calendar,
            calendar.Weather
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

        var locationInfo = await GetStoreLocationInfo();
        var calendar = await GetCalendarDayAsync(targetDate, locationInfo);
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

        var locationInfo = await GetStoreLocationInfo();
        
        var result = new List<CalendarDayDto>();
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            var calendar = await GetCalendarDayAsync(date, locationInfo);
            result.Add(calendar);
        }

        return Ok(result);
    }

    /// <summary>
    /// Helper method to build calendar day information
    /// </summary>
    private async Task<CalendarDayDto> GetCalendarDayAsync(DateTime date, StoreLocationInfo? locationInfo)
    {
        bool isHoliday = false;
        string? holidayName = null;
        if (!string.IsNullOrWhiteSpace(locationInfo?.CountryCode))
        {
            (isHoliday, holidayName) = await _holidayService.IsHolidayAsync(date, locationInfo.CountryCode);
        }

        // IsSchoolHoliday is now internal static, so we call it directly from the class
        var isSchoolHoliday = HolidayService.IsSchoolHoliday(date);
        var isWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;

        WeatherForecastDto? weather = null;
        if (locationInfo?.Latitude.HasValue == true && locationInfo?.Longitude.HasValue == true)
        {
            weather = await _weatherService.GetWeatherForecastAsync(date, locationInfo.Latitude.Value, locationInfo.Longitude.Value);
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
    
    /// <summary>
    /// Helper method to get location information for the current user's store.
    /// </summary>
    private async Task<StoreLocationInfo?> GetStoreLocationInfo()
    {
        var store = await _storeService.GetStoreAsync();
        if (store == null) return null;

        decimal? latitude = (store.Latitude == 0 && store.Longitude == 0) ? null : store.Latitude;
        decimal? longitude = (store.Latitude == 0 && store.Longitude == 0) ? null : store.Longitude;
        string? countryCode = store.CountryCode;

        if (string.IsNullOrWhiteSpace(countryCode) && latitude.HasValue && longitude.HasValue)
        {
            // GetCountryCodeFromCoordinatesAsync is now internal, so we need to cast to the concrete class to access it
            // or use reflection if we want to stick to the interface.
            // However, since we are in the same assembly (or friend assembly), we can just cast.
            // But wait, ForecastController is in SmartSusChef.Api, and HolidayService is also in SmartSusChef.Api.
            // So we can just cast _holidayService to HolidayService to access the internal method.
            if (_holidayService is HolidayService concreteService)
            {
                countryCode = await concreteService.GetCountryCodeFromCoordinatesAsync(latitude.Value, longitude.Value);
            }
        }

        return new StoreLocationInfo(latitude, longitude, countryCode);
    }
}

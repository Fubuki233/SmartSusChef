using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSusChef.Api.DTOs;
using SmartSusChef.Api.Services;

namespace SmartSusChef.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ForecastController : ControllerBase
{
    private readonly IForecastService _forecastService;
    private readonly IWeatherService _weatherService;
    private readonly IHolidayService _holidayService;

    public ForecastController(
        IForecastService forecastService,
        IWeatherService weatherService,
        IHolidayService holidayService)
    {
        _forecastService = forecastService;
        _weatherService = weatherService;
        _holidayService = holidayService;
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
        return Ok(weather);
    }

    [HttpGet("holidays/{year}")]
    public async Task<ActionResult<List<HolidayDto>>> GetHolidays(int year)
    {
        if (year < 2020 || year > 2030)
        {
            return BadRequest(new { message = "Year must be between 2020 and 2030" });
        }

        var holidays = await _holidayService.GetHolidaysAsync(year);
        return Ok(holidays);
    }
}

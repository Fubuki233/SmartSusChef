namespace SmartSusChef.Api.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSusChef.Api.Services;
using SmartSusChef.Api.DTOs;

[Authorize] // Only logged-in managers can see strategic data
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly ISalesService _salesService;
    private readonly IWastageService _wastageService;
    private readonly IHolidayService _holidayService;
    private readonly IWeatherService _weatherService;

    public DashboardController(
        ISalesService salesService, 
        IWastageService wastageService,
        IHolidayService holidayService,
        IWeatherService weatherService)
    {
        _salesService = salesService;
        _wastageService = wastageService;
        _holidayService = holidayService;
        _weatherService = weatherService;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetDashboardSummary()
    {
        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddDays(-29);

        // 1. Fetch the 30-day sales trend you verified in tests
        var salesTrend = await _salesService.GetTrendAsync(startDate, endDate);

        // 2. Calculate the total carbon impact of wastage for this period
        var wastageImpact = await _wastageService.GetTotalWastageImpactAsync(startDate, endDate);

        // 3. Check if today is a holiday to explain any sudden spikes in the chart
        var isHolidayToday = await _holidayService.IsHolidayAsync(endDate);
        
        // 4. Get current weather signal
        var currentWeather = await _weatherService.GetCurrentWeatherAsync();

        return Ok(new 
        {
            SalesTrend = salesTrend,
            TotalWastageCarbonKg = wastageImpact,
            IsHolidayToday = isHolidayToday,
            CurrentWeather = currentWeather,
            Period = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}"
        });
    }
}

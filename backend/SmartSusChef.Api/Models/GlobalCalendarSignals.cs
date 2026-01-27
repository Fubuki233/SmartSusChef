namespace SmartSusChef.Api.Models;

public class GlobalCalendarSignals
{
    public DateTime Date { get; set; }

    public bool IsHoliday { get; set; }
    
    public string HolidayName { get; set; } = string.Empty;

    public bool IsSchoolHoliday { get; set; }

    // ERD specifies decimal for RainMm to track precise rainfall data
    public decimal RainMm { get; set; }

    public string WeatherDesc { get; set; } = string.Empty;
}
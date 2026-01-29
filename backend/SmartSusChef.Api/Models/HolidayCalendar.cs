namespace SmartSusChef.Api.Models;

public class HolidayCalendar
{
    public string CountryCode { get; set; } = string.Empty;
    public int Year { get; set; }
    public string HolidaysJson { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}

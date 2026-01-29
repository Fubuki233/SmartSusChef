namespace SmartSusChef.Api.Models;

public class WeatherDaily
{
    public int StoreId { get; set; }
    public DateTime Date { get; set; }
    public decimal Temperature { get; set; }
    public string Condition { get; set; } = string.Empty;
    public int Humidity { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}

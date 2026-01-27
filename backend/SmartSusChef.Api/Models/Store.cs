namespace SmartSusChef.Api.Models;

/// <summary>
/// Store information (singleton - only one record allowed in system)
/// </summary>
public class Store
{
    /// <summary>
    /// Fixed ID = 1
    /// </summary>
    public int Id { get; set; } = 1;

   
    // Corporate & Identity Fields
    public string CompanyName { get; set; } = string.Empty; //
    public string UEN { get; set; } = string.Empty; //
    public string StoreName { get; set; } = string.Empty;
    public string OutletLocation { get; set; } = string.Empty; 

    // Store Details
    public DateTime OpeningDate { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? Address { get; set; }
    public string ContactNumber { get; set; } = string.Empty; 
    
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties - Inverse relationships for EF Core
    
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Ingredient> Ingredients { get; set; } = new List<Ingredient>();
    public ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
    public ICollection<SalesData> SalesData { get; set; } = new List<SalesData>();
    public ICollection<WastageData> WastageData { get; set; } = new List<WastageData>();
    public ICollection<ForecastData> ForecastData { get; set; } = new List<ForecastData>();
}

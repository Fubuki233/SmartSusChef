using System.ComponentModel.DataAnnotations;

namespace SmartSusChef.Api.Models;

/// <summary>
/// Store information - each manager has their own store
/// </summary>
public class Store
{
    /// <summary>
    /// Auto-increment ID
    /// </summary>
    public int Id { get; set; }


    // Corporate & Identity Fields
    [Required]
    [StringLength(100)]
    public string CompanyName { get; set; } = string.Empty; //

    [Required]
    [StringLength(50)]
    public string UEN { get; set; } = string.Empty; // Increased length for international business reg numbers

    [Required]
    [StringLength(100)]
    public string StoreName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string OutletLocation { get; set; } = string.Empty;

    // Store Details
    public DateTime OpeningDate { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    
    [StringLength(3, MinimumLength = 2)]
    [RegularExpression(@"^[A-Z]{2,3}$", ErrorMessage = "Country code must be 2 or 3 uppercase letters (ISO 3166).")]
    public string? CountryCode { get; set; }
    
    [StringLength(500)]
    public string? Address { get; set; }

    [Required]
    [RegularExpression(@"^\+[1-9]\d{1,14}$", ErrorMessage = "Phone number must be in E.164 format (e.g., +1234567890).")]
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

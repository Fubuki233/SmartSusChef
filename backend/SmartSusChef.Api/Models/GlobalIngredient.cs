using System.ComponentModel.DataAnnotations;

namespace SmartSusChef.Api.Models;

public class GlobalIngredient
{
    public Guid Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Unit { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal CarbonFootprint { get; set; } // kg CO2 per unit

    public bool IsDefault { get; set; } = false;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

namespace SmartSusChef.Api.Models;

public class Ingredient
{
    public Guid Id { get; set; }
    // Foreign Key for the Store
    public int StoreId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal CarbonFootprint { get; set; } // kg CO2 per unit
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    // Link to the Store entity
    public Store Store { get; set; } = null!;
    public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
    public ICollection<WastageData> WastageRecords { get; set; } = new List<WastageData>();
}

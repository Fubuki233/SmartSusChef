namespace SmartSusChef.Api.Models;

public class WastageData
{
    public Guid Id { get; set; }
    public int StoreId { get; set; }
    public DateTime Date { get; set; }
    // Both are nullable so you can log one or the other
    public Guid? IngredientId { get; set; }
    public Guid? RecipeId { get; set; }
    public decimal Quantity { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Store Store { get; set; } = null!;

    // These MUST be nullable (?) because a specific record 
    // might not have one or the other.
    public Ingredient? Ingredient { get; set; } 
    public Recipe? Recipe { get; set; }
}

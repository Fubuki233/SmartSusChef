namespace SmartSusChef.Api.Models;

public class WastageData
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public Guid IngredientId { get; set; }
    public decimal Quantity { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Ingredient Ingredient { get; set; } = null!;
}

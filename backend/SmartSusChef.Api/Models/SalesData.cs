namespace SmartSusChef.Api.Models;

public class SalesData
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public Guid RecipeId { get; set; }
    public int Quantity { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Recipe Recipe { get; set; } = null!;
}

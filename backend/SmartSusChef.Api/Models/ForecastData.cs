namespace SmartSusChef.Api.Models;

public class ForecastData
{
    public Guid Id { get; set; }

    // Linked to a specific Store for multi-tenant isolation
    public int StoreId { get; set; }

    // The specific Recipe being predicted
    public Guid RecipeId { get; set; }

    public DateTime ForecastDate { get; set; }

    public int PredictedQuantity { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Store Store { get; set; } = null!;
    public Recipe Recipe { get; set; } = null!;
}
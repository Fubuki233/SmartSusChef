using System.Text.Json.Serialization;

namespace SmartSusChef.Api.Models;

public class Recipe
{
    public Guid Id { get; set; }
    public int StoreId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public bool IsSubRecipe { get; set; }
    public bool IsSellable { get; set; }

    // Navigation properties
    public Store Store { get; set; } = null!;
    public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
    public ICollection<SalesData> SalesRecords { get; set; } = new List<SalesData>();
    // 1. Tracking "Finished Goods" Waste
    // 2. Machine Learning Benefits
    public ICollection<WastageData> WastageRecords { get; set; } = new List<WastageData>();
}

public class RecipeIngredient
{
    public Guid Id { get; set; }
    public Guid RecipeId { get; set; }
    
    // If IngredientId mandatory (non-nullable),
    // the database will force you to provide an ingredient ID
    // even when you are trying to add a Sub-Recipe
    // to a parent recipe.
    public Guid? IngredientId { get; set; }
    public Guid? ChildRecipeId { get; set; }
    public decimal Quantity { get; set; }

    // Navigation properties
    [JsonIgnore]
    public Recipe Recipe { get; set; } = null!; // YC: advised to add in to avoid circular ref

    public Ingredient? Ingredient { get; set; }
    public Recipe? ChildRecipe { get; set; } // The Sub-Recipe
}

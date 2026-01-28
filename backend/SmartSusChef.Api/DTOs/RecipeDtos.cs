namespace SmartSusChef.Api.DTOs;

public record RecipeDto(
    string Id,
    string Name,
    bool IsSellable,  // Added per final ERD 
    bool IsSubRecipe, // Added per final ERD 
    List<RecipeIngredientDto> Ingredients
);

public record RecipeIngredientDto(
    string? IngredientId,   // Nullable to support Child Recipes 
    string? ChildRecipeId,  // Added to support BOM hierarchy 
    string DisplayName,     // Helper to show either Ingredient Name or Recipe Name
    string Unit,
    decimal Quantity
);

public record CreateRecipeRequest(
    string Name,
    bool IsSellable,
    bool IsSubRecipe,
    List<CreateRecipeIngredientRequest> Ingredients
);

public record CreateRecipeIngredientRequest(
    string? IngredientId,  // Either IngredientId or ChildRecipeId must be provided
    string? ChildRecipeId,
    decimal Quantity
);

public record UpdateRecipeRequest(
    string Name,
    bool IsSellable,
    bool IsSubRecipe,
    List<CreateRecipeIngredientRequest> Ingredients
);
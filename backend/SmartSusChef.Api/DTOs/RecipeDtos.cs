namespace SmartSusChef.Api.DTOs;

public record RecipeDto(
    string Id,
    string Name,
    bool IsSellable,
    bool IsSubRecipe,
    List<RecipeIngredientDto> Ingredients,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record RecipeIngredientDto(
    string? IngredientId,
    string? ChildRecipeId,
    string DisplayName,
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
    string? IngredientId,
    string? ChildRecipeId,
    decimal Quantity
);

public record UpdateRecipeRequest(
    string Name,
    bool IsSellable,
    bool IsSubRecipe,
    List<CreateRecipeIngredientRequest> Ingredients
);
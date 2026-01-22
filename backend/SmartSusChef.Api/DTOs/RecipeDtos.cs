namespace SmartSusChef.Api.DTOs;

public record RecipeDto(
    string Id,
    string Name,
    List<RecipeIngredientDto> Ingredients
);

public record RecipeIngredientDto(
    string IngredientId,
    string IngredientName,
    string Unit,
    decimal Quantity
);

public record CreateRecipeRequest(
    string Name,
    List<CreateRecipeIngredientRequest> Ingredients
);

public record CreateRecipeIngredientRequest(
    string IngredientId,
    decimal Quantity
);

public record UpdateRecipeRequest(
    string Name,
    List<CreateRecipeIngredientRequest> Ingredients
);

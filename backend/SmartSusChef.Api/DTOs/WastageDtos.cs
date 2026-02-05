namespace SmartSusChef.Api.DTOs;

public record WastageDataDto(
    string Id,
    string Date,
    string? IngredientId,
    string? RecipeId,
    string DisplayName, // Ingredient Name or Recipe Name
    string Unit,
    decimal Quantity,
    decimal CarbonFootprint,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreateWastageDataRequest(
    string Date,
    string? IngredientId,
    string? RecipeId,
    decimal Quantity
);

public record UpdateWastageDataRequest(
    string Date,
    string? IngredientId,
    string? RecipeId,
    decimal Quantity
);

public record WastageTrendDto(
    string Date,
    decimal TotalQuantity,
    decimal TotalCarbonFootprint,
    List<ItemWastageDto> ItemBreakdown
);

public record ItemWastageDto(
    string? IngredientId,
    string? RecipeId,
    string DisplayName,
    string Unit,
    decimal Quantity,
    decimal CarbonFootprint
);

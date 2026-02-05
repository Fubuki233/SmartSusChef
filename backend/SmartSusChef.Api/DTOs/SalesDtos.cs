namespace SmartSusChef.Api.DTOs;

public record SalesDataDto(
    string Id,
    string Date,
    string RecipeId,
    string RecipeName,
    int Quantity,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreateSalesDataRequest(
    string Date,
    string RecipeId,
    int Quantity
);

public record UpdateSalesDataRequest(
    int Quantity
);

public record SalesTrendDto(
    string Date,
    int TotalQuantity,
    List<RecipeSalesDto> RecipeBreakdown
);

public record RecipeSalesDto(
    string RecipeId,
    string RecipeName,
    int Quantity
);

public record IngredientUsageDto(
    string IngredientId,
    string IngredientName,
    string Unit,
    decimal Quantity
);

public record ImportSalesDataRequest(
    List<CreateSalesDataRequest> SalesData
);

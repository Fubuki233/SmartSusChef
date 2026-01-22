namespace SmartSusChef.Api.DTOs;

public record WastageDataDto(
    string Id,
    string Date,
    string IngredientId,
    string IngredientName,
    string Unit,
    decimal Quantity,
    decimal CarbonFootprint
);

public record CreateWastageDataRequest(
    string Date,
    string IngredientId,
    decimal Quantity
);

public record UpdateWastageDataRequest(
    string Date,
    string IngredientId,
    decimal Quantity
);

public record WastageTrendDto(
    string Date,
    decimal TotalQuantity,
    decimal TotalCarbonFootprint,
    List<IngredientWastageDto> IngredientBreakdown
);

public record IngredientWastageDto(
    string IngredientId,
    string IngredientName,
    string Unit,
    decimal Quantity,
    decimal CarbonFootprint
);

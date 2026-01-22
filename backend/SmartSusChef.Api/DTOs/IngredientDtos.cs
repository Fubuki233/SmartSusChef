namespace SmartSusChef.Api.DTOs;

public record IngredientDto(
    string Id,
    string Name,
    string Unit,
    decimal CarbonFootprint
);

public record CreateIngredientRequest(
    string Name,
    string Unit,
    decimal CarbonFootprint
);

public record UpdateIngredientRequest(
    string Name,
    string Unit,
    decimal CarbonFootprint
);

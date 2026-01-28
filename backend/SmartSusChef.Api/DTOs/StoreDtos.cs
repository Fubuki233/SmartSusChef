namespace SmartSusChef.Api.DTOs;

public record StoreDto(
    int Id,
    string CompanyName,
    string UEN,
    string StoreName,
    string OutletLocation,
    string ContactNumber,
    DateTime OpeningDate,
    decimal Latitude,
    decimal Longitude,
    string? Address,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreateStoreRequest(
    string CompanyName,
    string UEN,
    string StoreName,
    string OutletLocation,
    string ContactNumber,
    DateTime OpeningDate,
    decimal Latitude,
    decimal Longitude,
    string? Address,
    bool IsActive = true
);

public record UpdateStoreRequest(
    string? CompanyName,
    string? UEN,
    string? StoreName,
    string? OutletLocation,
    string? ContactNumber,
    DateTime? OpeningDate,
    decimal? Latitude,
    decimal? Longitude,
    string? Address,
    bool? IsActive
);

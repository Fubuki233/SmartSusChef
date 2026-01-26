namespace SmartSusChef.Api.DTOs;

public record StoreDto(
    int Id,
    string StoreName,
    DateTime OpeningDate,
    decimal Latitude,
    decimal Longitude,
    string? Address,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreateStoreRequest(
    string StoreName,
    DateTime OpeningDate,
    decimal Latitude,
    decimal Longitude,
    string? Address,
    bool IsActive = true
);

public record UpdateStoreRequest(
    string? StoreName,
    DateTime? OpeningDate,
    decimal? Latitude,
    decimal? Longitude,
    string? Address,
    bool? IsActive
);

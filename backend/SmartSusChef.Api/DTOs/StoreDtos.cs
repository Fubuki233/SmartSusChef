using System.ComponentModel.DataAnnotations;

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
    string? CountryCode,
    string? Address,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreateStoreRequest(
    [Required][StringLength(100)] string CompanyName,
    [Required][StringLength(50)] string UEN,
    [Required][StringLength(100)] string StoreName,
    [Required][StringLength(100)] string OutletLocation,
    [Required][RegularExpression(@"^\+[1-9]\d{1,14}$", ErrorMessage = "Phone number must be in E.164 format (e.g., +1234567890).")] string ContactNumber,
    DateTime OpeningDate,
    decimal Latitude,
    decimal Longitude,
    [StringLength(3, MinimumLength = 2)][RegularExpression(@"^[A-Z]{2,3}$", ErrorMessage = "Country code must be 2 or 3 uppercase letters (ISO 3166).")] string? CountryCode,
    [StringLength(500)] string? Address,
    bool IsActive = true
);

public record UpdateStoreRequest(
    [StringLength(100)] string? CompanyName,
    [StringLength(50)] string? UEN,
    [StringLength(100)] string? StoreName,
    [StringLength(100)] string? OutletLocation,
    [RegularExpression(@"^\+[1-9]\d{1,14}$", ErrorMessage = "Phone number must be in E.164 format (e.g., +1234567890).")] string? ContactNumber,
    DateTime? OpeningDate,
    decimal? Latitude,
    decimal? Longitude,
    [StringLength(3, MinimumLength = 2)][RegularExpression(@"^[A-Z]{2,3}$", ErrorMessage = "Country code must be 2 or 3 uppercase letters (ISO 3166).")] string? CountryCode,
    [StringLength(500)] string? Address,
    bool? IsActive
);

namespace SmartSusChef.Api.Models;

/// <summary>
/// Store information (singleton - only one record allowed in system)
/// </summary>
public class Store
{
    /// <summary>
    /// Fixed ID = 1
    /// </summary>
    public int Id { get; set; } = 1;

    /// <summary>
    /// Store name
    /// </summary>
    public string StoreName { get; set; } = string.Empty;

    /// <summary>
    /// Opening date
    /// </summary>
    public DateTime OpeningDate { get; set; }

    /// <summary>
    /// Latitude (-90 to 90)
    /// </summary>
    public decimal Latitude { get; set; }

    /// <summary>
    /// Longitude (-180 to 180)
    /// </summary>
    public decimal Longitude { get; set; }

    /// <summary>
    /// Detailed address (optional)
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Is the store currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

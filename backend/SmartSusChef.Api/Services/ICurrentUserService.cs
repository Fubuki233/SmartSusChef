namespace SmartSusChef.Api.Services;

/// <summary>
/// Service to access the current authenticated user's context
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current user's Store ID from the JWT token
    /// </summary>
    int StoreId { get; }

    /// <summary>
    /// Gets the current user's ID from the JWT token
    /// </summary>
    Guid UserId { get; }

    /// <summary>
    /// Gets the current user's role
    /// </summary>
    string Role { get; }

    /// <summary>
    /// Checks if a user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }
}

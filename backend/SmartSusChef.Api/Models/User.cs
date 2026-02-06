using System.ComponentModel.DataAnnotations;

namespace SmartSusChef.Api.Models;

public class User
{
    public Guid Id { get; set; }
    
    public int StoreId { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 3)]
    [RegularExpression(@"^[a-zA-Z0-9._-]+$", ErrorMessage = "Username can only contain letters, numbers, periods, underscores, and hyphens.")]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Invalid email format.")]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    // Updated regex to support international names (Unicode characters)
    // \p{L} matches any unicode letter, \p{M} matches marks (accents)
    [RegularExpression(@"^[\p{L}\p{M}\s\-\']+$", ErrorMessage = "Name can only contain letters, spaces, hyphens, and apostrophes.")]
    public string Name { get; set; } = string.Empty;

    public UserRole Role { get; set; }
    
    // Tracks if the user is currently Active or Inactive
    [RegularExpression("^(Active|Inactive)$", ErrorMessage = "Status must be either 'Active' or 'Inactive'.")]
    public string UserStatus { get; set; } = "Active";
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation property
    // Every user belongs to a specific store
    public Store Store { get; set; } = null!;
}

public enum UserRole
{
    Employee,
    Manager
}

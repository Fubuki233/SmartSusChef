namespace SmartSusChef.Api.Models;

public class User
{
    public Guid Id { get; set; }
    
    public int StoreId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public UserRole Role { get; set; }
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

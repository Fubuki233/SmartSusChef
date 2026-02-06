using System.ComponentModel.DataAnnotations;

namespace SmartSusChef.Api.DTOs;

public record LoginRequest(
    [Required] string Username,
    [Required] string Password
);

public record LoginResponse(string Token, UserDto User, bool StoreSetupRequired);

public record UserDto(
    string Id,
    string Username,
    string Name,
    string Email,
    string Role,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

// Registration DTOs
public record RegisterManagerRequest(
    [Required][StringLength(50, MinimumLength = 3)] string Username,
    [Required][RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{12,}$", ErrorMessage = "Password must be at least 12 characters long and contain at least one uppercase letter, one lowercase letter, one number, and one special character.")] string Password,
    [Required][StringLength(100, MinimumLength = 2)][RegularExpression(@"^[\p{L}\p{M}\s\-\']+$", ErrorMessage = "Name can only contain letters, spaces, hyphens, and apostrophes.")] string Name,
    [Required][EmailAddress][RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Invalid email format.")] string Email
);

public record RegisterResponse(string Token, UserDto User, bool StoreSetupRequired);

// Registration result for better error handling
public enum RegisterErrorType
{
    None,
    UsernameExists,
    ManagerAlreadyExists
}

public record RegisterResult(RegisterResponse? Response, RegisterErrorType ErrorType);

// User management DTOs
public record CreateUserRequest(
    [Required][StringLength(50, MinimumLength = 3)] string Username,
    [Required][RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{12,}$", ErrorMessage = "Password must be at least 12 characters long and contain at least one uppercase letter, one lowercase letter, one number, and one special character.")] string Password,
    [Required][StringLength(100, MinimumLength = 2)][RegularExpression(@"^[\p{L}\p{M}\s\-\']+$", ErrorMessage = "Name can only contain letters, spaces, hyphens, and apostrophes.")] string Name,
    [Required][EmailAddress][RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Invalid email format.")] string Email,
    [Required] string Role // "manager" or "employee"
);

public record UpdateUserRequest(
    [StringLength(50, MinimumLength = 3)] string? Username,
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{12,}$", ErrorMessage = "Password must be at least 12 characters long and contain at least one uppercase letter, one lowercase letter, one number, and one special character.")] string? Password,
    [StringLength(100, MinimumLength = 2)][RegularExpression(@"^[\p{L}\p{M}\s\-\']+$", ErrorMessage = "Name can only contain letters, spaces, hyphens, and apostrophes.")] string? Name,
    [EmailAddress][RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Invalid email format.")] string? Email,
    string? Role,
    string? Status // "Active" or "Inactive"
);

public record UpdateProfileRequest(
    [StringLength(100, MinimumLength = 2)][RegularExpression(@"^[\p{L}\p{M}\s\-\']+$", ErrorMessage = "Name can only contain letters, spaces, hyphens, and apostrophes.")] string? Name,
    [EmailAddress][RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Invalid email format.")] string? Email
);

public record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required][RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{12,}$", ErrorMessage = "Password must be at least 12 characters long and contain at least one uppercase letter, one lowercase letter, one number, and one special character.")] string NewPassword
);

public record ForgotPasswordRequest(
    [Required] string EmailOrUsername
);

public record ForgotPasswordResponse(
    string TemporaryPassword
);

public record UserListDto(
    string Id,
    string Username,
    string Name,
    string Email,
    string Role,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

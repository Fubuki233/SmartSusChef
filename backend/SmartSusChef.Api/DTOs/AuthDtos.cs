namespace SmartSusChef.Api.DTOs;

public record LoginRequest(string Username, string Password);

public record LoginResponse(string Token, UserDto User, bool StoreSetupRequired);

public record UserDto(
    string Id,
    string Username,
    string Name,
    string Email,
    string Role,
    string Status
);

// Registration DTOs
public record RegisterManagerRequest(
    string Username,
    string Password,
    string Name,
    string Email
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
    string Username,
    string Password,
    string Name,
    string Email,
    string Role // "manager" or "employee"
);

public record UpdateUserRequest(
    string? Username,
    string? Password,
    string? Name,
    string? Email,
    string? Role,
    string? Status // "Active" or "Inactive"
);

public record UpdateProfileRequest(
    string? Name,
    string? Email
);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);

public record ForgotPasswordRequest(
    string EmailOrUsername
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
    DateTime CreatedAt
);

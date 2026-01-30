using SmartSusChef.Api.DTOs;

namespace SmartSusChef.Api.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<UserDto?> GetUserByIdAsync(Guid userId);
    Task<RegisterResult> RegisterManagerAsync(RegisterManagerRequest request);
    Task<UserListDto?> CreateUserAsync(CreateUserRequest request, int storeId);
    Task<List<UserListDto>> GetAllUsersAsync(int storeId);
    Task<UserListDto?> UpdateUserAsync(Guid userId, UpdateUserRequest request);
    Task<UserDto?> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
    Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
    Task<string?> ResetPasswordAsync(string emailOrUsername);
    Task<bool> DeleteUserAsync(Guid userId);
    Task<bool> IsStoreSetupRequiredAsync(Guid userId);
}

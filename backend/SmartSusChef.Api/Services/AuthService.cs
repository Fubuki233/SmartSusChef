using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmartSusChef.Api.Data;
using SmartSusChef.Api.DTOs;
using SmartSusChef.Api.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SmartSusChef.Api.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users
            .Include(u => u.Store)
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        var token = GenerateJwtToken(user);
        var userDto = new UserDto(
            user.Id.ToString(),
            user.Username,
            user.Name,
            user.Email,
            user.Role.ToString().ToLower(),
            user.UserStatus
        );

        // Check if store setup is required (empty store name means not set up)
        var storeSetupRequired = string.IsNullOrEmpty(user.Store?.StoreName);

        return new LoginResponse(token, userDto, storeSetupRequired);
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return null;

        return new UserDto(
            user.Id.ToString(),
            user.Username,
            user.Name,
            user.Email,
            user.Role.ToString().ToLower(),
            user.UserStatus
        );
    }

    public async Task<RegisterResult> RegisterManagerAsync(RegisterManagerRequest request)
    {
        // Check if username already exists
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (existingUser != null)
        {
            return new RegisterResult(null, RegisterErrorType.UsernameExists);
        }

        // Create new empty store for this manager (multi-store system)
        var store = new Store
        {
            // Id is auto-generated
            CompanyName = "",
            UEN = "",
            StoreName = "", // Empty - indicates setup required
            OutletLocation = "",
            ContactNumber = "",
            OpeningDate = DateTime.UtcNow,
            Latitude = 1.3521m, // Default Singapore coordinates
            Longitude = 103.8198m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Store.Add(store);
        await _context.SaveChangesAsync();

        // Create manager user
        var newUser = new User
        {
            Id = Guid.NewGuid(),
            StoreId = store.Id,
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Name = request.Name,
            Email = request.Email,
            Role = UserRole.Manager,
            UserStatus = "Active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(newUser);
        var userDto = new UserDto(
            newUser.Id.ToString(),
            newUser.Username,
            newUser.Name,
            newUser.Email,
            newUser.Role.ToString().ToLower(),
            newUser.UserStatus
        );

        // storeSetupRequired is true for new stores (empty StoreName)
        return new RegisterResult(new RegisterResponse(token, userDto, true), RegisterErrorType.None);
    }

    public async Task<UserListDto?> CreateUserAsync(CreateUserRequest request, int storeId)
    {
        // Check if username already exists (case-insensitive)
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == request.Username.ToLower());
        if (existingUser != null)
        {
            return null;
        }

        var role = request.Role.ToLower() == "manager" ? UserRole.Manager : UserRole.Employee;

        var user = new User
        {
            Id = Guid.NewGuid(),
            StoreId = storeId,
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Name = request.Name,
            Email = request.Email,
            Role = role,
            UserStatus = "Active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        try
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("Duplicate entry") == true)
        {
            // Handle race condition where username was created between check and insert
            return null;
        }

        return new UserListDto(
            user.Id.ToString(),
            user.Username,
            user.Name,
            user.Email,
            user.Role.ToString().ToLower(),
            user.UserStatus,
            user.CreatedAt
        );
    }

    public async Task<List<UserListDto>> GetAllUsersAsync(int storeId)
    {
        var users = await _context.Users
            .Where(u => u.StoreId == storeId)
            .OrderBy(u => u.CreatedAt)
            .ToListAsync();

        return users.Select(u => new UserListDto(
            u.Id.ToString(),
            u.Username,
            u.Name,
            u.Email,
            u.Role.ToString().ToLower(),
            u.UserStatus,
            u.CreatedAt
        )).ToList();
    }

    public async Task<UserListDto?> UpdateUserAsync(Guid userId, UpdateUserRequest request)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return null;

        if (!string.IsNullOrEmpty(request.Username))
        {
            // Check if new username is taken by another user
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username && u.Id != userId);
            if (existingUser != null) return null;
            user.Username = request.Username;
        }

        if (!string.IsNullOrEmpty(request.Password))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        if (!string.IsNullOrEmpty(request.Name))
            user.Name = request.Name;

        if (!string.IsNullOrEmpty(request.Email))
            user.Email = request.Email;

        if (!string.IsNullOrEmpty(request.Role))
            user.Role = request.Role.ToLower() == "manager" ? UserRole.Manager : UserRole.Employee;

        if (!string.IsNullOrEmpty(request.Status))
            user.UserStatus = request.Status;

        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new UserListDto(
            user.Id.ToString(),
            user.Username,
            user.Name,
            user.Email,
            user.Role.ToString().ToLower(),
            user.UserStatus,
            user.CreatedAt
        );
    }

    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IsStoreSetupRequiredAsync(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.Store)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return true;

        return string.IsNullOrEmpty(user.Store?.StoreName);
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("StoreId", user.StoreId.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpiryMinutes"])),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

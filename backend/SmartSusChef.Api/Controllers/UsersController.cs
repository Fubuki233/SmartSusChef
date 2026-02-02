using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSusChef.Api.DTOs;
using SmartSusChef.Api.Services;
using System.Security.Claims;

namespace SmartSusChef.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IAuthService _authService;

    public UsersController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Get all users in the store (Manager only)
    /// </summary>
    [HttpGet]
    // Removed [Authorize(Roles = "Manager")] to prevent 403 loops for Employees
    public async Task<ActionResult<List<UserListDto>>> GetAllUsers()
    {
        var storeId = GetStoreIdFromClaims();
        if (storeId == null)
        {
            return BadRequest(new { message = "Store ID not found in token" });
        }

        var users = await _authService.GetAllUsersAsync(storeId.Value);
        return Ok(users);
    }

    /// Create a new user with a mandatory initial password (Manager only)
    
    [HttpPost]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<UserListDto>> CreateUser([FromBody] CreateUserRequest request)
    {
        // 1. Validate that a password was actually provided
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Initial password is required for new users." });
        }

        // 2. Validate password length 
        if (request.Password.Length < 6)
        {
            return BadRequest(new { message = "Password must be at least 6 characters long." });
        }

        var storeId = GetStoreIdFromClaims();
        if (storeId == null)
        {
            return BadRequest(new { message = "Store ID not found in token" });
        }

        // 3. Pass the request (now containing the password) to the service
        // Ensure AuthService.CreateUserAsync hashes this password using BCrypt
        var user = await _authService.CreateUserAsync(request, storeId.Value);

        if (user == null)
        {
            return Conflict(new { message = "Username already exists" });
        }

        return CreatedAtAction(nameof(GetAllUsers), user);
    }

    /// <summary>
    /// Update an existing user (Manager only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<UserListDto>> UpdateUser(string id, [FromBody] UpdateUserRequest request)
    {
        if (!Guid.TryParse(id, out var userId))
        {
            return BadRequest(new { message = "Invalid user ID" });
        }

        var user = await _authService.UpdateUserAsync(userId, request);

        if (user == null)
        {
            return NotFound(new { message = "User not found or username already taken" });
        }

        return Ok(user);
    }

    /// <summary>
    /// Delete a user (Manager only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult> DeleteUser(string id)
    {
        if (!Guid.TryParse(id, out var userId))
        {
            return BadRequest(new { message = "Invalid user ID" });
        }

        // Prevent deleting own account
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (currentUserId == id)
        {
            return BadRequest(new { message = "Cannot delete your own account" });
        }

        var result = await _authService.DeleteUserAsync(userId);

        if (!result)
        {
            return NotFound(new { message = "User not found" });
        }

        return NoContent();
    }

    private int? GetStoreIdFromClaims()
    {
        var storeIdClaim = User.FindFirst("StoreId")?.Value;
        if (int.TryParse(storeIdClaim, out var storeId))
        {
            return storeId;
        }
        return null;
    }
}

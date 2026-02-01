using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSusChef.Api.DTOs;
using SmartSusChef.Api.Services;

namespace SmartSusChef.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Register a new manager account with a new empty store
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterManagerRequest request)
    {
        try
        {
            var result = await _authService.RegisterManagerAsync(request);

            if (result.Response == null)
            {
                return result.ErrorType switch
                {
                    RegisterErrorType.UsernameExists => Conflict(new { message = "Username already exists. Please choose a different username." }),
                    _ => BadRequest(new { message = "Registration failed" })
                };
            }

            return CreatedAtAction(nameof(GetCurrentUser), result.Response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Registration failed: {ex.Message}" });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAsync(request);

        if (response == null)
        {
            return Unauthorized(new { message = "Invalid username or password" });
        }

        return Ok(response);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ForgotPasswordResponse>> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EmailOrUsername))
        {
            return BadRequest(new { message = "Email or username is required" });
        }

        var tempPassword = await _authService.ResetPasswordAsync(request.EmailOrUsername);
        if (tempPassword == null)
        {
            return NotFound(new { message = "User not found" });
        }

        return Ok(new ForgotPasswordResponse(tempPassword));
    }

    /// <summary>
    /// Check if store setup is required for current user
    /// </summary>
    [HttpGet("store-setup-required")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<ActionResult<object>> CheckStoreSetupRequired()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (userId == null || !Guid.TryParse(userId, out var userGuid))
        {
            return Unauthorized();
        }

        var required = await _authService.IsStoreSetupRequiredAsync(userGuid);
        return Ok(new { storeSetupRequired = required });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (userId == null || !Guid.TryParse(userId, out var userGuid))
        {
            return Unauthorized();
        }

        var user = await _authService.GetUserByIdAsync(userGuid);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult<UserDto>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (userId == null || !Guid.TryParse(userId, out var userGuid))
        {
            return Unauthorized();
        }

        var updated = await _authService.UpdateProfileAsync(userGuid, request);

        if (updated == null)
        {
            return NotFound();
        }

        return Ok(updated);
    }

    [HttpPut("password")]
    [Authorize]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (userId == null || !Guid.TryParse(userId, out var userGuid))
        {
            return Unauthorized();
        }

        var success = await _authService.ChangePasswordAsync(userGuid, request.CurrentPassword, request.NewPassword);
        if (!success)
        {
            return BadRequest(new { message = "Current password is incorrect" });
        }

        return NoContent();
    }
}

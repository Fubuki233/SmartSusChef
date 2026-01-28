using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSusChef.Api.DTOs;
using SmartSusChef.Api.Services;
using System.Security.Claims;

namespace SmartSusChef.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StoreController : ControllerBase
{
    private readonly IStoreService _storeService;

    public StoreController(IStoreService storeService)
    {
        _storeService = storeService;
    }

    /// <summary>
    /// Get store information for current user
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<StoreDto>> GetStore()
    {
        var storeId = GetStoreIdFromClaims();
        if (storeId == null)
        {
            return BadRequest(new { message = "Store ID not found in token" });
        }

        var store = await _storeService.GetStoreByIdAsync(storeId.Value);

        if (store == null)
        {
            return NotFound(new { message = "Store not found" });
        }

        return Ok(store);
    }

    /// <summary>
    /// Check if store setup is complete
    /// </summary>
    [HttpGet("status")]
    [Authorize]
    public async Task<ActionResult<object>> GetStoreStatus()
    {
        var storeId = GetStoreIdFromClaims();
        if (storeId == null)
        {
            return BadRequest(new { message = "Store ID not found in token" });
        }

        var isSetupComplete = await _storeService.IsStoreSetupCompleteAsync(storeId.Value);
        return Ok(new { isSetupComplete, storeSetupRequired = !isSetupComplete });
    }

    /// <summary>
    /// Setup/Update store information (Manager only)
    /// This is used for initial setup after registration
    /// </summary>
    [HttpPost("setup")]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<StoreDto>> SetupStore([FromBody] UpdateStoreRequest request)
    {
        var storeId = GetStoreIdFromClaims();
        if (storeId == null)
        {
            return BadRequest(new { message = "Store ID not found in token" });
        }

        // Validate required fields for initial setup
        if (string.IsNullOrEmpty(request.StoreName))
        {
            return BadRequest(new { message = "Store name is required for setup" });
        }

        var store = await _storeService.UpdateStoreByIdAsync(storeId.Value, request);

        if (store == null)
        {
            return NotFound(new { message = "Store not found" });
        }

        return Ok(store);
    }

    /// <summary>
    /// Update store information (Manager only)
    /// </summary>
    [HttpPut]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<StoreDto>> UpdateStore([FromBody] UpdateStoreRequest request)
    {
        var storeId = GetStoreIdFromClaims();
        if (storeId == null)
        {
            return BadRequest(new { message = "Store ID not found in token" });
        }

        var store = await _storeService.UpdateStoreByIdAsync(storeId.Value, request);

        if (store == null)
        {
            return NotFound(new { message = "Store not found" });
        }

        return Ok(store);
    }

    /// <summary>
    /// Initialize store information (Legacy - can only be called once)
    /// </summary>
    [HttpPost("initialize")]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<StoreDto>> InitializeStore([FromBody] CreateStoreRequest request)
    {
        try
        {
            var store = await _storeService.InitializeStoreAsync(request);
            return CreatedAtAction(nameof(GetStore), store);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
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

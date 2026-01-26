using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSusChef.Api.DTOs;
using SmartSusChef.Api.Services;

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
    /// Get store information
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<StoreDto>> GetStore()
    {
        var store = await _storeService.GetStoreAsync();

        if (store == null)
        {
            return NotFound(new { message = "Store has not been initialized yet" });
        }

        return Ok(store);
    }

    /// <summary>
    /// Check if store has been initialized
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<object>> GetStoreStatus()
    {
        var isInitialized = await _storeService.IsStoreInitializedAsync();
        return Ok(new { isInitialized });
    }

    /// <summary>
    /// Initialize store information (can only be called once)
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

    /// <summary>
    /// Update store information
    /// </summary>
    [HttpPut]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<StoreDto>> UpdateStore([FromBody] UpdateStoreRequest request)
    {
        var store = await _storeService.UpdateStoreAsync(request);

        if (store == null)
        {
            return NotFound(new { message = "Store has not been initialized yet. Please initialize first." });
        }

        return Ok(store);
    }
}

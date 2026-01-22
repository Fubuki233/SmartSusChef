using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSusChef.Api.DTOs;
using SmartSusChef.Api.Services;

namespace SmartSusChef.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WastageController : ControllerBase
{
    private readonly IWastageService _wastageService;

    public WastageController(IWastageService wastageService)
    {
        _wastageService = wastageService;
    }

    [HttpGet]
    public async Task<ActionResult<List<WastageDataDto>>> GetAll(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var wastageData = await _wastageService.GetAllAsync(startDate, endDate);
        return Ok(wastageData);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<WastageDataDto>> GetById(Guid id)
    {
        var wastageData = await _wastageService.GetByIdAsync(id);

        if (wastageData == null)
        {
            return NotFound();
        }

        return Ok(wastageData);
    }

    [HttpGet("trend")]
    public async Task<ActionResult<List<WastageTrendDto>>> GetTrend(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var trend = await _wastageService.GetTrendAsync(startDate, endDate);
        return Ok(trend);
    }

    [HttpPost]
    public async Task<ActionResult<WastageDataDto>> Create([FromBody] CreateWastageDataRequest request)
    {
        var wastageData = await _wastageService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = wastageData.Id }, wastageData);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<WastageDataDto>> Update(Guid id, [FromBody] UpdateWastageDataRequest request)
    {
        var wastageData = await _wastageService.UpdateAsync(id, request);

        if (wastageData == null)
        {
            return NotFound();
        }

        return Ok(wastageData);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _wastageService.DeleteAsync(id);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }
}

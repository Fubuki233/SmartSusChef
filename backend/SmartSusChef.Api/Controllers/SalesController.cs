using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSusChef.Api.DTOs;
using SmartSusChef.Api.Services;

namespace SmartSusChef.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SalesController : ControllerBase
{
    private readonly ISalesService _salesService;

    public SalesController(ISalesService salesService)
    {
        _salesService = salesService;
    }

    [HttpGet]
    public async Task<ActionResult<List<SalesDataDto>>> GetAll(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var salesData = await _salesService.GetAllAsync(startDate, endDate);
        return Ok(salesData);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SalesDataDto>> GetById(Guid id)
    {
        var salesData = await _salesService.GetByIdAsync(id);

        if (salesData == null)
        {
            return NotFound();
        }

        return Ok(salesData);
    }

    [HttpGet("trend")]
    public async Task<ActionResult<List<SalesTrendDto>>> GetTrend(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var trend = await _salesService.GetTrendAsync(startDate, endDate);
        return Ok(trend);
    }

    [HttpGet("ingredients/{date}")]
    public async Task<ActionResult<List<IngredientUsageDto>>> GetIngredientUsageByDate(DateTime date)
    {
        var usage = await _salesService.GetIngredientUsageByDateAsync(date);
        return Ok(usage);
    }

    [HttpGet("recipes/{date}")]
    public async Task<ActionResult<List<RecipeSalesDto>>> GetRecipeSalesByDate(DateTime date)
    {
        var sales = await _salesService.GetRecipeSalesByDateAsync(date);
        return Ok(sales);
    }

    [HttpPost]
    public async Task<ActionResult<SalesDataDto>> Create([FromBody] CreateSalesDataRequest request)
    {
        var salesData = await _salesService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = salesData.Id }, salesData);
    }

    [HttpPost("import")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Import([FromBody] ImportSalesDataRequest request)
    {
        await _salesService.ImportAsync(request.SalesData);
        return Ok(new { message = "Sales data imported successfully", count = request.SalesData.Count });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<SalesDataDto>> Update(Guid id, [FromBody] UpdateSalesDataRequest request)
    {
        var salesData = await _salesService.UpdateAsync(id, request);

        if (salesData == null)
        {
            return NotFound();
        }

        return Ok(salesData);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _salesService.DeleteAsync(id);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }
}

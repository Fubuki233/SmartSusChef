using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSusChef.Api.DTOs;
using SmartSusChef.Api.Services;

namespace SmartSusChef.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class IngredientsController : ControllerBase
{
    private readonly IIngredientService _ingredientService;

    public IngredientsController(IIngredientService ingredientService)
    {
        _ingredientService = ingredientService;
    }

    [HttpGet]
    public async Task<ActionResult<List<IngredientDto>>> GetAll()
    {
        var ingredients = await _ingredientService.GetAllAsync();
        return Ok(ingredients);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<IngredientDto>> GetById(Guid id)
    {
        var ingredient = await _ingredientService.GetByIdAsync(id);

        if (ingredient == null)
        {
            return NotFound();
        }

        return Ok(ingredient);
    }

    [HttpPost]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<IngredientDto>> Create([FromBody] CreateIngredientRequest request)
    {
        var ingredient = await _ingredientService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = ingredient.Id }, ingredient);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<IngredientDto>> Update(Guid id, [FromBody] UpdateIngredientRequest request)
    {
        var ingredient = await _ingredientService.UpdateAsync(id, request);

        if (ingredient == null)
        {
            return NotFound();
        }

        return Ok(ingredient);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _ingredientService.DeleteAsync(id);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }
}

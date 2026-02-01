using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSusChef.Api.DTOs;
using SmartSusChef.Api.Services;

namespace SmartSusChef.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RecipesController : ControllerBase
{
    private readonly IRecipeService _recipeService;

    public RecipesController(IRecipeService recipeService)
    {
        _recipeService = recipeService;
    }

    [HttpGet]
    public async Task<ActionResult<List<RecipeDto>>> GetAll()
    {
        var recipes = await _recipeService.GetAllAsync();
        return Ok(recipes);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RecipeDto>> GetById(Guid id)
    {
        var recipe = await _recipeService.GetByIdAsync(id);

        if (recipe == null)
        {
            return NotFound();
        }

        return Ok(recipe);
    }

    [HttpPost]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<RecipeDto>> Create([FromBody] CreateRecipeRequest request)
    {
        try
        {
            var recipe = await _recipeService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = recipe.Id }, recipe);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<RecipeDto>> Update(Guid id, [FromBody] UpdateRecipeRequest request)
    {
        try
        {
            var recipe = await _recipeService.UpdateAsync(id, request);

            if (recipe == null)
            {
                return NotFound();
            }

            return Ok(recipe);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var result = await _recipeService.DeleteAsync(id);

            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}

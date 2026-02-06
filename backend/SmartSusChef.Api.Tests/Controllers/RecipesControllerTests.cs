using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using SmartSusChef.Api.Controllers;
using SmartSusChef.Api.DTOs;
using SmartSusChef.Api.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartSusChef.Api.Tests.Controllers;

public class RecipesControllerTests
{
    private readonly Mock<IRecipeService> _mockRecipeService;
    private readonly RecipesController _controller;

    public RecipesControllerTests()
    {
        _mockRecipeService = new Mock<IRecipeService>();
        _controller = new RecipesController(_mockRecipeService.Object);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOk_WithListOfRecipes()
    {
        // Arrange
        var recipes = new List<RecipeDto> { new(Guid.NewGuid().ToString(), "Pizza", true, false, new List<RecipeIngredientDto>(), DateTime.UtcNow, DateTime.UtcNow) };
        _mockRecipeService.Setup(s => s.GetAllAsync()).ReturnsAsync(recipes);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsAssignableFrom<List<RecipeDto>>(actionResult.Value);
        Assert.Single(value);
    }

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenRecipeExists()
    {
        // Arrange
        var recipeId = Guid.NewGuid();
        var recipe = new RecipeDto(recipeId.ToString(), "Pizza", true, false, new List<RecipeIngredientDto>(), DateTime.UtcNow, DateTime.UtcNow);
        _mockRecipeService.Setup(s => s.GetByIdAsync(recipeId)).ReturnsAsync(recipe);

        // Act
        var result = await _controller.GetById(recipeId);

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsType<RecipeDto>(actionResult.Value);
        Assert.Equal(recipeId.ToString(), value.Id);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenRecipeDoesNotExist()
    {
        // Arrange
        var recipeId = Guid.NewGuid();
        _mockRecipeService.Setup(s => s.GetByIdAsync(recipeId)).ReturnsAsync((RecipeDto?)null);

        // Act
        var result = await _controller.GetById(recipeId);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_ShouldReturnCreatedAtAction_WhenSuccessful()
    {
        // Arrange
        var request = new CreateRecipeRequest("Burger", true, false, new List<CreateRecipeIngredientRequest>());
        var recipeId = Guid.NewGuid();
        var recipe = new RecipeDto(recipeId.ToString(), "Burger", true, false, new List<RecipeIngredientDto>(), DateTime.UtcNow, DateTime.UtcNow);
        _mockRecipeService.Setup(s => s.CreateAsync(request)).ReturnsAsync(recipe);

        // Act
        var result = await _controller.Create(request);

        // Assert
        var actionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal("GetById", actionResult.ActionName);
        Assert.Equal(recipeId.ToString(), actionResult.RouteValues?["id"]?.ToString());
    }
    
    [Fact]
    public async Task Update_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var recipeId = Guid.NewGuid();
        var request = new UpdateRecipeRequest("New Burger", true, false, new List<CreateRecipeIngredientRequest>());
        var recipe = new RecipeDto(recipeId.ToString(), "New Burger", true, false, new List<RecipeIngredientDto>(), DateTime.UtcNow, DateTime.UtcNow);
        _mockRecipeService.Setup(s => s.UpdateAsync(recipeId, request)).ReturnsAsync(recipe);

        // Act
        var result = await _controller.Update(recipeId, request);

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsType<RecipeDto>(actionResult.Value);
        Assert.Equal("New Burger", value.Name);
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent_WhenSuccessful()
    {
        // Arrange
        var recipeId = Guid.NewGuid();
        _mockRecipeService.Setup(s => s.DeleteAsync(recipeId)).ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(recipeId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }
}

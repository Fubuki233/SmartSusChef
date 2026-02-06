using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using SmartSusChef.Api.Controllers;
using SmartSusChef.Api.DTOs;
using SmartSusChef.Api.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace SmartSusChef.Api.Tests.Controllers;

public class IngredientsControllerTests
{
    private readonly Mock<IIngredientService> _mockIngredientService;
    private readonly IngredientsController _controller;

    public IngredientsControllerTests()
    {
        _mockIngredientService = new Mock<IIngredientService>();
        _controller = new IngredientsController(_mockIngredientService.Object);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOk_WithListOfIngredients()
    {
        // Arrange
        var ingredients = new List<IngredientDto> { new(Guid.NewGuid().ToString(), "Flour", "kg", 0.5m, DateTime.UtcNow, DateTime.UtcNow) };
        _mockIngredientService.Setup(s => s.GetAllAsync()).ReturnsAsync(ingredients);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsAssignableFrom<List<IngredientDto>>(actionResult.Value);
        Assert.Single(value);
    }

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenIngredientExists()
    {
        // Arrange
        var ingredientId = Guid.NewGuid();
        var ingredient = new IngredientDto(ingredientId.ToString(), "Flour", "kg", 0.5m, DateTime.UtcNow, DateTime.UtcNow);
        _mockIngredientService.Setup(s => s.GetByIdAsync(ingredientId)).ReturnsAsync(ingredient);

        // Act
        var result = await _controller.GetById(ingredientId);

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsType<IngredientDto>(actionResult.Value);
        Assert.Equal(ingredientId.ToString(), value.Id);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenIngredientDoesNotExist()
    {
        // Arrange
        var ingredientId = Guid.NewGuid();
        _mockIngredientService.Setup(s => s.GetByIdAsync(ingredientId)).ReturnsAsync((IngredientDto?)null);

        // Act
        var result = await _controller.GetById(ingredientId);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_ShouldReturnCreatedAtAction_WhenSuccessful()
    {
        // Arrange
        var request = new CreateIngredientRequest("Sugar", "kg", 0.8m);
        var ingredientId = Guid.NewGuid();
        var ingredient = new IngredientDto(ingredientId.ToString(), "Sugar", "kg", 0.8m, DateTime.UtcNow, DateTime.UtcNow);
        _mockIngredientService.Setup(s => s.CreateAsync(request)).ReturnsAsync(ingredient);

        // Act
        var result = await _controller.Create(request);

        // Assert
        var actionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal("GetById", actionResult.ActionName);
        Assert.Equal(ingredientId.ToString(), actionResult.RouteValues?["id"]?.ToString());
    }

    [Fact]
    public async Task Create_ShouldReturnConflict_WhenIngredientExists()
    {
        // Arrange
        var request = new CreateIngredientRequest("Sugar", "kg", 0.8m);
        _mockIngredientService.Setup(s => s.CreateAsync(request)).ThrowsAsync(new InvalidOperationException());

        // Act
        var result = await _controller.Create(request);

        // Assert
        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task Update_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var ingredientId = Guid.NewGuid();
        var request = new UpdateIngredientRequest("New Sugar", "g", 0.9m);
        var ingredient = new IngredientDto(ingredientId.ToString(), "New Sugar", "g", 0.9m, DateTime.UtcNow, DateTime.UtcNow);
        _mockIngredientService.Setup(s => s.UpdateAsync(ingredientId, request)).ReturnsAsync(ingredient);

        // Act
        var result = await _controller.Update(ingredientId, request);

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsType<IngredientDto>(actionResult.Value);
        Assert.Equal("New Sugar", value.Name);
    }

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenIngredientDoesNotExist()
    {
        // Arrange
        var ingredientId = Guid.NewGuid();
        var request = new UpdateIngredientRequest("New Sugar", "g", 0.9m);
        _mockIngredientService.Setup(s => s.UpdateAsync(ingredientId, request)).ReturnsAsync((IngredientDto?)null);

        // Act
        var result = await _controller.Update(ingredientId, request);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent_WhenSuccessful()
    {
        // Arrange
        var ingredientId = Guid.NewGuid();
        _mockIngredientService.Setup(s => s.DeleteAsync(ingredientId)).ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(ingredientId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenIngredientDoesNotExist()
    {
        // Arrange
        var ingredientId = Guid.NewGuid();
        _mockIngredientService.Setup(s => s.DeleteAsync(ingredientId)).ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(ingredientId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}

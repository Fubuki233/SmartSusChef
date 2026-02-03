using Microsoft.EntityFrameworkCore;
using SmartSusChef.Api.Data;
using SmartSusChef.Api.DTOs;
using SmartSusChef.Api.Models;

namespace SmartSusChef.Api.Services;

public class RecipeService : IRecipeService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    private int CurrentStoreId => _currentUserService.StoreId;

    public RecipeService(ApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<List<RecipeDto>> GetAllAsync()
    {
        var recipes = await _context.Recipes
            .AsNoTracking()
            .Include(r => r.RecipeIngredients).ThenInclude(ri => ri.Ingredient)
            .Include(r => r.RecipeIngredients).ThenInclude(ri => ri.ChildRecipe)
            .Where(r => r.StoreId == CurrentStoreId)
            .OrderBy(r => r.Name)
            .ToListAsync();

        return recipes.Select(MapToDto).ToList();
    }

    public async Task<RecipeDto?> GetByIdAsync(Guid id)
    {
        var recipe = await _context.Recipes
            .AsNoTracking()
            .Include(r => r.RecipeIngredients).ThenInclude(ri => ri.Ingredient)
            .Include(r => r.RecipeIngredients).ThenInclude(ri => ri.ChildRecipe)
            .FirstOrDefaultAsync(r => r.Id == id && r.StoreId == CurrentStoreId);

        return recipe == null ? null : MapToDto(recipe);
    }

    public async Task<RecipeDto> CreateAsync(CreateRecipeRequest request)
    {
        var nameExists = await _context.Recipes
            .AnyAsync(r => r.StoreId == CurrentStoreId && r.Name == request.Name);
        if (nameExists)
            throw new InvalidOperationException("Recipe name already exists");

        // Validate basic structure (e.g. either ingredient or sub-recipe, not both)
        ValidateIngredientsStructure(request.Ingredients);

        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            StoreId = CurrentStoreId,
            Name = request.Name,
            IsSellable = request.IsSellable,
            IsSubRecipe = request.IsSubRecipe,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Aggregate duplicate ingredients
        var aggregatedIngredients = AggregateIngredients(request.Ingredients);

        foreach (var riReq in aggregatedIngredients)
        {
            recipe.RecipeIngredients.Add(new RecipeIngredient
            {
                Id = Guid.NewGuid(),
                RecipeId = recipe.Id,
                IngredientId = string.IsNullOrEmpty(riReq.IngredientId) ? null : Guid.Parse(riReq.IngredientId),
                ChildRecipeId = string.IsNullOrEmpty(riReq.ChildRecipeId) ? null : Guid.Parse(riReq.ChildRecipeId),
                Quantity = riReq.Quantity
            });
        }

        _context.Recipes.Add(recipe);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(recipe.Id) ?? throw new Exception("Failed to retrieve created recipe");
    }

    public async Task<RecipeDto?> UpdateAsync(Guid id, UpdateRecipeRequest request)
    {
        var recipe = await _context.Recipes
            .Include(r => r.RecipeIngredients)
            .FirstOrDefaultAsync(r => r.Id == id && r.StoreId == CurrentStoreId);

        if (recipe == null) return null;

        if (!string.Equals(recipe.Name, request.Name, StringComparison.Ordinal))
        {
            var nameExists = await _context.Recipes
                .AnyAsync(r => r.StoreId == CurrentStoreId && r.Name == request.Name && r.Id != id);
            if (nameExists)
                throw new InvalidOperationException("Recipe name already exists");
        }

        ValidateIngredientsStructure(request.Ingredients);

        if (recipe.IsSubRecipe && !request.IsSubRecipe)
        {
            var isChild = await _context.RecipeIngredients
                .AnyAsync(ri => ri.ChildRecipeId == id);
            if (isChild)
                throw new InvalidOperationException("Sub-recipe is used by other recipes and cannot be converted to main recipe");
        }

        recipe.Name = request.Name;
        recipe.IsSellable = request.IsSellable;
        recipe.IsSubRecipe = request.IsSubRecipe;
        recipe.UpdatedAt = DateTime.UtcNow;

        // Remove existing ingredients. This approach is compatible with the in-memory provider.
        if (recipe.RecipeIngredients.Any())
        {
            _context.RecipeIngredients.RemoveRange(recipe.RecipeIngredients);
        }
        
        // Save changes to the recipe details and remove the old ingredients
        await _context.SaveChangesAsync();

        // Aggregate duplicate ingredients
        var aggregatedIngredients = AggregateIngredients(request.Ingredients);

        // Add new ingredients
        foreach (var riReq in aggregatedIngredients)
        {
            var newIngredient = new RecipeIngredient
            {
                Id = Guid.NewGuid(),
                RecipeId = recipe.Id,
                IngredientId = string.IsNullOrEmpty(riReq.IngredientId) ? null : Guid.Parse(riReq.IngredientId),
                ChildRecipeId = string.IsNullOrEmpty(riReq.ChildRecipeId) ? null : Guid.Parse(riReq.ChildRecipeId),
                Quantity = riReq.Quantity
            };
            _context.RecipeIngredients.Add(newIngredient);
        }

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<decimal> CalculateTotalCarbonFootprintAsync(Guid recipeId)
    {
        var recipe = await _context.Recipes
            .AsNoTracking()
            .Include(r => r.RecipeIngredients).ThenInclude(ri => ri.Ingredient)
            .FirstOrDefaultAsync(r => r.Id == recipeId && r.StoreId == CurrentStoreId);

        if (recipe == null) return 0;

        decimal totalFootprint = 0;

        foreach (var ri in recipe.RecipeIngredients)
        {
            if (ri.Ingredient != null)
            {
                totalFootprint += ri.Ingredient.CarbonFootprint * ri.Quantity;
            }
            else if (ri.ChildRecipeId.HasValue)
            {
                // Recursive call to handle sub-recipes
                totalFootprint += (await CalculateTotalCarbonFootprintAsync(ri.ChildRecipeId.Value)) * ri.Quantity;
            }
        }

        return totalFootprint;
    }

    public Task<List<IngredientUsageDto>> GetFlattenedIngredientsAsync(Guid recipeId)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var recipe = await _context.Recipes
            .FirstOrDefaultAsync(r => r.Id == id && r.StoreId == CurrentStoreId);

        if (recipe == null) return false;

        var isChild = await _context.RecipeIngredients
            .AnyAsync(ri => ri.ChildRecipeId == id);

        if (isChild)
        {
            throw new InvalidOperationException("Recipe is used as a sub-recipe and cannot be deleted");
        }

        _context.Recipes.Remove(recipe);
        await _context.SaveChangesAsync();
        return true;
    }

    private static void ValidateIngredientsStructure(List<CreateRecipeIngredientRequest> ingredients)
    {
        foreach (var item in ingredients)
        {
            var hasIngredient = !string.IsNullOrWhiteSpace(item.IngredientId);
            var hasChildRecipe = !string.IsNullOrWhiteSpace(item.ChildRecipeId);

            if (hasIngredient == hasChildRecipe)
            {
                throw new InvalidOperationException("Each recipe item must specify either an ingredient or a sub-recipe");
            }
        }
    }

    private static List<CreateRecipeIngredientRequest> AggregateIngredients(List<CreateRecipeIngredientRequest> ingredients)
    {
        var aggregated = new List<CreateRecipeIngredientRequest>();

        // Group by IngredientId
        var ingredientGroups = ingredients
            .Where(i => !string.IsNullOrWhiteSpace(i.IngredientId))
            .GroupBy(i => i.IngredientId);

        foreach (var group in ingredientGroups)
        {
            aggregated.Add(new CreateRecipeIngredientRequest(
                group.Key,
                null,
                group.Sum(i => i.Quantity)
            ));
        }

        // Group by ChildRecipeId
        var childRecipeGroups = ingredients
            .Where(i => !string.IsNullOrWhiteSpace(i.ChildRecipeId))
            .GroupBy(i => i.ChildRecipeId);

        foreach (var group in childRecipeGroups)
        {
            aggregated.Add(new CreateRecipeIngredientRequest(
                null,
                group.Key,
                group.Sum(i => i.Quantity)
            ));
        }

        return aggregated;
    }

    private static RecipeDto MapToDto(Recipe recipe)
    {
        return new RecipeDto(
            recipe.Id.ToString(),
            recipe.Name,
            recipe.IsSellable,
            recipe.IsSubRecipe,
            recipe.RecipeIngredients.Select(ri => new RecipeIngredientDto(
                ri.IngredientId?.ToString(),
                ri.ChildRecipeId?.ToString(),
                ri.Ingredient?.Name ?? ri.ChildRecipe?.Name ?? "Unknown",
                ri.Ingredient?.Unit ?? "unit",
                ri.Quantity
            )).ToList()
        );
    }
}

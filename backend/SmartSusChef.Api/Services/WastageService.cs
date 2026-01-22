using Microsoft.EntityFrameworkCore;
using SmartSusChef.Api.Data;
using SmartSusChef.Api.DTOs;
using SmartSusChef.Api.Models;

namespace SmartSusChef.Api.Services;

public class WastageService : IWastageService
{
    private readonly ApplicationDbContext _context;

    public WastageService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<WastageDataDto>> GetAllAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.WastageData
            .Include(w => w.Ingredient)
            .AsQueryable();

        if (startDate.HasValue)
            query = query.Where(w => w.Date >= startDate.Value.Date);

        if (endDate.HasValue)
            query = query.Where(w => w.Date <= endDate.Value.Date);

        var wastageData = await query
            .OrderByDescending(w => w.Date)
            .ToListAsync();

        return wastageData.Select(MapToDto).ToList();
    }

    public async Task<WastageDataDto?> GetByIdAsync(Guid id)
    {
        var wastageData = await _context.WastageData
            .Include(w => w.Ingredient)
            .FirstOrDefaultAsync(w => w.Id == id);

        return wastageData == null ? null : MapToDto(wastageData);
    }

    public async Task<WastageDataDto> CreateAsync(CreateWastageDataRequest request)
    {
        var wastageData = new WastageData
        {
            Id = Guid.NewGuid(),
            Date = DateTime.Parse(request.Date).Date,
            IngredientId = Guid.Parse(request.IngredientId),
            Quantity = request.Quantity,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.WastageData.Add(wastageData);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(wastageData.Id) ?? throw new Exception("Failed to retrieve created wastage data");
    }

    public async Task<WastageDataDto?> UpdateAsync(Guid id, UpdateWastageDataRequest request)
    {
        var wastageData = await _context.WastageData
            .Include(w => w.Ingredient)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (wastageData == null) return null;

        wastageData.Date = DateTime.Parse(request.Date).Date;
        wastageData.IngredientId = Guid.Parse(request.IngredientId);
        wastageData.Quantity = request.Quantity;
        wastageData.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(wastageData);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var wastageData = await _context.WastageData.FindAsync(id);
        if (wastageData == null) return false;

        _context.WastageData.Remove(wastageData);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<WastageTrendDto>> GetTrendAsync(DateTime startDate, DateTime endDate)
    {
        var wastageData = await _context.WastageData
            .Include(w => w.Ingredient)
            .Where(w => w.Date >= startDate.Date && w.Date <= endDate.Date)
            .ToListAsync();

        var grouped = wastageData
            .GroupBy(w => w.Date.Date)
            .OrderBy(g => g.Key)
            .Select(g => new WastageTrendDto(
                g.Key.ToString("yyyy-MM-dd"),
                g.Sum(w => w.Quantity),
                g.Sum(w => w.Quantity * w.Ingredient.CarbonFootprint),
                g.GroupBy(w => w.IngredientId)
                    .Select(ig => new IngredientWastageDto(
                        ig.Key.ToString(),
                        ig.First().Ingredient.Name,
                        ig.First().Ingredient.Unit,
                        ig.Sum(w => w.Quantity),
                        ig.Sum(w => w.Quantity * w.Ingredient.CarbonFootprint)
                    ))
                    .ToList()
            ))
            .ToList();

        return grouped;
    }

    private static WastageDataDto MapToDto(WastageData wastageData)
    {
        return new WastageDataDto(
            wastageData.Id.ToString(),
            wastageData.Date.ToString("yyyy-MM-dd"),
            wastageData.IngredientId.ToString(),
            wastageData.Ingredient.Name,
            wastageData.Ingredient.Unit,
            wastageData.Quantity,
            wastageData.Quantity * wastageData.Ingredient.CarbonFootprint
        );
    }
}

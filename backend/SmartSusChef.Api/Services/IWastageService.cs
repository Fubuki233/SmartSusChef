using SmartSusChef.Api.DTOs;

namespace SmartSusChef.Api.Services;

public interface IWastageService
{
    // Standard CRUD - Scoped to StoreId
    Task<List<WastageDataDto>> GetAllAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<WastageDataDto?> GetByIdAsync(Guid id);
    Task<WastageDataDto> CreateAsync(CreateWastageDataRequest request);
    Task<WastageDataDto?> UpdateAsync(Guid id, UpdateWastageDataRequest request);
    Task<bool> DeleteAsync(Guid id);
    
    /// Returns exactly N data points for the date range, including days with zero wastage.
    /// This ensures your Web trend charts have a continuous timeline as per your unit test case.
    Task<List<WastageTrendDto>> GetTrendAsync(DateTime startDate, DateTime endDate);
    
    /// NEW: Provides total carbon footprint impact specifically from wastage.
    /// Uses the recursive Recipe calculation for complex dishes.
    Task<decimal> GetTotalWastageImpactAsync(DateTime startDate, DateTime endDate);
}

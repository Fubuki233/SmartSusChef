using SmartSusChef.Api.DTOs;

namespace SmartSusChef.Api.Services;

public interface IWastageService
{
    Task<List<WastageDataDto>> GetAllAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<WastageDataDto?> GetByIdAsync(Guid id);
    Task<WastageDataDto> CreateAsync(CreateWastageDataRequest request);
    Task<WastageDataDto?> UpdateAsync(Guid id, UpdateWastageDataRequest request);
    Task<bool> DeleteAsync(Guid id);
    Task<List<WastageTrendDto>> GetTrendAsync(DateTime startDate, DateTime endDate);
}

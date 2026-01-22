using SmartSusChef.Api.DTOs;

namespace SmartSusChef.Api.Services;

public interface ISalesService
{
    Task<List<SalesDataDto>> GetAllAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<SalesDataDto?> GetByIdAsync(Guid id);
    Task<SalesDataDto> CreateAsync(CreateSalesDataRequest request);
    Task<SalesDataDto?> UpdateAsync(Guid id, UpdateSalesDataRequest request);
    Task<bool> DeleteAsync(Guid id);
    Task<List<SalesTrendDto>> GetTrendAsync(DateTime startDate, DateTime endDate);
    Task<List<IngredientUsageDto>> GetIngredientUsageByDateAsync(DateTime date);
    Task<List<RecipeSalesDto>> GetRecipeSalesByDateAsync(DateTime date);
    Task ImportAsync(List<CreateSalesDataRequest> salesData);
}

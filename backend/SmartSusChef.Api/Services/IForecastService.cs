using SmartSusChef.Api.DTOs;

namespace SmartSusChef.Api.Services;

public interface IForecastService
{
    Task<List<ForecastDto>> GetForecastAsync(int days = 7, int includePastDays = 0);
    Task<List<ForecastSummaryDto>> GetForecastSummaryAsync(int days = 7, int includePastDays = 0);
}

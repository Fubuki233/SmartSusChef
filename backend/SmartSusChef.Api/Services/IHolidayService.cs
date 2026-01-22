using SmartSusChef.Api.DTOs;

namespace SmartSusChef.Api.Services;

public interface IHolidayService
{
    Task<List<HolidayDto>> GetHolidaysAsync(int year);
}

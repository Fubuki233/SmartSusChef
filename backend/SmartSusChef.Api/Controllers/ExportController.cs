using System.Globalization;
using System.Text;
using CsvHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSusChef.Api.Services;

namespace SmartSusChef.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExportController : ControllerBase
{
    private readonly ISalesService _salesService;
    private readonly IWastageService _wastageService;
    private readonly IForecastService _forecastService;

    public ExportController(
        ISalesService salesService,
        IWastageService wastageService,
        IForecastService forecastService)
    {
        _salesService = salesService;
        _wastageService = wastageService;
        _forecastService = forecastService;
    }

    [HttpGet("sales/csv")]
    public async Task<IActionResult> ExportSalesCsv([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var data = await _salesService.GetAllAsync(startDate, endDate);

        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        csv.WriteRecords(data);
        await writer.FlushAsync();

        return File(memoryStream.ToArray(), "text/csv", $"sales_export_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpGet("wastage/csv")]
    public async Task<IActionResult> ExportWastageCsv([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var data = await _wastageService.GetAllAsync(startDate, endDate);

        // ✅ Correct
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        csv.WriteRecords(data);
        await writer.FlushAsync();

        return File(memoryStream.ToArray(), "text/csv", $"wastage_export_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpGet("forecast/csv")]
    public async Task<IActionResult> ExportForecastCsv([FromQuery] int days = 7)
    {
        var data = await _forecastService.GetForecastAsync(days);

        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        csv.WriteRecords(data);
        await writer.FlushAsync();

        return File(memoryStream.ToArray(), "text/csv", $"forecast_export_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpGet("sales/pdf")]
    public IActionResult ExportSalesPdf([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        // Placeholder for PDF generation
        // In a real app, use a library like PdfSharp or QuestPDF
        var content = $"Sales Report\nPeriod: {startDate:d} - {endDate:d}\n\n(PDF generation not implemented)";
        var bytes = Encoding.UTF8.GetBytes(content);

        return File(bytes, "application/pdf", $"sales_report_{DateTime.UtcNow:yyyyMMdd}.pdf");
    }
}

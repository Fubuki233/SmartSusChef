using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartSusChef.Api.Data;
using SmartSusChef.Api.DTOs;
using SmartSusChef.Api.Services;

namespace SmartSusChef.Api.Controllers;

/// <summary>
/// Controller for ML model status, training, and prediction management.
/// Exposes endpoints so the frontend can display ML model status
/// and trigger training when needed.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MlController : ControllerBase
{
    private readonly IMlPredictionService _mlService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IStoreService _storeService;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<MlController> _logger;

    public MlController(
        IMlPredictionService mlService,
        ICurrentUserService currentUserService,
        IStoreService storeService,
        ApplicationDbContext dbContext,
        ILogger<MlController> logger)
    {
        _mlService = mlService;
        _currentUserService = currentUserService;
        _storeService = storeService;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Get ML model status for the current user's store.
    /// Returns whether models exist, if training is in progress,
    /// available dishes, and data availability.
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<MlStatusResponseDto>> GetStatus()
    {
        try
        {
            var storeId = _currentUserService.StoreId;
            var status = await _mlService.GetStoreStatusAsync(storeId);

            // When the ML service is unavailable, query the database directly
            // to provide accurate data availability info instead of showing "0 days".
            if (!status.ServiceAvailable)
            {
                _logger.LogInformation("ML service unavailable for store {StoreId}, querying database for data availability.", storeId);

                var dbDaysAvailable = await _dbContext.SalesData
                    .Where(s => s.StoreId == storeId)
                    .Select(s => s.Date.Date)
                    .Distinct()
                    .CountAsync();

                var dbStatus = dbDaysAvailable >= 100 ? "can_train" : "insufficient_data";
                var dbMessage = dbDaysAvailable >= 100
                    ? $"ML service is offline. Sufficient data available ({dbDaysAvailable} days). Training will start when ML service reconnects."
                    : $"ML service is offline. Currently {dbDaysAvailable} days of data. Need at least 100 days of sales data.";

                return Ok(new MlStatusResponseDto(
                    StoreId: storeId,
                    HasModels: false,
                    IsTraining: false,
                    Dishes: null,
                    DaysAvailable: dbDaysAvailable,
                    Status: dbStatus,
                    Message: dbMessage
                ));
            }

            return Ok(new MlStatusResponseDto(
                StoreId: status.StoreId,
                HasModels: status.HasModels,
                IsTraining: status.IsTraining,
                Dishes: status.Dishes,
                DaysAvailable: status.DaysAvailable,
                Status: status.HasModels ? "ready" :
                        status.IsTraining ? "training" :
                        (status.DaysAvailable.HasValue && status.DaysAvailable.Value >= 100) ? "can_train" :
                        "insufficient_data",
                Message: status.HasModels ? "Models are ready for prediction." :
                         status.IsTraining ? "Models are currently being trained. Please wait." :
                         (status.DaysAvailable.HasValue && status.DaysAvailable.Value >= 100)
                             ? $"Sufficient data available ({status.DaysAvailable} days). You can start training."
                             : $"Insufficient data ({status.DaysAvailable ?? 0} days). Need at least 100 days of sales data.",
                TrainingProgress: status.TrainingProgress
            ));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get ML status. ML service may be unavailable.");

            // Even if ML service is down, query DB for actual data availability
            var storeId = _currentUserService.StoreId;
            int dbDays = 0;
            try
            {
                dbDays = await _dbContext.SalesData
                    .Where(s => s.StoreId == storeId)
                    .Select(s => s.Date.Date)
                    .Distinct()
                    .CountAsync();
            }
            catch (Exception dbEx)
            {
                _logger.LogWarning(dbEx, "Also failed to query database for data availability.");
            }

            var status = dbDays >= 100 ? "can_train" : "insufficient_data";
            var message = dbDays >= 100
                ? $"ML service is offline. Sufficient data available ({dbDays} days). Training will start when ML service reconnects."
                : $"ML service is offline. Currently {dbDays} days of data. Need at least 100 days of sales data.";

            return Ok(new MlStatusResponseDto(
                StoreId: storeId,
                HasModels: false,
                IsTraining: false,
                Dishes: null,
                DaysAvailable: dbDays,
                Status: status,
                Message: message
            ));
        }
    }

    /// <summary>
    /// Trigger ML model training for the current user's store.
    /// Training runs asynchronously on the ML backend.
    /// </summary>
    [HttpPost("train")]
    public async Task<ActionResult<MlTrainResponseDto>> TriggerTraining()
    {
        try
        {
            var storeId = _currentUserService.StoreId;
            var result = await _mlService.TriggerTrainingAsync(storeId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger ML training.");
            return StatusCode(503, new { message = "ML service is unavailable. Please try again later." });
        }
    }

    /// <summary>
    /// Request ML predictions for the current user's store.
    /// This is an explicit prediction trigger (the forecast endpoint also calls ML internally).
    /// </summary>
    [HttpPost("predict")]
    public async Task<ActionResult<MlStorePredictResponseDto>> Predict([FromQuery] int days = 7)
    {
        try
        {
            var storeId = _currentUserService.StoreId;
            var store = await _storeService.GetStoreAsync();

            var result = await _mlService.GetStorePredictionsAsync(
                storeId,
                horizonDays: days,
                latitude: store?.Latitude,
                longitude: store?.Longitude,
                countryCode: store?.CountryCode
            );

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get ML predictions.");
            return StatusCode(503, new { message = "ML service is unavailable. Please try again later." });
        }
    }
}

/// <summary>
/// Response DTO for ML status endpoint — enriched with user-friendly status and message.
/// </summary>
public record MlStatusResponseDto(
    int StoreId,
    bool HasModels,
    bool IsTraining,
    List<string>? Dishes,
    int? DaysAvailable,
    string Status,     // "ready" | "training" | "can_train" | "insufficient_data" | "unavailable"
    string Message,
    MlTrainingProgressDto? TrainingProgress = null
);

using SmartSusChef.Api.DTOs;

namespace SmartSusChef.Api.Services;

/// <summary>
/// Service for communicating with the ML prediction backend.
/// </summary>
public interface IMlPredictionService
{
    /// <summary>
    /// Check ML model status for the current store.
    /// </summary>
    Task<MlStoreStatusDto> GetStoreStatusAsync(int storeId);

    /// <summary>
    /// Trigger model training for a store.
    /// Returns immediately; training runs asynchronously on ML side.
    /// </summary>
    Task<MlTrainResponseDto> TriggerTrainingAsync(int storeId);

    /// <summary>
    /// Request predictions for all dishes of a store.
    /// Automatically triggers training if models don't exist but data is sufficient.
    /// </summary>
    Task<MlStorePredictResponseDto> GetStorePredictionsAsync(int storeId, int horizonDays, decimal? latitude, decimal? longitude, string? countryCode);
}

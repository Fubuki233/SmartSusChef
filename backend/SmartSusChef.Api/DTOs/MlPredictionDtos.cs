namespace SmartSusChef.Api.DTOs;

// =====================================================================
// DTOs for ML prediction service communication
// =====================================================================

/// <summary>
/// Response from ML /store/{id}/status
/// </summary>
public record MlTrainingProgressDto(
    int Trained,
    int Failed,
    int Total,
    string? CurrentDish
);

public record MlStoreStatusDto(
    int StoreId,
    bool HasModels,
    bool IsTraining,
    List<string>? Dishes,
    int? DaysAvailable,
    MlTrainingProgressDto? TrainingProgress = null,
    bool ServiceAvailable = true
);

/// <summary>
/// Response from ML /store/{id}/train
/// </summary>
public record MlTrainResponseDto(
    string Status,
    int StoreId,
    string? Message
);

/// <summary>
/// Response from ML /store/{id}/predict
/// </summary>
public record MlStorePredictResponseDto(
    int StoreId,
    string Status,
    string? Message,
    int? DaysAvailable,
    Dictionary<string, MlDishPredictionDto>? Predictions
);

/// <summary>
/// Prediction result for a single dish from ML
/// </summary>
public record MlDishPredictionDto(
    string Dish,
    string? Model,
    string? ModelCombo,
    int? HorizonDays,
    string? StartDate,
    List<MlDayPredictionDto>? Predictions,
    string? Error
);

/// <summary>
/// Single day prediction
/// </summary>
public record MlDayPredictionDto(
    string Date,
    double Yhat,
    double? ProphetYhat,
    double? ResidualHat
);

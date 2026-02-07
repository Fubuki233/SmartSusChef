using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SmartSusChef.Api.DTOs;

namespace SmartSusChef.Api.Services;

/// <summary>
/// Implementation that calls the ML FastAPI backend via HTTP.
/// </summary>
public class MlPredictionService : IMlPredictionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MlPredictionService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public MlPredictionService(HttpClient httpClient, ILogger<MlPredictionService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Timeout for status check specifically (much shorter than training/predict).
    /// </summary>
    private static readonly TimeSpan StatusCheckTimeout = TimeSpan.FromSeconds(10);

    public async Task<MlStoreStatusDto> GetStoreStatusAsync(int storeId)
    {
        try
        {
            var requestUrl = $"/store/{storeId}/status";
            _logger.LogInformation("ML request: GET {BaseAddress}{Path}", _httpClient.BaseAddress, requestUrl);
            using var cts = new CancellationTokenSource(StatusCheckTimeout);
            var response = await _httpClient.GetAsync(requestUrl, cts.Token);
            _logger.LogInformation("ML response: {StatusCode} from {RequestUri}", (int)response.StatusCode, response.RequestMessage?.RequestUri);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("ML /store/{StoreId}/status returned {StatusCode}, body: {Body}", storeId, (int)response.StatusCode, errorBody);
                return new MlStoreStatusDto(
                    StoreId: storeId,
                    HasModels: false,
                    IsTraining: false,
                    Dishes: null,
                    DaysAvailable: null,
                    ServiceAvailable: false
                );
            }

            var body = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body))
            {
                throw new InvalidOperationException("ML service returned an empty response for store status");
            }

            var json = JsonSerializer.Deserialize<JsonElement>(body, JsonOptions);

            // Parse training progress if present
            MlTrainingProgressDto? trainingProgress = null;
            if (json.TryGetProperty("training_progress", out var tp) && tp.ValueKind == JsonValueKind.Object)
            {
                trainingProgress = new MlTrainingProgressDto(
                    Trained: tp.TryGetProperty("trained", out var tr) ? tr.GetInt32() : 0,
                    Failed: tp.TryGetProperty("failed", out var fl) ? fl.GetInt32() : 0,
                    Total: tp.TryGetProperty("total", out var tl) ? tl.GetInt32() : 0,
                    CurrentDish: tp.TryGetProperty("current_dish", out var cd) && cd.ValueKind == JsonValueKind.String ? cd.GetString() : null
                );
            }

            return new MlStoreStatusDto(
                StoreId: json.TryGetProperty("store_id", out var sid) ? sid.GetInt32() : storeId,
                HasModels: json.TryGetProperty("has_models", out var hm) && hm.GetBoolean(),
                IsTraining: json.TryGetProperty("is_training", out var it) && it.GetBoolean(),
                Dishes: json.TryGetProperty("dishes", out var d) && d.ValueKind == JsonValueKind.Array
                    ? d.EnumerateArray().Select(x => x.GetString()!).ToList()
                    : null,
                DaysAvailable: json.TryGetProperty("days_available", out var da) && da.ValueKind == JsonValueKind.Number
                    ? da.GetInt32()
                    : null,
                TrainingProgress: trainingProgress
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ML service unavailable for store {StoreId} status check", storeId);
            return new MlStoreStatusDto(
                StoreId: storeId,
                HasModels: false,
                IsTraining: false,
                Dishes: null,
                DaysAvailable: null,
                ServiceAvailable: false
            );
        }
    }

    public async Task<MlTrainResponseDto> TriggerTrainingAsync(int storeId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/store/{storeId}/train", null);

            var body = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body))
            {
                return new MlTrainResponseDto(
                    Status: response.IsSuccessStatusCode ? "unknown" : "error",
                    StoreId: storeId,
                    Message: $"ML service returned empty response ({(int)response.StatusCode})"
                );
            }

            var json = JsonSerializer.Deserialize<JsonElement>(body, JsonOptions);

            var status = json.GetProperty("status").GetString() ?? "error";
            var message = json.TryGetProperty("message", out var m) ? m.GetString() : null;

            // Handle 400 (insufficient data) — ML returns error detail as JSON
            if (!response.IsSuccessStatusCode && json.TryGetProperty("detail", out var detail))
            {
                if (detail.ValueKind == JsonValueKind.Object)
                {
                    status = detail.TryGetProperty("status", out var s) ? s.GetString() ?? "error" : "error";
                    message = detail.TryGetProperty("message", out var msg) ? msg.GetString() : response.ReasonPhrase;
                }
                else
                {
                    message = detail.GetString();
                }
            }

            return new MlTrainResponseDto(
                Status: status,
                StoreId: storeId,
                Message: message
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger ML training for store {StoreId}", storeId);
            throw;
        }
    }

    public async Task<MlStorePredictResponseDto> GetStorePredictionsAsync(
        int storeId, int horizonDays, decimal? latitude, decimal? longitude, string? countryCode)
    {
        try
        {
            var requestBody = new
            {
                store_id = storeId,
                horizon_days = horizonDays,
                latitude = latitude.HasValue ? (double?)Convert.ToDouble(latitude.Value) : null,
                longitude = longitude.HasValue ? (double?)Convert.ToDouble(longitude.Value) : null,
                country_code = countryCode,
            };

            var response = await _httpClient.PostAsJsonAsync($"/store/{storeId}/predict", requestBody, JsonOptions);

            var body = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body))
            {
                _logger.LogWarning("ML /store/{StoreId}/predict returned empty body (HTTP {StatusCode})", storeId, (int)response.StatusCode);
                return new MlStorePredictResponseDto(
                    StoreId: storeId,
                    Status: "error",
                    Message: $"ML service returned empty response ({(int)response.StatusCode})",
                    DaysAvailable: null,
                    Predictions: null
                );
            }

            var json = JsonSerializer.Deserialize<JsonElement>(body, JsonOptions);

            var status = json.GetProperty("status").GetString() ?? "error";
            var message = json.TryGetProperty("message", out var m) ? m.GetString() : null;
            int? daysAvailable = json.TryGetProperty("days_available", out var da) && da.ValueKind == JsonValueKind.Number
                ? da.GetInt32() : null;

            Dictionary<string, MlDishPredictionDto>? predictions = null;

            if (status == "ok" && json.TryGetProperty("predictions", out var preds) && preds.ValueKind == JsonValueKind.Object)
            {
                predictions = new Dictionary<string, MlDishPredictionDto>();

                foreach (var prop in preds.EnumerateObject())
                {
                    var dishJson = prop.Value;

                    // Check if it's an error entry
                    if (dishJson.TryGetProperty("error", out var errProp))
                    {
                        predictions[prop.Name] = new MlDishPredictionDto(
                            Dish: prop.Name, Model: null, ModelCombo: null,
                            HorizonDays: null, StartDate: null, Predictions: null,
                            Error: errProp.GetString()
                        );
                        continue;
                    }

                    var dayPredictions = new List<MlDayPredictionDto>();
                    if (dishJson.TryGetProperty("predictions", out var predsArr) && predsArr.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var dayJson in predsArr.EnumerateArray())
                        {
                            dayPredictions.Add(new MlDayPredictionDto(
                                Date: dayJson.GetProperty("date").GetString()!,
                                Yhat: dayJson.GetProperty("yhat").GetDouble(),
                                ProphetYhat: dayJson.TryGetProperty("prophet_yhat", out var py) ? py.GetDouble() : null,
                                ResidualHat: dayJson.TryGetProperty("residual_hat", out var rh) ? rh.GetDouble() : null
                            ));
                        }
                    }

                    predictions[prop.Name] = new MlDishPredictionDto(
                        Dish: dishJson.TryGetProperty("dish", out var dn) ? dn.GetString()! : prop.Name,
                        Model: dishJson.TryGetProperty("model", out var md) ? md.GetString() : null,
                        ModelCombo: dishJson.TryGetProperty("model_combo", out var mc) ? mc.GetString() : null,
                        HorizonDays: dishJson.TryGetProperty("horizon_days", out var hd) ? hd.GetInt32() : null,
                        StartDate: dishJson.TryGetProperty("start_date", out var sd) ? sd.GetString() : null,
                        Predictions: dayPredictions,
                        Error: null
                    );
                }
            }

            return new MlStorePredictResponseDto(
                StoreId: storeId,
                Status: status,
                Message: message,
                DaysAvailable: daysAvailable,
                Predictions: predictions
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get ML predictions for store {StoreId}", storeId);
            throw;
        }
    }
}

using Xunit;
using Moq;
using Moq.Protected;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartSusChef.Api.Services;
using SmartSusChef.Api.DTOs;
using System.Text.Json;

namespace SmartSusChef.Api.Tests.Services;

public class MlPredictionServiceTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger<MlPredictionService>> _mockLogger;
    private readonly MlPredictionService _service;

    public MlPredictionServiceTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:8000")
        };
        _mockLogger = new Mock<ILogger<MlPredictionService>>();
        _service = new MlPredictionService(_httpClient, _mockLogger.Object);
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, string content)
    {
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            });
    }

    [Fact]
    public async Task GetStoreStatusAsync_ShouldReturnStatus_WhenApiCallIsSuccessful()
    {
        // Arrange
        var responseContent = JsonSerializer.Serialize(new { store_id = 1, has_models = true, is_training = false });
        SetupHttpResponse(HttpStatusCode.OK, responseContent);

        // Act
        var result = await _service.GetStoreStatusAsync(1);

        // Assert
        Assert.True(result.HasModels);
        Assert.False(result.IsTraining);
    }

    [Fact]
    public async Task GetStoreStatusAsync_ShouldReturnNotAvailable_WhenApiCallFails()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.InternalServerError, "{}");

        // Act
        var result = await _service.GetStoreStatusAsync(1);

        // Assert
        Assert.False(result.ServiceAvailable);
    }

    [Fact]
    public async Task TriggerTrainingAsync_ShouldReturnResponse_WhenApiCallIsSuccessful()
    {
        // Arrange
        var responseContent = JsonSerializer.Serialize(new { status = "training_started", message = "Training started" });
        SetupHttpResponse(HttpStatusCode.OK, responseContent);

        // Act
        var result = await _service.TriggerTrainingAsync(1);

        // Assert
        Assert.Equal("training_started", result.Status);
        Assert.Equal("Training started", result.Message);
    }

    [Fact]
    public async Task GetStorePredictionsAsync_ShouldReturnPredictions_WhenApiCallIsSuccessful()
    {
        // Arrange
        var responseContent = @"{
            ""status"": ""ok"",
            ""predictions"": {
                ""Pizza"": {
                    ""dish"": ""Pizza"",
                    ""predictions"": [
                        { ""date"": ""2024-01-01"", ""yhat"": 10.0 }
                    ]
                }
            }
        }";
        SetupHttpResponse(HttpStatusCode.OK, responseContent);

        // Act
        var result = await _service.GetStorePredictionsAsync(1, 7, 1.0m, 1.0m, "US");

        // Assert
        Assert.Equal("ok", result.Status);
        Assert.NotNull(result.Predictions);
        Assert.True(result.Predictions.ContainsKey("Pizza"));
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartSusChef.Api.Data;

namespace SmartSusChef.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HealthController> _logger;

    public HealthController(ApplicationDbContext context, ILogger<HealthController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Basic health check endpoint
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0"
        });
    }

    /// <summary>
    /// Detailed health check including database connectivity
    /// </summary>
    [HttpGet("detailed")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDetailed()
    {
        var health = new HealthCheckResult
        {
            Status = "healthy",
            Timestamp = DateTime.UtcNow,
            Version = GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0",
            Checks = new Dictionary<string, HealthCheckItem>()
        };

        // Check database connectivity
        try
        {
            var canConnect = await _context.Database.CanConnectAsync();
            health.Checks["database"] = new HealthCheckItem
            {
                Status = canConnect ? "healthy" : "unhealthy",
                Message = canConnect ? "Database connection successful" : "Cannot connect to database"
            };

            if (!canConnect)
            {
                health.Status = "unhealthy";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            health.Checks["database"] = new HealthCheckItem
            {
                Status = "unhealthy",
                Message = $"Database error: {ex.Message}"
            };
            health.Status = "unhealthy";
        }

        // Check memory usage
        var process = System.Diagnostics.Process.GetCurrentProcess();
        var memoryMB = process.WorkingSet64 / 1024 / 1024;
        health.Checks["memory"] = new HealthCheckItem
        {
            Status = memoryMB < 500 ? "healthy" : "warning",
            Message = $"Memory usage: {memoryMB} MB"
        };

        var statusCode = health.Status == "healthy" ? 200 : 503;
        return StatusCode(statusCode, health);
    }

    /// <summary>
    /// Liveness probe for Kubernetes/ECS
    /// </summary>
    [HttpGet("live")]
    [AllowAnonymous]
    public IActionResult Live()
    {
        return Ok(new { status = "alive" });
    }

    /// <summary>
    /// Readiness probe for Kubernetes/ECS
    /// </summary>
    [HttpGet("ready")]
    [AllowAnonymous]
    public async Task<IActionResult> Ready()
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync();
            if (canConnect)
            {
                return Ok(new { status = "ready" });
            }
            return StatusCode(503, new { status = "not ready", reason = "database unavailable" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Readiness check failed");
            return StatusCode(503, new { status = "not ready", reason = ex.Message });
        }
    }
}

public class HealthCheckResult
{
    public string Status { get; set; } = "healthy";
    public DateTime Timestamp { get; set; }
    public string Version { get; set; } = "1.0.0";
    public Dictionary<string, HealthCheckItem> Checks { get; set; } = new();
}

public class HealthCheckItem
{
    public string Status { get; set; } = "healthy";
    public string Message { get; set; } = string.Empty;
}

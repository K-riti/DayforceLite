using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DayforceLite.Infrastructure.Data;

namespace DayforceLite.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    private readonly DayforceDbContext _context;
    private readonly ILogger<HealthController> _logger;

    public HealthController(DayforceDbContext context, ILogger<HealthController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var health = new HealthCheckResult
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0"
        };

        try
        {
            // Check database connectivity
            await _context.Database.CanConnectAsync();
            health.Database = "Connected";
        }
        catch (Exception ex)
        {
            health.Status = "Degraded";
            health.Database = $"Error: {ex.Message}";
            _logger.LogWarning(ex, "Database health check failed");
        }

        return health.Status == "Healthy" ? Ok(health) : StatusCode(503, health);
    }

    [HttpGet("ready")]
    public IActionResult Ready()
    {
        return Ok(new { Status = "Ready", Timestamp = DateTime.UtcNow });
    }

    [HttpGet("live")]
    public IActionResult Live()
    {
        return Ok(new { Status = "Alive", Timestamp = DateTime.UtcNow });
    }
}

public class HealthCheckResult
{
    public string Status { get; set; } = "Healthy";
    public DateTime Timestamp { get; set; }
    public string Version { get; set; } = string.Empty;
    public string? Database { get; set; }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DayforceLite.API.DTOs;
using DayforceLite.Core.Models;
using DayforceLite.Core.Services;

namespace DayforceLite.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuditController : ControllerBase
{
    private readonly IAuditService _service;
    private readonly ILogger<AuditController> _logger;

    public AuditController(IAuditService service, ILogger<AuditController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("recent")]
    public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetRecent([FromQuery] int count = 100)
    {
        if (count <= 0 || count > 1000)
        {
            count = 100;
        }

        var logs = await _service.GetRecentActivityAsync(count);
        return Ok(logs.Select(MapToDto));
    }

    [HttpGet("entity/{entityType}/{entityId}")]
    public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetEntityHistory(
        string entityType, string entityId)
    {
        var logs = await _service.GetEntityHistoryAsync(entityType, entityId);
        return Ok(logs.Select(MapToDto));
    }

    [HttpGet("user/{userId:int}")]
    public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetUserActivity(
        int userId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        var logs = await _service.GetUserActivityAsync(userId, fromDate, toDate);
        return Ok(logs.Select(MapToDto));
    }

    [HttpGet("date-range")]
    public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetByDateRange(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] string? entityType = null)
    {
        if (fromDate > toDate)
        {
            return BadRequest("fromDate must be before or equal to toDate");
        }

        var logs = await _service.GetActivityByDateRangeAsync(fromDate, toDate, entityType);
        return Ok(logs.Select(MapToDto));
    }

    [HttpPost("query")]
    public async Task<ActionResult<IEnumerable<AuditLogDto>>> Query([FromBody] AuditQueryRequest request)
    {
        IEnumerable<AuditLog> logs;

        if (request.UserId.HasValue)
        {
            logs = await _service.GetUserActivityAsync(request.UserId.Value, request.FromDate, request.ToDate);
        }
        else if (request.FromDate.HasValue && request.ToDate.HasValue)
        {
            logs = await _service.GetActivityByDateRangeAsync(
                request.FromDate.Value, request.ToDate.Value, request.EntityType);
        }
        else
        {
            logs = await _service.GetRecentActivityAsync(100);
        }

        return Ok(logs.Select(MapToDto));
    }

    private static AuditLogDto MapToDto(AuditLog a) => new(
        a.AuditLogId,
        a.EntityType,
        a.EntityId,
        a.Action,
        a.OldValues,
        a.NewValues,
        a.UserId,
        a.UserName,
        a.Timestamp,
        a.IpAddress);
}

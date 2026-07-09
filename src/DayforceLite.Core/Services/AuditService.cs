using System.Text.Json;
using Microsoft.AspNetCore.Http;
using DayforceLite.Core.Interfaces;
using DayforceLite.Core.Models;

namespace DayforceLite.Core.Services;

public class AuditService : IAuditService
{
    private readonly IAuditRepository _repository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AuditService(IAuditRepository repository, IHttpContextAccessor httpContextAccessor)
    {
        _repository = repository;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(string entityType, string entityId, string action, object? oldValues = null, object? newValues = null)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var userId = GetCurrentUserId(httpContext);
        var userName = GetCurrentUserName(httpContext);

        var auditLog = new AuditLog
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues, JsonOptions) : null,
            NewValues = newValues != null ? JsonSerializer.Serialize(newValues, JsonOptions) : null,
            UserId = userId,
            UserName = userName,
            Timestamp = DateTime.UtcNow,
            IpAddress = GetClientIpAddress(httpContext)
        };

        await _repository.CreateAsync(auditLog);
    }

    public async Task LogAsync<T>(T entity, string action, object? oldValues = null) where T : class
    {
        var entityType = typeof(T).Name;
        var entityId = GetEntityId(entity);

        await LogAsync(entityType, entityId, action, oldValues, entity);
    }

    public async Task<IEnumerable<AuditLog>> GetEntityHistoryAsync(string entityType, string entityId)
    {
        return await _repository.GetByEntityAsync(entityType, entityId);
    }

    public async Task<IEnumerable<AuditLog>> GetUserActivityAsync(int userId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        return await _repository.GetByUserAsync(userId, fromDate, toDate);
    }

    public async Task<IEnumerable<AuditLog>> GetRecentActivityAsync(int count = 100)
    {
        return await _repository.GetRecentAsync(count);
    }

    public async Task<IEnumerable<AuditLog>> GetActivityByDateRangeAsync(DateTime fromDate, DateTime toDate, string? entityType = null)
    {
        return await _repository.GetByDateRangeAsync(fromDate, toDate, entityType);
    }

    private static int? GetCurrentUserId(HttpContext? httpContext)
    {
        var userIdClaim = httpContext?.User?.FindFirst("sub") 
                       ?? httpContext?.User?.FindFirst("EmployeeId");

        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }

        return null;
    }

    private static string? GetCurrentUserName(HttpContext? httpContext)
    {
        return httpContext?.User?.Identity?.Name 
            ?? httpContext?.User?.FindFirst("name")?.Value;
    }

    private static string? GetClientIpAddress(HttpContext? httpContext)
    {
        if (httpContext == null) return null;

        // Check for forwarded header (behind proxy/load balancer)
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',').First().Trim();
        }

        return httpContext.Connection.RemoteIpAddress?.ToString();
    }

    private static string GetEntityId<T>(T entity) where T : class
    {
        // Try to find common ID property names
        var type = typeof(T);
        var idProperty = type.GetProperty($"{type.Name}Id") 
                      ?? type.GetProperty("Id")
                      ?? type.GetProperty("ID");

        if (idProperty != null)
        {
            var value = idProperty.GetValue(entity);
            return value?.ToString() ?? "unknown";
        }

        return "unknown";
    }
}

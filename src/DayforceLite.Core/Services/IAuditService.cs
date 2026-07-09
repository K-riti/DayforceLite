using DayforceLite.Core.Models;

namespace DayforceLite.Core.Services;

public interface IAuditService
{
    Task LogAsync(string entityType, string entityId, string action, object? oldValues = null, object? newValues = null);
    Task LogAsync<T>(T entity, string action, object? oldValues = null) where T : class;
    Task<IEnumerable<AuditLog>> GetEntityHistoryAsync(string entityType, string entityId);
    Task<IEnumerable<AuditLog>> GetUserActivityAsync(int userId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<IEnumerable<AuditLog>> GetRecentActivityAsync(int count = 100);
    Task<IEnumerable<AuditLog>> GetActivityByDateRangeAsync(DateTime fromDate, DateTime toDate, string? entityType = null);
}

using DayforceLite.Core.Models;

namespace DayforceLite.Core.Interfaces;

public interface IAuditRepository
{
    Task<AuditLog?> GetByIdAsync(long auditLogId);
    Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityType, string entityId);
    Task<IEnumerable<AuditLog>> GetByUserAsync(int userId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, string? entityType = null);
    Task<IEnumerable<AuditLog>> GetRecentAsync(int count = 100);
    Task<long> CreateAsync(AuditLog auditLog);
}

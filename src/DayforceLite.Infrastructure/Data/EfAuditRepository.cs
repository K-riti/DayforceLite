using Microsoft.EntityFrameworkCore;
using DayforceLite.Core.Interfaces;
using DayforceLite.Core.Models;

namespace DayforceLite.Infrastructure.Data;

public class EfAuditRepository : IAuditRepository
{
    private readonly DayforceDbContext _context;

    public EfAuditRepository(DayforceDbContext context)
    {
        _context = context;
    }

    public async Task<AuditLog?> GetByIdAsync(long auditLogId)
    {
        return await _context.AuditLogs.FindAsync(auditLogId);
    }

    public async Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityType, string entityId)
    {
        return await _context.AuditLogs
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByUserAsync(int userId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.AuditLogs.Where(a => a.UserId == userId);

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= toDate.Value);
        }

        return await query.OrderByDescending(a => a.Timestamp).ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, string? entityType = null)
    {
        var query = _context.AuditLogs
            .Where(a => a.Timestamp >= fromDate && a.Timestamp <= toDate);

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(a => a.EntityType == entityType);
        }

        return await query.OrderByDescending(a => a.Timestamp).ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetRecentAsync(int count = 100)
    {
        return await _context.AuditLogs
            .OrderByDescending(a => a.Timestamp)
            .Take(count)
            .ToListAsync();
    }

    public async Task<long> CreateAsync(AuditLog auditLog)
    {
        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
        return auditLog.AuditLogId;
    }
}

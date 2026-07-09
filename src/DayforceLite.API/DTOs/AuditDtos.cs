namespace DayforceLite.API.DTOs;

public record AuditLogDto(
    long AuditLogId,
    string EntityType,
    string EntityId,
    string Action,
    string? OldValues,
    string? NewValues,
    int? UserId,
    string? UserName,
    DateTime Timestamp,
    string? IpAddress
);

public record AuditQueryRequest(
    DateTime? FromDate,
    DateTime? ToDate,
    string? EntityType,
    int? UserId
);

namespace DayforceLite.API.DTOs;

public record LeaveRequestDto(
    int LeaveRequestId,
    int EmployeeId,
    string? EmployeeName,
    string LeaveType,
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalDays,
    string? Reason,
    string Status,
    int? ApprovedBy,
    DateTime? ApprovedAt,
    string? ApproverComments,
    DateTime CreatedAt
);

public record CreateLeaveRequest(
    string LeaveType,
    DateTime StartDate,
    DateTime EndDate,
    string? Reason
);

public record ApproveRejectRequest(
    string? Comments
);

public record LeaveBalanceDto(
    int EmployeeId,
    int Year,
    decimal VacationDays,
    decimal VacationUsed,
    decimal VacationRemaining,
    decimal SickDays,
    decimal SickUsed,
    decimal SickRemaining,
    decimal PersonalDays,
    decimal PersonalUsed,
    decimal PersonalRemaining
);

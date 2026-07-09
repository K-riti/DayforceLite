namespace DayforceLite.API.DTOs;

public record TimesheetDto(
    int TimesheetId,
    int EmployeeId,
    DateTime WeekStartDate,
    decimal RegularHours,
    decimal OvertimeHours,
    string Status,
    DateTime? SubmittedAt,
    DateTime? ApprovedAt
);

public record CreateTimesheetRequest(
    int EmployeeId,
    DateTime WeekStartDate,
    decimal RegularHours,
    decimal OvertimeHours
);

public record UpdateTimesheetRequest(
    decimal RegularHours,
    decimal OvertimeHours,
    string Status
);

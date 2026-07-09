namespace DayforceLite.API.DTOs;

public record DepartmentDto(
    int DepartmentId,
    string Name,
    string CostCentre,
    string? Description,
    int? ManagerId,
    bool IsActive,
    int EmployeeCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateDepartmentRequest(
    string Name,
    string CostCentre,
    string? Description,
    int? ManagerId
);

public record UpdateDepartmentRequest(
    string Name,
    string CostCentre,
    string? Description,
    int? ManagerId
);

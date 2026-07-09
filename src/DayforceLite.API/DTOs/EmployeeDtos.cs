namespace DayforceLite.API.DTOs;

public record EmployeeDto(
    int EmployeeId,
    string FirstName,
    string LastName,
    string Email,
    int DepartmentId,
    decimal HourlyRate,
    DateTime StartDate,
    string Department
);

public record CreateEmployeeRequest(
    string FirstName,
    string LastName,
    string Email,
    int DepartmentId,
    decimal HourlyRate,
    DateTime StartDate
);

public record UpdateEmployeeRequest(
    string FirstName,
    string LastName,
    string Email,
    int DepartmentId,
    decimal HourlyRate,
    DateTime StartDate
);

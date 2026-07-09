namespace DayforceLite.API.DTOs;

public record PayrollRecordDto(
    int PayrollId,
    int EmployeeId,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    decimal GrossPay,
    decimal TaxDeduction,
    decimal NetPay,
    DateTime ProcessedAt
);

public record ProcessPayrollRequest(
    int EmployeeId,
    DateTime PeriodStart,
    DateTime PeriodEnd
);

public record PayrollSummaryDto(
    int EmployeeId,
    string FullName,
    decimal TotalGross,
    decimal TotalNet,
    int PayslipCount
);

using DayforceLite.Core.Models;

namespace DayforceLite.Core.Services;

public interface IPayrollService
{
    Task<PayrollRecord> ProcessPayrollAsync(int employeeId, DateTime periodStart, DateTime periodEnd);
    Task<IEnumerable<PayrollRecord>> GetPayrollHistoryAsync(int employeeId);
    decimal CalculateGrossPay(decimal regularHours, decimal overtimeHours, decimal hourlyRate);
    decimal CalculateTax(decimal grossPay);
}

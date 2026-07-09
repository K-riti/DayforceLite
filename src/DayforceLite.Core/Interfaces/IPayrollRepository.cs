using DayforceLite.Core.Models;

namespace DayforceLite.Core.Interfaces;

public interface IPayrollRepository
{
    Task<PayrollRecord?> GetByIdAsync(int payrollId);
    Task<IEnumerable<PayrollRecord>> GetByEmployeeIdAsync(int employeeId);
    Task<IEnumerable<PayrollRecord>> GetByPeriodAsync(DateTime start, DateTime end);
    Task<int> CreateAsync(PayrollRecord record);
}

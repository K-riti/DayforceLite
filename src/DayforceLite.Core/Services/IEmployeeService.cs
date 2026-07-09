using DayforceLite.Core.Models;

namespace DayforceLite.Core.Services;

public interface IEmployeeService
{
    Task<Employee> GetByIdAsync(int employeeId);
    Task<IEnumerable<Employee>> GetAllAsync(string? searchTerm = null);
    Task<int> CreateAsync(Employee employee);
    Task UpdateAsync(Employee employee);
    Task DeleteAsync(int employeeId);
    Task<PayrollSummary> GetPayrollSummaryAsync(int employeeId, DateTime from, DateTime to);
}

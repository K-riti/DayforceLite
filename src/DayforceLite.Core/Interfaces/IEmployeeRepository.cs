using DayforceLite.Core.Models;

namespace DayforceLite.Core.Interfaces;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(int employeeId);
    Task<IEnumerable<Employee>> GetAllAsync(string? searchTerm = null);
    Task<int> CreateAsync(Employee employee);
    Task UpdateAsync(Employee employee);
    Task DeleteAsync(int employeeId);
    Task<bool> ExistsAsync(int employeeId);
    Task<PayrollSummary> GetPayrollSummaryAsync(int employeeId, DateTime from, DateTime to);
}

using DayforceLite.Core.Models;

namespace DayforceLite.Core.Interfaces;

public interface IDepartmentRepository
{
    Task<Department?> GetByIdAsync(int departmentId);
    Task<IEnumerable<Department>> GetAllAsync(bool includeInactive = false);
    Task<int> CreateAsync(Department department);
    Task UpdateAsync(Department department);
    Task DeleteAsync(int departmentId);
    Task<bool> ExistsAsync(int departmentId);
    Task<int> GetEmployeeCountAsync(int departmentId);
    Task<bool> HasActiveEmployeesAsync(int departmentId);
}

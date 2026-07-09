using DayforceLite.Core.Models;

namespace DayforceLite.Core.Services;

public interface IDepartmentService
{
    Task<Department> GetByIdAsync(int departmentId);
    Task<IEnumerable<Department>> GetAllAsync(bool includeInactive = false);
    Task<int> CreateAsync(Department department);
    Task UpdateAsync(Department department);
    Task DeleteAsync(int departmentId);
}

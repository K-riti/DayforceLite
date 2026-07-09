using DayforceLite.Core.Exceptions;
using DayforceLite.Core.Interfaces;
using DayforceLite.Core.Models;

namespace DayforceLite.Core.Services;

public class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _repository;

    public DepartmentService(IDepartmentRepository repository)
    {
        _repository = repository;
    }

    public async Task<Department> GetByIdAsync(int departmentId)
    {
        var department = await _repository.GetByIdAsync(departmentId);

        if (department is null)
        {
            throw new NotFoundException(nameof(Department), departmentId);
        }

        department.EmployeeCount = await _repository.GetEmployeeCountAsync(departmentId);
        return department;
    }

    public async Task<IEnumerable<Department>> GetAllAsync(bool includeInactive = false)
    {
        var departments = await _repository.GetAllAsync(includeInactive);

        // Populate employee counts
        foreach (var dept in departments)
        {
            dept.EmployeeCount = await _repository.GetEmployeeCountAsync(dept.DepartmentId);
        }

        return departments;
    }

    public async Task<int> CreateAsync(Department department)
    {
        ValidateDepartment(department);

        department.CreatedAt = DateTime.UtcNow;
        department.IsActive = true;

        return await _repository.CreateAsync(department);
    }

    public async Task UpdateAsync(Department department)
    {
        if (!await _repository.ExistsAsync(department.DepartmentId))
        {
            throw new NotFoundException(nameof(Department), department.DepartmentId);
        }

        ValidateDepartment(department);

        department.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(department);
    }

    public async Task DeleteAsync(int departmentId)
    {
        if (!await _repository.ExistsAsync(departmentId))
        {
            throw new NotFoundException(nameof(Department), departmentId);
        }

        if (await _repository.HasActiveEmployeesAsync(departmentId))
        {
            throw new InvalidOperationException(
                "Cannot delete department with active employees. Reassign or deactivate employees first.");
        }

        await _repository.DeleteAsync(departmentId);
    }

    private static void ValidateDepartment(Department department)
    {
        if (string.IsNullOrWhiteSpace(department.Name))
        {
            throw new ArgumentException("Department name is required", nameof(department));
        }

        if (department.Name.Length > 100)
        {
            throw new ArgumentException("Department name cannot exceed 100 characters", nameof(department));
        }

        if (string.IsNullOrWhiteSpace(department.CostCentre))
        {
            throw new ArgumentException("Cost centre is required", nameof(department));
        }

        if (department.CostCentre.Length > 20)
        {
            throw new ArgumentException("Cost centre cannot exceed 20 characters", nameof(department));
        }
    }
}

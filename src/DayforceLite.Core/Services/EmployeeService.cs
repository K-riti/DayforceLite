using DayforceLite.Core.Exceptions;
using DayforceLite.Core.Interfaces;
using DayforceLite.Core.Models;

namespace DayforceLite.Core.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _repository;

    public EmployeeService(IEmployeeRepository repository)
    {
        _repository = repository;
    }

    public async Task<Employee> GetByIdAsync(int employeeId)
    {
        var employee = await _repository.GetByIdAsync(employeeId);

        if (employee is null)
        {
            throw new NotFoundException(nameof(Employee), employeeId);
        }

        return employee;
    }

    public async Task<IEnumerable<Employee>> GetAllAsync(string? searchTerm = null)
    {
        return await _repository.GetAllAsync(searchTerm);
    }

    public async Task<int> CreateAsync(Employee employee)
    {
        employee.CreatedAt = DateTime.UtcNow;
        employee.IsActive = true;
        return await _repository.CreateAsync(employee);
    }

    public async Task UpdateAsync(Employee employee)
    {
        if (!await _repository.ExistsAsync(employee.EmployeeId))
        {
            throw new NotFoundException(nameof(Employee), employee.EmployeeId);
        }

        await _repository.UpdateAsync(employee);
    }

    public async Task DeleteAsync(int employeeId)
    {
        if (!await _repository.ExistsAsync(employeeId))
        {
            throw new NotFoundException(nameof(Employee), employeeId);
        }

        await _repository.DeleteAsync(employeeId);
    }

    public async Task<PayrollSummary> GetPayrollSummaryAsync(int employeeId, DateTime from, DateTime to)
    {
        if (!await _repository.ExistsAsync(employeeId))
        {
            throw new NotFoundException(nameof(Employee), employeeId);
        }

        return await _repository.GetPayrollSummaryAsync(employeeId, from, to);
    }
}

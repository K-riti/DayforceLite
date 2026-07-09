using DayforceLite.Core.Models;

namespace DayforceLite.Core.Interfaces;

public interface ITimesheetRepository
{
    Task<Timesheet?> GetByIdAsync(int timesheetId);
    Task<IEnumerable<Timesheet>> GetByEmployeeIdAsync(int employeeId);
    Task<IEnumerable<Timesheet>> GetAllAsync(string? status = null);
    Task<int> CreateAsync(Timesheet timesheet);
    Task UpdateAsync(Timesheet timesheet);
    Task DeleteAsync(int timesheetId);
}

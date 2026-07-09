using DayforceLite.Core.Models;

namespace DayforceLite.Core.Services;

public interface ITimesheetService
{
    Task<Timesheet?> GetByIdAsync(int timesheetId);
    Task<IEnumerable<Timesheet>> GetByEmployeeIdAsync(int employeeId);
    Task<IEnumerable<Timesheet>> GetAllAsync(string? status = null);
    Task<int> CreateAsync(Timesheet timesheet);
    Task UpdateAsync(Timesheet timesheet);
    Task SubmitAsync(int timesheetId);
    Task ApproveAsync(int timesheetId, int approverId);
    Task DeleteAsync(int timesheetId);
}

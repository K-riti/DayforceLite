using DayforceLite.Core.Exceptions;
using DayforceLite.Core.Interfaces;
using DayforceLite.Core.Models;

namespace DayforceLite.Core.Services;

public class TimesheetService : ITimesheetService
{
    private readonly ITimesheetRepository _repository;
    private readonly IEmployeeRepository _employeeRepository;

    public TimesheetService(ITimesheetRepository repository, IEmployeeRepository employeeRepository)
    {
        _repository = repository;
        _employeeRepository = employeeRepository;
    }

    public async Task<Timesheet?> GetByIdAsync(int timesheetId)
    {
        return await _repository.GetByIdAsync(timesheetId);
    }

    public async Task<IEnumerable<Timesheet>> GetByEmployeeIdAsync(int employeeId)
    {
        if (!await _employeeRepository.ExistsAsync(employeeId))
        {
            throw new NotFoundException(nameof(Employee), employeeId);
        }

        return await _repository.GetByEmployeeIdAsync(employeeId);
    }

    public async Task<IEnumerable<Timesheet>> GetAllAsync(string? status = null)
    {
        return await _repository.GetAllAsync(status);
    }

    public async Task<int> CreateAsync(Timesheet timesheet)
    {
        if (!await _employeeRepository.ExistsAsync(timesheet.EmployeeId))
        {
            throw new NotFoundException(nameof(Employee), timesheet.EmployeeId);
        }

        ValidateHours(timesheet);
        timesheet.Status = TimesheetStatus.Draft;

        return await _repository.CreateAsync(timesheet);
    }

    public async Task UpdateAsync(Timesheet timesheet)
    {
        var existing = await _repository.GetByIdAsync(timesheet.TimesheetId)
            ?? throw new NotFoundException(nameof(Timesheet), timesheet.TimesheetId);

        if (existing.Status == TimesheetStatus.Approved)
        {
            throw new InvalidOperationException("Cannot modify an approved timesheet");
        }

        ValidateHours(timesheet);
        await _repository.UpdateAsync(timesheet);
    }

    public async Task SubmitAsync(int timesheetId)
    {
        var timesheet = await _repository.GetByIdAsync(timesheetId)
            ?? throw new NotFoundException(nameof(Timesheet), timesheetId);

        if (timesheet.Status != TimesheetStatus.Draft)
        {
            throw new InvalidOperationException("Only draft timesheets can be submitted");
        }

        timesheet.Status = TimesheetStatus.Submitted;
        timesheet.SubmittedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(timesheet);
    }

    public async Task ApproveAsync(int timesheetId, int approverId)
    {
        var timesheet = await _repository.GetByIdAsync(timesheetId)
            ?? throw new NotFoundException(nameof(Timesheet), timesheetId);

        if (timesheet.Status != TimesheetStatus.Submitted)
        {
            throw new InvalidOperationException("Only submitted timesheets can be approved");
        }

        if (timesheet.EmployeeId == approverId)
        {
            throw new InvalidOperationException("Cannot approve your own timesheet");
        }

        timesheet.Status = TimesheetStatus.Approved;
        timesheet.ApprovedAt = DateTime.UtcNow;
        timesheet.ApprovedBy = approverId;

        await _repository.UpdateAsync(timesheet);
    }

    public async Task DeleteAsync(int timesheetId)
    {
        var timesheet = await _repository.GetByIdAsync(timesheetId)
            ?? throw new NotFoundException(nameof(Timesheet), timesheetId);

        if (timesheet.Status == TimesheetStatus.Approved)
        {
            throw new InvalidOperationException("Cannot delete an approved timesheet");
        }

        await _repository.DeleteAsync(timesheetId);
    }

    private static void ValidateHours(Timesheet timesheet)
    {
        if (timesheet.RegularHours < 0)
        {
            throw new ArgumentException("Regular hours cannot be negative", nameof(timesheet));
        }

        if (timesheet.OvertimeHours < 0)
        {
            throw new ArgumentException("Overtime hours cannot be negative", nameof(timesheet));
        }

        if (timesheet.RegularHours > 60)
        {
            throw new ArgumentException("Regular hours cannot exceed 60 per week", nameof(timesheet));
        }

        if (timesheet.OvertimeHours > 40)
        {
            throw new ArgumentException("Overtime hours cannot exceed 40 per week", nameof(timesheet));
        }
    }
}

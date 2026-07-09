using Microsoft.EntityFrameworkCore;
using DayforceLite.Core.Interfaces;
using DayforceLite.Core.Models;

namespace DayforceLite.Infrastructure.Data;

public class EfTimesheetRepository : ITimesheetRepository
{
    private readonly DayforceDbContext _context;

    public EfTimesheetRepository(DayforceDbContext context)
    {
        _context = context;
    }

    public async Task<Timesheet?> GetByIdAsync(int timesheetId)
    {
        return await _context.Timesheets
            .Include(t => t.Employee)
            .FirstOrDefaultAsync(t => t.TimesheetId == timesheetId);
    }

    public async Task<IEnumerable<Timesheet>> GetByEmployeeIdAsync(int employeeId)
    {
        return await _context.Timesheets
            .Where(t => t.EmployeeId == employeeId)
            .OrderByDescending(t => t.WeekStartDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Timesheet>> GetAllAsync(string? status = null)
    {
        var query = _context.Timesheets.Include(t => t.Employee).AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(t => t.Status == status);
        }

        return await query.OrderByDescending(t => t.WeekStartDate).ToListAsync();
    }

    public async Task<int> CreateAsync(Timesheet timesheet)
    {
        _context.Timesheets.Add(timesheet);
        await _context.SaveChangesAsync();
        return timesheet.TimesheetId;
    }

    public async Task UpdateAsync(Timesheet timesheet)
    {
        _context.Timesheets.Update(timesheet);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int timesheetId)
    {
        var timesheet = await _context.Timesheets.FindAsync(timesheetId);
        if (timesheet != null)
        {
            _context.Timesheets.Remove(timesheet);
            await _context.SaveChangesAsync();
        }
    }
}

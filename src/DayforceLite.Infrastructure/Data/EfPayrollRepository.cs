using Microsoft.EntityFrameworkCore;
using DayforceLite.Core.Interfaces;
using DayforceLite.Core.Models;

namespace DayforceLite.Infrastructure.Data;

public class EfPayrollRepository : IPayrollRepository
{
    private readonly DayforceDbContext _context;

    public EfPayrollRepository(DayforceDbContext context)
    {
        _context = context;
    }

    public async Task<PayrollRecord?> GetByIdAsync(int payrollId)
    {
        return await _context.PayrollRecords
            .Include(p => p.Employee)
            .FirstOrDefaultAsync(p => p.PayrollId == payrollId);
    }

    public async Task<IEnumerable<PayrollRecord>> GetByEmployeeIdAsync(int employeeId)
    {
        return await _context.PayrollRecords
            .Where(p => p.EmployeeId == employeeId)
            .OrderByDescending(p => p.PeriodEnd)
            .ToListAsync();
    }

    public async Task<IEnumerable<PayrollRecord>> GetByPeriodAsync(DateTime start, DateTime end)
    {
        return await _context.PayrollRecords
            .Include(p => p.Employee)
            .Where(p => p.PeriodStart >= start && p.PeriodEnd <= end)
            .OrderByDescending(p => p.ProcessedAt)
            .ToListAsync();
    }

    public async Task<int> CreateAsync(PayrollRecord record)
    {
        _context.PayrollRecords.Add(record);
        await _context.SaveChangesAsync();
        return record.PayrollId;
    }
}

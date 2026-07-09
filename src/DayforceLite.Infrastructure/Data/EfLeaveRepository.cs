using Microsoft.EntityFrameworkCore;
using DayforceLite.Core.Interfaces;
using DayforceLite.Core.Models;

namespace DayforceLite.Infrastructure.Data;

public class EfLeaveRepository : ILeaveRepository
{
    private readonly DayforceDbContext _context;

    public EfLeaveRepository(DayforceDbContext context)
    {
        _context = context;
    }

    public async Task<LeaveRequest?> GetRequestByIdAsync(int leaveRequestId)
    {
        return await _context.LeaveRequests
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.LeaveRequestId == leaveRequestId);
    }

    public async Task<IEnumerable<LeaveRequest>> GetRequestsByEmployeeAsync(int employeeId)
    {
        return await _context.LeaveRequests
            .Where(r => r.EmployeeId == employeeId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<LeaveRequest>> GetPendingRequestsAsync()
    {
        return await _context.LeaveRequests
            .Include(r => r.Employee)
            .Where(r => r.Status == LeaveRequestStatus.Pending)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<LeaveRequest>> GetRequestsByStatusAsync(string status)
    {
        return await _context.LeaveRequests
            .Include(r => r.Employee)
            .Where(r => r.Status == status)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> CreateRequestAsync(LeaveRequest request)
    {
        _context.LeaveRequests.Add(request);
        await _context.SaveChangesAsync();
        return request.LeaveRequestId;
    }

    public async Task UpdateRequestAsync(LeaveRequest request)
    {
        _context.LeaveRequests.Update(request);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> HasOverlappingRequestAsync(
        int employeeId, DateTime startDate, DateTime endDate, int? excludeRequestId = null)
    {
        var query = _context.LeaveRequests
            .Where(r => r.EmployeeId == employeeId)
            .Where(r => r.Status != LeaveRequestStatus.Cancelled && r.Status != LeaveRequestStatus.Rejected)
            .Where(r => r.StartDate <= endDate && r.EndDate >= startDate);

        if (excludeRequestId.HasValue)
        {
            query = query.Where(r => r.LeaveRequestId != excludeRequestId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<LeaveBalance?> GetBalanceAsync(int employeeId, int year)
    {
        return await _context.LeaveBalances
            .FirstOrDefaultAsync(b => b.EmployeeId == employeeId && b.Year == year);
    }

    public async Task<int> CreateBalanceAsync(LeaveBalance balance)
    {
        _context.LeaveBalances.Add(balance);
        await _context.SaveChangesAsync();
        return balance.LeaveBalanceId;
    }

    public async Task UpdateBalanceAsync(LeaveBalance balance)
    {
        _context.LeaveBalances.Update(balance);
        await _context.SaveChangesAsync();
    }
}

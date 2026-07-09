using DayforceLite.Core.Models;

namespace DayforceLite.Core.Interfaces;

public interface ILeaveRepository
{
    // Leave Requests
    Task<LeaveRequest?> GetRequestByIdAsync(int leaveRequestId);
    Task<IEnumerable<LeaveRequest>> GetRequestsByEmployeeAsync(int employeeId);
    Task<IEnumerable<LeaveRequest>> GetPendingRequestsAsync();
    Task<IEnumerable<LeaveRequest>> GetRequestsByStatusAsync(string status);
    Task<int> CreateRequestAsync(LeaveRequest request);
    Task UpdateRequestAsync(LeaveRequest request);
    Task<bool> HasOverlappingRequestAsync(int employeeId, DateTime startDate, DateTime endDate, int? excludeRequestId = null);

    // Leave Balances
    Task<LeaveBalance?> GetBalanceAsync(int employeeId, int year);
    Task<int> CreateBalanceAsync(LeaveBalance balance);
    Task UpdateBalanceAsync(LeaveBalance balance);
}

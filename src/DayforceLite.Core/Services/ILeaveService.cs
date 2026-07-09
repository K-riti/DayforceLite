using DayforceLite.Core.Models;

namespace DayforceLite.Core.Services;

public interface ILeaveService
{
    // Leave Requests
    Task<LeaveRequest> GetRequestByIdAsync(int leaveRequestId);
    Task<IEnumerable<LeaveRequest>> GetMyRequestsAsync(int employeeId);
    Task<IEnumerable<LeaveRequest>> GetPendingRequestsAsync();
    Task<int> SubmitRequestAsync(LeaveRequest request);
    Task ApproveRequestAsync(int leaveRequestId, int approverId, string? comments = null);
    Task RejectRequestAsync(int leaveRequestId, int approverId, string? comments = null);
    Task CancelRequestAsync(int leaveRequestId, int employeeId);

    // Leave Balances
    Task<LeaveBalance> GetBalanceAsync(int employeeId, int? year = null);
    Task InitializeBalanceAsync(int employeeId, int year);
}

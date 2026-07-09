using DayforceLite.Core.Exceptions;
using DayforceLite.Core.Interfaces;
using DayforceLite.Core.Models;

namespace DayforceLite.Core.Services;

public class LeaveService : ILeaveService
{
    private readonly ILeaveRepository _leaveRepository;
    private readonly IEmployeeRepository _employeeRepository;

    // Default leave allocations per year
    private const decimal DefaultVacationDays = 15m;
    private const decimal DefaultSickDays = 10m;
    private const decimal DefaultPersonalDays = 3m;

    public LeaveService(ILeaveRepository leaveRepository, IEmployeeRepository employeeRepository)
    {
        _leaveRepository = leaveRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<LeaveRequest> GetRequestByIdAsync(int leaveRequestId)
    {
        var request = await _leaveRepository.GetRequestByIdAsync(leaveRequestId);
        if (request is null)
        {
            throw new NotFoundException(nameof(LeaveRequest), leaveRequestId);
        }
        return request;
    }

    public async Task<IEnumerable<LeaveRequest>> GetMyRequestsAsync(int employeeId)
    {
        if (!await _employeeRepository.ExistsAsync(employeeId))
        {
            throw new NotFoundException(nameof(Employee), employeeId);
        }
        return await _leaveRepository.GetRequestsByEmployeeAsync(employeeId);
    }

    public async Task<IEnumerable<LeaveRequest>> GetPendingRequestsAsync()
    {
        return await _leaveRepository.GetPendingRequestsAsync();
    }

    public async Task<int> SubmitRequestAsync(LeaveRequest request)
    {
        // Validate employee exists
        if (!await _employeeRepository.ExistsAsync(request.EmployeeId))
        {
            throw new NotFoundException(nameof(Employee), request.EmployeeId);
        }

        // Validate leave type
        if (!LeaveTypes.IsValid(request.LeaveType))
        {
            throw new ArgumentException($"Invalid leave type: {request.LeaveType}");
        }

        // Validate dates
        if (request.StartDate > request.EndDate)
        {
            throw new ArgumentException("Start date must be before or equal to end date");
        }

        if (request.StartDate < DateTime.Today)
        {
            throw new ArgumentException("Cannot request leave for past dates");
        }

        // Calculate total days (excluding weekends for simplicity)
        request.TotalDays = CalculateBusinessDays(request.StartDate, request.EndDate);

        if (request.TotalDays <= 0)
        {
            throw new ArgumentException("Leave request must be for at least one business day");
        }

        // Check for overlapping requests
        if (await _leaveRepository.HasOverlappingRequestAsync(
            request.EmployeeId, request.StartDate, request.EndDate))
        {
            throw new InvalidOperationException("You already have a leave request for these dates");
        }

        // Check balance (skip for unpaid leave)
        if (request.LeaveType != LeaveTypes.Unpaid)
        {
            var balance = await GetOrCreateBalanceAsync(request.EmployeeId, request.StartDate.Year);
            var available = GetAvailableBalance(balance, request.LeaveType);

            if (request.TotalDays > available)
            {
                throw new InvalidOperationException(
                    $"Insufficient {request.LeaveType} balance. Available: {available}, Requested: {request.TotalDays}");
            }
        }

        request.Status = LeaveRequestStatus.Pending;
        request.CreatedAt = DateTime.UtcNow;

        return await _leaveRepository.CreateRequestAsync(request);
    }

    public async Task ApproveRequestAsync(int leaveRequestId, int approverId, string? comments = null)
    {
        var request = await GetRequestByIdAsync(leaveRequestId);

        if (request.Status != LeaveRequestStatus.Pending)
        {
            throw new InvalidOperationException("Only pending requests can be approved");
        }

        if (request.EmployeeId == approverId)
        {
            throw new InvalidOperationException("Cannot approve your own leave request");
        }

        // Deduct from balance
        if (request.LeaveType != LeaveTypes.Unpaid)
        {
            var balance = await GetOrCreateBalanceAsync(request.EmployeeId, request.StartDate.Year);
            DeductFromBalance(balance, request.LeaveType, request.TotalDays);
            await _leaveRepository.UpdateBalanceAsync(balance);
        }

        request.Status = LeaveRequestStatus.Approved;
        request.ApprovedBy = approverId;
        request.ApprovedAt = DateTime.UtcNow;
        request.ApproverComments = comments;
        request.UpdatedAt = DateTime.UtcNow;

        await _leaveRepository.UpdateRequestAsync(request);
    }

    public async Task RejectRequestAsync(int leaveRequestId, int approverId, string? comments = null)
    {
        var request = await GetRequestByIdAsync(leaveRequestId);

        if (request.Status != LeaveRequestStatus.Pending)
        {
            throw new InvalidOperationException("Only pending requests can be rejected");
        }

        request.Status = LeaveRequestStatus.Rejected;
        request.ApprovedBy = approverId;
        request.ApprovedAt = DateTime.UtcNow;
        request.ApproverComments = comments;
        request.UpdatedAt = DateTime.UtcNow;

        await _leaveRepository.UpdateRequestAsync(request);
    }

    public async Task CancelRequestAsync(int leaveRequestId, int employeeId)
    {
        var request = await GetRequestByIdAsync(leaveRequestId);

        if (request.EmployeeId != employeeId)
        {
            throw new InvalidOperationException("You can only cancel your own leave requests");
        }

        if (request.Status == LeaveRequestStatus.Cancelled)
        {
            throw new InvalidOperationException("Request is already cancelled");
        }

        // If approved, restore balance
        if (request.Status == LeaveRequestStatus.Approved && request.LeaveType != LeaveTypes.Unpaid)
        {
            var balance = await GetOrCreateBalanceAsync(request.EmployeeId, request.StartDate.Year);
            RestoreToBalance(balance, request.LeaveType, request.TotalDays);
            await _leaveRepository.UpdateBalanceAsync(balance);
        }

        request.Status = LeaveRequestStatus.Cancelled;
        request.UpdatedAt = DateTime.UtcNow;

        await _leaveRepository.UpdateRequestAsync(request);
    }

    public async Task<LeaveBalance> GetBalanceAsync(int employeeId, int? year = null)
    {
        var targetYear = year ?? DateTime.Today.Year;
        return await GetOrCreateBalanceAsync(employeeId, targetYear);
    }

    public async Task InitializeBalanceAsync(int employeeId, int year)
    {
        if (!await _employeeRepository.ExistsAsync(employeeId))
        {
            throw new NotFoundException(nameof(Employee), employeeId);
        }

        var existing = await _leaveRepository.GetBalanceAsync(employeeId, year);
        if (existing != null)
        {
            throw new InvalidOperationException($"Balance already exists for year {year}");
        }

        var balance = new LeaveBalance
        {
            EmployeeId = employeeId,
            Year = year,
            VacationDays = DefaultVacationDays,
            SickDays = DefaultSickDays,
            PersonalDays = DefaultPersonalDays,
            UpdatedAt = DateTime.UtcNow
        };

        await _leaveRepository.CreateBalanceAsync(balance);
    }

    private async Task<LeaveBalance> GetOrCreateBalanceAsync(int employeeId, int year)
    {
        var balance = await _leaveRepository.GetBalanceAsync(employeeId, year);

        if (balance is null)
        {
            balance = new LeaveBalance
            {
                EmployeeId = employeeId,
                Year = year,
                VacationDays = DefaultVacationDays,
                SickDays = DefaultSickDays,
                PersonalDays = DefaultPersonalDays,
                UpdatedAt = DateTime.UtcNow
            };
            balance.LeaveBalanceId = await _leaveRepository.CreateBalanceAsync(balance);
        }

        return balance;
    }

    private static decimal GetAvailableBalance(LeaveBalance balance, string leaveType)
    {
        return leaveType switch
        {
            LeaveTypes.Vacation => balance.VacationRemaining,
            LeaveTypes.Sick => balance.SickRemaining,
            LeaveTypes.Personal => balance.PersonalRemaining,
            _ => 0
        };
    }

    private static void DeductFromBalance(LeaveBalance balance, string leaveType, decimal days)
    {
        switch (leaveType)
        {
            case LeaveTypes.Vacation:
                balance.VacationUsed += days;
                break;
            case LeaveTypes.Sick:
                balance.SickUsed += days;
                break;
            case LeaveTypes.Personal:
                balance.PersonalUsed += days;
                break;
        }
        balance.UpdatedAt = DateTime.UtcNow;
    }

    private static void RestoreToBalance(LeaveBalance balance, string leaveType, decimal days)
    {
        switch (leaveType)
        {
            case LeaveTypes.Vacation:
                balance.VacationUsed = Math.Max(0, balance.VacationUsed - days);
                break;
            case LeaveTypes.Sick:
                balance.SickUsed = Math.Max(0, balance.SickUsed - days);
                break;
            case LeaveTypes.Personal:
                balance.PersonalUsed = Math.Max(0, balance.PersonalUsed - days);
                break;
        }
        balance.UpdatedAt = DateTime.UtcNow;
    }

    private static decimal CalculateBusinessDays(DateTime start, DateTime end)
    {
        decimal days = 0;
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
            {
                days++;
            }
        }
        return days;
    }
}

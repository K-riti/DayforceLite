using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DayforceLite.API.DTOs;
using DayforceLite.Core.Models;
using DayforceLite.Core.Services;

namespace DayforceLite.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaveController : ControllerBase
{
    private readonly ILeaveService _service;
    private readonly ILogger<LeaveController> _logger;

    public LeaveController(ILeaveService service, ILogger<LeaveController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("my-requests")]
    public async Task<ActionResult<IEnumerable<LeaveRequestDto>>> GetMyRequests()
    {
        var employeeId = GetCurrentEmployeeId();
        var requests = await _service.GetMyRequestsAsync(employeeId);
        return Ok(requests.Select(MapToDto));
    }

    [HttpGet("pending")]
    public async Task<ActionResult<IEnumerable<LeaveRequestDto>>> GetPendingRequests()
    {
        var requests = await _service.GetPendingRequestsAsync();
        return Ok(requests.Select(MapToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LeaveRequestDto>> GetById(int id)
    {
        var request = await _service.GetRequestByIdAsync(id);
        return Ok(MapToDto(request));
    }

    [HttpPost]
    public async Task<ActionResult<LeaveRequestDto>> Submit([FromBody] CreateLeaveRequest request)
    {
        var employeeId = GetCurrentEmployeeId();

        var leaveRequest = new LeaveRequest
        {
            EmployeeId = employeeId,
            LeaveType = request.LeaveType,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Reason = request.Reason
        };

        var id = await _service.SubmitRequestAsync(leaveRequest);
        leaveRequest.LeaveRequestId = id;

        _logger.LogInformation(
            "Leave request {RequestId} submitted by employee {EmployeeId}: {LeaveType} from {StartDate} to {EndDate}",
            id, employeeId, request.LeaveType, request.StartDate, request.EndDate);

        return CreatedAtAction(nameof(GetById), new { id }, MapToDto(leaveRequest));
    }

    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id, [FromBody] ApproveRejectRequest? request)
    {
        var approverId = GetCurrentEmployeeId();
        await _service.ApproveRequestAsync(id, approverId, request?.Comments);

        _logger.LogInformation("Leave request {RequestId} approved by {ApproverId}", id, approverId);

        return NoContent();
    }

    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] ApproveRejectRequest? request)
    {
        var approverId = GetCurrentEmployeeId();
        await _service.RejectRequestAsync(id, approverId, request?.Comments);

        _logger.LogInformation("Leave request {RequestId} rejected by {ApproverId}", id, approverId);

        return NoContent();
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var employeeId = GetCurrentEmployeeId();
        await _service.CancelRequestAsync(id, employeeId);

        _logger.LogInformation("Leave request {RequestId} cancelled by employee {EmployeeId}", id, employeeId);

        return NoContent();
    }

    [HttpGet("balance")]
    public async Task<ActionResult<LeaveBalanceDto>> GetMyBalance([FromQuery] int? year)
    {
        var employeeId = GetCurrentEmployeeId();
        var balance = await _service.GetBalanceAsync(employeeId, year);
        return Ok(MapBalanceToDto(balance));
    }

    [HttpGet("balance/{employeeId:int}")]
    public async Task<ActionResult<LeaveBalanceDto>> GetEmployeeBalance(int employeeId, [FromQuery] int? year)
    {
        var balance = await _service.GetBalanceAsync(employeeId, year);
        return Ok(MapBalanceToDto(balance));
    }

    [HttpGet("types")]
    [AllowAnonymous]
    public ActionResult<string[]> GetLeaveTypes()
    {
        return Ok(LeaveTypes.All);
    }

    private int GetCurrentEmployeeId()
    {
        // In production, extract from JWT claims
        return 1;
    }

    private static LeaveRequestDto MapToDto(LeaveRequest r) => new(
        r.LeaveRequestId,
        r.EmployeeId,
        r.Employee?.FullName,
        r.LeaveType,
        r.StartDate,
        r.EndDate,
        r.TotalDays,
        r.Reason,
        r.Status,
        r.ApprovedBy,
        r.ApprovedAt,
        r.ApproverComments,
        r.CreatedAt);

    private static LeaveBalanceDto MapBalanceToDto(LeaveBalance b) => new(
        b.EmployeeId,
        b.Year,
        b.VacationDays,
        b.VacationUsed,
        b.VacationRemaining,
        b.SickDays,
        b.SickUsed,
        b.SickRemaining,
        b.PersonalDays,
        b.PersonalUsed,
        b.PersonalRemaining);
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DayforceLite.API.DTOs;
using DayforceLite.Core.Models;
using DayforceLite.Core.Services;

namespace DayforceLite.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TimesheetController : ControllerBase
{
    private readonly ITimesheetService _service;
    private readonly ILogger<TimesheetController> _logger;

    public TimesheetController(ITimesheetService service, ILogger<TimesheetController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TimesheetDto>>> GetAll([FromQuery] string? status)
    {
        var timesheets = await _service.GetAllAsync(status);
        var dtos = timesheets.Select(MapToDto);
        return Ok(dtos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TimesheetDto>> GetById(int id)
    {
        var timesheet = await _service.GetByIdAsync(id);
        if (timesheet is null)
        {
            return NotFound();
        }
        return Ok(MapToDto(timesheet));
    }

    [HttpGet("employee/{employeeId:int}")]
    public async Task<ActionResult<IEnumerable<TimesheetDto>>> GetByEmployee(int employeeId)
    {
        var timesheets = await _service.GetByEmployeeIdAsync(employeeId);
        var dtos = timesheets.Select(MapToDto);
        return Ok(dtos);
    }

    [HttpPost]
    public async Task<ActionResult<TimesheetDto>> Create([FromBody] CreateTimesheetRequest request)
    {
        var timesheet = new Timesheet
        {
            EmployeeId = request.EmployeeId,
            WeekStartDate = request.WeekStartDate,
            RegularHours = request.RegularHours,
            OvertimeHours = request.OvertimeHours
        };

        var id = await _service.CreateAsync(timesheet);
        timesheet.TimesheetId = id;

        return CreatedAtAction(nameof(GetById), new { id }, MapToDto(timesheet));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTimesheetRequest request)
    {
        var timesheet = await _service.GetByIdAsync(id);
        if (timesheet is null)
        {
            return NotFound();
        }

        timesheet.RegularHours = request.RegularHours;
        timesheet.OvertimeHours = request.OvertimeHours;

        await _service.UpdateAsync(timesheet);
        return NoContent();
    }

    [HttpPost("{id:int}/submit")]
    public async Task<IActionResult> Submit(int id)
    {
        await _service.SubmitAsync(id);
        return NoContent();
    }

    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        // Get approver ID from JWT claims
        var approverId = GetCurrentUserId();
        await _service.ApproveAsync(id, approverId);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    private int GetCurrentUserId()
    {
        // In production, extract from JWT claims
        return 1;
    }

    private static TimesheetDto MapToDto(Timesheet t) => new(
        t.TimesheetId,
        t.EmployeeId,
        t.WeekStartDate,
        t.RegularHours,
        t.OvertimeHours,
        t.Status,
        t.SubmittedAt,
        t.ApprovedAt);
}

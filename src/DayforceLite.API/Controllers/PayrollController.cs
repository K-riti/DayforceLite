using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DayforceLite.API.DTOs;
using DayforceLite.Core.Services;

namespace DayforceLite.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PayrollController : ControllerBase
{
    private readonly IPayrollService _payrollService;
    private readonly ILogger<PayrollController> _logger;

    public PayrollController(IPayrollService payrollService, ILogger<PayrollController> logger)
    {
        _payrollService = payrollService;
        _logger = logger;
    }

    [HttpPost("process")]
    public async Task<ActionResult<PayrollRecordDto>> ProcessPayroll([FromBody] ProcessPayrollRequest request)
    {
        var record = await _payrollService.ProcessPayrollAsync(
            request.EmployeeId,
            request.PeriodStart,
            request.PeriodEnd);

        var dto = new PayrollRecordDto(
            record.PayrollId,
            record.EmployeeId,
            record.PeriodStart,
            record.PeriodEnd,
            record.GrossPay,
            record.TaxDeduction,
            record.NetPay,
            record.ProcessedAt);

        return Ok(dto);
    }

    [HttpGet("employee/{employeeId:int}")]
    public async Task<ActionResult<IEnumerable<PayrollRecordDto>>> GetPayrollHistory(int employeeId)
    {
        var records = await _payrollService.GetPayrollHistoryAsync(employeeId);
        var dtos = records.Select(r => new PayrollRecordDto(
            r.PayrollId,
            r.EmployeeId,
            r.PeriodStart,
            r.PeriodEnd,
            r.GrossPay,
            r.TaxDeduction,
            r.NetPay,
            r.ProcessedAt));

        return Ok(dtos);
    }

    [HttpGet("calculate")]
    public ActionResult<decimal> CalculateGrossPay(
        [FromQuery] decimal regularHours,
        [FromQuery] decimal overtimeHours,
        [FromQuery] decimal hourlyRate)
    {
        var grossPay = _payrollService.CalculateGrossPay(regularHours, overtimeHours, hourlyRate);
        return Ok(new { GrossPay = grossPay });
    }
}

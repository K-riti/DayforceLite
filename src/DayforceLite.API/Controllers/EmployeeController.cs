using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DayforceLite.API.DTOs;
using DayforceLite.Core.Models;
using DayforceLite.Core.Services;
using DayforceLite.Infrastructure.Search;

namespace DayforceLite.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeeController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly ElasticSearchService _searchService;
    private readonly ILogger<EmployeeController> _logger;

    public EmployeeController(
        IEmployeeService employeeService,
        ElasticSearchService searchService,
        ILogger<EmployeeController> logger)
    {
        _employeeService = employeeService;
        _searchService = searchService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetAll([FromQuery] string? search)
    {
        var employees = await _employeeService.GetAllAsync(search);
        var dtos = employees.Select(MapToDto);
        return Ok(dtos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmployeeDto>> GetById(int id)
    {
        var employee = await _employeeService.GetByIdAsync(id);
        return Ok(MapToDto(employee));
    }

    [HttpPost]
    public async Task<ActionResult<EmployeeDto>> Create([FromBody] CreateEmployeeRequest request)
    {
        var employee = new Employee
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            DepartmentId = request.DepartmentId,
            HourlyRate = request.HourlyRate,
            StartDate = request.StartDate
        };

        var id = await _employeeService.CreateAsync(employee);
        employee.EmployeeId = id;

        // Index in Elasticsearch
        try
        {
            await _searchService.IndexEmployeeAsync(employee);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to index employee {EmployeeId} in Elasticsearch", id);
        }

        return CreatedAtAction(nameof(GetById), new { id }, MapToDto(employee));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEmployeeRequest request)
    {
        var employee = new Employee
        {
            EmployeeId = id,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            DepartmentId = request.DepartmentId,
            HourlyRate = request.HourlyRate,
            StartDate = request.StartDate
        };

        await _employeeService.UpdateAsync(employee);

        // Update in Elasticsearch
        try
        {
            await _searchService.IndexEmployeeAsync(employee);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update employee {EmployeeId} in Elasticsearch", id);
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _employeeService.DeleteAsync(id);

        // Remove from Elasticsearch
        try
        {
            await _searchService.DeleteEmployeeAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete employee {EmployeeId} from Elasticsearch", id);
        }

        return NoContent();
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<int>>> Search([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest("Search query is required");
        }

        var employeeIds = await _searchService.SearchAsync(query);
        return Ok(employeeIds);
    }

    [HttpGet("{id:int}/payroll-summary")]
    public async Task<ActionResult<PayrollSummaryDto>> GetPayrollSummary(
        int id,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        var summary = await _employeeService.GetPayrollSummaryAsync(id, from, to);
        return Ok(new PayrollSummaryDto(
            summary.EmployeeId,
            summary.FullName,
            summary.TotalGross,
            summary.TotalNet,
            summary.PayslipCount));
    }

    private static EmployeeDto MapToDto(Employee e) => new(
        e.EmployeeId,
        e.FirstName,
        e.LastName,
        e.Email,
        e.DepartmentId,
        e.HourlyRate,
        e.StartDate,
        e.Department);
}

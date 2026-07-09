using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DayforceLite.API.DTOs;
using DayforceLite.Core.Models;
using DayforceLite.Core.Services;

namespace DayforceLite.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DepartmentController : ControllerBase
{
    private readonly IDepartmentService _service;
    private readonly ILogger<DepartmentController> _logger;

    public DepartmentController(IDepartmentService service, ILogger<DepartmentController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DepartmentDto>>> GetAll(
        [FromQuery] bool includeInactive = false)
    {
        var departments = await _service.GetAllAsync(includeInactive);
        var dtos = departments.Select(MapToDto);
        return Ok(dtos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DepartmentDto>> GetById(int id)
    {
        var department = await _service.GetByIdAsync(id);
        return Ok(MapToDto(department));
    }

    [HttpPost]
    public async Task<ActionResult<DepartmentDto>> Create([FromBody] CreateDepartmentRequest request)
    {
        var department = new Department
        {
            Name = request.Name,
            CostCentre = request.CostCentre,
            Description = request.Description,
            ManagerId = request.ManagerId
        };

        var id = await _service.CreateAsync(department);
        department.DepartmentId = id;

        _logger.LogInformation("Department {DepartmentId} created: {Name}", id, department.Name);

        return CreatedAtAction(nameof(GetById), new { id }, MapToDto(department));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDepartmentRequest request)
    {
        var department = new Department
        {
            DepartmentId = id,
            Name = request.Name,
            CostCentre = request.CostCentre,
            Description = request.Description,
            ManagerId = request.ManagerId
        };

        await _service.UpdateAsync(department);

        _logger.LogInformation("Department {DepartmentId} updated", id);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);

        _logger.LogInformation("Department {DepartmentId} deleted", id);

        return NoContent();
    }

    private static DepartmentDto MapToDto(Department d) => new(
        d.DepartmentId,
        d.Name,
        d.CostCentre,
        d.Description,
        d.ManagerId,
        d.IsActive,
        d.EmployeeCount,
        d.CreatedAt,
        d.UpdatedAt);
}

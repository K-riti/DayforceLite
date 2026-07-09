using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using DayforceLite.API.Controllers;
using DayforceLite.API.DTOs;
using DayforceLite.Core.Exceptions;
using DayforceLite.Core.Models;
using DayforceLite.Core.Services;
using DayforceLite.Infrastructure.Search;

namespace DayforceLite.UnitTests.Controllers;

public class EmployeeControllerTests
{
    private readonly Mock<IEmployeeService> _serviceMock = new();
    private readonly Mock<ElasticSearchService> _searchServiceMock;
    private readonly Mock<ILogger<EmployeeController>> _loggerMock = new();
    private readonly EmployeeController _sut;

    public EmployeeControllerTests()
    {
        // ElasticSearchService requires IConfiguration, using mock with constructor parameter
        _searchServiceMock = new Mock<ElasticSearchService>(MockBehavior.Loose, 
            new object[] { Mock.Of<Microsoft.Extensions.Configuration.IConfiguration>() });

        _sut = new EmployeeController(
            _serviceMock.Object,
            _searchServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithEmployees()
    {
        // Arrange
        var employees = new List<Employee>
        {
            new() { EmployeeId = 1, FirstName = "John", LastName = "Doe", Email = "john@test.com", Department = "IT" }
        };
        _serviceMock.Setup(s => s.GetAllAsync(null)).ReturnsAsync(employees);

        // Act
        var result = await _sut.GetAll(null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dtos = Assert.IsAssignableFrom<IEnumerable<EmployeeDto>>(okResult.Value);
        Assert.Single(dtos);
    }

    [Fact]
    public async Task GetById_WhenFound_ReturnsOkWithEmployee()
    {
        // Arrange
        var employee = new Employee 
        { 
            EmployeeId = 1, 
            FirstName = "John", 
            LastName = "Doe", 
            Email = "john@test.com",
            DepartmentId = 1,
            HourlyRate = 50m,
            StartDate = DateTime.Today,
            Department = "IT"
        };
        _serviceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(employee);

        // Act
        var result = await _sut.GetById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<EmployeeDto>(okResult.Value);
        Assert.Equal("John", dto.FirstName);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtAction()
    {
        // Arrange
        var request = new CreateEmployeeRequest("John", "Doe", "john@test.com", 1, 50m, DateTime.Today);
        _serviceMock.Setup(s => s.CreateAsync(It.IsAny<Employee>())).ReturnsAsync(1);

        // Act
        var result = await _sut.Create(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(EmployeeController.GetById), createdResult.ActionName);
    }

    [Fact]
    public async Task Update_ReturnsNoContent()
    {
        // Arrange
        var request = new UpdateEmployeeRequest("John", "Doe", "john@test.com", 1, 55m, DateTime.Today);
        _serviceMock.Setup(s => s.UpdateAsync(It.IsAny<Employee>())).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Update(1, request);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        // Arrange
        _serviceMock.Setup(s => s.DeleteAsync(1)).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Delete(1);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Search_WithEmptyQuery_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.Search("");

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }
}

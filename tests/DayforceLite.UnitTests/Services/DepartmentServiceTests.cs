using Moq;
using Xunit;
using DayforceLite.Core.Exceptions;
using DayforceLite.Core.Interfaces;
using DayforceLite.Core.Models;
using DayforceLite.Core.Services;

namespace DayforceLite.UnitTests.Services;

public class DepartmentServiceTests
{
    private readonly Mock<IDepartmentRepository> _mockRepository;
    private readonly DepartmentService _service;

    public DepartmentServiceTests()
    {
        _mockRepository = new Mock<IDepartmentRepository>();
        _service = new DepartmentService(_mockRepository.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingDepartment_ReturnsDepartment()
    {
        // Arrange
        var department = new Department { DepartmentId = 1, Name = "Engineering", CostCentre = "ENG-001" };
        _mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(department);
        _mockRepository.Setup(r => r.GetEmployeeCountAsync(1)).ReturnsAsync(5);

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Engineering", result.Name);
        Assert.Equal(5, result.EmployeeCount);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingDepartment_ThrowsNotFoundException()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Department?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(999));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllDepartmentsWithEmployeeCounts()
    {
        // Arrange
        var departments = new List<Department>
        {
            new() { DepartmentId = 1, Name = "Engineering", CostCentre = "ENG-001" },
            new() { DepartmentId = 2, Name = "HR", CostCentre = "HR-001" }
        };
        _mockRepository.Setup(r => r.GetAllAsync(false)).ReturnsAsync(departments);
        _mockRepository.Setup(r => r.GetEmployeeCountAsync(1)).ReturnsAsync(10);
        _mockRepository.Setup(r => r.GetEmployeeCountAsync(2)).ReturnsAsync(3);

        // Act
        var result = (await _service.GetAllAsync()).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(10, result[0].EmployeeCount);
        Assert.Equal(3, result[1].EmployeeCount);
    }

    [Fact]
    public async Task CreateAsync_ValidDepartment_CreatesDepartment()
    {
        // Arrange
        var department = new Department { Name = "Finance", CostCentre = "FIN-001" };
        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<Department>())).ReturnsAsync(1);

        // Act
        var result = await _service.CreateAsync(department);

        // Assert
        Assert.Equal(1, result);
        Assert.True(department.IsActive);
        Assert.True(department.CreatedAt <= DateTime.UtcNow);
        _mockRepository.Verify(r => r.CreateAsync(department), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_EmptyName_ThrowsArgumentException()
    {
        // Arrange
        var department = new Department { Name = "", CostCentre = "FIN-001" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(department));
    }

    [Fact]
    public async Task CreateAsync_EmptyCostCentre_ThrowsArgumentException()
    {
        // Arrange
        var department = new Department { Name = "Finance", CostCentre = "" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(department));
    }

    [Fact]
    public async Task UpdateAsync_ExistingDepartment_UpdatesDepartment()
    {
        // Arrange
        var department = new Department { DepartmentId = 1, Name = "Engineering Updated", CostCentre = "ENG-002" };
        _mockRepository.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);

        // Act
        await _service.UpdateAsync(department);

        // Assert
        Assert.NotNull(department.UpdatedAt);
        _mockRepository.Verify(r => r.UpdateAsync(department), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingDepartment_ThrowsNotFoundException()
    {
        // Arrange
        var department = new Department { DepartmentId = 999, Name = "Test", CostCentre = "TST-001" };
        _mockRepository.Setup(r => r.ExistsAsync(999)).ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(department));
    }

    [Fact]
    public async Task DeleteAsync_DepartmentWithNoActiveEmployees_Deletes()
    {
        // Arrange
        _mockRepository.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
        _mockRepository.Setup(r => r.HasActiveEmployeesAsync(1)).ReturnsAsync(false);

        // Act
        await _service.DeleteAsync(1);

        // Assert
        _mockRepository.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_DepartmentWithActiveEmployees_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockRepository.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
        _mockRepository.Setup(r => r.HasActiveEmployeesAsync(1)).ReturnsAsync(true);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteAsync(1));
        Assert.Contains("active employees", ex.Message);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingDepartment_ThrowsNotFoundException()
    {
        // Arrange
        _mockRepository.Setup(r => r.ExistsAsync(999)).ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(999));
    }
}

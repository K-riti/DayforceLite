using Moq;
using Xunit;
using DayforceLite.Core.Exceptions;
using DayforceLite.Core.Interfaces;
using DayforceLite.Core.Models;
using DayforceLite.Core.Services;

namespace DayforceLite.UnitTests.Services;

public class EmployeeServiceTests
{
    private readonly Mock<IEmployeeRepository> _repoMock = new();
    private readonly EmployeeService _sut;

    public EmployeeServiceTests()
    {
        _sut = new EmployeeService(_repoMock.Object);
    }

    [Fact]
    public async Task GetById_WhenEmployeeExists_ReturnsEmployee()
    {
        // Arrange
        var expected = new Employee { EmployeeId = 1, FirstName = "Kriti" };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(expected);

        // Act
        var result = await _sut.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Kriti", result.FirstName);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Employee?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(99));
    }

    [Fact]
    public async Task GetAll_ReturnsAllEmployees()
    {
        // Arrange
        var employees = new List<Employee>
        {
            new() { EmployeeId = 1, FirstName = "John" },
            new() { EmployeeId = 2, FirstName = "Jane" }
        };
        _repoMock.Setup(r => r.GetAllAsync(null)).ReturnsAsync(employees);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task CreateAsync_SetsCreatedAtAndIsActive()
    {
        // Arrange
        var employee = new Employee
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com"
        };
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Employee>())).ReturnsAsync(1);

        // Act
        var result = await _sut.CreateAsync(employee);

        // Assert
        Assert.Equal(1, result);
        Assert.True(employee.IsActive);
        Assert.True(employee.CreatedAt > DateTime.MinValue);
    }

    [Fact]
    public async Task UpdateAsync_WhenEmployeeNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var employee = new Employee { EmployeeId = 99 };
        _repoMock.Setup(r => r.ExistsAsync(99)).ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.UpdateAsync(employee));
    }

    [Fact]
    public async Task UpdateAsync_WhenEmployeeExists_UpdatesSuccessfully()
    {
        // Arrange
        var employee = new Employee { EmployeeId = 1, FirstName = "Updated" };
        _repoMock.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Employee>())).Returns(Task.CompletedTask);

        // Act
        await _sut.UpdateAsync(employee);

        // Assert
        _repoMock.Verify(r => r.UpdateAsync(employee), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenEmployeeNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _repoMock.Setup(r => r.ExistsAsync(99)).ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(99));
    }

    [Fact]
    public async Task DeleteAsync_WhenEmployeeExists_DeletesSuccessfully()
    {
        // Arrange
        _repoMock.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
        _repoMock.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);

        // Act
        await _sut.DeleteAsync(1);

        // Assert
        _repoMock.Verify(r => r.DeleteAsync(1), Times.Once);
    }
}

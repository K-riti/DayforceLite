using Moq;
using Xunit;
using DayforceLite.Core.Exceptions;
using DayforceLite.Core.Interfaces;
using DayforceLite.Core.Models;
using DayforceLite.Core.Services;

namespace DayforceLite.UnitTests.Services;

public class TimesheetServiceTests
{
    private readonly Mock<ITimesheetRepository> _timesheetRepoMock = new();
    private readonly Mock<IEmployeeRepository> _employeeRepoMock = new();
    private readonly TimesheetService _sut;

    public TimesheetServiceTests()
    {
        _sut = new TimesheetService(_timesheetRepoMock.Object, _employeeRepoMock.Object);
    }

    [Fact]
    public async Task CreateAsync_WhenEmployeeNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var timesheet = new Timesheet { EmployeeId = 99, RegularHours = 40 };
        _employeeRepoMock.Setup(r => r.ExistsAsync(99)).ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.CreateAsync(timesheet));
    }

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesTimesheet()
    {
        // Arrange
        var timesheet = new Timesheet { EmployeeId = 1, RegularHours = 40, OvertimeHours = 5 };
        _employeeRepoMock.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
        _timesheetRepoMock.Setup(r => r.CreateAsync(It.IsAny<Timesheet>())).ReturnsAsync(1);

        // Act
        var result = await _sut.CreateAsync(timesheet);

        // Assert
        Assert.Equal(1, result);
        Assert.Equal(TimesheetStatus.Draft, timesheet.Status);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -5)]
    [InlineData(70, 0)]
    [InlineData(0, 50)]
    public async Task CreateAsync_WithInvalidHours_ThrowsArgumentException(decimal regular, decimal overtime)
    {
        // Arrange
        var timesheet = new Timesheet { EmployeeId = 1, RegularHours = regular, OvertimeHours = overtime };
        _employeeRepoMock.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(timesheet));
    }

    [Fact]
    public async Task SubmitAsync_WhenTimesheetNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _timesheetRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Timesheet?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.SubmitAsync(99));
    }

    [Fact]
    public async Task SubmitAsync_WhenNotDraft_ThrowsInvalidOperationException()
    {
        // Arrange
        var timesheet = new Timesheet { TimesheetId = 1, Status = TimesheetStatus.Submitted };
        _timesheetRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(timesheet);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.SubmitAsync(1));
    }

    [Fact]
    public async Task SubmitAsync_WhenDraft_UpdatesStatusAndSubmittedAt()
    {
        // Arrange
        var timesheet = new Timesheet { TimesheetId = 1, Status = TimesheetStatus.Draft };
        _timesheetRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(timesheet);
        _timesheetRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Timesheet>())).Returns(Task.CompletedTask);

        // Act
        await _sut.SubmitAsync(1);

        // Assert
        Assert.Equal(TimesheetStatus.Submitted, timesheet.Status);
        Assert.NotNull(timesheet.SubmittedAt);
    }

    [Fact]
    public async Task ApproveAsync_WhenNotSubmitted_ThrowsInvalidOperationException()
    {
        // Arrange
        var timesheet = new Timesheet { TimesheetId = 1, Status = TimesheetStatus.Draft };
        _timesheetRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(timesheet);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ApproveAsync(1, 2));
    }

    [Fact]
    public async Task ApproveAsync_WhenApproverIsOwner_ThrowsInvalidOperationException()
    {
        // Arrange
        var timesheet = new Timesheet { TimesheetId = 1, EmployeeId = 1, Status = TimesheetStatus.Submitted };
        _timesheetRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(timesheet);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ApproveAsync(1, 1));
    }

    [Fact]
    public async Task ApproveAsync_WhenValid_UpdatesStatusAndApprovalDetails()
    {
        // Arrange
        var timesheet = new Timesheet { TimesheetId = 1, EmployeeId = 1, Status = TimesheetStatus.Submitted };
        _timesheetRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(timesheet);
        _timesheetRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Timesheet>())).Returns(Task.CompletedTask);

        // Act
        await _sut.ApproveAsync(1, 2);

        // Assert
        Assert.Equal(TimesheetStatus.Approved, timesheet.Status);
        Assert.NotNull(timesheet.ApprovedAt);
        Assert.Equal(2, timesheet.ApprovedBy);
    }

    [Fact]
    public async Task DeleteAsync_WhenApproved_ThrowsInvalidOperationException()
    {
        // Arrange
        var timesheet = new Timesheet { TimesheetId = 1, Status = TimesheetStatus.Approved };
        _timesheetRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(timesheet);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.DeleteAsync(1));
    }

    [Fact]
    public async Task UpdateAsync_WhenApproved_ThrowsInvalidOperationException()
    {
        // Arrange
        var existing = new Timesheet { TimesheetId = 1, Status = TimesheetStatus.Approved };
        var updated = new Timesheet { TimesheetId = 1, RegularHours = 45 };
        _timesheetRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.UpdateAsync(updated));
    }
}

using Moq;
using Xunit;
using DayforceLite.Core.Exceptions;
using DayforceLite.Core.Interfaces;
using DayforceLite.Core.Models;
using DayforceLite.Core.Services;

namespace DayforceLite.UnitTests.Services;

public class PayrollServiceTests
{
    private readonly Mock<IPayrollRepository> _payrollRepoMock = new();
    private readonly Mock<ITimesheetRepository> _timesheetRepoMock = new();
    private readonly Mock<IEmployeeRepository> _employeeRepoMock = new();
    private readonly PayrollService _sut;

    public PayrollServiceTests()
    {
        _sut = new PayrollService(
            _payrollRepoMock.Object,
            _timesheetRepoMock.Object,
            _employeeRepoMock.Object);
    }

    [Fact]
    public void CalculateGrossPay_WithRegularAndOvertime_ReturnsCorrectAmount()
    {
        // Arrange
        decimal regularHours = 40m;
        decimal overtimeHours = 8m;
        decimal hourlyRate = 100m;

        // Act
        var result = _sut.CalculateGrossPay(regularHours, overtimeHours, hourlyRate);

        // Assert
        // Regular: 40 * 100 = 4000, OT: 8 * 100 * 1.5 = 1200, Total: 5200
        Assert.Equal(5200m, result);
    }

    [Fact]
    public void CalculateTax_Returns20PercentOfGross()
    {
        // Arrange
        decimal grossPay = 5000m;

        // Act
        var result = _sut.CalculateTax(grossPay);

        // Assert
        Assert.Equal(1000m, result);
    }

    [Fact]
    public async Task ProcessPayrollAsync_WhenEmployeeNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _employeeRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Employee?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => 
            _sut.ProcessPayrollAsync(99, DateTime.Today.AddDays(-7), DateTime.Today));
    }

    [Fact]
    public async Task ProcessPayrollAsync_CreatesPayrollRecord()
    {
        // Arrange
        var employee = new Employee { EmployeeId = 1, HourlyRate = 50m };
        var timesheets = new List<Timesheet>
        {
            new() { EmployeeId = 1, WeekStartDate = DateTime.Today.AddDays(-7), RegularHours = 40, OvertimeHours = 5, Status = "Approved" }
        };

        _employeeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(employee);
        _timesheetRepoMock.Setup(r => r.GetByEmployeeIdAsync(1)).ReturnsAsync(timesheets);
        _payrollRepoMock.Setup(r => r.CreateAsync(It.IsAny<PayrollRecord>())).ReturnsAsync(1);

        // Act
        var result = await _sut.ProcessPayrollAsync(1, DateTime.Today.AddDays(-14), DateTime.Today);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.EmployeeId);
        // Regular: 40 * 50 = 2000, OT: 5 * 50 * 1.5 = 375, Total: 2375
        Assert.Equal(2375m, result.GrossPay);
    }

    [Fact]
    public async Task GetPayrollHistoryAsync_WhenEmployeeNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _employeeRepoMock.Setup(r => r.ExistsAsync(99)).ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetPayrollHistoryAsync(99));
    }

    [Fact]
    public async Task GetPayrollHistoryAsync_ReturnsRecords()
    {
        // Arrange
        var records = new List<PayrollRecord>
        {
            new() { PayrollId = 1, EmployeeId = 1, GrossPay = 5000m },
            new() { PayrollId = 2, EmployeeId = 1, GrossPay = 5200m }
        };

        _employeeRepoMock.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
        _payrollRepoMock.Setup(r => r.GetByEmployeeIdAsync(1)).ReturnsAsync(records);

        // Act
        var result = await _sut.GetPayrollHistoryAsync(1);

        // Assert
        Assert.Equal(2, result.Count());
    }
}

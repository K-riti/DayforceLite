using Moq;
using Xunit;
using DayforceLite.Core.Exceptions;
using DayforceLite.Core.Interfaces;
using DayforceLite.Core.Models;
using DayforceLite.Core.Services;

namespace DayforceLite.UnitTests.Services;

public class LeaveServiceTests
{
    private readonly Mock<ILeaveRepository> _mockLeaveRepo;
    private readonly Mock<IEmployeeRepository> _mockEmployeeRepo;
    private readonly LeaveService _service;

    public LeaveServiceTests()
    {
        _mockLeaveRepo = new Mock<ILeaveRepository>();
        _mockEmployeeRepo = new Mock<IEmployeeRepository>();
        _service = new LeaveService(_mockLeaveRepo.Object, _mockEmployeeRepo.Object);
    }

    [Fact]
    public async Task GetRequestByIdAsync_ExistingRequest_ReturnsRequest()
    {
        // Arrange
        var request = new LeaveRequest 
        { 
            LeaveRequestId = 1, 
            EmployeeId = 1, 
            LeaveType = LeaveTypes.Vacation,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(5),
            Status = LeaveRequestStatus.Pending
        };
        _mockLeaveRepo.Setup(r => r.GetRequestByIdAsync(1)).ReturnsAsync(request);

        // Act
        var result = await _service.GetRequestByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(LeaveTypes.Vacation, result.LeaveType);
    }

    [Fact]
    public async Task GetRequestByIdAsync_NonExistingRequest_ThrowsNotFoundException()
    {
        // Arrange
        _mockLeaveRepo.Setup(r => r.GetRequestByIdAsync(999)).ReturnsAsync((LeaveRequest?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetRequestByIdAsync(999));
    }

    [Fact]
    public async Task SubmitRequestAsync_ValidRequest_CreatesRequest()
    {
        // Arrange
        var request = new LeaveRequest
        {
            EmployeeId = 1,
            LeaveType = LeaveTypes.Vacation,
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddDays(3),
            Reason = "Family vacation"
        };

        var balance = new LeaveBalance
        {
            EmployeeId = 1,
            Year = DateTime.Today.Year,
            VacationDays = 15,
            VacationUsed = 0
        };

        _mockEmployeeRepo.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
        _mockLeaveRepo.Setup(r => r.HasOverlappingRequestAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
            .ReturnsAsync(false);
        _mockLeaveRepo.Setup(r => r.GetBalanceAsync(1, DateTime.Today.Year)).ReturnsAsync(balance);
        _mockLeaveRepo.Setup(r => r.CreateRequestAsync(It.IsAny<LeaveRequest>())).ReturnsAsync(1);

        // Act
        var result = await _service.SubmitRequestAsync(request);

        // Assert
        Assert.Equal(1, result);
        Assert.Equal(LeaveRequestStatus.Pending, request.Status);
        _mockLeaveRepo.Verify(r => r.CreateRequestAsync(request), Times.Once);
    }

    [Fact]
    public async Task SubmitRequestAsync_EndDateBeforeStartDate_ThrowsArgumentException()
    {
        // Arrange
        var request = new LeaveRequest
        {
            EmployeeId = 1,
            LeaveType = LeaveTypes.Vacation,
            StartDate = DateTime.Today.AddDays(5),
            EndDate = DateTime.Today.AddDays(1) // End before start
        };

        _mockEmployeeRepo.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.SubmitRequestAsync(request));
        Assert.Contains("Start date", ex.Message);
    }

    [Fact]
    public async Task SubmitRequestAsync_OverlappingRequest_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new LeaveRequest
        {
            EmployeeId = 1,
            LeaveType = LeaveTypes.Vacation,
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddDays(3)
        };

        _mockEmployeeRepo.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
        _mockLeaveRepo.Setup(r => r.HasOverlappingRequestAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
            .ReturnsAsync(true);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SubmitRequestAsync(request));
        Assert.Contains("already have a leave request", ex.Message);
    }

    [Fact]
    public async Task SubmitRequestAsync_InsufficientBalance_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new LeaveRequest
        {
            EmployeeId = 1,
            LeaveType = LeaveTypes.Vacation,
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddDays(20) // Request 20 days
        };

        var balance = new LeaveBalance
        {
            EmployeeId = 1,
            Year = DateTime.Today.Year,
            VacationDays = 15,
            VacationUsed = 10 // Only 5 remaining
        };

        _mockEmployeeRepo.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
        _mockLeaveRepo.Setup(r => r.HasOverlappingRequestAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
            .ReturnsAsync(false);
        _mockLeaveRepo.Setup(r => r.GetBalanceAsync(1, DateTime.Today.Year)).ReturnsAsync(balance);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SubmitRequestAsync(request));
        Assert.Contains("Insufficient", ex.Message);
    }

    [Fact]
    public async Task ApproveRequestAsync_PendingRequest_ApprovesAndDeductsBalance()
    {
        // Arrange
        var request = new LeaveRequest
        {
            LeaveRequestId = 1,
            EmployeeId = 1,
            LeaveType = LeaveTypes.Vacation,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(2),
            TotalDays = 3,
            Status = LeaveRequestStatus.Pending
        };

        var balance = new LeaveBalance
        {
            EmployeeId = 1,
            Year = DateTime.Today.Year,
            VacationDays = 15,
            VacationUsed = 0
        };

        _mockLeaveRepo.Setup(r => r.GetRequestByIdAsync(1)).ReturnsAsync(request);
        _mockLeaveRepo.Setup(r => r.GetBalanceAsync(1, DateTime.Today.Year)).ReturnsAsync(balance);

        // Act
        await _service.ApproveRequestAsync(1, 2, "Approved for vacation");

        // Assert
        Assert.Equal(LeaveRequestStatus.Approved, request.Status);
        Assert.Equal(2, request.ApprovedBy);
        Assert.NotNull(request.ApprovedAt);
        Assert.Equal("Approved for vacation", request.ApproverComments);
        Assert.Equal(3, balance.VacationUsed); // Balance deducted
        _mockLeaveRepo.Verify(r => r.UpdateRequestAsync(request), Times.Once);
        _mockLeaveRepo.Verify(r => r.UpdateBalanceAsync(balance), Times.Once);
    }

    [Fact]
    public async Task ApproveRequestAsync_AlreadyApproved_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new LeaveRequest
        {
            LeaveRequestId = 1,
            Status = LeaveRequestStatus.Approved
        };
        _mockLeaveRepo.Setup(r => r.GetRequestByIdAsync(1)).ReturnsAsync(request);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.ApproveRequestAsync(1, 2, null));
        Assert.Contains("Only pending", ex.Message);
    }

    [Fact]
    public async Task RejectRequestAsync_PendingRequest_Rejects()
    {
        // Arrange
        var request = new LeaveRequest
        {
            LeaveRequestId = 1,
            EmployeeId = 1,
            Status = LeaveRequestStatus.Pending
        };
        _mockLeaveRepo.Setup(r => r.GetRequestByIdAsync(1)).ReturnsAsync(request);

        // Act
        await _service.RejectRequestAsync(1, 2, "Not enough coverage");

        // Assert
        Assert.Equal(LeaveRequestStatus.Rejected, request.Status);
        Assert.Equal(2, request.ApprovedBy);
        Assert.Equal("Not enough coverage", request.ApproverComments);
        _mockLeaveRepo.Verify(r => r.UpdateRequestAsync(request), Times.Once);
    }

    [Fact]
    public async Task CancelRequestAsync_PendingRequest_ByOwner_Cancels()
    {
        // Arrange
        var request = new LeaveRequest
        {
            LeaveRequestId = 1,
            EmployeeId = 1,
            Status = LeaveRequestStatus.Pending
        };
        _mockLeaveRepo.Setup(r => r.GetRequestByIdAsync(1)).ReturnsAsync(request);

        // Act
        await _service.CancelRequestAsync(1, 1); // Employee 1 cancels their own request

        // Assert
        Assert.Equal(LeaveRequestStatus.Cancelled, request.Status);
        _mockLeaveRepo.Verify(r => r.UpdateRequestAsync(request), Times.Once);
    }

    [Fact]
    public async Task CancelRequestAsync_NotOwner_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new LeaveRequest
        {
            LeaveRequestId = 1,
            EmployeeId = 1,
            Status = LeaveRequestStatus.Pending
        };
        _mockLeaveRepo.Setup(r => r.GetRequestByIdAsync(1)).ReturnsAsync(request);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.CancelRequestAsync(1, 2)); // Employee 2 tries to cancel Employee 1's request
        Assert.Contains("only cancel your own", ex.Message);
    }

    [Fact]
    public async Task CancelRequestAsync_ApprovedRequest_RestoresBalance()
    {
        // Arrange
        var request = new LeaveRequest
        {
            LeaveRequestId = 1,
            EmployeeId = 1,
            LeaveType = LeaveTypes.Vacation,
            StartDate = DateTime.Today.AddDays(5),
            EndDate = DateTime.Today.AddDays(7),
            TotalDays = 3,
            Status = LeaveRequestStatus.Approved
        };

        var balance = new LeaveBalance
        {
            EmployeeId = 1,
            Year = DateTime.Today.Year,
            VacationDays = 15,
            VacationUsed = 3
        };

        _mockLeaveRepo.Setup(r => r.GetRequestByIdAsync(1)).ReturnsAsync(request);
        _mockLeaveRepo.Setup(r => r.GetBalanceAsync(1, DateTime.Today.Year)).ReturnsAsync(balance);

        // Act
        await _service.CancelRequestAsync(1, 1);

        // Assert
        Assert.Equal(LeaveRequestStatus.Cancelled, request.Status);
        Assert.Equal(0, balance.VacationUsed); // Balance restored
        _mockLeaveRepo.Verify(r => r.UpdateBalanceAsync(balance), Times.Once);
    }

    [Fact]
    public async Task GetBalanceAsync_ExistingBalance_ReturnsBalance()
    {
        // Arrange
        var balance = new LeaveBalance
        {
            EmployeeId = 1,
            Year = 2024,
            VacationDays = 15,
            SickDays = 10,
            PersonalDays = 3
        };
        _mockLeaveRepo.Setup(r => r.GetBalanceAsync(1, 2024)).ReturnsAsync(balance);

        // Act
        var result = await _service.GetBalanceAsync(1, 2024);

        // Assert
        Assert.Equal(15, result.VacationDays);
        Assert.Equal(10, result.SickDays);
    }

    [Fact]
    public async Task GetBalanceAsync_NoBalanceExists_CreatesNewBalance()
    {
        // Arrange
        _mockLeaveRepo.Setup(r => r.GetBalanceAsync(1, 2024)).ReturnsAsync((LeaveBalance?)null);
        _mockLeaveRepo.Setup(r => r.CreateBalanceAsync(It.IsAny<LeaveBalance>())).ReturnsAsync(1);

        // Act
        var result = await _service.GetBalanceAsync(1, 2024);

        // Assert
        Assert.Equal(1, result.EmployeeId);
        Assert.Equal(2024, result.Year);
        Assert.Equal(15, result.VacationDays); // Default vacation days
        _mockLeaveRepo.Verify(r => r.CreateBalanceAsync(It.IsAny<LeaveBalance>()), Times.Once);
    }

    [Fact]
    public async Task InitializeBalanceAsync_CreatesNewBalance()
    {
        // Arrange
        _mockEmployeeRepo.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
        _mockLeaveRepo.Setup(r => r.GetBalanceAsync(1, 2024)).ReturnsAsync((LeaveBalance?)null);
        _mockLeaveRepo.Setup(r => r.CreateBalanceAsync(It.IsAny<LeaveBalance>())).ReturnsAsync(1);

        // Act
        await _service.InitializeBalanceAsync(1, 2024);

        // Assert
        _mockLeaveRepo.Verify(r => r.CreateBalanceAsync(It.Is<LeaveBalance>(b => 
            b.EmployeeId == 1 && 
            b.Year == 2024 && 
            b.VacationDays == 15)), Times.Once);
    }

    [Fact]
    public async Task InitializeBalanceAsync_BalanceAlreadyExists_ThrowsInvalidOperationException()
    {
        // Arrange
        var existingBalance = new LeaveBalance { EmployeeId = 1, Year = 2024 };
        _mockEmployeeRepo.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
        _mockLeaveRepo.Setup(r => r.GetBalanceAsync(1, 2024)).ReturnsAsync(existingBalance);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.InitializeBalanceAsync(1, 2024));
        Assert.Contains("already exists", ex.Message);
    }
}

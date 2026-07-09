using Moq;
using Xunit;
using Microsoft.AspNetCore.Http;
using DayforceLite.Core.Interfaces;
using DayforceLite.Core.Models;
using DayforceLite.Core.Services;
using System.Security.Claims;

namespace DayforceLite.UnitTests.Services;

public class AuditServiceTests
{
    private readonly Mock<IAuditRepository> _mockRepository;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly AuditService _service;

    public AuditServiceTests()
    {
        _mockRepository = new Mock<IAuditRepository>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _service = new AuditService(_mockRepository.Object, _mockHttpContextAccessor.Object);
    }

    [Fact]
    public async Task LogAsync_WithEntityTypeAndId_CreatesAuditLog()
    {
        // Arrange
        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<AuditLog>())).ReturnsAsync(1);
        SetupHttpContext(userId: 1, userName: "John Doe");

        // Act
        await _service.LogAsync("Employee", "123", AuditAction.Created, null, new { Name = "Test" });

        // Assert
        _mockRepository.Verify(r => r.CreateAsync(It.Is<AuditLog>(a =>
            a.EntityType == "Employee" &&
            a.EntityId == "123" &&
            a.Action == AuditAction.Created &&
            a.UserId == 1 &&
            a.UserName == "John Doe" &&
            a.NewValues != null)), Times.Once);
    }

    [Fact]
    public async Task LogAsync_WithEntity_ExtractsEntityId()
    {
        // Arrange
        var employee = new Employee { EmployeeId = 42, FirstName = "Test", LastName = "User" };
        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<AuditLog>())).ReturnsAsync(1);
        SetupHttpContext();

        // Act
        await _service.LogAsync(employee, AuditAction.Updated, new { FirstName = "Old" });

        // Assert
        _mockRepository.Verify(r => r.CreateAsync(It.Is<AuditLog>(a =>
            a.EntityType == "Employee" &&
            a.EntityId == "42" &&
            a.Action == AuditAction.Updated)), Times.Once);
    }

    [Fact]
    public async Task GetEntityHistoryAsync_ReturnsAuditLogs()
    {
        // Arrange
        var logs = new List<AuditLog>
        {
            new() { AuditLogId = 1, EntityType = "Employee", EntityId = "1", Action = AuditAction.Created },
            new() { AuditLogId = 2, EntityType = "Employee", EntityId = "1", Action = AuditAction.Updated }
        };
        _mockRepository.Setup(r => r.GetByEntityAsync("Employee", "1")).ReturnsAsync(logs);

        // Act
        var result = (await _service.GetEntityHistoryAsync("Employee", "1")).ToList();

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetUserActivityAsync_ReturnsUserAuditLogs()
    {
        // Arrange
        var logs = new List<AuditLog>
        {
            new() { AuditLogId = 1, UserId = 1, Action = AuditAction.Created },
            new() { AuditLogId = 2, UserId = 1, Action = AuditAction.Updated }
        };
        _mockRepository.Setup(r => r.GetByUserAsync(1, null, null)).ReturnsAsync(logs);

        // Act
        var result = (await _service.GetUserActivityAsync(1)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetRecentActivityAsync_ReturnsRecentLogs()
    {
        // Arrange
        var logs = new List<AuditLog>
        {
            new() { AuditLogId = 1, Timestamp = DateTime.UtcNow.AddMinutes(-5) },
            new() { AuditLogId = 2, Timestamp = DateTime.UtcNow.AddMinutes(-10) }
        };
        _mockRepository.Setup(r => r.GetRecentAsync(50)).ReturnsAsync(logs);

        // Act
        var result = (await _service.GetRecentActivityAsync(50)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetActivityByDateRangeAsync_ReturnsLogsInRange()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.AddDays(-7);
        var toDate = DateTime.UtcNow;
        var logs = new List<AuditLog>
        {
            new() { AuditLogId = 1, Timestamp = DateTime.UtcNow.AddDays(-3) }
        };
        _mockRepository.Setup(r => r.GetByDateRangeAsync(fromDate, toDate, "Employee")).ReturnsAsync(logs);

        // Act
        var result = (await _service.GetActivityByDateRangeAsync(fromDate, toDate, "Employee")).ToList();

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task LogAsync_WithForwardedHeader_ExtractsIpAddress()
    {
        // Arrange
        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<AuditLog>())).ReturnsAsync(1);
        SetupHttpContext(forwardedFor: "192.168.1.100, 10.0.0.1");

        // Act
        await _service.LogAsync("Test", "1", AuditAction.Created);

        // Assert
        _mockRepository.Verify(r => r.CreateAsync(It.Is<AuditLog>(a =>
            a.IpAddress == "192.168.1.100")), Times.Once);
    }

    private void SetupHttpContext(int? userId = null, string? userName = null, string? forwardedFor = null)
    {
        var httpContext = new DefaultHttpContext();

        if (userId.HasValue || !string.IsNullOrEmpty(userName))
        {
            var claims = new List<Claim>();
            if (userId.HasValue)
            {
                claims.Add(new Claim("sub", userId.Value.ToString()));
            }
            if (!string.IsNullOrEmpty(userName))
            {
                claims.Add(new Claim("name", userName));
            }
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        }

        if (!string.IsNullOrEmpty(forwardedFor))
        {
            httpContext.Request.Headers["X-Forwarded-For"] = forwardedFor;
        }

        _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);
    }
}

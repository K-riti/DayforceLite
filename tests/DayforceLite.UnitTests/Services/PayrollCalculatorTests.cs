using Xunit;
using DayforceLite.Core.Services;

namespace DayforceLite.UnitTests.Services;

public class PayrollCalculatorTests
{
    [Theory]
    [InlineData(40, 0, 100, 4000)]   // 40 regular hrs @ $100
    [InlineData(40, 8, 100, 5200)]   // 40 regular + 8 OT @ 1.5x
    [InlineData(0, 0, 100, 0)]       // No hours
    [InlineData(20, 10, 50, 1750)]   // 20 regular + 10 OT @ $50
    public void Calculate_ReturnsCorrectAmount(
        decimal regular, decimal overtime, decimal rate, decimal expected)
    {
        // Act
        var result = PayrollCalculator.Calculate(regular, overtime, rate);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Calculate_WithOnlyRegularHours_ReturnsCorrectAmount()
    {
        // Arrange
        decimal regularHours = 40m;
        decimal overtimeHours = 0m;
        decimal hourlyRate = 25m;

        // Act
        var result = PayrollCalculator.Calculate(regularHours, overtimeHours, hourlyRate);

        // Assert
        Assert.Equal(1000m, result);
    }

    [Fact]
    public void Calculate_WithOnlyOvertimeHours_ReturnsCorrectAmount()
    {
        // Arrange
        decimal regularHours = 0m;
        decimal overtimeHours = 10m;
        decimal hourlyRate = 20m;

        // Act
        var result = PayrollCalculator.Calculate(regularHours, overtimeHours, hourlyRate);

        // Assert
        Assert.Equal(300m, result); // 10 * 20 * 1.5 = 300
    }
}

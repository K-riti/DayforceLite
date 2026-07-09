namespace DayforceLite.Core.Services;

public static class PayrollCalculator
{
    private const decimal OvertimeMultiplier = 1.5m;

    public static decimal Calculate(decimal regularHours, decimal overtimeHours, decimal hourlyRate)
    {
        return (regularHours * hourlyRate) + (overtimeHours * hourlyRate * OvertimeMultiplier);
    }
}

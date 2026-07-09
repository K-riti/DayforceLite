namespace DayforceLite.WCF;

public class LegacyPayrollService : ILegacyPayrollService
{
    private const decimal TaxRate = 0.20m;
    private const decimal OvertimeMultiplier = 1.5m;
    private const decimal DefaultHourlyRate = 100m;

    public PayrollResponse ProcessPayroll(PayrollRequest request)
    {
        var gross = request.RegularHours * request.HourlyRate
                  + request.OvertimeHours * request.HourlyRate * OvertimeMultiplier;

        var tax = gross * TaxRate;

        return new PayrollResponse
        {
            EmployeeId = request.EmployeeId,
            GrossPay = gross,
            TaxAmount = tax,
            NetPay = gross - tax,
            ProcessedAt = DateTime.UtcNow
        };
    }

    public decimal CalculateGrossPay(int employeeId, decimal hours, decimal overtimeHours)
    {
        // Using default rate - in production would fetch from employee record
        return hours * DefaultHourlyRate + overtimeHours * DefaultHourlyRate * OvertimeMultiplier;
    }

    public decimal CalculateTax(decimal grossPay)
    {
        return grossPay * TaxRate;
    }
}

using DayforceLite.Core.Exceptions;
using DayforceLite.Core.Interfaces;
using DayforceLite.Core.Models;

namespace DayforceLite.Core.Services;

public class PayrollService : IPayrollService
{
    private readonly IPayrollRepository _payrollRepository;
    private readonly ITimesheetRepository _timesheetRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private const decimal TaxRate = 0.20m;
    private const decimal OvertimeMultiplier = 1.5m;

    public PayrollService(
        IPayrollRepository payrollRepository,
        ITimesheetRepository timesheetRepository,
        IEmployeeRepository employeeRepository)
    {
        _payrollRepository = payrollRepository;
        _timesheetRepository = timesheetRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<PayrollRecord> ProcessPayrollAsync(int employeeId, DateTime periodStart, DateTime periodEnd)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId)
            ?? throw new NotFoundException(nameof(Employee), employeeId);

        var timesheets = await _timesheetRepository.GetByEmployeeIdAsync(employeeId);
        var periodTimesheets = timesheets
            .Where(t => t.WeekStartDate >= periodStart && t.WeekStartDate <= periodEnd && t.Status == "Approved")
            .ToList();

        var totalRegularHours = periodTimesheets.Sum(t => t.RegularHours);
        var totalOvertimeHours = periodTimesheets.Sum(t => t.OvertimeHours);

        var grossPay = CalculateGrossPay(totalRegularHours, totalOvertimeHours, employee.HourlyRate);
        var taxDeduction = CalculateTax(grossPay);

        var record = new PayrollRecord
        {
            EmployeeId = employeeId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            GrossPay = grossPay,
            TaxDeduction = taxDeduction,
            NetPay = grossPay - taxDeduction,
            ProcessedAt = DateTime.UtcNow
        };

        await _payrollRepository.CreateAsync(record);
        return record;
    }

    public async Task<IEnumerable<PayrollRecord>> GetPayrollHistoryAsync(int employeeId)
    {
        if (!await _employeeRepository.ExistsAsync(employeeId))
        {
            throw new NotFoundException(nameof(Employee), employeeId);
        }

        return await _payrollRepository.GetByEmployeeIdAsync(employeeId);
    }

    public decimal CalculateGrossPay(decimal regularHours, decimal overtimeHours, decimal hourlyRate)
    {
        return (regularHours * hourlyRate) + (overtimeHours * hourlyRate * OvertimeMultiplier);
    }

    public decimal CalculateTax(decimal grossPay)
    {
        return grossPay * TaxRate;
    }
}

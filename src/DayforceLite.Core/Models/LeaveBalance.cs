namespace DayforceLite.Core.Models;

public class LeaveBalance
{
    public int LeaveBalanceId { get; set; }
    public int EmployeeId { get; set; }
    public int Year { get; set; }
    public decimal VacationDays { get; set; }
    public decimal SickDays { get; set; }
    public decimal PersonalDays { get; set; }
    public decimal VacationUsed { get; set; }
    public decimal SickUsed { get; set; }
    public decimal PersonalUsed { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Computed properties
    public decimal VacationRemaining => VacationDays - VacationUsed;
    public decimal SickRemaining => SickDays - SickUsed;
    public decimal PersonalRemaining => PersonalDays - PersonalUsed;

    // Navigation
    public Employee? Employee { get; set; }
}

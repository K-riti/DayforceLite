namespace DayforceLite.Core.Models;

public class Timesheet
{
    public int TimesheetId { get; set; }
    public int EmployeeId { get; set; }
    public DateTime WeekStartDate { get; set; }
    public decimal RegularHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public string Status { get; set; } = "Draft"; // Draft/Submitted/Approved
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public int? ApprovedBy { get; set; }

    // Navigation property
    public Employee? Employee { get; set; }
}

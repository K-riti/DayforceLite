namespace DayforceLite.Core.Models;

public class LeaveRequest
{
    public int LeaveRequestId { get; set; }
    public int EmployeeId { get; set; }
    public string LeaveType { get; set; } = LeaveTypes.Vacation;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalDays { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = LeaveRequestStatus.Pending;
    public int? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApproverComments { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public Employee? Employee { get; set; }
}

public static class LeaveTypes
{
    public const string Vacation = "Vacation";
    public const string Sick = "Sick";
    public const string Personal = "Personal";
    public const string Bereavement = "Bereavement";
    public const string Unpaid = "Unpaid";

    public static readonly string[] All = [Vacation, Sick, Personal, Bereavement, Unpaid];
    public static bool IsValid(string type) => All.Contains(type);
}

public static class LeaveRequestStatus
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All = [Pending, Approved, Rejected, Cancelled];
    public static bool IsValid(string status) => All.Contains(status);
}

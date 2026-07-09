namespace DayforceLite.Core.Models;

public class Employee
{
    public int EmployeeId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public decimal HourlyRate { get; set; }
    public DateTime StartDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public byte[]? RowVersion { get; set; }

    // Navigation property
    public string Department { get; set; } = string.Empty;

    public string FullName => $"{FirstName} {LastName}";
}

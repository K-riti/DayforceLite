namespace DayforceLite.Core.Models;

public class PayrollSummary
{
    public int EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public decimal TotalGross { get; set; }
    public decimal TotalNet { get; set; }
    public int PayslipCount { get; set; }
}

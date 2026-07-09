namespace DayforceLite.Core.Models;

public class PayrollRecord
{
    public int PayrollId { get; set; }
    public int EmployeeId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal GrossPay { get; set; }
    public decimal TaxDeduction { get; set; }
    public decimal NetPay { get; set; }
    public DateTime ProcessedAt { get; set; }

    // Navigation property
    public Employee? Employee { get; set; }
}

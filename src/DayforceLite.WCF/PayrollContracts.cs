using System.Runtime.Serialization;

namespace DayforceLite.WCF;

[DataContract(Namespace = "http://dayforceLite/payroll")]
public class PayrollRequest
{
    [DataMember]
    public int EmployeeId { get; set; }

    [DataMember]
    public decimal RegularHours { get; set; }

    [DataMember]
    public decimal OvertimeHours { get; set; }

    [DataMember]
    public decimal HourlyRate { get; set; }
}

[DataContract(Namespace = "http://dayforceLite/payroll")]
public class PayrollResponse
{
    [DataMember]
    public int EmployeeId { get; set; }

    [DataMember]
    public decimal GrossPay { get; set; }

    [DataMember]
    public decimal TaxAmount { get; set; }

    [DataMember]
    public decimal NetPay { get; set; }

    [DataMember]
    public DateTime ProcessedAt { get; set; }
}

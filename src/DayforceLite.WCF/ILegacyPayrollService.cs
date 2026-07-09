using CoreWCF;

namespace DayforceLite.WCF;

[ServiceContract(Namespace = "http://dayforceLite/payroll")]
public interface ILegacyPayrollService
{
    [OperationContract]
    PayrollResponse ProcessPayroll(PayrollRequest request);

    [OperationContract]
    decimal CalculateGrossPay(int employeeId, decimal hours, decimal overtimeHours);

    [OperationContract]
    decimal CalculateTax(decimal grossPay);
}

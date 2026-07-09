using Microsoft.Extensions.DependencyInjection;
using DayforceLite.Core.Services;

namespace DayforceLite.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IPayrollService, PayrollService>();
        services.AddScoped<ITimesheetService, TimesheetService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<ILeaveService, LeaveService>();
        services.AddScoped<IAuditService, AuditService>();

        return services;
    }
}

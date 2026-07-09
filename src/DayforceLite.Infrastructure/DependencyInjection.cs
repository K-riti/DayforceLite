using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DayforceLite.Core.Interfaces;
using DayforceLite.Core.Services;
using DayforceLite.Infrastructure.Data;
using DayforceLite.Infrastructure.Search;

namespace DayforceLite.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure Entity Framework
        services.AddDbContext<DayforceDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DayforceDb"),
                sqlOptions => sqlOptions.EnableRetryOnFailure(3)));

        // Register repositories
        services.AddScoped<IEmployeeRepository, AdoEmployeeRepository>();
        services.AddScoped<ITimesheetRepository, EfTimesheetRepository>();
        services.AddScoped<IPayrollRepository, EfPayrollRepository>();
        services.AddScoped<IDepartmentRepository, AdoDepartmentRepository>();
        services.AddScoped<ILeaveRepository, EfLeaveRepository>();
        services.AddScoped<IAuditRepository, EfAuditRepository>();

        // Register Elasticsearch service
        services.AddSingleton<ElasticSearchService>();

        return services;
    }
}

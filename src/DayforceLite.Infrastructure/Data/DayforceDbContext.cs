using Microsoft.EntityFrameworkCore;
using DayforceLite.Core.Models;

namespace DayforceLite.Infrastructure.Data;

public class DayforceDbContext : DbContext
{
    public DayforceDbContext(DbContextOptions<DayforceDbContext> options) : base(options)
    {
    }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Timesheet> Timesheets => Set<Timesheet>();
    public DbSet<PayrollRecord> PayrollRecords => Set<PayrollRecord>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.DepartmentId);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CostCentre).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Ignore(e => e.EmployeeCount);
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId);
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.HourlyRate).HasColumnType("decimal(10,2)");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.Ignore(e => e.Department);
            entity.Ignore(e => e.FullName);
        });

        modelBuilder.Entity<Timesheet>(entity =>
        {
            entity.HasKey(e => e.TimesheetId);
            entity.Property(e => e.RegularHours).HasColumnType("decimal(5,2)");
            entity.Property(e => e.OvertimeHours).HasColumnType("decimal(5,2)").HasDefaultValue(0);
            entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("Draft");
            entity.HasOne(e => e.Employee)
                  .WithMany()
                  .HasForeignKey(e => e.EmployeeId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PayrollRecord>(entity =>
        {
            entity.HasKey(e => e.PayrollId);
            entity.Property(e => e.GrossPay).HasColumnType("decimal(12,2)");
            entity.Property(e => e.TaxDeduction).HasColumnType("decimal(12,2)");
            entity.Property(e => e.NetPay).HasColumnType("decimal(12,2)");
            entity.Property(e => e.ProcessedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.HasOne(e => e.Employee)
                  .WithMany()
                  .HasForeignKey(e => e.EmployeeId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.HasKey(e => e.LeaveRequestId);
            entity.Property(e => e.LeaveType).HasMaxLength(20).IsRequired();
            entity.Property(e => e.TotalDays).HasColumnType("decimal(5,2)");
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("Pending");
            entity.Property(e => e.ApproverComments).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.HasOne(e => e.Employee)
                  .WithMany()
                  .HasForeignKey(e => e.EmployeeId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LeaveBalance>(entity =>
        {
            entity.HasKey(e => e.LeaveBalanceId);
            entity.Property(e => e.VacationDays).HasColumnType("decimal(5,2)");
            entity.Property(e => e.SickDays).HasColumnType("decimal(5,2)");
            entity.Property(e => e.PersonalDays).HasColumnType("decimal(5,2)");
            entity.Property(e => e.VacationUsed).HasColumnType("decimal(5,2)").HasDefaultValue(0);
            entity.Property(e => e.SickUsed).HasColumnType("decimal(5,2)").HasDefaultValue(0);
            entity.Property(e => e.PersonalUsed).HasColumnType("decimal(5,2)").HasDefaultValue(0);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.HasIndex(e => new { e.EmployeeId, e.Year }).IsUnique();
            entity.Ignore(e => e.VacationRemaining);
            entity.Ignore(e => e.SickRemaining);
            entity.Ignore(e => e.PersonalRemaining);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.AuditLogId);
            entity.Property(e => e.EntityType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.EntityId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Action).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Timestamp).HasDefaultValueSql("GETUTCDATE()");
            entity.HasIndex(e => e.EntityType);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.UserId);
        });
    }
}

using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using DayforceLite.Core.Interfaces;
using DayforceLite.Core.Models;

namespace DayforceLite.Infrastructure.Data;

public class AdoEmployeeRepository : IEmployeeRepository
{
    private readonly string _connectionString;

    public AdoEmployeeRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DayforceDb")!;
    }

    public async Task<Employee?> GetByIdAsync(int employeeId)
    {
        const string sql = @"
            SELECT e.EmployeeId, e.FirstName, e.LastName, e.Email,
                   e.HourlyRate, e.StartDate, e.DepartmentId, e.IsActive,
                   e.CreatedAt, d.Name AS Department
            FROM Employees e
            JOIN Departments d ON e.DepartmentId = d.DepartmentId
            WHERE e.EmployeeId = @EmployeeId AND e.IsActive = 1";

        await using var conn = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = employeeId;

        await conn.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync()) return null;

        return MapEmployee(reader);
    }

    public async Task<IEnumerable<Employee>> GetAllAsync(string? searchTerm = null)
    {
        var sql = @"
            SELECT e.EmployeeId, e.FirstName, e.LastName, e.Email,
                   e.HourlyRate, e.StartDate, e.DepartmentId, e.IsActive,
                   e.CreatedAt, d.Name AS Department
            FROM Employees e
            JOIN Departments d ON e.DepartmentId = d.DepartmentId
            WHERE e.IsActive = 1";

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            sql += @" AND (e.FirstName LIKE @SearchTerm 
                      OR e.LastName LIKE @SearchTerm 
                      OR e.Email LIKE @SearchTerm)";
        }

        sql += " ORDER BY e.LastName, e.FirstName";

        var employees = new List<Employee>();

        await using var conn = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand(sql, conn);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            cmd.Parameters.Add("@SearchTerm", SqlDbType.NVarChar, 100).Value = $"%{searchTerm}%";
        }

        await conn.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            employees.Add(MapEmployee(reader));
        }

        return employees;
    }

    public async Task<int> CreateAsync(Employee employee)
    {
        const string sql = @"
            INSERT INTO Employees (FirstName, LastName, Email, DepartmentId, HourlyRate, StartDate, IsActive, CreatedAt)
            OUTPUT INSERTED.EmployeeId
            VALUES (@FirstName, @LastName, @Email, @DepartmentId, @HourlyRate, @StartDate, @IsActive, @CreatedAt)";

        await using var conn = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand(sql, conn);

        cmd.Parameters.Add("@FirstName", SqlDbType.NVarChar, 100).Value = employee.FirstName;
        cmd.Parameters.Add("@LastName", SqlDbType.NVarChar, 100).Value = employee.LastName;
        cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 255).Value = employee.Email;
        cmd.Parameters.Add("@DepartmentId", SqlDbType.Int).Value = employee.DepartmentId;
        cmd.Parameters.Add("@HourlyRate", SqlDbType.Decimal).Value = employee.HourlyRate;
        cmd.Parameters.Add("@StartDate", SqlDbType.Date).Value = employee.StartDate;
        cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = employee.IsActive;
        cmd.Parameters.Add("@CreatedAt", SqlDbType.DateTime2).Value = employee.CreatedAt;

        await conn.OpenAsync();
        var result = await cmd.ExecuteScalarAsync();
        return (int)result!;
    }

    public async Task UpdateAsync(Employee employee)
    {
        const string sql = @"
            UPDATE Employees 
            SET FirstName = @FirstName,
                LastName = @LastName,
                Email = @Email,
                DepartmentId = @DepartmentId,
                HourlyRate = @HourlyRate,
                StartDate = @StartDate
            WHERE EmployeeId = @EmployeeId";

        await using var conn = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand(sql, conn);

        cmd.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = employee.EmployeeId;
        cmd.Parameters.Add("@FirstName", SqlDbType.NVarChar, 100).Value = employee.FirstName;
        cmd.Parameters.Add("@LastName", SqlDbType.NVarChar, 100).Value = employee.LastName;
        cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 255).Value = employee.Email;
        cmd.Parameters.Add("@DepartmentId", SqlDbType.Int).Value = employee.DepartmentId;
        cmd.Parameters.Add("@HourlyRate", SqlDbType.Decimal).Value = employee.HourlyRate;
        cmd.Parameters.Add("@StartDate", SqlDbType.Date).Value = employee.StartDate;

        await conn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(int employeeId)
    {
        const string sql = "UPDATE Employees SET IsActive = 0 WHERE EmployeeId = @EmployeeId";

        await using var conn = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = employeeId;

        await conn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<bool> ExistsAsync(int employeeId)
    {
        const string sql = "SELECT COUNT(1) FROM Employees WHERE EmployeeId = @EmployeeId AND IsActive = 1";

        await using var conn = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = employeeId;

        await conn.OpenAsync();
        var result = await cmd.ExecuteScalarAsync();
        return (int)result! > 0;
    }

    public async Task<PayrollSummary> GetPayrollSummaryAsync(int employeeId, DateTime from, DateTime to)
    {
        await using var conn = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand("usp_GetEmployeePayrollSummary", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@EmployeeId", employeeId);
        cmd.Parameters.AddWithValue("@FromDate", from.Date);
        cmd.Parameters.AddWithValue("@ToDate", to.Date);

        await conn.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new PayrollSummary
            {
                EmployeeId = reader.GetInt32(0),
                FullName = reader.GetString(1),
                TotalGross = reader.IsDBNull(2) ? 0 : reader.GetDecimal(2),
                TotalNet = reader.IsDBNull(3) ? 0 : reader.GetDecimal(3),
                PayslipCount = reader.IsDBNull(4) ? 0 : reader.GetInt32(4)
            };
        }

        return new PayrollSummary { EmployeeId = employeeId };
    }

    private static Employee MapEmployee(SqlDataReader reader)
    {
        return new Employee
        {
            EmployeeId = reader.GetInt32(reader.GetOrdinal("EmployeeId")),
            FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
            LastName = reader.GetString(reader.GetOrdinal("LastName")),
            Email = reader.GetString(reader.GetOrdinal("Email")),
            HourlyRate = reader.GetDecimal(reader.GetOrdinal("HourlyRate")),
            StartDate = reader.GetDateTime(reader.GetOrdinal("StartDate")),
            DepartmentId = reader.GetInt32(reader.GetOrdinal("DepartmentId")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            Department = reader.GetString(reader.GetOrdinal("Department"))
        };
    }
}

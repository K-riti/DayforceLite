using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using DayforceLite.Core.Interfaces;
using DayforceLite.Core.Models;

namespace DayforceLite.Infrastructure.Data;

public class AdoDepartmentRepository : IDepartmentRepository
{
    private readonly string _connectionString;

    public AdoDepartmentRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DayforceDb")!;
    }

    public async Task<Department?> GetByIdAsync(int departmentId)
    {
        const string sql = @"
            SELECT DepartmentId, Name, CostCentre, Description, ManagerId, 
                   IsActive, CreatedAt, UpdatedAt
            FROM Departments
            WHERE DepartmentId = @DepartmentId";

        await using var conn = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add("@DepartmentId", SqlDbType.Int).Value = departmentId;

        await conn.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync()) return null;

        return MapDepartment(reader);
    }

    public async Task<IEnumerable<Department>> GetAllAsync(bool includeInactive = false)
    {
        var sql = @"
            SELECT DepartmentId, Name, CostCentre, Description, ManagerId, 
                   IsActive, CreatedAt, UpdatedAt
            FROM Departments";

        if (!includeInactive)
        {
            sql += " WHERE IsActive = 1";
        }

        sql += " ORDER BY Name";

        var departments = new List<Department>();

        await using var conn = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand(sql, conn);

        await conn.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            departments.Add(MapDepartment(reader));
        }

        return departments;
    }

    public async Task<int> CreateAsync(Department department)
    {
        const string sql = @"
            INSERT INTO Departments (Name, CostCentre, Description, ManagerId, IsActive, CreatedAt)
            OUTPUT INSERTED.DepartmentId
            VALUES (@Name, @CostCentre, @Description, @ManagerId, @IsActive, @CreatedAt)";

        await using var conn = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand(sql, conn);

        cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = department.Name;
        cmd.Parameters.Add("@CostCentre", SqlDbType.NVarChar, 20).Value = department.CostCentre;
        cmd.Parameters.Add("@Description", SqlDbType.NVarChar, 500).Value = 
            (object?)department.Description ?? DBNull.Value;
        cmd.Parameters.Add("@ManagerId", SqlDbType.Int).Value = 
            (object?)department.ManagerId ?? DBNull.Value;
        cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = department.IsActive;
        cmd.Parameters.Add("@CreatedAt", SqlDbType.DateTime2).Value = department.CreatedAt;

        await conn.OpenAsync();
        var result = await cmd.ExecuteScalarAsync();
        return (int)result!;
    }

    public async Task UpdateAsync(Department department)
    {
        const string sql = @"
            UPDATE Departments 
            SET Name = @Name,
                CostCentre = @CostCentre,
                Description = @Description,
                ManagerId = @ManagerId,
                UpdatedAt = @UpdatedAt
            WHERE DepartmentId = @DepartmentId";

        await using var conn = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand(sql, conn);

        cmd.Parameters.Add("@DepartmentId", SqlDbType.Int).Value = department.DepartmentId;
        cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = department.Name;
        cmd.Parameters.Add("@CostCentre", SqlDbType.NVarChar, 20).Value = department.CostCentre;
        cmd.Parameters.Add("@Description", SqlDbType.NVarChar, 500).Value = 
            (object?)department.Description ?? DBNull.Value;
        cmd.Parameters.Add("@ManagerId", SqlDbType.Int).Value = 
            (object?)department.ManagerId ?? DBNull.Value;
        cmd.Parameters.Add("@UpdatedAt", SqlDbType.DateTime2).Value = department.UpdatedAt;

        await conn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(int departmentId)
    {
        const string sql = "UPDATE Departments SET IsActive = 0 WHERE DepartmentId = @DepartmentId";

        await using var conn = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add("@DepartmentId", SqlDbType.Int).Value = departmentId;

        await conn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<bool> ExistsAsync(int departmentId)
    {
        const string sql = "SELECT COUNT(1) FROM Departments WHERE DepartmentId = @DepartmentId AND IsActive = 1";

        await using var conn = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add("@DepartmentId", SqlDbType.Int).Value = departmentId;

        await conn.OpenAsync();
        var result = await cmd.ExecuteScalarAsync();
        return (int)result! > 0;
    }

    public async Task<int> GetEmployeeCountAsync(int departmentId)
    {
        const string sql = @"
            SELECT COUNT(*) FROM Employees 
            WHERE DepartmentId = @DepartmentId AND IsActive = 1";

        await using var conn = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add("@DepartmentId", SqlDbType.Int).Value = departmentId;

        await conn.OpenAsync();
        var result = await cmd.ExecuteScalarAsync();
        return (int)result!;
    }

    public async Task<bool> HasActiveEmployeesAsync(int departmentId)
    {
        return await GetEmployeeCountAsync(departmentId) > 0;
    }

    private static Department MapDepartment(SqlDataReader reader)
    {
        return new Department
        {
            DepartmentId = reader.GetInt32(reader.GetOrdinal("DepartmentId")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            CostCentre = reader.GetString(reader.GetOrdinal("CostCentre")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description")) 
                ? null : reader.GetString(reader.GetOrdinal("Description")),
            ManagerId = reader.IsDBNull(reader.GetOrdinal("ManagerId")) 
                ? null : reader.GetInt32(reader.GetOrdinal("ManagerId")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) 
                ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
        };
    }
}

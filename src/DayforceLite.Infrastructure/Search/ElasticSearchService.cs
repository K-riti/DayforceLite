using Microsoft.Extensions.Configuration;
using Nest;
using DayforceLite.Core.Models;

namespace DayforceLite.Infrastructure.Search;

public class ElasticSearchService
{
    private readonly ElasticClient _client;

    public ElasticSearchService(IConfiguration config)
    {
        var uri = config["ElasticSearch:Uri"] ?? "http://localhost:9200";
        var settings = new ConnectionSettings(new Uri(uri))
            .DefaultIndex("employees");
        _client = new ElasticClient(settings);
    }

    public async Task IndexEmployeeAsync(Employee employee)
    {
        await _client.IndexDocumentAsync(new
        {
            employee.EmployeeId,
            FullName = $"{employee.FirstName} {employee.LastName}",
            employee.Email,
            employee.Department
        });
    }

    public async Task<IEnumerable<int>> SearchAsync(string query)
    {
        var response = await _client.SearchAsync<dynamic>(s => s
            .Query(q => q
                .MultiMatch(m => m
                    .Fields(f => f
                        .Field("fullName^3")
                        .Field("email")
                        .Field("department"))
                    .Query(query)
                    .Fuzziness(Fuzziness.Auto))));

        return response.Hits.Select(h => (int)h.Source.employeeId);
    }

    public async Task DeleteEmployeeAsync(int employeeId)
    {
        await _client.DeleteByQueryAsync<dynamic>(d => d
            .Query(q => q
                .Term(t => t.Field("employeeId").Value(employeeId))));
    }

    public async Task<bool> EnsureIndexExistsAsync()
    {
        var existsResponse = await _client.Indices.ExistsAsync("employees");

        if (!existsResponse.Exists)
        {
            var createResponse = await _client.Indices.CreateAsync("employees", c => c
                .Map(m => m
                    .Properties(p => p
                        .Number(n => n.Name("employeeId").Type(NumberType.Integer))
                        .Text(t => t.Name("fullName").Analyzer("standard"))
                        .Text(t => t.Name("email").Analyzer("standard"))
                        .Text(t => t.Name("department").Analyzer("standard")))));

            return createResponse.IsValid;
        }

        return true;
    }
}

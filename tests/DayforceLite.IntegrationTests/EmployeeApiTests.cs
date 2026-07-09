using Testcontainers.MsSql;
using Xunit;

namespace DayforceLite.IntegrationTests;

public class EmployeeApiTests : IAsyncLifetime
{
    private readonly MsSqlContainer _container;
    private bool _dockerAvailable;

    public EmployeeApiTests()
    {
        _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();
    }

    public async Task InitializeAsync()
    {
        try
        {
            await _container.StartAsync();
            _dockerAvailable = true;
        }
        catch (Exception)
        {
            _dockerAvailable = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (_dockerAvailable)
        {
            await _container.DisposeAsync();
        }
    }

    [SkippableFact]
    public void ConnectionString_IsNotEmpty()
    {
        Skip.IfNot(_dockerAvailable, "Docker is not available");

        var connectionString = _container.GetConnectionString();
        Assert.NotEmpty(connectionString);
    }
}

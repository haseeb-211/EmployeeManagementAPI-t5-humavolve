using EmployeeManagement.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EmployeeManagement.Api.Tests.Infrastructure;

/// <summary>
/// Web application factory that switches the application to a SQLite shared in-memory database
/// through configuration ("Database:Provider" = "Sqlite"). A keeper connection keeps the
/// in-memory database alive for the lifetime of the factory.
/// </summary>
public sealed class EmployeeApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _keeperConnection;

    public EmployeeApiFactory()
    {
        var connectionString =
            $"Data Source=file:employee-api-tests-{Guid.NewGuid():N}?mode=memory&cache=shared";

        ConnectionString = connectionString;
        _keeperConnection = new SqliteConnection(connectionString);
        _keeperConnection.Open();
    }

    public string ConnectionString { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "Sqlite",
                ["ConnectionStrings:DefaultConnection"] = ConnectionString,
            });
        });
    }

    public void EnsureDatabaseCreated()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.EnsureCreated();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _keeperConnection.Dispose();
        }
    }
}

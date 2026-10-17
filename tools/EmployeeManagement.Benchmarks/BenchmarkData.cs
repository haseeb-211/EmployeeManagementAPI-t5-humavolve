using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Benchmarks;

/// <summary>
/// Creates and seeds the dedicated benchmark database. A separate database is used so that
/// benchmark activity never touches the production API database.
/// </summary>
internal static class BenchmarkData
{
    private static readonly string[] Departments =
        ["Engineering", "Finance", "Human Resources", "Sales", "Marketing", "Support"];

    public static async Task EnsureSeededAsync(
        string connectionString,
        int rowCount,
        CancellationToken cancellationToken)
    {
        var options = CreateOptions(connectionString);

        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync(cancellationToken);

        var existingCount = await db.Employees.CountAsync(cancellationToken);
        if (existingCount == rowCount)
        {
            return;
        }

        if (existingCount > 0)
        {
            await db.Employees.ExecuteDeleteAsync(cancellationToken);
        }

        var employees = GenerateEmployees(rowCount);

        foreach (var chunk in employees.Chunk(500))
        {
            db.Employees.AddRange(chunk);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public static DbContextOptions<ApplicationDbContext> CreateOptions(string connectionString) =>
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString)
            .Options;

    private static List<Employee> GenerateEmployees(int rowCount)
    {
        var random = new Random(42);
        var employees = new List<Employee>(rowCount);

        for (var i = 1; i <= rowCount; i++)
        {
            employees.Add(new Employee
            {
                FullName = $"Employee {i:D5}",
                Email = $"employee{i:D5}@example.com",
                Department = Departments[random.Next(Departments.Length)],
                Salary = 30_000m + (decimal)(random.NextDouble() * 120_000),
                CreatedAt = DateTime.UtcNow.AddDays(-random.Next(0, 365)),
            });
        }

        return employees;
    }
}

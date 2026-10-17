using EmployeeManagement.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Benchmarks;

/// <summary>
/// Two genuinely independent, read-only database operations. Each operation creates its own
/// <see cref="ApplicationDbContext"/> instance so the two can run concurrently without violating
/// the EF Core rule that a single context must never be used by multiple operations at once.
/// </summary>
internal static class BenchmarkOperations
{
    public static async Task<int> ReadOrderedPageAsync(
        DbContextOptions<ApplicationDbContext> options,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using var db = new ApplicationDbContext(options);

        var employees = await db.Employees
            .AsNoTracking()
            .OrderBy(employee => employee.Id)
            .Select(employee => new { employee.Id, employee.FullName, employee.Department, employee.Salary })
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return employees.Count;
    }

    public static async Task<decimal> SumSalariesAsync(
        DbContextOptions<ApplicationDbContext> options,
        CancellationToken cancellationToken)
    {
        await using var db = new ApplicationDbContext(options);

        return await db.Employees
            .AsNoTracking()
            .SumAsync(employee => employee.Salary, cancellationToken);
    }
}

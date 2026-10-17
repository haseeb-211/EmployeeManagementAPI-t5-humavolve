using EmployeeManagement.Api.Services;
using EmployeeManagement.Api.Tests.Infrastructure;

namespace EmployeeManagement.Api.Tests;

public class ServiceCancellationTests
{
    [Theory]
    [InlineData("get-all")]
    [InlineData("get-by-id")]
    [InlineData("create")]
    [InlineData("update")]
    [InlineData("delete")]
    public async Task ServiceOperations_WithAlreadyCancelledToken_ThrowOperationCanceled(string operation)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var service = new EmployeeService(database.Context);

        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();

        Func<Task> act = operation switch
        {
            "get-all" => () => service.GetAllAsync(cancellationSource.Token),
            "get-by-id" => () => service.GetByIdAsync(1, cancellationSource.Token),
            "create" => () => service.CreateAsync(
                new Dtos.CreateEmployeeDto
                {
                    FullName = "Cancelled Person",
                    Email = "cancelled@example.com",
                    Department = "Engineering",
                    Salary = 1000m,
                },
                cancellationSource.Token),
            "update" => () => service.UpdateAsync(
                1,
                new Dtos.UpdateEmployeeDto
                {
                    FullName = "Cancelled Person",
                    Email = "cancelled@example.com",
                    Department = "Engineering",
                    Salary = 1000m,
                },
                cancellationSource.Token),
            "delete" => () => service.DeleteAsync(1, cancellationSource.Token),
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unknown operation."),
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(act);
    }

    [Fact]
    public async Task GetAllAsync_WithoutCancellation_ReturnsEmptyList()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var service = new EmployeeService(database.Context);

        var employees = await service.GetAllAsync(CancellationToken.None);

        Assert.Empty(employees);
    }
}

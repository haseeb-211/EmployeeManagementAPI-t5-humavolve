using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EmployeeManagement.Api.Dtos;
using EmployeeManagement.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Api.Tests;

public class EmployeeApiTests : IClassFixture<EmployeeApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _client;

    public EmployeeApiTests(EmployeeApiFactory factory)
    {
        factory.EnsureDatabaseCreated();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithCreatedEmployees()
    {
        var first = await CreateAsync("Ada Lovelace", NewEmail(), "Engineering", 95_000m);
        var second = await CreateAsync("Grace Hopper", NewEmail(), "Research", 105_000m);

        var response = await _client.GetAsync("/api/employees");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var employees = await response.Content.ReadFromJsonAsync<List<EmployeeDto>>(JsonOptions);
        Assert.NotNull(employees);
        Assert.Contains(employees, employee => employee.Id == first.Id);
        Assert.Contains(employees, employee => employee.Id == second.Id);
    }

    [Fact]
    public async Task GetById_ExistingEmployee_ReturnsOkWithEmployee()
    {
        var created = await CreateAsync("Linus Torvalds", NewEmail(), "Engineering", 120_000m);

        var response = await _client.GetAsync($"/api/employees/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var employee = await response.Content.ReadFromJsonAsync<EmployeeDto>(JsonOptions);
        Assert.NotNull(employee);
        Assert.Equal(created.Id, employee.Id);
        Assert.Equal("Linus Torvalds", employee.FullName);
        Assert.Equal(created.Email, employee.Email);
        Assert.Equal("Engineering", employee.Department);
        Assert.Equal(120_000m, employee.Salary);
        Assert.Equal(created.CreatedAt, employee.CreatedAt);
    }

    [Theory]
    [InlineData(999_999)]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetById_UnknownOrInvalidId_ReturnsNotFound(int id)
    {
        var response = await _client.GetAsync($"/api/employees/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_ValidEmployee_Returns201WithLocationAndBody()
    {
        var email = NewEmail();
        var beforeCreate = DateTime.UtcNow.AddMinutes(-1);

        var response = await _client.PostAsJsonAsync(
            "/api/employees",
            new { fullName = "Margaret Hamilton", email, department = "Apollo", salary = 110_000.55m });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var employee = await response.Content.ReadFromJsonAsync<EmployeeDto>(JsonOptions);
        Assert.NotNull(employee);
        Assert.True(employee.Id > 0);
        Assert.Equal("Margaret Hamilton", employee.FullName);
        Assert.Equal(email, employee.Email);
        Assert.Equal("Apollo", employee.Department);
        Assert.Equal(110_000.55m, employee.Salary);
        Assert.InRange(employee.CreatedAt, beforeCreate, DateTime.UtcNow.AddMinutes(1));

        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith(
            $"/api/employees/{employee.Id}",
            response.Headers.Location!.ToString(),
            StringComparison.OrdinalIgnoreCase);

        var fetched = await _client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
    }

    [Fact]
    public async Task Create_IgnoresClientSuppliedServerManagedValues()
    {
        var email = NewEmail();

        var response = await _client.PostAsJsonAsync(
            "/api/employees",
            new
            {
                id = 987_654,
                fullName = "Server Managed",
                email,
                department = "Engineering",
                salary = 50_000m,
                createdAt = "2000-01-01T00:00:00Z",
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var employee = await response.Content.ReadFromJsonAsync<EmployeeDto>(JsonOptions);
        Assert.NotNull(employee);
        Assert.NotEqual(987_654, employee.Id);
        Assert.True(employee.CreatedAt.Year >= 2020, "CreatedAt must be server generated.");
    }

    [Theory]
    [InlineData("not-an-email", "Engineering", 1000)]
    [InlineData("valid@example.com", "Engineering", -1)]
    [InlineData("", "Engineering", 1000)]
    [InlineData("valid@example.com", "", 1000)]
    public async Task Create_InvalidEmployee_Returns400AndDoesNotPersist(
        string email,
        string department,
        decimal salary)
    {
        var countBefore = await CountEmployeesAsync();

        var response = await _client.PostAsJsonAsync(
            "/api/employees",
            new { fullName = "Invalid Person", email, department, salary });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.True(problem.Errors.Count > 0);

        var countAfter = await CountEmployeesAsync();
        Assert.Equal(countBefore, countAfter);
    }

    [Fact]
    public async Task Update_ExistingEmployee_Returns204AndPersistsChanges()
    {
        var created = await CreateAsync("Old Name", NewEmail(), "Support", 40_000m);
        var newEmail = NewEmail();

        var response = await _client.PutAsJsonAsync(
            $"/api/employees/{created.Id}",
            new { fullName = "New Name", email = newEmail, department = "Engineering", salary = 88_000.25m });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var updated = await _client.GetAsync($"/api/employees/{created.Id}");
        var employee = await updated.Content.ReadFromJsonAsync<EmployeeDto>(JsonOptions);

        Assert.NotNull(employee);
        Assert.Equal("New Name", employee.FullName);
        Assert.Equal(newEmail, employee.Email);
        Assert.Equal("Engineering", employee.Department);
        Assert.Equal(88_000.25m, employee.Salary);
        Assert.Equal(created.CreatedAt, employee.CreatedAt);
    }

    [Fact]
    public async Task Update_NonExistingEmployee_Returns404()
    {
        var response = await _client.PutAsJsonAsync(
            "/api/employees/999999",
            new { fullName = "Ghost", email = NewEmail(), department = "Nowhere", salary = 1m });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_InvalidBody_Returns400()
    {
        var created = await CreateAsync("Valid Person", NewEmail(), "Support", 40_000m);

        var response = await _client.PutAsJsonAsync(
            $"/api/employees/{created.Id}",
            new { fullName = "", email = "invalid-email", department = "", salary = -5m });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_InvalidId_Returns400()
    {
        var response = await _client.PutAsJsonAsync(
            "/api/employees/0",
            new { fullName = "Any", email = NewEmail(), department = "Any", salary = 1m });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ExistingEmployee_Returns204AndRemovesEmployee()
    {
        var created = await CreateAsync("Delete Me", NewEmail(), "Support", 30_000m);

        var deleteResponse = await _client.DeleteAsync($"/api/employees/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/employees/{created.Id}");

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_NonExistingEmployee_Returns404()
    {
        var response = await _client.DeleteAsync("/api/employees/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_InvalidId_Returns400()
    {
        var response = await _client.DeleteAsync("/api/employees/0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static string NewEmail() => $"employee-{Guid.NewGuid():N}@example.com";

    private async Task<EmployeeDto> CreateAsync(
        string fullName,
        string email,
        string department,
        decimal salary)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/employees",
            new { fullName, email, department, salary });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var employee = await response.Content.ReadFromJsonAsync<EmployeeDto>(JsonOptions);
        Assert.NotNull(employee);

        return employee;
    }

    private async Task<int> CountEmployeesAsync()
    {
        var response = await _client.GetAsync("/api/employees");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var employees = await response.Content.ReadFromJsonAsync<List<EmployeeDto>>(JsonOptions);
        Assert.NotNull(employees);

        return employees.Count;
    }
}

using System.ComponentModel.DataAnnotations;
using EmployeeManagement.Api.Entities;

namespace EmployeeManagement.Api.Dtos;

public class EmployeeDto
{
    public int Id { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Department { get; init; } = string.Empty;

    public decimal Salary { get; init; }

    public DateTime CreatedAt { get; init; }

    public static EmployeeDto FromEntity(Employee employee) => new()
    {
        Id = employee.Id,
        FullName = employee.FullName,
        Email = employee.Email,
        Department = employee.Department,
        Salary = employee.Salary,
        CreatedAt = employee.CreatedAt,
    };
}

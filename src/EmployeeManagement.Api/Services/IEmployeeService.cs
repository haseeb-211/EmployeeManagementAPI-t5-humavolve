using EmployeeManagement.Api.Dtos;

namespace EmployeeManagement.Api.Services;

public interface IEmployeeService
{
    Task<List<EmployeeDto>> GetAllAsync(CancellationToken cancellationToken);

    Task<EmployeeDto?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(int id, UpdateEmployeeDto dto, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}

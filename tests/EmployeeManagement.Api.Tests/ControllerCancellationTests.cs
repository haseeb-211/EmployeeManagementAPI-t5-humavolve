using EmployeeManagement.Api.Controllers;
using EmployeeManagement.Api.Dtos;
using EmployeeManagement.Api.Services;

namespace EmployeeManagement.Api.Tests;

public class ControllerCancellationTests
{
    [Theory]
    [InlineData("get-all")]
    [InlineData("get-by-id")]
    [InlineData("create")]
    [InlineData("update")]
    [InlineData("delete")]
    public async Task ControllerActions_PropagateCancellationTokenToService(string action)
    {
        var spyService = new SpyEmployeeService();
        var controller = new EmployeesController(spyService);

        using var cancellationSource = new CancellationTokenSource();
        var token = cancellationSource.Token;

        switch (action)
        {
            case "get-all":
                await controller.GetAll(token);
                break;
            case "get-by-id":
                await controller.GetById(1, token);
                break;
            case "create":
                await controller.Create(new CreateEmployeeDto(), token);
                break;
            case "update":
                await controller.Update(1, new UpdateEmployeeDto(), token);
                break;
            case "delete":
                await controller.Delete(1, token);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, "Unknown action.");
        }

        Assert.Equal(token, spyService.ReceivedToken);
    }

    private sealed class SpyEmployeeService : IEmployeeService
    {
        public CancellationToken ReceivedToken { get; private set; }

        public Task<List<EmployeeDto>> GetAllAsync(CancellationToken cancellationToken)
        {
            ReceivedToken = cancellationToken;
            return Task.FromResult(new List<EmployeeDto>());
        }

        public Task<EmployeeDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            ReceivedToken = cancellationToken;
            return Task.FromResult<EmployeeDto?>(null);
        }

        public Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto, CancellationToken cancellationToken)
        {
            ReceivedToken = cancellationToken;
            return Task.FromResult(new EmployeeDto { Id = 1 });
        }

        public Task<bool> UpdateAsync(int id, UpdateEmployeeDto dto, CancellationToken cancellationToken)
        {
            ReceivedToken = cancellationToken;
            return Task.FromResult(true);
        }

        public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
        {
            ReceivedToken = cancellationToken;
            return Task.FromResult(true);
        }
    }
}

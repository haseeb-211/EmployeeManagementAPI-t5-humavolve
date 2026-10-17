using System.Text.Json;
using EmployeeManagement.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace EmployeeManagement.Api.Tests;

public class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task ClientAbortedRequest_Returns499()
    {
        var (handler, context, _) = CreateHandler();

        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();
        context.RequestAborted = cancellationSource.Token;

        var handled = await handler.TryHandleAsync(
            context,
            new OperationCanceledException(cancellationSource.Token),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(499, context.Response.StatusCode);
    }

    [Fact]
    public async Task UnexpectedException_Returns500ProblemDetailsWithoutInternals()
    {
        var (handler, context, responseBody) = CreateHandler();

        var handled = await handler.TryHandleAsync(
            context,
            new InvalidOperationException("kaboom-secret-details"),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);

        var body = await ReadBodyAsync(responseBody);
        Assert.Contains("An unexpected error occurred", body);
        Assert.DoesNotContain("kaboom-secret-details", body);
        Assert.DoesNotContain(nameof(InvalidOperationException), body);
        Assert.DoesNotContain("stackTrace", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ConnectionString", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CancellationWithoutClientAbort_Returns503ProblemDetails()
    {
        var (handler, context, responseBody) = CreateHandler();

        var handled = await handler.TryHandleAsync(
            context,
            new TaskCanceledException(),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);

        var body = await ReadBodyAsync(responseBody);
        Assert.Contains("Request cancelled", body);
    }

    private static (GlobalExceptionHandler Handler, HttpContext Context, MemoryStream Body) CreateHandler()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddProblemDetails()
            .BuildServiceProvider();

        var handler = new GlobalExceptionHandler(
            NullLogger<GlobalExceptionHandler>.Instance,
            services.GetRequiredService<IProblemDetailsService>());

        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/employees";

        var body = new MemoryStream();
        context.Response.Body = body;

        return (handler, context, body);
    }

    private static async Task<string> ReadBodyAsync(MemoryStream body)
    {
        body.Position = 0;
        using var reader = new StreamReader(body);
        var text = await reader.ReadToEndAsync();

        // Round-trip through JSON parsing so whitespace/formatting differences do not matter.
        using var document = JsonDocument.Parse(text);
        return document.RootElement.GetRawText();
    }
}

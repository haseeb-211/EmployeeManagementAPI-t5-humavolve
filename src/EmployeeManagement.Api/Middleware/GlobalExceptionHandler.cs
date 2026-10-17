using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Api.Middleware;

/// <summary>
/// Centralized exception handler that converts unexpected exceptions into <see cref="ProblemDetails"/>
/// responses without exposing internal details, and keeps client/server cancellation separate
/// from genuine failures.
/// </summary>
public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    private const int ClientClosedRequestStatus = 499;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation(
                "Request {Method} {Path} was aborted by the client and has been cancelled.",
                httpContext.Request.Method,
                httpContext.Request.Path);

            if (!httpContext.Response.HasStarted)
            {
                // Client closed the request (nginx-style 499): nothing useful can be sent back.
                httpContext.Response.StatusCode = ClientClosedRequestStatus;
            }

            return true;
        }

        if (exception is OperationCanceledException)
        {
            logger.LogWarning(
                exception,
                "Request {Method} {Path} was cancelled before completion.",
                httpContext.Request.Method,
                httpContext.Request.Path);

            var cancellationProblem = new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Request cancelled",
                Detail = "The request was cancelled before it completed.",
                Instance = httpContext.Request.Path,
            };
            cancellationProblem.Extensions["traceId"] = httpContext.TraceIdentifier;

            if (!httpContext.Response.HasStarted)
            {
                httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            }

            return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = cancellationProblem,
            });
        }

        logger.LogError(
            exception,
            "Unhandled exception while processing {Method} {Path}.",
            httpContext.Request.Method,
            httpContext.Request.Path);

        var errorProblem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred",
            Detail = "An unexpected error occurred while processing the request. The error has been logged.",
            Instance = httpContext.Request.Path,
        };
        errorProblem.Extensions["traceId"] = httpContext.TraceIdentifier;

        if (!httpContext.Response.HasStarted)
        {
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        }

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = errorProblem,
        });
    }
}

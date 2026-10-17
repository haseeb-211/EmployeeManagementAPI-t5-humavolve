using System.Diagnostics;

namespace EmployeeManagement.Benchmarks;

/// <summary>
/// Measures the same workload executed sequentially (await A, then await B) versus concurrently
/// (Task.WhenAll) using <see cref="Stopwatch"/>. Callers must supply two genuinely independent
/// operations; if database operations are used, each operation must use its own DbContext
/// because EF Core does not support concurrent operations on a single context instance.
/// </summary>
public static class SequentialVsConcurrentBenchmark
{
    public static async Task<PerformanceReport> RunAsync(
        Func<CancellationToken, Task> operationA,
        Func<CancellationToken, Task> operationB,
        int iterations,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operationA);
        ArgumentNullException.ThrowIfNull(operationB);

        if (iterations < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(iterations), "Iterations must be at least 1.");
        }

        var stopwatch = Stopwatch.StartNew();

        for (var i = 0; i < iterations; i++)
        {
            await operationA(cancellationToken);
            await operationB(cancellationToken);
        }

        stopwatch.Stop();
        var sequentialElapsed = stopwatch.Elapsed;

        stopwatch.Restart();

        for (var i = 0; i < iterations; i++)
        {
            await Task.WhenAll(
                operationA(cancellationToken),
                operationB(cancellationToken));
        }

        stopwatch.Stop();
        var concurrentElapsed = stopwatch.Elapsed;

        return new PerformanceReport(sequentialElapsed, concurrentElapsed, iterations);
    }
}

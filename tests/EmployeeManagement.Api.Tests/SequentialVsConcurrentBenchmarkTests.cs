using EmployeeManagement.Benchmarks;

namespace EmployeeManagement.Api.Tests;

public class SequentialVsConcurrentBenchmarkTests
{
    [Fact]
    public async Task RunAsync_InvokesBothOperationsOncePerIterationInEachPhase()
    {
        var operationACount = 0;
        var operationBCount = 0;

        var report = await SequentialVsConcurrentBenchmark.RunAsync(
            _ =>
            {
                Interlocked.Increment(ref operationACount);
                return Task.CompletedTask;
            },
            _ =>
            {
                Interlocked.Increment(ref operationBCount);
                return Task.CompletedTask;
            },
            iterations: 3);

        Assert.Equal(6, operationACount);
        Assert.Equal(6, operationBCount);
        Assert.Equal(3, report.Iterations);
    }

    [Fact]
    public async Task RunAsync_SequentialPhase_DoesNotStartOperationBWhileOperationAIsPending()
    {
        var operationACount = 0;
        var operationBCount = 0;
        var releaseOperationA = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var runTask = SequentialVsConcurrentBenchmark.RunAsync(
            _ =>
            {
                Interlocked.Increment(ref operationACount);
                return releaseOperationA.Task;
            },
            _ =>
            {
                Interlocked.Increment(ref operationBCount);
                return Task.CompletedTask;
            },
            iterations: 1);

        await WaitUntilAsync(() => Volatile.Read(ref operationACount) == 1);
        Assert.Equal(0, Volatile.Read(ref operationBCount));

        releaseOperationA.SetResult();
        await runTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(2, Volatile.Read(ref operationACount));
        Assert.Equal(2, Volatile.Read(ref operationBCount));
    }

    [Fact]
    public async Task RunAsync_ConcurrentPhase_InvokesBothOperationsBeforeAwaitingCompletion()
    {
        // Operation A completes in the sequential phase, but in the concurrent phase it only
        // completes once operation B has been invoked. If the runner awaited A before starting
        // B, this test would time out instead of completing.
        var operationACount = 0;
        var operationBInvokedDuringConcurrentPhase = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var reportTask = SequentialVsConcurrentBenchmark.RunAsync(
            _ => Interlocked.Increment(ref operationACount) == 1
                ? Task.CompletedTask
                : operationBInvokedDuringConcurrentPhase.Task,
            _ =>
            {
                if (Volatile.Read(ref operationACount) >= 2)
                {
                    operationBInvokedDuringConcurrentPhase.TrySetResult();
                }

                return Task.CompletedTask;
            },
            iterations: 1);

        var report = await reportTask.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.NotNull(report);
        Assert.Equal(2, Volatile.Read(ref operationACount));
    }

    [Fact]
    public async Task RunAsync_PropagatesOperationFailures()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            SequentialVsConcurrentBenchmark.RunAsync(
                _ => Task.CompletedTask,
                _ => throw new InvalidOperationException("operation failed"),
                iterations: 1));
    }

    [Fact]
    public async Task RunAsync_RejectsInvalidArguments()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            SequentialVsConcurrentBenchmark.RunAsync(
                null!,
                _ => Task.CompletedTask,
                iterations: 1));

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            SequentialVsConcurrentBenchmark.RunAsync(
                _ => Task.CompletedTask,
                null!,
                iterations: 1));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            SequentialVsConcurrentBenchmark.RunAsync(
                _ => Task.CompletedTask,
                _ => Task.CompletedTask,
                iterations: 0));
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);

        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException("The expected condition was not met within 5 seconds.");
            }

            await Task.Delay(10);
        }
    }
}

using System.Text;

namespace EmployeeManagement.Benchmarks;

/// <summary>
/// Result of a sequential-versus-concurrent measurement run.
/// </summary>
public sealed class PerformanceReport
{
    public PerformanceReport(TimeSpan sequential, TimeSpan concurrent, int iterations)
    {
        if (iterations < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(iterations), "Iterations must be at least 1.");
        }

        Sequential = sequential;
        Concurrent = concurrent;
        Iterations = iterations;
    }

    public TimeSpan Sequential { get; }

    public TimeSpan Concurrent { get; }

    public int Iterations { get; }

    public TimeSpan Difference => Sequential - Concurrent;

    public double? ImprovementPercent =>
        PerformanceCalculator.CalculateImprovementPercent(Sequential, Concurrent);

    public string Format()
    {
        var builder = new StringBuilder();

        builder.AppendLine("Performance Measurement Report");
        builder.AppendLine("------------------------------");
        builder.AppendLine($"Iterations per approach: {Iterations}");
        builder.AppendLine($"Sequential Execution: {FormatDuration(Sequential)}");
        builder.AppendLine($"Concurrent Execution: {FormatDuration(Concurrent)}");
        builder.AppendLine($"Time Difference: {FormatDuration(Difference)}");
        builder.AppendLine(
            $"Performance Improvement: {(ImprovementPercent is { } percent ? $"{percent:0.00}%" : "N/A")}");
        builder.AppendLine();
        builder.AppendLine("Conclusion:");
        builder.AppendLine(BuildConclusion());

        return builder.ToString();
    }

    private string BuildConclusion()
    {
        if (ImprovementPercent is not { } percent)
        {
            return "The measured duration was zero or too small for a reliable percentage calculation. " +
                   "Re-run the benchmark with a larger workload.";
        }

        if (percent > 0)
        {
            return $"Running the two independent operations concurrently was {percent:0.00}% faster than " +
                   $"running them sequentially in this run. Concurrency helps when the operations spend most " +
                   $"of their time waiting on I/O (database round trips). Results depend on database load, " +
                   $"network latency, connection pooling and workload size, so they do not generalise to " +
                   $"every environment.";
        }

        return $"Running the two independent operations concurrently was {Math.Abs(percent):0.00}% slower " +
               $"than running them sequentially in this run. With small, fast or contended queries the " +
               $"overhead of opening extra connections and scheduling tasks can outweigh the benefit of " +
               $"overlap. Results depend on database load, network latency, connection pooling and workload " +
               $"size.";
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration >= TimeSpan.FromSeconds(1))
        {
            return $"{duration.TotalSeconds:0.000} s";
        }

        if (duration >= TimeSpan.FromMilliseconds(1))
        {
            return $"{duration.TotalMilliseconds:0.00} ms";
        }

        if (duration >= TimeSpan.FromMicroseconds(1))
        {
            return $"{duration.TotalMicroseconds:0.00} µs";
        }

        return $"{duration.Ticks} ticks";
    }
}

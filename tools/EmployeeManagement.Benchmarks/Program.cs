namespace EmployeeManagement.Benchmarks;

internal static class Program
{
    private const string DefaultConnection =
        "Server=(localdb)\\MSSQLLocalDB;Database=EmployeeManagementBenchmarks;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

    private const int DefaultIterations = 5;
    private const int DefaultRowCount = 5000;
    private const int PageSize = 1000;

    private static readonly string[] InterestingConnectionStringKeys =
        ["server", "data source", "database", "initial catalog"];

    public static async Task<int> Main(string[] args)
    {
        var connectionString =
            GetOption(args, "--connection")
            ?? Environment.GetEnvironmentVariable("BENCHMARK_CONNECTION")
            ?? DefaultConnection;

        var iterations = ParseIntOption(args, "--iterations") ?? DefaultIterations;
        var rowCount = ParseIntOption(args, "--rows") ?? DefaultRowCount;

        if (iterations < 1 || rowCount < 1)
        {
            Console.Error.WriteLine("Both --iterations and --rows must be positive integers.");
            return 1;
        }

        using var cancellationSource = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationSource.Cancel();
        };

        Console.WriteLine("Employee Management API - sequential vs concurrent benchmark");
        Console.WriteLine($"Target: {DescribeConnectionString(connectionString)}");
        Console.WriteLine($"Rows: {rowCount:N0}; page size: {PageSize:N0}; iterations per approach: {iterations}");
        Console.WriteLine();

        try
        {
            await BenchmarkData.EnsureSeededAsync(connectionString, rowCount, cancellationSource.Token);

            var sequentialContextOptions = BenchmarkData.CreateOptions(connectionString);
            var concurrentContextOptions = BenchmarkData.CreateOptions(connectionString);

            // Warm-up round (excluded from measurement): JIT compilation, connection pooling, page cache.
            await BenchmarkOperations.ReadOrderedPageAsync(sequentialContextOptions, PageSize, cancellationSource.Token);
            await BenchmarkOperations.SumSalariesAsync(concurrentContextOptions, cancellationSource.Token);

            var report = await SequentialVsConcurrentBenchmark.RunAsync(
                cancellationToken => BenchmarkOperations.ReadOrderedPageAsync(
                    sequentialContextOptions, PageSize, cancellationToken),
                cancellationToken => BenchmarkOperations.SumSalariesAsync(
                    concurrentContextOptions, cancellationToken),
                iterations,
                cancellationSource.Token);

            Console.WriteLine(report.Format());
            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Benchmark cancelled.");
            return 130;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Benchmark failed: {exception.Message}");
            return 1;
        }
    }

    private static string? GetOption(string[] args, string name)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return null;
    }

    private static int? ParseIntOption(string[] args, string name) =>
        GetOption(args, name) is { } value && int.TryParse(value, out var parsed) ? parsed : null;

    private static string DescribeConnectionString(string connectionString)
    {
        var parts = connectionString
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(part =>
            {
                var separatorIndex = part.IndexOf('=');
                return separatorIndex > 0
                       && InterestingConnectionStringKeys.Contains(
                           part[..separatorIndex].Trim(),
                           StringComparer.OrdinalIgnoreCase);
            });

        var description = string.Join("; ", parts);
        return description.Length > 0 ? description : "custom connection string";
    }
}

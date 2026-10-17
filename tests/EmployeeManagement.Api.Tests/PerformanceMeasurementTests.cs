using EmployeeManagement.Benchmarks;

namespace EmployeeManagement.Api.Tests;

public class PerformanceCalculatorTests
{
    [Fact]
    public void CalculateImprovement_WhenConcurrentIsFaster_ReturnsPositivePercentage()
    {
        var percent = PerformanceCalculator.CalculateImprovementPercent(
            sequential: TimeSpan.FromMilliseconds(200),
            concurrent: TimeSpan.FromMilliseconds(100));

        Assert.NotNull(percent);
        Assert.Equal(50d, percent.Value, precision: 5);
    }

    [Fact]
    public void CalculateImprovement_WhenConcurrentIsSlower_ReturnsNegativePercentage()
    {
        var percent = PerformanceCalculator.CalculateImprovementPercent(
            sequential: TimeSpan.FromMilliseconds(100),
            concurrent: TimeSpan.FromMilliseconds(150));

        Assert.NotNull(percent);
        Assert.Equal(-50d, percent.Value, precision: 5);
    }

    [Fact]
    public void CalculateImprovement_WhenConcurrentIsInstant_ReturnsHundredPercent()
    {
        var percent = PerformanceCalculator.CalculateImprovementPercent(
            sequential: TimeSpan.FromMilliseconds(50),
            concurrent: TimeSpan.Zero);

        Assert.NotNull(percent);
        Assert.Equal(100d, percent.Value, precision: 5);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 10)]
    public void CalculateImprovement_WhenSequentialDurationIsZero_ReturnsNull(
        long sequentialTicks,
        long concurrentTicks)
    {
        var percent = PerformanceCalculator.CalculateImprovementPercent(
            TimeSpan.FromTicks(sequentialTicks),
            TimeSpan.FromTicks(concurrentTicks));

        Assert.Null(percent);
    }

    [Fact]
    public void CalculateImprovement_WhenSequentialDurationIsTooSmall_ReturnsNull()
    {
        var percent = PerformanceCalculator.CalculateImprovementPercent(
            sequential: TimeSpan.FromTicks(50),
            concurrent: TimeSpan.FromTicks(10));

        Assert.Null(percent);
    }
}

public class PerformanceReportTests
{
    [Fact]
    public void Constructor_RejectsInvalidIterations()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PerformanceReport(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1), iterations: 0));
    }

    [Fact]
    public void Difference_ReturnsSequentialMinusConcurrent()
    {
        var report = new PerformanceReport(
            sequential: TimeSpan.FromMilliseconds(300),
            concurrent: TimeSpan.FromMilliseconds(120),
            iterations: 3);

        Assert.Equal(TimeSpan.FromMilliseconds(180), report.Difference);
        Assert.Equal(3, report.Iterations);
    }

    [Fact]
    public void Format_WithMeaningfulDurations_ReportsValuesAndPercentage()
    {
        var report = new PerformanceReport(
            sequential: TimeSpan.FromMilliseconds(200),
            concurrent: TimeSpan.FromMilliseconds(100),
            iterations: 5);

        var text = report.Format();

        Assert.Contains("Performance Measurement Report", text);
        Assert.Contains("Sequential Execution: 200.00 ms", text);
        Assert.Contains("Concurrent Execution: 100.00 ms", text);
        Assert.Contains("Time Difference: 100.00 ms", text);
        Assert.Contains("Performance Improvement: 50.00%", text);
        Assert.Contains("Conclusion:", text);
    }

    [Fact]
    public void Format_WhenDurationIsTooSmall_ReportsNotApplicable()
    {
        var report = new PerformanceReport(
            sequential: TimeSpan.Zero,
            concurrent: TimeSpan.Zero,
            iterations: 1);

        var text = report.Format();

        Assert.Contains("Performance Improvement: N/A", text);
        Assert.Contains("too small", text);
    }

    [Fact]
    public void Format_WhenConcurrentIsSlower_ReportsDegradation()
    {
        var report = new PerformanceReport(
            sequential: TimeSpan.FromMilliseconds(100),
            concurrent: TimeSpan.FromMilliseconds(150),
            iterations: 1);

        var text = report.Format();

        Assert.Contains("Performance Improvement: -50.00%", text);
        Assert.Contains("slower", text);
    }
}

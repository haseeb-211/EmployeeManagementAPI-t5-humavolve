namespace EmployeeManagement.Benchmarks;

/// <summary>
/// Pure calculation logic for sequential vs concurrent measurements so it can be unit tested.
/// </summary>
public static class PerformanceCalculator
{
    /// <summary>
    /// Durations below this threshold are considered too small for a meaningful percentage
    /// (100 ticks = 10 microseconds).
    /// </summary>
    public static readonly TimeSpan MinimumReliableDuration = TimeSpan.FromTicks(100);

    /// <summary>
    /// Returns the percentage improvement of the concurrent run over the sequential run
    /// ((sequential - concurrent) / sequential * 100), or <c>null</c> when the sequential
    /// duration is zero, negative or too small for a reliable calculation.
    /// A negative value means the concurrent run was slower.
    /// </summary>
    public static double? CalculateImprovementPercent(TimeSpan sequential, TimeSpan concurrent)
    {
        if (sequential < MinimumReliableDuration)
        {
            return null;
        }

        if (concurrent < TimeSpan.Zero)
        {
            concurrent = TimeSpan.Zero;
        }

        return (sequential.Ticks - concurrent.Ticks) / (double)sequential.Ticks * 100d;
    }
}

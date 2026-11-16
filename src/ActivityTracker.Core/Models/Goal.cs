namespace ActivityTracker.Core.Models;

public enum GoalPeriod
{
    Day,
    Week,
    Month,
    Year,

    /// <summary>From the start date up to the deadline (or today if there is none).</summary>
    Total,
}

public enum GoalMetric
{
    /// <summary>Number of entries in the period.</summary>
    Count,

    /// <summary>Sum of entry values in the period (Quantity activities).</summary>
    Sum,
}

/// <summary>An objective such as "3 times per week" or "500 pages before the deadline".</summary>
public sealed class Goal
{
    public GoalPeriod Period { get; set; } = GoalPeriod.Week;

    public GoalMetric Metric { get; set; } = GoalMetric.Count;

    public double Target { get; set; } = 3;
}

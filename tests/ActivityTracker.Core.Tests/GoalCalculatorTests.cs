using ActivityTracker.Core.Models;
using ActivityTracker.Core.Services;
using static ActivityTracker.Core.Tests.TestData;

namespace ActivityTracker.Core.Tests;

public class GoalCalculatorTests
{
    [Fact]
    public void No_goal_returns_null()
    {
        Assert.Null(GoalCalculator.Progress(Check(), [], Monday, DayOfWeek.Monday));
    }

    [Fact]
    public void Weekly_count_only_counts_current_week()
    {
        var a = Check();
        a.Goal = new Goal { Period = GoalPeriod.Week, Metric = GoalMetric.Count, Target = 3 };
        Entry[] entries =
        [
            On(a, Monday.AddDays(-1)), // previous week (Sunday)
            On(a, Monday),
            On(a, Monday.AddDays(2)),
            On(Check("Other"), Monday),
        ];

        var p = GoalCalculator.Progress(a, entries, Monday.AddDays(3), DayOfWeek.Monday)!;

        Assert.Equal(Monday, p.Start);
        Assert.Equal(Monday.AddDays(6), p.End);
        Assert.Equal(2, p.Current);
        Assert.False(p.IsMet);
        Assert.Equal(1, p.Remaining);
        Assert.Equal(2.0 / 3, p.Ratio, 5);
    }

    [Fact]
    public void Sum_goal_adds_values_and_ratio_is_capped()
    {
        var a = Quantity();
        a.Goal = new Goal { Period = GoalPeriod.Month, Metric = GoalMetric.Sum, Target = 60 };
        Entry[] entries = [On(a, Monday, 45), On(a, Monday.AddDays(1), 30)];

        var p = GoalCalculator.Progress(a, entries, Monday, DayOfWeek.Monday)!;

        Assert.Equal(75, p.Current);
        Assert.True(p.IsMet);
        Assert.Equal(1, p.Ratio);
    }

    [Fact]
    public void Sum_metric_on_check_activity_falls_back_to_count()
    {
        var a = Check();
        a.Goal = new Goal { Period = GoalPeriod.Day, Metric = GoalMetric.Sum, Target = 1 };
        var p = GoalCalculator.Progress(a, [On(a, Monday)], Monday, DayOfWeek.Monday)!;
        Assert.Equal(1, p.Current);
    }

    [Fact]
    public void Total_goal_runs_from_start_to_deadline()
    {
        var a = Quantity("Reading", "pages");
        a.StartDate = Monday.AddDays(-10);
        a.Deadline = Monday.AddDays(10);
        a.Goal = new Goal { Period = GoalPeriod.Total, Metric = GoalMetric.Sum, Target = 500 };
        Entry[] entries = [On(a, Monday.AddDays(-11), 999), On(a, Monday.AddDays(-5), 100), On(a, Monday, 50)];

        var p = GoalCalculator.Progress(a, entries, Monday, DayOfWeek.Monday)!;

        Assert.Equal(a.StartDate, p.Start);
        Assert.Equal(a.Deadline, p.End);
        Assert.Equal(150, p.Current);
    }

    [Fact]
    public void Describe_goal()
    {
        var a = Quantity();
        a.Goal = new Goal { Period = GoalPeriod.Week, Metric = GoalMetric.Sum, Target = 150 };
        Assert.Equal("150 min per week", GoalCalculator.Describe(a));

        var b = Check();
        b.Goal = new Goal { Period = GoalPeriod.Week, Metric = GoalMetric.Count, Target = 3 };
        Assert.Equal("3× per week", GoalCalculator.Describe(b));
    }
}

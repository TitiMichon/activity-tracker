using ActivityTracker.Core.Models;
using ActivityTracker.Core.Services;
using static ActivityTracker.Core.Tests.TestData;

namespace ActivityTracker.Core.Tests;

public class StreakCalculatorTests
{
    [Fact]
    public void No_entries_means_no_streak()
    {
        var a = Check();
        Assert.Equal(0, StreakCalculator.Current(a, [], Monday));
        Assert.Equal(0, StreakCalculator.Longest(a, [], Monday));
    }

    [Fact]
    public void Unscheduled_streak_counts_consecutive_days()
    {
        var a = Check();
        Entry[] entries = [On(a, Monday), On(a, Monday.AddDays(-1)), On(a, Monday.AddDays(-2)), On(a, Monday.AddDays(-4))];
        Assert.Equal(3, StreakCalculator.Current(a, entries, Monday));
    }

    [Fact]
    public void Missing_today_does_not_break_streak()
    {
        var a = Check();
        Entry[] entries = [On(a, Monday.AddDays(-1)), On(a, Monday.AddDays(-2))];
        Assert.Equal(2, StreakCalculator.Current(a, entries, Monday));
    }

    [Fact]
    public void Missing_yesterday_breaks_streak()
    {
        var a = Check();
        Entry[] entries = [On(a, Monday.AddDays(-2)), On(a, Monday.AddDays(-3))];
        Assert.Equal(0, StreakCalculator.Current(a, entries, Monday));
    }

    [Fact]
    public void Scheduled_streak_ignores_non_due_days()
    {
        var a = Check(frequency: Frequency.Weekly);
        a.Schedule.DaysOfWeek = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday];
        // Done on Mon 16, Fri 13, Wed 11, Mon 9 - weekend skipped, not a break.
        Entry[] entries = [On(a, Monday), On(a, Monday.AddDays(-3)), On(a, Monday.AddDays(-5)), On(a, Monday.AddDays(-7))];
        Assert.Equal(4, StreakCalculator.Current(a, entries, Monday));
    }

    [Fact]
    public void Longest_streak_finds_best_run()
    {
        var a = Check(start: Monday.AddDays(-20));
        var entries = new List<Entry>();
        for (var i = 15; i >= 11; i--)
        {
            entries.Add(On(a, Monday.AddDays(-i))); // 5-day run
        }

        entries.Add(On(a, Monday.AddDays(-1)));
        entries.Add(On(a, Monday));

        Assert.Equal(5, StreakCalculator.Longest(a, entries, Monday));
        Assert.Equal(2, StreakCalculator.Current(a, entries, Monday));
    }
}

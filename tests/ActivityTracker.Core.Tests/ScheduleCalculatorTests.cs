using ActivityTracker.Core.Models;
using ActivityTracker.Core.Services;
using static ActivityTracker.Core.Tests.TestData;

namespace ActivityTracker.Core.Tests;

public class ScheduleCalculatorTests
{
    [Fact]
    public void None_is_never_due()
    {
        Assert.False(ScheduleCalculator.IsDue(Check(), Monday));
    }

    [Fact]
    public void Daily_is_due_every_day_from_start_date()
    {
        var a = Check(frequency: Frequency.Daily, start: Monday);
        Assert.False(ScheduleCalculator.IsDue(a, Monday.AddDays(-1)));
        Assert.True(ScheduleCalculator.IsDue(a, Monday));
        Assert.True(ScheduleCalculator.IsDue(a, Monday.AddDays(100)));
    }

    [Fact]
    public void Nothing_is_due_after_deadline_or_when_archived()
    {
        var a = Check(frequency: Frequency.Daily, start: Monday);
        a.Deadline = Monday.AddDays(2);
        Assert.True(ScheduleCalculator.IsDue(a, Monday.AddDays(2)));
        Assert.False(ScheduleCalculator.IsDue(a, Monday.AddDays(3)));

        a.Archived = true;
        Assert.False(ScheduleCalculator.IsDue(a, Monday));
    }

    [Fact]
    public void Weekly_uses_selected_days()
    {
        var a = Check(frequency: Frequency.Weekly);
        a.Schedule.DaysOfWeek = [DayOfWeek.Monday, DayOfWeek.Friday];

        var due = ScheduleCalculator.DueDates(a, Monday, Monday.AddDays(13)).ToList();

        Assert.Equal([Monday, Monday.AddDays(4), Monday.AddDays(7), Monday.AddDays(11)], due);
    }

    [Fact]
    public void Weekly_without_days_uses_start_weekday()
    {
        var a = Check(frequency: Frequency.Weekly, start: Monday.AddDays(2)); // Wednesday
        var due = ScheduleCalculator.DueDates(a, Monday, Monday.AddDays(13)).ToList();
        Assert.Equal([Monday.AddDays(2), Monday.AddDays(9)], due);
    }

    [Fact]
    public void Monthly_clamps_to_month_length()
    {
        var a = Check(frequency: Frequency.Monthly, start: new DateOnly(2026, 1, 1));
        a.Schedule.DayOfMonth = 31;

        Assert.True(ScheduleCalculator.IsDue(a, new DateOnly(2026, 2, 28)));
        Assert.True(ScheduleCalculator.IsDue(a, new DateOnly(2026, 4, 30)));
        Assert.True(ScheduleCalculator.IsDue(a, new DateOnly(2026, 5, 31)));
        Assert.False(ScheduleCalculator.IsDue(a, new DateOnly(2026, 5, 30)));
    }

    [Fact]
    public void Every_n_days_counts_from_start()
    {
        var a = Check(frequency: Frequency.EveryNDays, start: Monday);
        a.Schedule.Interval = 3;
        var due = ScheduleCalculator.DueDates(a, Monday, Monday.AddDays(9)).ToList();
        Assert.Equal([Monday, Monday.AddDays(3), Monday.AddDays(6), Monday.AddDays(9)], due);
    }

    [Theory]
    [InlineData(Frequency.None, "Anytime")]
    [InlineData(Frequency.Daily, "Every day")]
    [InlineData(Frequency.EveryNDays, "Every 2 days")]
    public void Describe_is_human_readable(Frequency frequency, string expected)
    {
        Assert.Equal(expected, ScheduleCalculator.Describe(new Schedule { Frequency = frequency }));
    }

    [Fact]
    public void Describe_weekly_lists_days_from_monday()
    {
        var s = new Schedule { Frequency = Frequency.Weekly, DaysOfWeek = [DayOfWeek.Friday, DayOfWeek.Monday] };
        Assert.Equal("Mon, Fri", ScheduleCalculator.Describe(s));
    }
}

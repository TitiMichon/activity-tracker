using ActivityTracker.Core.Services;
using static ActivityTracker.Core.Tests.TestData;

namespace ActivityTracker.Core.Tests;

public class CalendarMathTests
{
    [Theory]
    [InlineData(DayOfWeek.Monday, 16)]
    [InlineData(DayOfWeek.Sunday, 15)]
    [InlineData(DayOfWeek.Tuesday, 17)]
    [InlineData(DayOfWeek.Friday, 13)]
    public void StartOfWeek_respects_first_day(DayOfWeek firstDay, int expectedDay)
    {
        Assert.Equal(new DateOnly(2026, 11, expectedDay), CalendarMath.StartOfWeek(Monday.AddDays(3), firstDay));
    }

    [Fact]
    public void WeekDays_returns_seven_consecutive_days()
    {
        var days = CalendarMath.WeekDays(Monday.AddDays(5), DayOfWeek.Monday);
        Assert.Equal(7, days.Length);
        Assert.Equal(Monday, days[0]);
        Assert.Equal(Monday.AddDays(6), days[6]);
    }

    [Fact]
    public void MonthWeeks_covers_whole_month_with_full_weeks()
    {
        var weeks = CalendarMath.MonthWeeks(2026, 11, DayOfWeek.Monday);

        // November 2026 starts on Sunday and ends on Monday → 6 rows.
        Assert.Equal(6, weeks.Count);
        Assert.All(weeks, w => Assert.Equal(7, w.Length));
        Assert.Equal(new DateOnly(2026, 10, 26), weeks[0][0]);
        Assert.Equal(new DateOnly(2026, 12, 6), weeks[^1][6]);
    }

    [Fact]
    public void Month_bounds()
    {
        Assert.Equal(new DateOnly(2028, 2, 1), CalendarMath.StartOfMonth(new DateOnly(2028, 2, 17)));
        Assert.Equal(new DateOnly(2028, 2, 29), CalendarMath.EndOfMonth(new DateOnly(2028, 2, 17)));
    }
}

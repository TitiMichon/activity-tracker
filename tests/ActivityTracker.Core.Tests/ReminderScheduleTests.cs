using ActivityTracker.Core.Models;
using ActivityTracker.Core.Services;
using static ActivityTracker.Core.Tests.TestData;

namespace ActivityTracker.Core.Tests;

public class NextReminderTests
{
    private static readonly DateTime Morning = Monday.ToDateTime(new TimeOnly(9, 30));

    private static Activity WithReminder(int hour, int minute = 0, Frequency frequency = Frequency.Daily)
    {
        var a = Check("Stretch", frequency);
        a.ReminderTime = new TimeOnly(hour, minute);
        return a;
    }

    [Fact]
    public void Next_is_the_earliest_reminder_still_ahead_today()
    {
        var evening = WithReminder(18, 30);
        var noon = WithReminder(12);
        var passed = WithReminder(8);

        Assert.Equal(Monday.ToDateTime(new TimeOnly(12, 0)), ReminderCalculator.Next([evening, noon, passed], [], Morning));
    }

    [Fact]
    public void Activities_done_today_are_skipped_until_tomorrow()
    {
        var noon = WithReminder(12);

        Assert.Equal(Monday.AddDays(1).ToDateTime(new TimeOnly(12, 0)), ReminderCalculator.Next([noon], [On(noon, Monday, hour: 9)], Morning));
    }

    [Fact]
    public void A_reminder_exactly_now_is_not_next()
    {
        var now = WithReminder(9, 30);

        Assert.Equal(Monday.AddDays(1).ToDateTime(new TimeOnly(9, 30)), ReminderCalculator.Next([now], [], Morning));
    }

    [Fact]
    public void Only_days_the_activity_is_planned_count()
    {
        var weekly = WithReminder(7, frequency: Frequency.Weekly);
        weekly.Schedule.DaysOfWeek = [DayOfWeek.Thursday];

        Assert.Equal(new DateTime(2026, 11, 19, 7, 0, 0), ReminderCalculator.Next([weekly], [], Morning));
    }

    [Fact]
    public void Monthly_reminders_are_found_weeks_ahead()
    {
        var monthly = WithReminder(10, frequency: Frequency.Monthly);
        monthly.Schedule.DayOfMonth = 10;

        Assert.Equal(new DateTime(2026, 12, 10, 10, 0, 0), ReminderCalculator.Next([monthly], [], Morning));
    }

    [Fact]
    public void Nothing_to_plan_without_active_reminders()
    {
        var archived = WithReminder(12);
        archived.Archived = true;

        Assert.Null(ReminderCalculator.Next([archived, Check("No reminder", Frequency.Daily)], [], Morning));
    }
}

public class ReminderLogTests
{
    [Fact]
    public void Round_trips_the_ids_of_the_day()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };

        Assert.Equal(ids.ToHashSet(), ReminderLog.Parse(ReminderLog.Format(Monday, ids), Monday));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("garbage")]
    [InlineData("2026-11-15|6f9619ff-8b86-d011-b42d-00cf4fc964ff")]
    public void Other_days_and_invalid_values_mean_nothing_was_sent(string? stored) =>
        Assert.Empty(ReminderLog.Parse(stored, Monday));
}

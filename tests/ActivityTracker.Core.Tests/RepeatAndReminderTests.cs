using ActivityTracker.Core.Models;
using ActivityTracker.Core.Persistence;
using ActivityTracker.Core.Services;
using static ActivityTracker.Core.Tests.TestData;

namespace ActivityTracker.Core.Tests;

public class DayProgressTests
{
    private static Activity Water(int times = 8)
    {
        var water = Check("Water", Frequency.Daily);
        water.Schedule.TimesPerDay = times;
        return water;
    }

    [Fact]
    public void Counts_entries_of_the_day_against_times_per_day()
    {
        var water = Water();
        var entries = new[] { On(water, Monday), On(water, Monday), On(water, Monday.AddDays(-1)) };

        var progress = DayProgress.Of(water, entries, Monday);

        Assert.Equal(2, progress.Count);
        Assert.Equal(8, progress.Required);
        Assert.False(progress.IsDone);
        Assert.Equal(6, progress.Remaining);
        Assert.Equal("2 / 8", progress.Text);
    }

    [Fact]
    public void Done_once_target_is_reached()
    {
        var water = Water(2);
        var progress = DayProgress.Of(water, [On(water, Monday), On(water, Monday), On(water, Monday)], Monday);
        Assert.True(progress.IsDone);
        Assert.Equal("Done ✓", progress.Text);
    }

    [Fact]
    public void Single_time_activities_keep_simple_text()
    {
        var sport = Check("Sport", Frequency.Daily);
        Assert.Equal("To do", DayProgress.Of(sport, [], Monday).Text);
        Assert.True(DayProgress.Of(sport, [On(sport, Monday)], Monday).IsDone);
    }

    [Fact]
    public void Zero_or_negative_times_per_day_counts_as_one()
    {
        Assert.Equal(1, DayProgress.RequiredFor(Water(0)));
    }

    [Fact]
    public void Streak_only_counts_days_where_target_was_reached()
    {
        var water = Water(2);
        var entries = new[]
        {
            On(water, Monday), On(water, Monday),                       // done
            On(water, Monday.AddDays(-1)), On(water, Monday.AddDays(-1)), // done
            On(water, Monday.AddDays(-2)),                              // only 1 of 2: breaks the streak
            On(water, Monday.AddDays(-3)), On(water, Monday.AddDays(-3)),
        };

        Assert.Equal(2, StreakCalculator.Current(water, entries, Monday));
        Assert.Equal(2, StreakCalculator.Longest(water, entries, Monday));
    }

    [Fact]
    public void Validator_rejects_out_of_range_times_per_day()
    {
        Assert.Contains("Times per day must be between 1 and 100.", ActivityValidator.Validate(Water(0)));
        Assert.Empty(ActivityValidator.Validate(Water(8)));
    }
}

public class ReminderCalculatorTests
{
    private static readonly DateTime Evening = Monday.ToDateTime(new TimeOnly(20, 30));

    private static Activity WithReminder(int hour, Frequency frequency = Frequency.Daily, int times = 1)
    {
        var a = Check("Stretch", frequency);
        a.ReminderTime = new TimeOnly(hour, 0);
        a.Schedule.TimesPerDay = times;
        return a;
    }

    [Fact]
    public void Reminds_when_time_has_passed_and_not_done()
    {
        var a = WithReminder(20);
        Assert.Equal([a], ReminderCalculator.Pending([a], [], Evening));
    }

    [Fact]
    public void Does_not_remind_before_the_time()
    {
        var a = WithReminder(21);
        Assert.Empty(ReminderCalculator.Pending([a], [], Evening));
    }

    [Fact]
    public void Does_not_remind_when_done()
    {
        var a = WithReminder(20);
        Assert.Empty(ReminderCalculator.Pending([a], [On(a, Monday)], Evening));
    }

    [Fact]
    public void Reminds_while_repeated_target_is_not_reached()
    {
        var water = WithReminder(20, times: 3);
        var entries = new[] { On(water, Monday), On(water, Monday) };

        Assert.Single(ReminderCalculator.Pending([water], entries, Evening));
        Assert.Equal("⭐ Stretch: 2 / 3 today", ReminderCalculator.Message(water, entries, Monday));
    }

    [Fact]
    public void Ignores_days_not_planned_archived_and_no_reminder()
    {
        var weekly = WithReminder(20, Frequency.Weekly);
        weekly.Schedule.DaysOfWeek = [DayOfWeek.Tuesday];
        var archived = WithReminder(20);
        archived.Archived = true;
        var none = Check("No reminder", Frequency.Daily);

        Assert.Empty(ReminderCalculator.Pending([weekly, archived, none], [], Evening));
    }
}

public class NewFieldsPersistenceTests
{
    [Fact]
    public void Hide_reminder_and_times_per_day_survive_a_round_trip()
    {
        var a = Check("Water", Frequency.Daily);
        a.HideFromCalendar = true;
        a.ReminderTime = new TimeOnly(19, 45);
        a.Schedule.TimesPerDay = 8;
        var data = new TrackerData { Activities = [a] };

        var back = TrackerJson.Deserialize(TrackerJson.Serialize(data)).Activities.Single();

        Assert.True(back.HideFromCalendar);
        Assert.Equal(new TimeOnly(19, 45), back.ReminderTime);
        Assert.Equal(8, back.Schedule.TimesPerDay);
    }

    [Fact]
    public void Data_saved_by_v1_0_loads_with_defaults()
    {
        const string v1 = """
            {"schemaVersion":1,"activities":[{"id":"6f9d0f5e-8a5b-4a3b-9d55-1b9f2b7c1a10","name":"Sport","schedule":{"frequency":"Daily"}}],"entries":[]}
            """;

        var a = TrackerJson.Deserialize(v1).Activities.Single();

        Assert.False(a.HideFromCalendar);
        Assert.Null(a.ReminderTime);
        Assert.Equal(1, a.Schedule.TimesPerDay);
    }
}

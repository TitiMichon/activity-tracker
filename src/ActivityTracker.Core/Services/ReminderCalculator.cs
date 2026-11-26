using ActivityTracker.Core.Models;

namespace ActivityTracker.Core.Services;

/// <summary>Decides which activities need a reminder right now.</summary>
public static class ReminderCalculator
{
    /// <summary>
    /// Active activities with a reminder time that has passed today, planned today and not done yet.
    /// </summary>
    public static IReadOnlyList<Activity> Pending(IEnumerable<Activity> activities, IEnumerable<Entry> entries, DateTime now)
    {
        var today = DateOnly.FromDateTime(now);
        var time = TimeOnly.FromDateTime(now);
        var todays = entries.Where(e => e.Date == today).ToList();
        return activities
            .Where(a => !a.Archived && a.ReminderTime is { } at && at <= time)
            .Where(a => ScheduleCalculator.IsDue(a, today))
            .Where(a => !DayProgress.Of(a, todays, today).IsDone)
            .ToList();
    }

    /// <summary>
    /// The next moment strictly after <paramref name="now"/> at which a reminder may be needed: the earliest reminder time
    /// of an active activity planned that day (and, today, not done yet). Looks at most <paramref name="days"/> days ahead.
    /// </summary>
    public static DateTime? Next(IEnumerable<Activity> activities, IEnumerable<Entry> entries, DateTime now, int days = 31)
    {
        var today = DateOnly.FromDateTime(now);
        var todays = entries.Where(e => e.Date == today).ToList();
        var withReminder = activities.Where(a => !a.Archived && a.ReminderTime is not null).ToList();

        for (var day = today; day <= today.AddDays(days); day = day.AddDays(1))
        {
            var times = withReminder
                .Where(a => ScheduleCalculator.IsDue(a, day))
                .Where(a => day != today || !DayProgress.Of(a, todays, day).IsDone)
                .Select(a => day.ToDateTime(a.ReminderTime!.Value))
                .Where(at => at > now)
                .ToList();
            if (times.Count > 0)
            {
                return times.Min();
            }
        }

        return null;
    }

    /// <summary>Notification text, e.g. "Water: 3 / 8 today".</summary>
    public static string Message(Activity activity, IEnumerable<Entry> entries, DateOnly today)
    {
        var progress = DayProgress.Of(activity, entries, today);
        return progress.Required > 1
            ? $"{activity.Icon} {activity.Name}: {progress.Count} / {progress.Required} today"
            : $"{activity.Icon} {activity.Name} is planned today and not done yet";
    }
}

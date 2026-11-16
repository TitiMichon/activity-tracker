using ActivityTracker.Core.Models;

namespace ActivityTracker.Core.Services;

/// <summary>Answers "is this activity planned on that day?".</summary>
public static class ScheduleCalculator
{
    public static bool IsDue(Activity activity, DateOnly date)
    {
        if (activity.Archived || date < activity.StartDate)
        {
            return false;
        }

        if (activity.Deadline is { } deadline && date > deadline)
        {
            return false;
        }

        var schedule = activity.Schedule;
        return schedule.Frequency switch
        {
            Frequency.None => false,
            Frequency.Daily => true,
            Frequency.Weekly => schedule.DaysOfWeek.Count == 0
                ? date.DayOfWeek == activity.StartDate.DayOfWeek
                : schedule.DaysOfWeek.Contains(date.DayOfWeek),
            Frequency.Monthly => date.Day == Math.Clamp(schedule.DayOfMonth, 1, DateTime.DaysInMonth(date.Year, date.Month)),
            Frequency.EveryNDays => (date.DayNumber - activity.StartDate.DayNumber) % Math.Max(1, schedule.Interval) == 0,
            _ => false,
        };
    }

    public static IEnumerable<DateOnly> DueDates(Activity activity, DateOnly from, DateOnly to)
    {
        for (var d = from; d <= to; d = d.AddDays(1))
        {
            if (IsDue(activity, d))
            {
                yield return d;
            }
        }
    }

    /// <summary>Human-readable summary, e.g. "Mon, Wed, Fri".</summary>
    public static string Describe(Schedule schedule) => schedule.Frequency switch
    {
        Frequency.None => "Anytime",
        Frequency.Daily => "Every day",
        Frequency.Weekly when schedule.DaysOfWeek.Count is 0 or 7 => schedule.DaysOfWeek.Count == 7 ? "Every day" : "Weekly",
        Frequency.Weekly => string.Join(", ", OrderedWeek(DayOfWeek.Monday)
            .Where(schedule.DaysOfWeek.Contains)
            .Select(d => d.ToString()[..3])),
        Frequency.Monthly => $"Monthly on day {schedule.DayOfMonth}",
        Frequency.EveryNDays => $"Every {Math.Max(1, schedule.Interval)} days",
        _ => "",
    };

    public static IEnumerable<DayOfWeek> OrderedWeek(DayOfWeek firstDay) =>
        Enumerable.Range(0, 7).Select(i => (DayOfWeek)(((int)firstDay + i) % 7));
}

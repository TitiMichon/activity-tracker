using ActivityTracker.Core.Models;

namespace ActivityTracker.Core.Services;

/// <summary>
/// Streaks count consecutive "expected" days that are done
/// (at least <see cref="Schedule.TimesPerDay"/> entries).
/// For unscheduled activities every day is expected; otherwise only due days count
/// and non-due days neither extend nor break the streak.
/// </summary>
public static class StreakCalculator
{
    private const int MaxLookbackDays = 3650;

    public static int Current(Activity activity, IEnumerable<Entry> entries, DateOnly today)
    {
        var days = ActiveDays(activity, entries);
        var streak = 0;
        for (var i = 0; i < MaxLookbackDays; i++)
        {
            var d = today.AddDays(-i);
            if (d < activity.StartDate && !days.Contains(d))
            {
                break;
            }

            if (!IsExpected(activity, d))
            {
                continue;
            }

            if (days.Contains(d))
            {
                streak++;
            }
            else if (d != today)
            {
                // Today is not over yet: missing today doesn't break the streak.
                break;
            }
        }

        return streak;
    }

    public static int Longest(Activity activity, IEnumerable<Entry> entries, DateOnly today)
    {
        var days = ActiveDays(activity, entries);
        if (days.Count == 0)
        {
            return 0;
        }

        var first = days.Min() < activity.StartDate ? days.Min() : activity.StartDate;
        int best = 0, run = 0;
        for (var d = first; d <= today; d = d.AddDays(1))
        {
            if (!IsExpected(activity, d))
            {
                continue;
            }

            if (days.Contains(d))
            {
                run++;
                best = Math.Max(best, run);
            }
            else if (d != today)
            {
                run = 0;
            }
        }

        return best;
    }

    private static bool IsExpected(Activity activity, DateOnly date) =>
        activity.Schedule.Frequency == Frequency.None || ScheduleCalculator.IsDue(activity, date);

    private static HashSet<DateOnly> ActiveDays(Activity activity, IEnumerable<Entry> entries) =>
        entries.Where(e => e.ActivityId == activity.Id)
            .GroupBy(e => e.Date)
            .Where(g => g.Count() >= DayProgress.RequiredFor(activity))
            .Select(g => g.Key)
            .ToHashSet();
}

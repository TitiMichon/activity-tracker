using ActivityTracker.Core.Models;

namespace ActivityTracker.Core.Services;

public sealed record GoalProgress(DateOnly Start, DateOnly End, double Current, double Target)
{
    public double Ratio => Target <= 0 ? 0 : Math.Min(1, Current / Target);

    public bool IsMet => Current >= Target;

    public double Remaining => Math.Max(0, Target - Current);
}

/// <summary>Computes objective progress for the period containing a given date.</summary>
public static class GoalCalculator
{
    public static (DateOnly Start, DateOnly End) PeriodRange(Activity activity, GoalPeriod period, DateOnly date, DayOfWeek firstDay) => period switch
    {
        GoalPeriod.Day => (date, date),
        GoalPeriod.Week => (CalendarMath.StartOfWeek(date, firstDay), CalendarMath.StartOfWeek(date, firstDay).AddDays(6)),
        GoalPeriod.Month => (CalendarMath.StartOfMonth(date), CalendarMath.EndOfMonth(date)),
        GoalPeriod.Year => (new DateOnly(date.Year, 1, 1), new DateOnly(date.Year, 12, 31)),
        GoalPeriod.Total => (activity.StartDate, activity.Deadline ?? date),
        _ => throw new ArgumentOutOfRangeException(nameof(period)),
    };

    public static double Measure(Activity activity, GoalMetric metric, IEnumerable<Entry> entries) =>
        metric == GoalMetric.Sum && activity.Kind == ActivityKind.Quantity
            ? entries.Sum(e => e.Value ?? 0)
            : entries.Count();

    /// <returns>Null when the activity has no goal.</returns>
    public static GoalProgress? Progress(Activity activity, IEnumerable<Entry> allEntries, DateOnly date, DayOfWeek firstDay)
    {
        if (activity.Goal is not { } goal)
        {
            return null;
        }

        var (start, end) = PeriodRange(activity, goal.Period, date, firstDay);
        var inPeriod = allEntries.Where(e => e.ActivityId == activity.Id && e.Date >= start && e.Date <= end);
        return new GoalProgress(start, end, Measure(activity, goal.Metric, inPeriod), goal.Target);
    }

    public static string Describe(Activity activity)
    {
        if (activity.Goal is not { } goal)
        {
            return "";
        }

        var what = goal.Metric == GoalMetric.Sum && activity.Kind == ActivityKind.Quantity
            ? $"{goal.Target:0.##} {activity.Unit}".TrimEnd()
            : $"{goal.Target:0.##}×";
        var when = goal.Period switch
        {
            GoalPeriod.Day => "per day",
            GoalPeriod.Week => "per week",
            GoalPeriod.Month => "per month",
            GoalPeriod.Year => "per year",
            _ => activity.Deadline is { } d ? $"by {d:yyyy-MM-dd}" : "in total",
        };
        return $"{what} {when}";
    }
}

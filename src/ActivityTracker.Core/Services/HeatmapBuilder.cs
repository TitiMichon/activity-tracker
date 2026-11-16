using ActivityTracker.Core.Models;

namespace ActivityTracker.Core.Services;

/// <param name="Level">0 (nothing) to 4 (max intensity).</param>
/// <param name="Color">Cell color; for Scale activities it is the color of the closest option.</param>
/// <param name="InRange">False for padding cells outside the requested range.</param>
public sealed record HeatmapCell(DateOnly Date, double Value, int Level, string? Color, bool InRange, int EntryCount);

public sealed record Heatmap(DateOnly From, DateOnly To, IReadOnlyList<HeatmapCell[]> Weeks, double Max)
{
    public IEnumerable<HeatmapCell> Cells => Weeks.SelectMany(w => w).Where(c => c.InRange);

    public int ActiveDays => Cells.Count(c => c.EntryCount > 0);

    public int TotalEntries => Cells.Sum(c => c.EntryCount);
}

/// <summary>Builds a GitHub-style heatmap: one column per week, one row per weekday.</summary>
public static class HeatmapBuilder
{
    public const int MaxLevel = 4;

    /// <param name="activity">Null means "all activities" (value = number of entries).</param>
    public static Heatmap Build(Activity? activity, IEnumerable<Entry> entries, DateOnly from, DateOnly to, DayOfWeek firstDay)
    {
        var byDay = entries
            .Where(e => activity is null || e.ActivityId == activity.Id)
            .Where(e => e.Date >= from && e.Date <= to)
            .GroupBy(e => e.Date)
            .ToDictionary(g => g.Key, g => g.ToList());

        var values = byDay.ToDictionary(kv => kv.Key, kv => DayValue(activity, kv.Value));
        var max = values.Count == 0 ? 0 : values.Values.Max();

        var weeks = new List<HeatmapCell[]>();
        for (var start = CalendarMath.StartOfWeek(from, firstDay); start <= to; start = start.AddDays(7))
        {
            weeks.Add(Enumerable.Range(0, 7).Select(i =>
            {
                var date = start.AddDays(i);
                var inRange = date >= from && date <= to;
                var dayEntries = byDay.GetValueOrDefault(date);
                var value = values.GetValueOrDefault(date);
                var (level, color) = activity?.Kind == ActivityKind.Scale && dayEntries is not null
                    ? (MaxLevel, ClosestOption(activity, value)?.Color ?? activity.Color)
                    : (Level(value, max), activity?.Color);
                return new HeatmapCell(date, value, inRange ? level : 0, color, inRange, inRange ? dayEntries?.Count ?? 0 : 0);
            }).ToArray());
        }

        return new Heatmap(from, to, weeks, max);
    }

    /// <summary>
    /// Value of a day: entry count (all activities / Check), sum of values (Quantity),
    /// or average option score (Scale).
    /// </summary>
    public static double DayValue(Activity? activity, IReadOnlyCollection<Entry> dayEntries)
    {
        if (dayEntries.Count == 0)
        {
            return 0;
        }

        return activity?.Kind switch
        {
            ActivityKind.Quantity => dayEntries.Sum(e => e.Value ?? 0),
            ActivityKind.Scale => dayEntries.Average(e => activity.FindOption(e.OptionId)?.Score ?? 0),
            _ => dayEntries.Count,
        };
    }

    public static int Level(double value, double max)
    {
        if (value <= 0 || max <= 0)
        {
            return 0;
        }

        return Math.Clamp((int)Math.Ceiling(value / max * MaxLevel), 1, MaxLevel);
    }

    public static ActivityOption? ClosestOption(Activity activity, double score) =>
        activity.Options.OrderBy(o => Math.Abs(o.Score - score)).FirstOrDefault();
}

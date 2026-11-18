using ActivityTracker.Core.Models;

namespace ActivityTracker.Core.Services;

/// <summary>How far an activity is on a given day, e.g. "3 / 8" glasses of water.</summary>
public sealed record DayProgress(int Count, int Required)
{
    public bool IsDone => Count >= Required;

    public int Remaining => Math.Max(0, Required - Count);

    /// <summary>"Done ✓", "3 / 8" or "To do".</summary>
    public string Text => IsDone ? "Done ✓" : Required > 1 ? $"{Count} / {Required}" : "To do";

    public static int RequiredFor(Activity activity) => Math.Max(1, activity.Schedule.TimesPerDay);

    public static DayProgress Of(Activity activity, IEnumerable<Entry> entries, DateOnly date) =>
        new(entries.Count(e => e.ActivityId == activity.Id && e.Date == date), RequiredFor(activity));
}

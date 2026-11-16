using ActivityTracker.Core.Models;

namespace ActivityTracker.Core.Tests;

internal static class TestData
{
    /// <summary>Monday 16 November 2026.</summary>
    public static readonly DateOnly Monday = new(2026, 11, 16);

    public static Activity Check(string name = "Sport", Frequency frequency = Frequency.None, DateOnly? start = null) => new()
    {
        Name = name,
        Kind = ActivityKind.Check,
        StartDate = start ?? Monday.AddDays(-60),
        Schedule = new Schedule { Frequency = frequency },
    };

    public static Activity Quantity(string name = "Guitar", string unit = "min") => new()
    {
        Name = name,
        Kind = ActivityKind.Quantity,
        Unit = unit,
        StartDate = Monday.AddDays(-60),
    };

    public static Activity Mood() => new()
    {
        Name = "Mood",
        Kind = ActivityKind.Scale,
        StartDate = Monday.AddDays(-60),
        Options =
        [
            new ActivityOption { Id = "bad", Label = "Bad", Icon = "😕", Color = "#ff0000", Score = 1 },
            new ActivityOption { Id = "ok", Label = "Ok", Icon = "😐", Color = "#ffff00", Score = 3 },
            new ActivityOption { Id = "great", Label = "Great", Icon = "😄", Color = "#00ff00", Score = 5 },
        ],
    };

    public static Entry On(Activity activity, DateOnly date, double? value = null, string? optionId = null, int hour = 12) => new()
    {
        ActivityId = activity.Id,
        Timestamp = date.ToDateTime(new TimeOnly(hour, 0)),
        Value = value,
        OptionId = optionId,
    };
}

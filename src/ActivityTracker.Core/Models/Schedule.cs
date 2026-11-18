namespace ActivityTracker.Core.Models;

public enum Frequency
{
    /// <summary>No planned occurrence: log whenever it happens.</summary>
    None,
    Daily,

    /// <summary>On selected days of the week.</summary>
    Weekly,

    /// <summary>On a given day of the month.</summary>
    Monthly,

    /// <summary>Every N days, counted from the start date.</summary>
    EveryNDays,
}

/// <summary>Periodicity of an activity.</summary>
public sealed class Schedule
{
    public Frequency Frequency { get; set; } = Frequency.None;

    /// <summary>Days used by <see cref="Frequency.Weekly"/>. Empty means "same weekday as the start date".</summary>
    public List<DayOfWeek> DaysOfWeek { get; set; } = [];

    /// <summary>N for <see cref="Frequency.EveryNDays"/>.</summary>
    public int Interval { get; set; } = 2;

    /// <summary>Day used by <see cref="Frequency.Monthly"/> (clamped to the month length).</summary>
    public int DayOfMonth { get; set; } = 1;

    /// <summary>How many entries make a planned day "done" (e.g. 8 glasses of water). At least 1.</summary>
    public int TimesPerDay { get; set; } = 1;
}

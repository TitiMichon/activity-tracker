namespace ActivityTracker.Core.Models;

/// <summary>How a single entry of an activity is measured.</summary>
public enum ActivityKind
{
    /// <summary>Done / not done (e.g. "Went to the gym").</summary>
    Check,

    /// <summary>A number with a unit (e.g. 30 minutes of guitar, 25 pages).</summary>
    Quantity,

    /// <summary>Pick one of predefined options, each with its own icon/color (e.g. mood).</summary>
    Scale,
}

/// <summary>An activity (category) that can be tracked: music, sport, reading, mood...</summary>
public sealed class Activity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "";

    public string? Description { get; set; }

    /// <summary>An emoji (or short text) used as the activity logo.</summary>
    public string Icon { get; set; } = "⭐";

    /// <summary>Hex color, e.g. <c>#4f46e5</c>.</summary>
    public string Color { get; set; } = "#4f46e5";

    public ActivityKind Kind { get; set; } = ActivityKind.Check;

    /// <summary>Unit for <see cref="ActivityKind.Quantity"/> activities (min, pages, km...).</summary>
    public string? Unit { get; set; }

    /// <summary>Value pre-filled when quick-adding a <see cref="ActivityKind.Quantity"/> entry.</summary>
    public double? DefaultValue { get; set; }

    /// <summary>Options for <see cref="ActivityKind.Scale"/> activities.</summary>
    public List<ActivityOption> Options { get; set; } = [];

    /// <summary>When the activity is expected to happen (periodicity).</summary>
    public Schedule Schedule { get; set; } = new();

    /// <summary>Optional objective, e.g. "3 times a week" or "120 minutes a week".</summary>
    public Goal? Goal { get; set; }

    /// <summary>First day the activity is tracked.</summary>
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    /// <summary>Optional deadline: the schedule stops and "Total" goals are measured up to this date.</summary>
    public DateOnly? Deadline { get; set; }

    public bool Archived { get; set; }

    /// <summary>Keeps the activity (entries and planned markers) off the calendar views.</summary>
    public bool HideFromCalendar { get; set; }

    /// <summary>
    /// Time of day at which to remind about the activity on days it is planned and not yet done.
    /// Null means no reminder.
    /// </summary>
    public TimeOnly? ReminderTime { get; set; }

    public int SortOrder { get; set; }

    public ActivityOption? FindOption(string? optionId) =>
        optionId is null ? null : Options.FirstOrDefault(o => o.Id == optionId);
}

/// <summary>One choice of a <see cref="ActivityKind.Scale"/> activity (e.g. "😄 Great").</summary>
public sealed class ActivityOption
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    public string Label { get; set; } = "";

    public string Icon { get; set; } = "🙂";

    public string Color { get; set; } = "#22c55e";

    /// <summary>Numeric score used for averages and the heatmap (higher = better).</summary>
    public double Score { get; set; }
}

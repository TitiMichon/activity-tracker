namespace ActivityTracker.Core.Models;

/// <summary>Everything the app persists. Serialized as a single JSON document.</summary>
public sealed class TrackerData
{
    /// <summary>
    /// Version of the saved format. Bump it with a step in <c>TrackerMigrations</c> whenever saved data
    /// must change shape. History: 1 = activities and entries (v1.0), 2 = tasks (v1.2).
    /// </summary>
    public const int CurrentSchemaVersion = 2;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public TrackerSettings Settings { get; set; } = new();

    public List<Activity> Activities { get; set; } = [];

    public List<Entry> Entries { get; set; } = [];

    public List<TaskItem> Tasks { get; set; } = [];
}

public sealed class TrackerSettings
{
    public DayOfWeek FirstDayOfWeek { get; set; } = DayOfWeek.Monday;
}

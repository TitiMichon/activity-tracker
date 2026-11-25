namespace ActivityTracker.Core.Models;

/// <summary>Everything the app persists. Serialized as a single JSON document.</summary>
public sealed class TrackerData
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public TrackerSettings Settings { get; set; } = new();

    public List<Activity> Activities { get; set; } = [];

    public List<Entry> Entries { get; set; } = [];

    public List<TodoItem> Todos { get; set; } = [];
}

public sealed class TrackerSettings
{
    public DayOfWeek FirstDayOfWeek { get; set; } = DayOfWeek.Monday;
}

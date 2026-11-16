using System.Text.Json.Serialization;

namespace ActivityTracker.Core.Models;

/// <summary>One logged event of an activity.</summary>
public sealed class Entry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ActivityId { get; set; }

    /// <summary>Local date and time of the event.</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>Value for Quantity activities (minutes, pages...).</summary>
    public double? Value { get; set; }

    /// <summary>Selected option for Scale activities.</summary>
    public string? OptionId { get; set; }

    public string? Note { get; set; }

    /// <summary>Per-event icon override (falls back to option, then activity).</summary>
    public string? Icon { get; set; }

    /// <summary>Per-event color override (falls back to option, then activity).</summary>
    public string? Color { get; set; }

    [JsonIgnore]
    public DateOnly Date => DateOnly.FromDateTime(Timestamp);
}

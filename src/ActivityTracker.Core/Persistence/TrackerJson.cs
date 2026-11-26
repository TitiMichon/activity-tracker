using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using ActivityTracker.Core.Models;

namespace ActivityTracker.Core.Persistence;

public static class TrackerJson
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Serialize(TrackerData data) => JsonSerializer.Serialize(data, Options);

    /// <exception cref="FormatException">The JSON is invalid or from a newer, unsupported version.</exception>
    public static TrackerData Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new TrackerData();
        }

        TrackerData? data;
        try
        {
            var node = JsonNode.Parse(json);
            if (node is null)
            {
                return new TrackerData();
            }

            if (node is not JsonObject root)
            {
                throw new FormatException("The data is not a valid Activity Tracker export.");
            }

            TrackerMigrations.Upgrade(root);
            data = root.Deserialize<TrackerData>(Options);
        }
        catch (JsonException ex)
        {
            throw new FormatException("The data is not a valid Activity Tracker export.", ex);
        }

        if (data is null)
        {
            return new TrackerData();
        }

        // Lists written as null by hand-edited files.
        data.Settings ??= new TrackerSettings();
        data.Activities ??= [];
        data.Entries ??= [];
        data.Tasks ??= [];
        foreach (var task in data.Tasks)
        {
            task.Subtasks ??= [];
        }

        return data;
    }

    public static T Clone<T>(T value) =>
        JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, Options), Options)!;
}

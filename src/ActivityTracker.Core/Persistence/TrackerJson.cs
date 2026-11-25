using System.Text.Json;
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
            data = JsonSerializer.Deserialize<TrackerData>(json, Options);
        }
        catch (JsonException ex)
        {
            throw new FormatException("The data is not a valid Activity Tracker export.", ex);
        }

        if (data is null)
        {
            return new TrackerData();
        }

        if (data.SchemaVersion > TrackerData.CurrentSchemaVersion)
        {
            throw new FormatException($"Data schema v{data.SchemaVersion} is newer than supported v{TrackerData.CurrentSchemaVersion}. Please update the app.");
        }

        // Exports from before to-dos existed (or edited by hand) may have no list at all.
        data.Todos ??= [];
        data.SchemaVersion = TrackerData.CurrentSchemaVersion;
        return data;
    }

    public static T Clone<T>(T value) =>
        JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, Options), Options)!;
}

using System.Text.Json.Nodes;
using ActivityTracker.Core.Models;

namespace ActivityTracker.Core.Persistence;

/// <summary>
/// Upgrades saved data (and backups) from older schema versions, step by step, on the raw JSON,
/// so a step can rename or reshape fields the current model no longer has.
/// </summary>
public static class TrackerMigrations
{
    /// <summary>Step <c>i</c> upgrades version <c>i + 1</c> to <c>i + 2</c>.</summary>
    private static readonly Action<JsonObject>[] Steps =
    [
        V1ToV2,
    ];

    /// <summary>Must be <see cref="TrackerData.CurrentSchemaVersion"/> - 1 (checked by a unit test).</summary>
    public static int StepCount => Steps.Length;

    /// <summary>Data saved before the version field existed counts as version 1.</summary>
    public static int VersionOf(JsonObject json) =>
        json["schemaVersion"] is JsonValue value && value.TryGetValue(out int version) ? version : 1;

    /// <summary>Upgrades <paramref name="json"/> in place to <see cref="TrackerData.CurrentSchemaVersion"/>.</summary>
    /// <exception cref="FormatException">The data is from a newer, unsupported version.</exception>
    public static void Upgrade(JsonObject json)
    {
        var version = VersionOf(json);
        if (version > TrackerData.CurrentSchemaVersion)
        {
            throw new FormatException($"Data schema v{version} is newer than supported v{TrackerData.CurrentSchemaVersion}. Please update the app.");
        }

        for (var v = Math.Max(version, 1); v < TrackerData.CurrentSchemaVersion; v++)
        {
            Steps[v - 1](json);
        }

        json["schemaVersion"] = TrackerData.CurrentSchemaVersion;
    }

    /// <summary>v1.2 adds tasks; activities and entries are unchanged.</summary>
    private static void V1ToV2(JsonObject json) => json["tasks"] ??= new JsonArray();
}

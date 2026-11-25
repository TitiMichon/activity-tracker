using ActivityTracker.Core.Persistence;

namespace ActivityTracker.UI.Platform;

/// <summary>Persists the tracker data as one JSON document in the platform's key-value storage.</summary>
public sealed class KeyValueTrackerStore(IKeyValueStorage storage) : ITrackerStore
{
    public const string Key = "activity-tracker:data";

    public Task<string?> LoadAsync() => storage.GetAsync(Key);

    public Task SaveAsync(string json) => storage.SetAsync(Key, json);
}

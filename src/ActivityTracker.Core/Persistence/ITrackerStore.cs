namespace ActivityTracker.Core.Persistence;

/// <summary>Raw storage for the serialized tracker data (browser localStorage, memory, file...).</summary>
public interface ITrackerStore
{
    Task<string?> LoadAsync();

    Task SaveAsync(string json);
}

public sealed class InMemoryTrackerStore(string? initial = null) : ITrackerStore
{
    public string? Json { get; private set; } = initial;

    public int SaveCount { get; private set; }

    public Task<string?> LoadAsync() => Task.FromResult(Json);

    public Task SaveAsync(string json)
    {
        Json = json;
        SaveCount++;
        return Task.CompletedTask;
    }
}

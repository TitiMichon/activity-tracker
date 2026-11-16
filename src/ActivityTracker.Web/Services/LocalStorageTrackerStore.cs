using ActivityTracker.Core.Persistence;
using Microsoft.JSInterop;

namespace ActivityTracker.Web.Services;

/// <summary>Persists the tracker data in the browser's localStorage.</summary>
public sealed class LocalStorageTrackerStore(IJSRuntime js) : ITrackerStore
{
    public const string Key = "activity-tracker:data";

    public async Task<string?> LoadAsync() => await js.InvokeAsync<string?>("tracker.storageGet", Key);

    public async Task SaveAsync(string json) => await js.InvokeVoidAsync("tracker.storageSet", Key, json);
}

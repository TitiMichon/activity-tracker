namespace ActivityTracker.UI.Platform;

/// <summary>Small persistent string storage: localStorage in the browser, app files on Android.</summary>
public interface IKeyValueStorage
{
    Task<string?> GetAsync(string key);

    Task SetAsync(string key, string value);
}

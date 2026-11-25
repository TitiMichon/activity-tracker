using Microsoft.JSInterop;

namespace ActivityTracker.UI.Platform;

/// <summary>localStorage through <c>window.tracker</c> (wwwroot/js/app.js).</summary>
public sealed class BrowserKeyValueStorage(IJSRuntime js) : IKeyValueStorage
{
    public async Task<string?> GetAsync(string key) => await js.InvokeAsync<string?>("tracker.storageGet", key);

    public async Task SetAsync(string key, string value) => await js.InvokeVoidAsync("tracker.storageSet", key, value);
}

/// <summary>Downloads the file through a temporary link.</summary>
public sealed class BrowserFileExporter(IJSRuntime js) : IFileExporter
{
    public async Task ExportAsync(string fileName, string contentType, string content) =>
        await js.InvokeVoidAsync("tracker.download", fileName, content);
}

/// <summary>Web Notifications API; only shown while the app is open.</summary>
public sealed class BrowserNotificationPlatform(IJSRuntime js) : INotificationPlatform
{
    public async Task<string> PermissionAsync() => await js.InvokeAsync<string?>("tracker.notificationPermission") ?? "unsupported";

    public async Task<string> RequestPermissionAsync() => await js.InvokeAsync<string?>("tracker.requestNotifications") ?? "unsupported";

    public async Task ShowAsync(string title, string message) => await js.InvokeVoidAsync("tracker.notify", title, message);
}

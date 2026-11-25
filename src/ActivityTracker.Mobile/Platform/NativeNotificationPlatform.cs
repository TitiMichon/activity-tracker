using ActivityTracker.UI.Platform;

namespace ActivityTracker.Mobile.Platform;

/// <summary>Android system notifications (the permission is needed from Android 13).</summary>
public sealed class NativeNotificationPlatform : INotificationPlatform
{
    private const string AskedKey = "notifications-asked";

    public async Task<string> PermissionAsync()
    {
        var status = await MainThread.InvokeOnMainThreadAsync(Permissions.CheckStatusAsync<Permissions.PostNotifications>);

        // Android reports "denied" until the user is asked once: show the "Allow notifications" button instead.
        return status == PermissionStatus.Denied && !Preferences.Default.Get(AskedKey, false) ? "default" : Map(status);
    }

    public async Task<string> RequestPermissionAsync()
    {
        var status = await MainThread.InvokeOnMainThreadAsync(Permissions.RequestAsync<Permissions.PostNotifications>);
        Preferences.Default.Set(AskedKey, true);
        return Map(status);
    }

    public Task ShowAsync(string title, string message)
    {
        Notifications.Show(title, message);
        return Task.CompletedTask;
    }

    private static string Map(PermissionStatus status) => status switch
    {
        PermissionStatus.Granted or PermissionStatus.Limited => "granted",
        PermissionStatus.Denied => "denied",
        PermissionStatus.Unknown => "default",
        _ => "unsupported",
    };
}

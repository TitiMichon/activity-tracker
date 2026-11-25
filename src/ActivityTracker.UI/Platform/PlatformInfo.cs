namespace ActivityTracker.UI.Platform;

/// <summary>Describes the host so screens can word platform-specific help correctly.</summary>
/// <param name="IsNative">True in the installed native app (Android), false in the browser.</param>
public sealed record PlatformInfo(bool IsNative)
{
    public static PlatformInfo Browser { get; } = new(false);

    public static PlatformInfo Android { get; } = new(true);

    public string WhereDataIsStored => IsNative ? "in the app" : "in your browser";

    public string WhenRemindersWork => IsNative
        ? "Reminders work while the app is running."
        : "Reminders work while the app is open, in a tab or installed on your device.";

    public string HowToUnblockNotifications => IsNative
        ? "Allow them in Android Settings → Apps → Activity Tracker → Notifications"
        : "Allow them in your browser's site settings";

    public string NotificationsUnsupported => IsNative ? "This device does not support notifications" : "This browser does not support notifications";
}

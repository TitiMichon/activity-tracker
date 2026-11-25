namespace ActivityTracker.UI.Platform;

/// <summary>System notifications for reminders.</summary>
public interface INotificationPlatform
{
    /// <summary>"granted", "denied", "default" (not asked yet) or "unsupported".</summary>
    Task<string> PermissionAsync();

    /// <summary>Asks the user when not asked yet; returns the resulting permission.</summary>
    Task<string> RequestPermissionAsync();

    Task ShowAsync(string title, string message);
}

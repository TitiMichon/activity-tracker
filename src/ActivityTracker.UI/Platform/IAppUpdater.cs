using ActivityTracker.Core.Services;

namespace ActivityTracker.UI.Platform;

/// <summary>Installs a newer version of the app. Only native hosts can; the web app updates through its service worker.</summary>
public interface IAppUpdater
{
    bool IsSupported { get; }

    /// <summary>Downloads the release and hands it to the system installer (which asks the user to confirm).</summary>
    Task InstallAsync(AppRelease release, IProgress<double>? progress = null, CancellationToken cancellationToken = default);
}

/// <summary>The system must first allow the app to install updates; its settings screen was opened for the user.</summary>
public sealed class InstallPermissionNeededException()
    : Exception("Allow Activity Tracker to install apps on the screen that just opened, then tap Update again.");

public sealed class NoAppUpdater : IAppUpdater
{
    public bool IsSupported => false;

    public Task InstallAsync(AppRelease release, IProgress<double>? progress = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("The web app updates itself through the browser.");
}

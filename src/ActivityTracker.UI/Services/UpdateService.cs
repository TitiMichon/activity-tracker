using System.Globalization;
using ActivityTracker.Core.Services;
using ActivityTracker.UI.Platform;

namespace ActivityTracker.UI.Services;

/// <summary>
/// Looks for a newer version of the app (at start, at most every <see cref="CheckInterval"/>, or on demand)
/// and installs it through the platform. Does nothing where the platform cannot install updates (web app).
/// </summary>
public sealed class UpdateService(IReleaseSource releases, IAppUpdater updater, IKeyValueStorage storage, TimeProvider time)
{
    public const string ChannelKey = "activity-tracker:update-channel";
    public const string LastCheckKey = "activity-tracker:update-last-check";
    public const string DismissedKey = "activity-tracker:update-dismissed";

    public static readonly TimeSpan CheckInterval = TimeSpan.FromHours(6);

    private bool _started;
    private string? _dismissed;

    public event Action? Changed;

    /// <summary>Version of the running app (settable for tests).</summary>
    public string CurrentVersion { get; set; } = AppInfo.Version;

    public bool IsSupported => updater.IsSupported;

    public UpdateChannel Channel { get; private set; } = UpdateChannel.Stable;

    /// <summary>A newer version on the chosen channel, found by the last check.</summary>
    public AppRelease? Available { get; private set; }

    /// <summary>True when <see cref="Available"/> was put off with "Later" (until a newer one comes).</summary>
    public bool IsDismissed => Available is not null && Available.Tag == _dismissed;

    public DateTimeOffset? LastCheck { get; private set; }

    public string? Error { get; private set; }

    /// <summary>A step the user has to take (not an error), e.g. allowing installs the first time.</summary>
    public string? Hint { get; private set; }

    public bool IsChecking { get; private set; }

    public bool IsInstalling { get; private set; }

    /// <summary>Download progress from 0 to 1 while installing.</summary>
    public double Progress { get; private set; }

    private SemVersion Current => SemVersion.TryParse(CurrentVersion, out var v) ? v : SemVersion.Parse("0.0.0");

    /// <summary>Loads the settings and checks for updates if the last check is old enough. Safe to call more than once.</summary>
    public async Task StartAsync()
    {
        if (_started || !IsSupported)
        {
            return;
        }

        _started = true;
        Channel = Enum.TryParse<UpdateChannel>(await storage.GetAsync(ChannelKey), out var channel) ? channel : AppUpdates.DefaultChannel(Current);
        _dismissed = await storage.GetAsync(DismissedKey);
        LastCheck = DateTimeOffset.TryParse(await storage.GetAsync(LastCheckKey), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var last) ? last : null;
        Changed?.Invoke();

        if (LastCheck is null || time.GetUtcNow() - LastCheck >= CheckInterval)
        {
            await CheckAsync();
        }
    }

    /// <summary>Asks GitHub for the published versions. Errors are kept in <see cref="Error"/>, never thrown.</summary>
    public async Task CheckAsync()
    {
        if (!IsSupported || IsChecking)
        {
            return;
        }

        IsChecking = true;
        Error = null;
        Changed?.Invoke();
        try
        {
            var all = await releases.GetReleasesAsync();
            Available = AppUpdates.Newest(all, Current, Channel);
            LastCheck = time.GetUtcNow();
            await storage.SetAsync(LastCheckKey, LastCheck.Value.ToString("O", CultureInfo.InvariantCulture));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            Error = ex is HttpRequestException { StatusCode: null } or TaskCanceledException
                ? "Could not reach GitHub. Check your connection."
                : ex.Message;
        }
        finally
        {
            IsChecking = false;
            Changed?.Invoke();
        }
    }

    /// <summary>Saves the channel and checks again with it.</summary>
    public async Task SetChannelAsync(UpdateChannel channel)
    {
        Channel = channel;
        await storage.SetAsync(ChannelKey, channel.ToString());
        await CheckAsync();
    }

    /// <summary>"Later": hides the banner until a newer version is published.</summary>
    public async Task DismissAsync()
    {
        if (Available is null)
        {
            return;
        }

        _dismissed = Available.Tag;
        await storage.SetAsync(DismissedKey, _dismissed);
        Changed?.Invoke();
    }

    /// <summary>Downloads <see cref="Available"/> and opens the system installer. Errors are kept in <see cref="Error"/>.</summary>
    public async Task InstallAsync()
    {
        if (Available is not { } release || IsInstalling)
        {
            return;
        }

        IsInstalling = true;
        Progress = 0;
        Error = null;
        Hint = null;
        Changed?.Invoke();
        try
        {
            await updater.InstallAsync(release, new Progress<double>(p =>
            {
                Progress = p;
                Changed?.Invoke();
            }));
        }
        catch (InstallPermissionNeededException ex)
        {
            Hint = ex.Message;
        }
        catch (Exception ex)
        {
            // Network, storage or the platform installer: shown to the user, never allowed to break the page.
            Error = $"The update could not be installed: {ex.Message}";
        }
        finally
        {
            IsInstalling = false;
            Changed?.Invoke();
        }
    }
}

namespace ActivityTracker.Core.Services;

/// <summary>A published version of the app with its Android package.</summary>
public sealed record AppRelease(SemVersion Version, string Tag, bool IsPrerelease, string? ApkUrl, string? Notes = null, DateTimeOffset? PublishedAt = null);

/// <summary>Which published versions to offer.</summary>
public enum UpdateChannel
{
    /// <summary>Only releases (1.2.0).</summary>
    Stable,

    /// <summary>Releases and the dev builds published for every change (1.2.1-dev.42).</summary>
    EveryChange,
}

public static class AppUpdates
{
    /// <summary>The newest release on <paramref name="channel"/> that is newer than <paramref name="current"/> and has an APK, if any.</summary>
    public static AppRelease? Newest(IEnumerable<AppRelease> releases, SemVersion current, UpdateChannel channel) =>
        releases
            .Where(r => r.ApkUrl is not null && r.Version > current)
            .Where(r => channel == UpdateChannel.EveryChange || !(r.IsPrerelease || r.Version.IsPrerelease))
            .OrderByDescending(r => r.Version)
            .FirstOrDefault();

    /// <summary>Dev builds follow every change by default; releases stay on releases.</summary>
    public static UpdateChannel DefaultChannel(SemVersion current) =>
        current.IsPrerelease ? UpdateChannel.EveryChange : UpdateChannel.Stable;
}

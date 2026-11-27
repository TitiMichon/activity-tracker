using ActivityTracker.Core.Services;
using ActivityTracker.UI.Platform;
using ActivityTracker.UI.Services;

namespace ActivityTracker.Web.Tests;

public sealed class FakeAppUpdater : IAppUpdater
{
    public bool IsSupported { get; set; }

    public List<AppRelease> Installed { get; } = [];

    public Exception? Failure { get; set; }

    public Task InstallAsync(AppRelease release, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        if (Failure is not null)
        {
            throw Failure;
        }

        Installed.Add(release);
        return Task.CompletedTask;
    }
}

public sealed class FakeReleaseSource : IReleaseSource
{
    public List<AppRelease> Releases { get; } = [];

    public Exception? Failure { get; set; }

    public int Calls { get; private set; }

    public Task<IReadOnlyList<AppRelease>> GetReleasesAsync(CancellationToken cancellationToken = default)
    {
        Calls++;
        return Failure is not null ? Task.FromException<IReadOnlyList<AppRelease>>(Failure) : Task.FromResult<IReadOnlyList<AppRelease>>(Releases);
    }

    public AppRelease Add(string tag, bool prerelease = false, bool withApk = true)
    {
        var release = new AppRelease(SemVersion.Parse(tag), tag, prerelease, withApk ? $"https://example.test/{tag}.apk" : null);
        Releases.Add(release);
        return release;
    }
}

public sealed class MemoryKeyValueStorage : IKeyValueStorage
{
    private readonly Dictionary<string, string> _values = [];

    public Task<string?> GetAsync(string key) => Task.FromResult(_values.GetValueOrDefault(key));

    public Task SetAsync(string key, string value)
    {
        _values[key] = value;
        return Task.CompletedTask;
    }
}

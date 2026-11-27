using System.Net.Http.Headers;
using ActivityTracker.Core.Services;
using ActivityTracker.UI.Platform;

namespace ActivityTracker.Mobile.Platform;

/// <summary>
/// Downloads the APK of a release into the cache folder and opens it with the system installer, which asks the user
/// to confirm. Android only installs it over the current app when it is signed with the same release key.
/// </summary>
public sealed class AndroidAppUpdater : IAppUpdater
{
    private const string ApkMimeType = "application/vnd.android.package-archive";

    private static readonly HttpClient Http = CreateClient();

    public bool IsSupported => true;

    public async Task InstallAsync(AppRelease release, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        if (release.ApkUrl is not { } url)
        {
            throw new InvalidOperationException($"v{release.Version} has no Android package.");
        }

        if (!CanInstallPackages())
        {
            await OpenInstallPermissionSettingsAsync();
            throw new InstallPermissionNeededException();
        }

        var folder = Path.Combine(FileSystem.CacheDirectory, "updates");
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, recursive: true); // older downloads
        }

        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"activity-tracker-v{release.Version}.apk");

        using (var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
        {
            response.EnsureSuccessStatusCode();
            var total = response.Content.Headers.ContentLength;
            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var target = File.Create(path);
            var buffer = new byte[81920];
            long done = 0;
            int read;
            while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                done += read;
                if (total > 0)
                {
                    progress?.Report((double)done / total.Value);
                }
            }
        }

        await MainThread.InvokeOnMainThreadAsync(() => Launcher.Default.OpenAsync(new OpenFileRequest
        {
            Title = $"Install Activity Tracker v{release.Version}",
            File = new ReadOnlyFile(path, ApkMimeType),
        }));
    }

    private static bool CanInstallPackages() =>
        !OperatingSystem.IsAndroidVersionAtLeast(26) || Android.App.Application.Context.PackageManager?.CanRequestPackageInstalls() != false;

    private static Task OpenInstallPermissionSettingsAsync() => MainThread.InvokeOnMainThreadAsync(() =>
    {
        var context = Android.App.Application.Context;
        var intent = new Android.Content.Intent(
            Android.Provider.Settings.ActionManageUnknownAppSources,
            Android.Net.Uri.Parse($"package:{context.PackageName}"));
        intent.AddFlags(Android.Content.ActivityFlags.NewTask);
        context.StartActivity(intent);
    });

    private static HttpClient CreateClient()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ActivityTracker", ActivityTracker.UI.Services.AppInfo.Version));
        return http;
    }
}

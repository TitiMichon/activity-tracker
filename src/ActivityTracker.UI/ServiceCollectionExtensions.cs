using ActivityTracker.Core.Persistence;
using ActivityTracker.Core.Services;
using ActivityTracker.UI.Platform;
using ActivityTracker.UI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ActivityTracker.UI;

public static class ServiceCollectionExtensions
{
    /// <summary>App services shared by every host. The host also registers the platform services (see <see cref="AddBrowserPlatform"/>).</summary>
    public static IServiceCollection AddActivityTracker(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ITrackerStore, KeyValueTrackerStore>();
        services.AddSingleton<TrackerService>();
        services.AddSingleton<ToastService>();
        services.AddSingleton<ReminderService>();
        services.AddSingleton<IReleaseSource>(_ => new GitHubReleases(GitHubReleases.CreateClient()));
        services.AddSingleton<UpdateService>();
        return services;
    }

    /// <summary>Platform services backed by browser APIs (web app).</summary>
    public static IServiceCollection AddBrowserPlatform(this IServiceCollection services)
    {
        services.AddSingleton(PlatformInfo.Browser);
        services.AddSingleton<IKeyValueStorage, BrowserKeyValueStorage>();
        services.AddSingleton<IFileExporter, BrowserFileExporter>();
        services.AddSingleton<INotificationPlatform, BrowserNotificationPlatform>();
        services.AddSingleton<IReminderScheduler, NoReminderScheduler>();
        services.AddSingleton<IAppUpdater, NoAppUpdater>(); // the service worker updates the web app
        return services;
    }
}

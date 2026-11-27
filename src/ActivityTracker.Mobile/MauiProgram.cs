using ActivityTracker.Mobile.Platform;
using ActivityTracker.UI;
using ActivityTracker.UI.Platform;

namespace ActivityTracker.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddActivityTracker();

        // Data lives in the app's private folder (not in the WebView): kept with the app and readable from native code.
        builder.Services.AddSingleton(PlatformInfo.Android);
        builder.Services.AddSingleton<IKeyValueStorage>(new FileKeyValueStorage(FileSystem.AppDataDirectory));
        builder.Services.AddSingleton<IFileExporter, ShareFileExporter>();
        builder.Services.AddSingleton<INotificationPlatform, NativeNotificationPlatform>();
        builder.Services.AddSingleton<IReminderScheduler, AndroidReminderScheduler>();
        builder.Services.AddSingleton<IAppUpdater, AndroidAppUpdater>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
#endif

        return builder.Build();
    }
}

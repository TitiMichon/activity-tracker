using Android.App;
using Android.Content;
using AndroidX.Core.App;

namespace ActivityTracker.Mobile;

/// <summary>Posts reminder notifications on the "Reminders" channel; tapping one opens the app.</summary>
public static class Notifications
{
    public const string ChannelId = "reminders";

    public static void EnsureChannel(Context context)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(26) && context.GetSystemService(Context.NotificationService) is NotificationManager manager)
        {
            manager.CreateNotificationChannel(new NotificationChannel(ChannelId, "Reminders", NotificationImportance.Default)
            {
                Description = "Planned activities that are not done yet",
            });
        }
    }

    public static void Show(string title, string message, int? id = null, Context? context = null)
    {
        context ??= global::Android.App.Application.Context;
        EnsureChannel(context);

        var open = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName!);
        var pending = open is null ? null : PendingIntent.GetActivity(context, 0, open, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
        var notification = new NotificationCompat.Builder(context, ChannelId)
            .SetSmallIcon(Resource.Mipmap.appicon_foreground)!
            .SetContentTitle(title)!
            .SetContentText(message)!
            .SetAutoCancel(true)!
            .SetContentIntent(pending)!
            .Build()!;
        NotificationManagerCompat.From(context)!.Notify(id ?? message.GetHashCode(), notification);
    }
}

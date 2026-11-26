using ActivityTracker.UI.Platform;
using Android.App;
using Android.Content;

namespace ActivityTracker.Mobile;

/// <summary>
/// Reminders while the app is closed: one Android alarm at the next reminder time. When it fires,
/// <see cref="ReminderAlarmReceiver"/> runs <see cref="BackgroundReminders"/> on the saved data, posts the
/// notifications and sets the alarm for the following reminder.
/// </summary>
public static class ReminderAlarms
{
    private const int RequestCode = 4201;

    /// <summary>Sets (or replaces) the single reminder alarm; null cancels it.</summary>
    public static void Schedule(Context context, DateTime? at)
    {
        if (context.GetSystemService(Context.AlarmService) is not AlarmManager manager)
        {
            return;
        }

        var intent = new Intent(context, typeof(ReminderAlarmReceiver));
        var pending = PendingIntent.GetBroadcast(context, RequestCode, intent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent)!;
        if (at is not { } time)
        {
            manager.Cancel(pending);
            return;
        }

        // Inexact but allowed while the phone is idle (Doze): no special "exact alarm" permission needed,
        // at the cost of a possible delay of a few minutes. A time in the past fires right away.
        var millis = new DateTimeOffset(DateTime.SpecifyKind(time, DateTimeKind.Local)).ToUnixTimeMilliseconds();
        manager.SetAndAllowWhileIdle(AlarmType.RtcWakeup, millis, pending);
    }

    /// <summary>Posts the reminders due now and plans the next alarm.</summary>
    public static async Task CheckAsync(Context context)
    {
        var storage = new FileKeyValueStorage(context.FilesDir!.AbsolutePath);
        var result = await new BackgroundReminders(storage, TimeProvider.System).CheckAsync();
        foreach (var reminder in result.Notifications)
        {
            Notifications.Show(reminder.Title, reminder.Message, reminder.ActivityId.GetHashCode(), context);
        }

        Schedule(context, result.Next);
    }

    /// <summary>Runs the check off the main thread while keeping the receiver alive until it ends.</summary>
    internal static void RunFromReceiver(BroadcastReceiver receiver, Context? context)
    {
        if (context is null)
        {
            return;
        }

        var async = receiver.GoAsync();
        Task.Run(async () =>
        {
            try
            {
                await CheckAsync(context);
            }
            catch (Exception ex)
            {
                Android.Util.Log.Warn("ActivityTracker", $"Reminder check failed: {ex}");
            }
            finally
            {
                async?.Finish();
            }
        });
    }
}

/// <summary>Fired by the reminder alarm.</summary>
[BroadcastReceiver(Enabled = true, Exported = false)]
public class ReminderAlarmReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent) => ReminderAlarms.RunFromReceiver(this, context);
}

/// <summary>Alarms are cleared on reboot and app update, and a clock / time zone change moves reminder times: set them again.</summary>
[BroadcastReceiver(Enabled = true, Exported = true)]
[IntentFilter([Intent.ActionBootCompleted, Intent.ActionMyPackageReplaced, Intent.ActionTimeChanged, Intent.ActionTimezoneChanged])]
public class ReminderRescheduleReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent) => ReminderAlarms.RunFromReceiver(this, context);
}

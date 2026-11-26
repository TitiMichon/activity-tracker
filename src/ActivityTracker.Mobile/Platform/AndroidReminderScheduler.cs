using ActivityTracker.UI.Platform;

namespace ActivityTracker.Mobile.Platform;

/// <summary>Android alarms: reminders are notified by the OS even when the app is closed.</summary>
public sealed class AndroidReminderScheduler : IReminderScheduler
{
    public bool NotifiesInBackground => true;

    public Task ScheduleAsync(DateTime? at)
    {
        ReminderAlarms.Schedule(global::Android.App.Application.Context, at);
        return Task.CompletedTask;
    }
}

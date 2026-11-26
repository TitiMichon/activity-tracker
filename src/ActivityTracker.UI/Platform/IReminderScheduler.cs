namespace ActivityTracker.UI.Platform;

/// <summary>
/// Lets the operating system wake the app for reminders, so they fire even when the app is closed.
/// Native apps plug an implementation (Android alarms); the browser has none (<see cref="NoReminderScheduler"/>).
/// </summary>
public interface IReminderScheduler
{
    /// <summary>
    /// True when the OS posts the reminder notifications itself (see <see cref="BackgroundReminders"/>):
    /// the in-app check then only shows its message, so a reminder is not notified twice.
    /// </summary>
    bool NotifiesInBackground { get; }

    /// <summary>Plans the next background check (a time already passed means "as soon as possible"); null cancels it.</summary>
    Task ScheduleAsync(DateTime? at);
}

/// <summary>No background reminders: notifications are only sent while the app is open.</summary>
public sealed class NoReminderScheduler : IReminderScheduler
{
    public bool NotifiesInBackground => false;

    public Task ScheduleAsync(DateTime? at) => Task.CompletedTask;
}

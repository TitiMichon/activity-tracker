using ActivityTracker.Core.Models;
using ActivityTracker.Core.Persistence;
using ActivityTracker.Core.Services;

namespace ActivityTracker.UI.Platform;

/// <summary>A reminder to post as a system notification.</summary>
public sealed record ReminderNotification(Guid ActivityId, string Title, string Message);

/// <summary>What a background check found: notifications to post now, and when to check again.</summary>
public sealed record BackgroundReminderResult(IReadOnlyList<ReminderNotification> Notifications, DateTime? Next);

/// <summary>
/// Reminder check run by the OS while the app may be closed (native apps): reads the saved data, returns the
/// reminders due and not notified yet today (each one at most once a day), and the time of the next check.
/// </summary>
public sealed class BackgroundReminders(IKeyValueStorage storage, TimeProvider time)
{
    public const string NotifiedKey = "activity-tracker:notified";

    public async Task<BackgroundReminderResult> CheckAsync()
    {
        var now = time.GetLocalNow().DateTime;
        var today = DateOnly.FromDateTime(now);

        TrackerData data;
        try
        {
            data = TrackerJson.Deserialize(await storage.GetAsync(KeyValueTrackerStore.Key));
        }
        catch (FormatException)
        {
            // Unreadable data: the app shows the problem when opened; nothing to remind meanwhile.
            return new([], null);
        }

        var notified = ReminderLog.Parse(await storage.GetAsync(NotifiedKey), today);
        var due = ReminderCalculator.Pending(data.Activities, data.Entries, now)
            .Where(a => notified.Add(a.Id))
            .Select(a => new ReminderNotification(a.Id, "Reminder", ReminderCalculator.Message(a, data.Entries, today)))
            .ToList();

        if (due.Count > 0)
        {
            await storage.SetAsync(NotifiedKey, ReminderLog.Format(today, notified));
        }

        return new(due, ReminderCalculator.Next(data.Activities, data.Entries, now));
    }
}

using ActivityTracker.Core.Models;
using ActivityTracker.Core.Services;
using ActivityTracker.UI.Platform;
using ActivityTracker.UI.Services;
using Microsoft.Extensions.Time.Testing;

namespace ActivityTracker.Web.Tests;

/// <summary>Reminders run by the OS while the app is closed (Android alarms), and how the app plans them.</summary>
public class BackgroundReminderTests
{
    private sealed class MemoryStorage : IKeyValueStorage
    {
        public Dictionary<string, string> Values { get; } = [];

        public Task<string?> GetAsync(string key) => Task.FromResult(Values.GetValueOrDefault(key));

        public Task SetAsync(string key, string value)
        {
            Values[key] = value;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingScheduler(bool notifiesInBackground = true) : IReminderScheduler
    {
        public List<DateTime?> Scheduled { get; } = [];

        public bool NotifiesInBackground => notifiesInBackground;

        public Task ScheduleAsync(DateTime? at)
        {
            Scheduled.Add(at);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingNotifications : INotificationPlatform
    {
        public List<string> Shown { get; } = [];

        public Task<string> PermissionAsync() => Task.FromResult("granted");

        public Task<string> RequestPermissionAsync() => Task.FromResult("granted");

        public Task ShowAsync(string title, string message)
        {
            Shown.Add(message);
            return Task.CompletedTask;
        }
    }

    private static readonly DateOnly Monday = new(2026, 11, 16);

    private readonly MemoryStorage _storage = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 16, 9, 30, 0, TimeSpan.Zero));
    private readonly TrackerService _tracker;

    public BackgroundReminderTests()
    {
        _time.SetLocalTimeZone(TimeZoneInfo.Utc);
        _tracker = new TrackerService(new KeyValueTrackerStore(_storage), _time);
        _tracker.EnsureLoadedAsync().GetAwaiter().GetResult();
    }

    private Activity Remind(string name, int hour)
    {
        var activity = new Activity
        {
            Name = name,
            Icon = "🧘",
            StartDate = Monday.AddDays(-10),
            Schedule = new Schedule { Frequency = Frequency.Daily },
            ReminderTime = new TimeOnly(hour, 0),
        };
        _tracker.SaveActivityAsync(activity).GetAwaiter().GetResult();
        return activity;
    }

    private BackgroundReminders Background => new(_storage, _time);

    [Fact]
    public async Task Notifies_due_reminders_from_the_saved_data_and_plans_the_next_one()
    {
        var stretch = Remind("Stretch", 9);
        Remind("Guitar", 18);

        var result = await Background.CheckAsync();

        var notification = Assert.Single(result.Notifications);
        Assert.Equal((stretch.Id, "Reminder", "🧘 Stretch is planned today and not done yet"), (notification.ActivityId, notification.Title, notification.Message));
        Assert.Equal(Monday.ToDateTime(new TimeOnly(18, 0)), result.Next);
    }

    [Fact]
    public async Task Each_reminder_is_notified_once_a_day()
    {
        Remind("Stretch", 9);

        Assert.Single((await Background.CheckAsync()).Notifications);
        Assert.Empty((await Background.CheckAsync()).Notifications);

        _time.Advance(TimeSpan.FromDays(1));
        Assert.Single((await Background.CheckAsync()).Notifications);
    }

    [Fact]
    public async Task Done_activities_are_not_notified()
    {
        var stretch = Remind("Stretch", 9);
        await _tracker.LogAsync(stretch.Id);

        var result = await Background.CheckAsync();

        Assert.Empty(result.Notifications);
        Assert.Equal(Monday.AddDays(1).ToDateTime(new TimeOnly(9, 0)), result.Next);
    }

    [Fact]
    public async Task Unreadable_data_notifies_nothing()
    {
        _storage.Values[KeyValueTrackerStore.Key] = "{ not json";

        var result = await Background.CheckAsync();

        Assert.Empty(result.Notifications);
        Assert.Null(result.Next);
    }

    [Fact]
    public async Task The_app_plans_the_next_reminder_when_data_changes()
    {
        var scheduler = new RecordingScheduler();
        using var reminders = new ReminderService(_tracker, new ToastService(), new RecordingNotifications(), _storage, scheduler, _time);

        Remind("Guitar", 18);
        await reminders.ScheduleAsync();
        Assert.Equal(Monday.ToDateTime(new TimeOnly(18, 0)), scheduler.Scheduled[^1]);

        // A reminder already due: ask for a check right away.
        Remind("Stretch", 9);
        await reminders.ScheduleAsync();
        Assert.Equal(_tracker.Now, scheduler.Scheduled[^1]);
    }

    [Fact]
    public async Task With_background_notifications_the_app_only_shows_its_message()
    {
        Remind("Stretch", 9);
        var notifications = new RecordingNotifications();
        var toasts = new ToastService();
        using var reminders = new ReminderService(_tracker, toasts, notifications, _storage, new RecordingScheduler(), _time);

        Assert.Single(await reminders.CheckAsync());

        Assert.Empty(notifications.Shown);
        Assert.Contains("Stretch", toasts.Current!.Message);
    }

    [Fact]
    public async Task Without_background_notifications_the_app_notifies_itself()
    {
        Remind("Stretch", 9);
        var notifications = new RecordingNotifications();
        using var reminders = new ReminderService(_tracker, new ToastService(), notifications, _storage, new RecordingScheduler(notifiesInBackground: false), _time);

        await reminders.CheckAsync();

        Assert.Single(notifications.Shown);
    }
}

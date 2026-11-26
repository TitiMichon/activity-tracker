using ActivityTracker.Core.Models;
using ActivityTracker.Core.Services;
using ActivityTracker.UI.Platform;
using ActivityTracker.UI.Services;
using Microsoft.Extensions.Time.Testing;

namespace ActivityTracker.Web.Tests;

/// <summary>The shared UI only talks to the platform through interfaces, so native hosts can plug their own.</summary>
public class PlatformTests
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

    private sealed class RecordingNotifications : INotificationPlatform
    {
        public List<string> Shown { get; } = [];

        public Task<string> PermissionAsync() => Task.FromResult("granted");

        public Task<string> RequestPermissionAsync() => Task.FromResult("granted");

        public Task ShowAsync(string title, string message)
        {
            Shown.Add($"{title}: {message}");
            return Task.CompletedTask;
        }
    }

    private sealed class FailingNotifications : INotificationPlatform
    {
        public Task<string> PermissionAsync() => throw new InvalidOperationException("no platform");

        public Task<string> RequestPermissionAsync() => throw new InvalidOperationException("no platform");

        public Task ShowAsync(string title, string message) => throw new InvalidOperationException("no platform");
    }

    private static readonly FakeTimeProvider Time = CreateTime();

    private static FakeTimeProvider CreateTime()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 11, 16, 9, 30, 0, TimeSpan.Zero));
        time.SetLocalTimeZone(TimeZoneInfo.Utc);
        return time;
    }

    private static async Task<TrackerService> TrackerWithReminderAsync(IKeyValueStorage storage)
    {
        var tracker = new TrackerService(new KeyValueTrackerStore(storage), Time);
        await tracker.EnsureLoadedAsync();
        await tracker.SaveActivityAsync(new Activity
        {
            Name = "Stretch",
            Icon = "🧘",
            StartDate = new DateOnly(2026, 11, 1),
            Schedule = new Schedule { Frequency = Frequency.Daily },
            ReminderTime = new TimeOnly(9, 0),
        });
        return tracker;
    }

    [Fact]
    public async Task Tracker_data_is_saved_in_the_key_value_storage()
    {
        var storage = new MemoryStorage();
        await TrackerWithReminderAsync(storage);

        Assert.Contains("Stretch", storage.Values[KeyValueTrackerStore.Key]);
    }

    [Fact]
    public async Task Reminders_use_the_platform_notifications_and_storage()
    {
        var storage = new MemoryStorage();
        var notifications = new RecordingNotifications();
        var tracker = await TrackerWithReminderAsync(storage);
        var reminders = new ReminderService(tracker, new ToastService(), notifications, storage, new NoReminderScheduler(), Time);

        await reminders.CheckAsync();

        Assert.Equal(["Reminder: 🧘 Stretch is planned today and not done yet"], notifications.Shown);
        Assert.StartsWith("2026-11-16|", storage.Values[ReminderService.StorageKey]);
        Assert.Equal("granted", await reminders.PermissionAsync());
    }

    [Fact]
    public async Task Failing_notifications_do_not_break_reminders()
    {
        var storage = new MemoryStorage();
        var toasts = new ToastService();
        var reminders = new ReminderService(await TrackerWithReminderAsync(storage), toasts, new FailingNotifications(), storage, new NoReminderScheduler(), Time);

        Assert.Single(await reminders.CheckAsync());
        Assert.Contains("Stretch", toasts.Current!.Message);
        Assert.Equal("unsupported", await reminders.PermissionAsync());
    }
}

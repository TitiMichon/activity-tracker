using System.Globalization;
using ActivityTracker.Core.Models;
using ActivityTracker.Core.Persistence;
using ActivityTracker.Core.Services;
using ActivityTracker.UI.Platform;
using ActivityTracker.UI.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace ActivityTracker.Web.Tests;

/// <summary>Renders components against an in-memory store and a frozen clock (Monday 16 Nov 2026, 09:30).</summary>
public abstract class WebTestBase : BunitContext
{
    protected static readonly DateOnly Today = new(2026, 11, 16);

    protected WebTestBase()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        Time = new FakeTimeProvider(new DateTimeOffset(2026, 11, 16, 9, 30, 0, TimeSpan.Zero));
        Time.SetLocalTimeZone(TimeZoneInfo.Utc);
        Tracker = new TrackerService(Store, Time);
        Tracker.EnsureLoadedAsync().GetAwaiter().GetResult();

        Services.AddSingleton<ITrackerStore>(Store);
        Services.AddSingleton(Tracker);
        Services.AddSingleton<ToastService>();
        Services.AddSingleton<TimeProvider>(Time);
        Services.AddSingleton<ReminderService>();
        Services.AddSingleton<IKeyValueStorage, BrowserKeyValueStorage>();
        Services.AddSingleton<IFileExporter, BrowserFileExporter>();
        Services.AddSingleton<INotificationPlatform, BrowserNotificationPlatform>();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    protected InMemoryTrackerStore Store { get; } = new();

    protected FakeTimeProvider Time { get; }

    protected TrackerService Tracker { get; }

    protected Activity Add(Activity activity)
    {
        Tracker.SaveActivityAsync(activity).GetAwaiter().GetResult();
        return activity;
    }

    /// <summary>Runs tracker operations synchronously: the in-memory store completes them immediately.</summary>
    protected static void Sync(Task task) => task.GetAwaiter().GetResult();

    protected static T Sync<T>(Task<T> task) => task.GetAwaiter().GetResult();

    protected Activity AddTemplate(string key) => Add(ActivityTemplates.Create(key, Today.AddDays(-30)));

    protected static Activity Check(string name, Frequency frequency = Frequency.None) => new()
    {
        Name = name,
        Icon = "✅",
        Color = "#22c55e",
        StartDate = Today.AddDays(-30),
        Schedule = new Schedule { Frequency = frequency },
    };
}

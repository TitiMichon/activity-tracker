using ActivityTracker.Core.Models;
using ActivityTracker.Web.Components;
using ActivityTracker.Web.Pages;
using ActivityTracker.Web.Services;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace ActivityTracker.Web.Tests;

public class HideFromCalendarTests : WebTestBase
{
    private NavigationManager Nav => Services.GetRequiredService<NavigationManager>();

    private Activity Hidden()
    {
        var a = Check("Secret", Frequency.Daily);
        a.HideFromCalendar = true;
        Add(a);
        Sync(Tracker.LogAsync(a.Id, at: new DateTime(2026, 11, 5, 18, 0, 0)));
        return a;
    }

    [Fact]
    public void Hidden_activity_has_no_entries_nor_planned_markers_in_month_view()
    {
        Hidden();
        var cut = Render<CalendarPage>();

        Assert.Empty(cut.Find("[data-date='2026-11-05']").QuerySelectorAll(".badge"));
        Assert.Empty(cut.Find("[data-date='2026-11-16']").QuerySelectorAll(".badge"));
    }

    [Fact]
    public void Hidden_activity_is_left_out_of_week_and_day_views()
    {
        Hidden();
        Nav.NavigateTo("calendar?view=week&date=2026-11-05");
        var week = Render<CalendarPage>();
        Assert.DoesNotContain("Secret", week.Find("[data-testid=week-view]").TextContent);

        Nav.NavigateTo("calendar?view=day&date=2026-11-05");
        var day = Render<CalendarPage>();
        Assert.Contains("Nothing logged on this day.", day.Find("[data-testid=day-view]").TextContent);
    }

    [Fact]
    public void Filtering_on_a_hidden_activity_still_shows_it()
    {
        var a = Hidden();
        Nav.NavigateTo($"calendar?activity={a.Id}");
        var cut = Render<CalendarPage>();

        Assert.Single(cut.Find("[data-date='2026-11-05']").QuerySelectorAll(".badge"));
        Assert.Contains("Secret (hidden)", cut.Find("[aria-label='Filter by activity']").TextContent);
    }

    [Fact]
    public void Editor_checkbox_hides_the_activity()
    {
        var a = Add(Check("Sport"));
        var cut = Render<ActivityEditorPage>(p => p.Add(x => x.Id, a.Id));

        cut.Find("[data-testid=activity-show-calendar]").Change(false);
        cut.Find("[data-testid=activity-form]").Submit();

        Assert.True(Tracker.GetActivity(a.Id)!.HideFromCalendar);
    }
}

public class RepeatedCheckInTests : WebTestBase
{
    private Activity Water()
    {
        var water = Check("Water", Frequency.Daily);
        water.Schedule.TimesPerDay = 8;
        return Add(water);
    }

    [Fact]
    public void Tapping_several_times_counts_toward_the_daily_target()
    {
        Water();
        var cut = Render<QuickAdd>();

        cut.Find("[aria-label='Log Water']").Click();
        cut.Find("[aria-label='Log Water']").Click();
        cut.Find("[aria-label='Log Water']").Click();

        Assert.Equal(3, Tracker.Entries.Count);
        cut.WaitForAssertion(() => Assert.Contains("3 / 8 today", cut.Find("[data-testid=qa-tile]").TextContent));
        Assert.Contains("partial", cut.Find("[data-testid=qa-tile]").ClassName);
    }

    [Fact]
    public void Tile_is_done_once_the_target_is_reached()
    {
        var water = Water();
        for (var i = 0; i < 8; i++)
        {
            Sync(Tracker.LogAsync(water.Id));
        }

        var cut = Render<QuickAdd>();
        Assert.Contains("done", cut.Find("[data-testid=qa-tile]").ClassName);
    }

    [Fact]
    public void Home_planned_list_shows_progress()
    {
        var water = Water();
        Sync(Tracker.LogAsync(water.Id));
        Sync(Tracker.LogAsync(water.Id));

        var cut = Render<Home>();

        Assert.Contains("2 / 8", cut.Find(".planned-list").TextContent);
    }

    [Fact]
    public void Month_view_groups_repeated_entries_with_a_count()
    {
        var water = Water();
        for (var i = 0; i < 3; i++)
        {
            Sync(Tracker.LogAsync(water.Id));
        }

        var cut = Render<CalendarPage>();
        var today = cut.Find("[data-date='2026-11-16']");

        Assert.Single(today.QuerySelectorAll(".badge"));
        Assert.Equal("3", today.QuerySelector("[data-testid=badge-count]")!.TextContent);
    }

    [Fact]
    public void Editor_saves_times_per_day_and_reminder()
    {
        var cut = Render<ActivityEditorPage>();

        cut.Find("[data-testid=activity-name]").Input("Water");
        cut.Find("[data-testid=activity-frequency]").Change(nameof(Frequency.Daily));
        cut.Find("[data-testid=activity-times-per-day]").Change("8");
        cut.Find("[data-testid=activity-has-reminder]").Change(true);
        cut.Find("[data-testid=activity-reminder-time]").Change("18:30");
        cut.Find("[data-testid=activity-form]").Submit();

        var a = Assert.Single(Tracker.Activities);
        Assert.Equal(8, a.Schedule.TimesPerDay);
        Assert.Equal(new TimeOnly(18, 30), a.ReminderTime);
        JSInterop.VerifyInvoke("tracker.requestNotifications");
    }

    [Fact]
    public void Unplanned_activity_drops_times_per_day_and_reminder()
    {
        var a = Check("Anytime");
        a.Schedule.TimesPerDay = 3;
        a.ReminderTime = new TimeOnly(9, 0);
        Add(a);
        var cut = Render<ActivityEditorPage>(p => p.Add(x => x.Id, a.Id));

        Assert.Empty(cut.FindAll("[data-testid=activity-times-per-day]"));
        cut.Find("[data-testid=activity-form]").Submit();

        Assert.Equal(1, Tracker.GetActivity(a.Id)!.Schedule.TimesPerDay);
        Assert.Null(Tracker.GetActivity(a.Id)!.ReminderTime);
    }
}

public class ReminderServiceTests : WebTestBase
{
    private ReminderService Reminders => Services.GetRequiredService<ReminderService>();

    private Activity Stretch(int hour)
    {
        var a = Check("Stretch", Frequency.Daily);
        a.ReminderTime = new TimeOnly(hour, 0);
        return Add(a);
    }

    [Fact]
    public void Sends_a_notification_and_toast_once_per_day()
    {
        var a = Stretch(9); // clock is 09:30
        var toast = Render<ToastHost>();

        Assert.Equal([a.Id], Sync(Reminders.CheckAsync()));
        Assert.Empty(Sync(Reminders.CheckAsync()));

        var call = Assert.Single(JSInterop.Invocations, i => i.Identifier == "tracker.notify");
        Assert.Equal("✅ Stretch is planned today and not done yet", call.Arguments[1]);
        toast.WaitForAssertion(() => Assert.Contains("Stretch", toast.Markup));
        Assert.Contains(JSInterop.Invocations, i => i.Identifier == "tracker.storageSet" && (string)i.Arguments[0]! == ReminderService.StorageKey);
    }

    [Fact]
    public void Waits_for_the_reminder_time_and_skips_done_activities()
    {
        var later = Stretch(10);
        var done = Stretch(8);
        Sync(Tracker.LogAsync(done.Id));

        Assert.Empty(Sync(Reminders.CheckAsync()));

        Time.Advance(TimeSpan.FromMinutes(31)); // 10:01
        Assert.Equal([later.Id], Sync(Reminders.CheckAsync()));
    }

    [Fact]
    public void Does_not_repeat_reminders_already_sent_today_before_a_reload()
    {
        var a = Stretch(9);
        JSInterop.Setup<string?>("tracker.storageGet", ReminderService.StorageKey).SetResult($"2026-11-16|{a.Id}");

        Assert.Empty(Sync(Reminders.CheckAsync()));
    }

    [Fact]
    public void Settings_lists_reminders_and_asks_for_permission()
    {
        Stretch(9);
        JSInterop.Setup<string?>("tracker.notificationPermission").SetResult("default");
        JSInterop.Setup<string?>("tracker.requestNotifications").SetResult("granted");

        var cut = Render<SettingsPage>();
        Assert.Contains("Stretch", cut.Find("[data-testid=reminder-list]").TextContent);
        Assert.Contains("09:00", cut.Find("[data-testid=reminder-list]").TextContent);

        cut.Find("[data-testid=enable-notifications]").Click();
        cut.WaitForAssertion(() => Assert.Contains("allowed", cut.Find("[data-testid=notification-status]").TextContent));
    }
}

using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace ActivityTracker.E2E;

/// <summary>End-to-end scenarios in a real browser against the published app.</summary>
[Collection(E2ECollection.Name)]
public class UserJourneyTests(AppFixture app)
{
    /// <summary>
    /// Clicks "Log {name}" on Today: in the day list when the activity is planned today (depends on the real date),
    /// otherwise in the folded "More for today" section.
    /// </summary>
    private static async Task LogOnTodayAsync(IPage page, string name)
    {
        // Role queries skip hidden elements, such as the content of a closed <details>.
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = $"Log {name}", Exact = true, IncludeHidden = true }).First).ToBeAttachedAsync();
        var visible = page.GetByRole(AriaRole.Button, new() { Name = $"Log {name}", Exact = true });
        if (await visible.CountAsync() == 0)
        {
            await OpenMoreForTodayAsync(page);
        }

        await visible.First.ClickAsync();
    }

    private static async Task OpenMoreForTodayAsync(IPage page)
    {
        var more = page.GetByTestId("more-today");
        if (await more.GetAttributeAsync("open") is null)
        {
            await more.Locator("summary").ClickAsync();
        }
    }

    [E2EFact]
    public async Task App_starts_on_today_with_empty_state()
    {
        var page = await app.NewPageAsync();

        await Expect(page.Locator("h1")).ToHaveTextAsync("Today");
        await Expect(page.GetByText("No activities yet.")).ToBeVisibleAsync();
    }

    [E2EFact]
    public async Task Settings_shows_semantic_version()
    {
        var page = await app.NewPageAsync("settings");

        await Expect(page.GetByTestId("app-version")).ToHaveTextAsync(new Regex(@"^Activity Tracker v\d+\.\d+\.\d+"));
    }

    [E2EFact]
    public async Task Template_quick_add_persists_and_shows_on_calendar_and_heatmap()
    {
        var page = await app.NewPageAsync("activities");
        await page.GetByTestId("template-sport").ClickAsync();
        await Expect(page.GetByTestId("activity-name")).ToHaveValueAsync("Sport");

        await page.GetByRole(AriaRole.Link, new() { Name = "Today" }).ClickAsync();
        await LogOnTodayAsync(page, "Sport");
        await Expect(page.Locator(".toast")).ToContainTextAsync("Sport");
        await Expect(page.GetByTestId("entry-list")).ToContainTextAsync("Sport");

        // Data survives a reload (localStorage).
        await page.ReloadAsync();
        await Expect(page.GetByTestId("entry-list")).ToContainTextAsync("Sport");

        await page.GetByRole(AriaRole.Link, new() { Name = "Calendar" }).ClickAsync();
        await Expect(page.Locator(".month-day.today .badge:not(.faded)")).ToHaveCountAsync(1);

        await page.GetByRole(AriaRole.Link, new() { Name = "Heatmap" }).ClickAsync();
        await Expect(page.GetByTestId("stat-active-days")).ToHaveTextAsync("1");
        await Expect(page.Locator(".hm-cell.today")).ToHaveAttributeAsync("data-level", "4");
    }

    [E2EFact]
    public async Task Create_custom_quantity_activity_and_log_a_value()
    {
        var page = await app.NewPageAsync("activities/new");

        await page.GetByTestId("activity-name").FillAsync("Guitar");
        await page.GetByRole(AriaRole.Button, new() { Name = "# Quantity" }).ClickAsync();
        await page.GetByTestId("activity-unit").FillAsync("min");
        await page.GetByRole(AriaRole.Button, new() { Name = "Icon 🎸" }).ClickAsync();
        await page.GetByTestId("activity-has-goal").CheckAsync();
        await page.GetByTestId("goal-target").FillAsync("120");
        await page.GetByTestId("activity-save").ClickAsync();

        await Expect(page.GetByTestId("activity-list")).ToContainTextAsync("Guitar");

        await page.GetByRole(AriaRole.Link, new() { Name = "Today" }).ClickAsync();
        await LogOnTodayAsync(page, "Guitar");
        await page.GetByLabel("Value").FillAsync("25");
        await page.GetByTestId("qa-add").ClickAsync();

        await Expect(page.GetByTestId("entry-list")).ToContainTextAsync("25 min");
        await Expect(page.GetByTestId("goal")).ToContainTextAsync("25 / 120");
    }

    [E2EFact]
    public async Task Mood_entry_colors_the_heatmap_with_option_color()
    {
        var page = await app.NewPageAsync("activities");
        await page.GetByTestId("template-mood").ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Today" }).ClickAsync();

        await LogOnTodayAsync(page, "Mood");
        await page.GetByRole(AriaRole.Button, new() { Name = "Great" }).ClickAsync();

        await page.GetByRole(AriaRole.Link, new() { Name = "Heatmap" }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "🙂 Mood" }).ClickAsync();
        await Expect(page.Locator(".hm-cell.today")).ToHaveAttributeAsync("style", new Regex("#16a34a"));
    }

    [E2EFact]
    public async Task Repeated_hidden_activity_with_reminder()
    {
        var page = await app.NewPageAsync("activities/new");

        await page.GetByTestId("activity-name").FillAsync("Water");
        await page.GetByTestId("activity-frequency").SelectOptionAsync("Daily");
        await page.GetByTestId("activity-times-per-day").FillAsync("3");
        await page.GetByTestId("activity-show-calendar").UncheckAsync();
        await page.GetByTestId("activity-has-reminder").CheckAsync();
        await page.GetByTestId("activity-reminder-time").FillAsync("00:00");
        await page.GetByTestId("activity-save").ClickAsync();

        // The reminder time has passed and nothing is logged: the reminder shows right away.
        await Expect(page.Locator(".toast")).ToContainTextAsync("Water: 0 / 3 today");

        // Planned daily: it is a row of today's list, each tap counts.
        await page.GetByRole(AriaRole.Link, new() { Name = "Today" }).ClickAsync();
        var row = page.GetByTestId("day-activity").Filter(new() { HasText = "Water" });
        var log = row.GetByRole(AriaRole.Button, new() { Name = "Log Water" });
        await log.ClickAsync();
        await Expect(log).ToHaveTextAsync("1/3");
        await log.ClickAsync();
        await log.ClickAsync();
        await Expect(row).ToContainTextAsync("Done ✓");
        await Expect(page.GetByTestId("day-summary")).ToHaveTextAsync("1 / 1 done");

        await page.GetByRole(AriaRole.Link, new() { Name = "Calendar" }).ClickAsync();
        await Expect(page.Locator(".month-day.today .badge")).ToHaveCountAsync(0);
    }

    [E2EFact]
    public async Task Editing_an_entry_time_keeps_the_typed_time()
    {
        // Regression for #9: in the browser, time fields snapped back to their previous value.
        var page = await app.NewPageAsync("activities");
        await page.GetByTestId("template-sport").ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Today" }).ClickAsync();
        await LogOnTodayAsync(page, "Sport");

        await OpenMoreForTodayAsync(page);
        await page.GetByRole(AriaRole.Button, new() { Name = "Edit Sport entry" }).ClickAsync();
        await page.GetByTestId("entry-editor").GetByLabel("Time").FillAsync("00:05");
        await page.GetByTestId("entry-save").ClickAsync();

        await Expect(page.GetByTestId("entry-list")).ToContainTextAsync("00:05");
    }

    [E2EFact]
    public async Task Tasks_added_on_today_are_ticked_and_kept_after_a_reload()
    {
        var page = await app.NewPageAsync();
        var input = page.GetByTestId("day-add-input");

        await input.FillAsync("Groceries");
        await input.PressAsync("Enter");
        await input.FillAsync("Get a present");
        await input.PressAsync("Enter");

        await Expect(page.GetByTestId("day-task")).ToHaveCountAsync(2);
        await Expect(input).ToHaveValueAsync("");
        await page.GetByRole(AriaRole.Checkbox, new() { Name = "Done: Groceries" }).CheckAsync();
        await Expect(page.GetByTestId("day-summary")).ToHaveTextAsync("1 / 2 done");

        await page.ReloadAsync();
        await Expect(page.GetByTestId("day-task").First).ToContainTextAsync("Get a present");
        await Expect(page.GetByRole(AriaRole.Checkbox, new() { Name = "Done: Groceries" })).ToBeCheckedAsync();

        // Delete, then undo.
        await page.GetByRole(AriaRole.Button, new() { Name = "Get a present" }).ClickAsync();
        await page.GetByTestId("task-delete").ClickAsync();
        await Expect(page.GetByTestId("day-task")).ToHaveCountAsync(1);
        await page.Locator(".toast").GetByRole(AriaRole.Button, new() { Name = "Undo" }).ClickAsync();
        await Expect(page.GetByTestId("day-task")).ToHaveCountAsync(2);
    }

    [E2EFact]
    public async Task Backlog_task_is_planned_for_today_and_sent_back()
    {
        var page = await app.NewPageAsync();
        var input = page.GetByTestId("backlog-add-input");

        // Wide screen: the backlog is the right column, the switch is hidden.
        await Expect(page.GetByTestId("backlog")).ToBeVisibleAsync();
        await Expect(page.GetByTestId("show-backlog")).ToBeHiddenAsync();
        await input.FillAsync("Repair the car");
        await input.PressAsync("Enter");
        await Expect(page.GetByTestId("backlog-task")).ToHaveCountAsync(1);
        await Expect(page.GetByTestId("day-task")).ToHaveCountAsync(0);

        await page.GetByTestId("backlog-task").GetByRole(AriaRole.Button, new() { Name = "Repair the car" }).ClickAsync();
        await page.GetByTestId("task-plan-today").ClickAsync();
        await Expect(page.GetByTestId("backlog-task")).ToHaveCountAsync(0);
        await Expect(page.GetByTestId("day-task")).ToContainTextAsync("Repair the car");

        await page.GetByTestId("day-task").GetByRole(AriaRole.Button, new() { Name = "Repair the car" }).ClickAsync();
        await page.GetByTestId("task-to-backlog").ClickAsync();
        await Expect(page.GetByTestId("backlog-task")).ToHaveCountAsync(1);

        // Phone: one column at a time.
        await page.SetViewportSizeAsync(390, 844);
        await Expect(page.GetByTestId("backlog")).ToBeHiddenAsync();
        await page.GetByTestId("show-backlog").ClickAsync();
        await Expect(page.GetByTestId("backlog")).ToBeVisibleAsync();
        await Expect(page.GetByTestId("day-list")).ToBeHiddenAsync();
        await Expect(page.GetByTestId("show-backlog")).ToHaveTextAsync("Backlog (1)");
    }

    [E2EFact]
    public async Task Deep_link_to_week_view_works()
    {
        var page = await app.NewPageAsync("calendar?view=week&date=2026-01-07");

        await Expect(page.GetByTestId("week-view")).ToBeVisibleAsync();
        await Expect(page.GetByTestId("calendar-title")).ToHaveTextAsync("5 – 11 Jan 2026");
    }
}

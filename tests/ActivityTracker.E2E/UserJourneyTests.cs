using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace ActivityTracker.E2E;

/// <summary>End-to-end scenarios in a real browser against the published app.</summary>
[Collection(E2ECollection.Name)]
public class UserJourneyTests(AppFixture app)
{
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
        await page.GetByRole(AriaRole.Button, new() { Name = "Log Sport" }).ClickAsync();
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
        await page.GetByRole(AriaRole.Button, new() { Name = "Log Guitar" }).ClickAsync();
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

        await page.GetByRole(AriaRole.Button, new() { Name = "Log Mood" }).ClickAsync();
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

        await page.GetByRole(AriaRole.Link, new() { Name = "Today" }).ClickAsync();
        var tile = page.GetByRole(AriaRole.Button, new() { Name = "Log Water" });
        await tile.ClickAsync();
        await Expect(tile).ToContainTextAsync("1 / 3 today");
        await tile.ClickAsync();
        await tile.ClickAsync();
        await Expect(tile).ToContainTextAsync("3 / 3 today");
        await Expect(page.Locator(".planned-list")).ToContainTextAsync("Done ✓");

        await page.GetByRole(AriaRole.Link, new() { Name = "Calendar" }).ClickAsync();
        await Expect(page.Locator(".month-day.today .badge")).ToHaveCountAsync(0);
    }

    [E2EFact]
    public async Task Deep_link_to_week_view_works()
    {
        var page = await app.NewPageAsync("calendar?view=week&date=2026-01-07");

        await Expect(page.GetByTestId("week-view")).ToBeVisibleAsync();
        await Expect(page.GetByTestId("calendar-title")).ToHaveTextAsync("5 – 11 Jan 2026");
    }
}

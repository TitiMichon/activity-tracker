using ActivityTracker.Core.Models;
using ActivityTracker.UI.Components;
using ActivityTracker.UI.Pages;
using ActivityTracker.UI.Services;
using Bunit;
using Microsoft.AspNetCore.Components.Forms;

namespace ActivityTracker.Web.Tests;

public class EntryEditorTests : WebTestBase
{
    [Fact]
    public void Editing_value_time_and_custom_look()
    {
        var music = AddTemplate("music");
        var entry = Sync(Tracker.LogAsync(music.Id, 30));
        var closed = false;

        var cut = Render<EntryEditor>(p => p.Add(x => x.Entry, entry).Add(x => x.OnClose, () => closed = true));
        cut.Find("input[type=number]").Change("45");
        cut.Find("input[type=time]").Change("07:15");
        cut.Find(".custom-look input[type=checkbox]").Change(true);
        cut.Find("[aria-label='Color #ec4899']").Click();
        cut.Find("[aria-label='Icon 🔥']").Click();
        cut.Find("[data-testid=entry-save]").Click();

        var saved = Assert.Single(Tracker.Entries);
        Assert.Equal(45, saved.Value);
        Assert.Equal(new DateTime(2026, 11, 16, 7, 15, 0), saved.Timestamp);
        Assert.Equal("#ec4899", saved.Color);
        Assert.Equal("🔥", saved.Icon);
        Assert.True(closed);
    }

    [Fact]
    public void Negative_quantity_is_rejected()
    {
        var music = AddTemplate("music");
        var entry = Sync(Tracker.LogAsync(music.Id, 30));

        var cut = Render<EntryEditor>(p => p.Add(x => x.Entry, entry));
        cut.Find("input[type=number]").Change("-5");
        cut.Find("[data-testid=entry-save]").Click();

        Assert.Contains("0 or more", cut.Find("[role=alert]").TextContent);
        Assert.Equal(30, Tracker.Entries.Single().Value);
    }

    [Fact]
    public void Entry_list_opens_editor_on_click()
    {
        var sport = Add(Check("Sport"));
        Sync(Tracker.LogAsync(sport.Id, note: "legs"));

        var cut = Render<EntryList>(p => p.Add(x => x.Entries, Tracker.Entries.ToList()));
        Assert.Contains("legs", cut.Markup);

        cut.Find(".entry-row").Click();
        Assert.Contains("Edit entry", cut.Find("[data-testid=entry-editor]").TextContent);
    }
}

public class HomeAndSettingsTests : WebTestBase
{
    [Fact]
    public void Home_shows_planned_items_and_objectives()
    {
        Add(Check("Stretch", Frequency.Daily));
        var music = AddTemplate("music"); // daily, 150 min / week
        Sync(Tracker.LogAsync(music.Id, 50));

        var cut = Render<Home>();

        Assert.Contains("Monday 16 November 2026", cut.Markup);
        var scheduled = cut.Find("[data-testid=scheduled-today]").TextContent;
        Assert.Contains("Stretch", scheduled);
        Assert.Contains("1 / 2 done", scheduled); // music is logged, stretch is not
        var goal = cut.Find("[data-testid=goal]");
        Assert.Contains("50 / 150 min", goal.TextContent);
        Assert.Equal("33", goal.QuerySelector("[role=progressbar]")!.GetAttribute("aria-valuenow"));
    }

    [Fact]
    public void Settings_changes_first_day_of_week()
    {
        var cut = Render<SettingsPage>();
        cut.Find("[data-testid=first-day]").Change(nameof(DayOfWeek.Sunday));
        Assert.Equal(DayOfWeek.Sunday, Tracker.FirstDayOfWeek);
        Assert.Contains("Sunday", Store.Json);
    }

    [Fact]
    public void Settings_export_downloads_json()
    {
        Add(Check("Sport"));
        var cut = Render<SettingsPage>();

        cut.FindAll("button").First(b => b.TextContent.Contains("Export")).Click();

        var call = JSInterop.VerifyInvoke("tracker.download");
        Assert.Equal("activity-tracker-2026-11-16.json", call.Arguments[0]);
        Assert.Contains("Sport", (string)call.Arguments[1]!);
    }

    [Fact]
    public void Settings_import_replaces_data_and_reports_invalid_files()
    {
        var other = new Core.Services.TrackerService(new Core.Persistence.InMemoryTrackerStore(), Time);
        Sync(other.SaveActivityAsync(Check("Imported")));
        var cut = Render<SettingsPage>();

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("{oops", "bad.json"));
        cut.WaitForAssertion(() => Assert.Contains("not a valid", cut.Find("[role=alert]").TextContent));

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText(other.Export(), "backup.json"));
        cut.WaitForAssertion(() => Assert.Equal("Imported", Assert.Single(Tracker.Activities).Name));
    }

    [Fact]
    public void Settings_shows_app_version()
    {
        var cut = Render<SettingsPage>();
        Assert.Equal($"Activity Tracker v{AppInfo.Version}", cut.Find("[data-testid=app-version]").TextContent);
        Assert.Matches(@"^\d+\.\d+\.\d+", AppInfo.Version);
    }
}

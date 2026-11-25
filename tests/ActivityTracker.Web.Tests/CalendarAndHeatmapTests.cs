using ActivityTracker.Core.Models;
using ActivityTracker.UI.Components;
using ActivityTracker.UI.Pages;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace ActivityTracker.Web.Tests;

public class CalendarPageTests : WebTestBase
{
    private NavigationManager Nav => Services.GetRequiredService<NavigationManager>();

    [Fact]
    public void Month_view_shows_full_weeks_and_entries_on_their_day()
    {
        var sport = Add(Check("Sport"));
        Sync(Tracker.LogAsync(sport.Id, at: new DateTime(2026, 11, 5, 18, 0, 0)));

        var cut = Render<CalendarPage>();

        Assert.Equal("November 2026", cut.Find("[data-testid=calendar-title]").TextContent);
        Assert.Equal(42, cut.FindAll(".month-day").Count);
        Assert.Equal("Mon", cut.FindAll(".month-dow")[0].TextContent);
        Assert.Single(cut.Find("[data-date='2026-11-05']").QuerySelectorAll(".badge"));
        Assert.Empty(cut.Find("[data-date='2026-11-06']").QuerySelectorAll(".badge"));
        Assert.Contains("today", cut.Find("[data-date='2026-11-16']").ClassName);
    }

    [Fact]
    public void Planned_but_not_done_activities_are_shown_faded()
    {
        Add(Check("Daily", Frequency.Daily));
        var cut = Render<CalendarPage>();
        Assert.Single(cut.Find("[data-date='2026-11-16']").QuerySelectorAll(".badge.faded"));
    }

    [Fact]
    public void First_day_of_week_setting_is_respected()
    {
        Sync(Tracker.SetFirstDayOfWeekAsync(DayOfWeek.Sunday));
        var cut = Render<CalendarPage>();
        Assert.Equal("Sun", cut.FindAll(".month-dow")[0].TextContent);
    }

    [Fact]
    public void Next_button_moves_one_month()
    {
        var cut = Render<CalendarPage>();
        cut.Find("[aria-label=Next]").Click();
        Assert.Contains("view=month", Nav.Uri);
        Assert.Contains("date=2026-12-16", Nav.Uri);
    }

    [Fact]
    public void Clicking_a_day_opens_day_view()
    {
        var cut = Render<CalendarPage>();
        cut.Find("[data-date='2026-11-03']").Click();
        Assert.Contains("view=day", Nav.Uri);
        Assert.Contains("date=2026-11-03", Nav.Uri);
    }

    [Fact]
    public void Week_view_lists_entries_per_day()
    {
        var music = AddTemplate("music");
        Sync(Tracker.LogAsync(music.Id, 25, at: new DateTime(2026, 11, 18, 20, 0, 0)));

        Nav.NavigateTo("calendar?view=week&date=2026-11-18");
        var cut = Render<CalendarPage>();

        Assert.Equal("16 – 22 Nov 2026", cut.Find("[data-testid=calendar-title]").TextContent);
        var columns = cut.FindAll(".week-col");
        Assert.Equal(7, columns.Count);
        Assert.Contains("25 min", columns[2].TextContent);
    }

    [Fact]
    public void Day_view_filters_by_activity()
    {
        var a = Add(Check("Sport"));
        var b = Add(Check("Chess"));
        Sync(Tracker.LogAsync(a.Id));
        Sync(Tracker.LogAsync(b.Id));

        Nav.NavigateTo($"calendar?view=day&activity={a.Id}");
        var cut = Render<CalendarPage>();

        var list = cut.Find("[data-testid=entry-list]");
        Assert.Contains("Sport", list.TextContent);
        Assert.DoesNotContain("Chess", list.TextContent);
    }
}

public class HeatmapTests : WebTestBase
{
    [Fact]
    public void Panel_shows_levels_and_stats_for_an_activity()
    {
        var music = AddTemplate("music");
        Sync(Tracker.LogAsync(music.Id, 60, at: Today.ToDateTime(new TimeOnly(8, 0))));
        Sync(Tracker.LogAsync(music.Id, 15, at: Today.AddDays(-1).ToDateTime(new TimeOnly(8, 0))));

        var cut = Render<HeatmapPanel>(p => p
            .Add(x => x.Activity, music)
            .Add(x => x.From, Today.AddDays(-6))
            .Add(x => x.To, Today));

        Assert.Equal("2", cut.Find("[data-testid=stat-active-days]").TextContent);
        var cells = cut.FindAll(".hm-cell[data-level]");
        Assert.Equal(7, cells.Count);
        Assert.Equal("4", cells[^1].GetAttribute("data-level"));
        Assert.Equal("1", cells[^2].GetAttribute("data-level"));
        Assert.Contains("60 min", cells[^1].GetAttribute("title"));
    }

    [Fact]
    public void Mood_cells_use_option_color()
    {
        var mood = AddTemplate("mood");
        Sync(Tracker.LogAsync(mood.Id, optionId: "awful"));

        var cut = Render<HeatmapPanel>(p => p.Add(x => x.Activity, mood).Add(x => x.From, Today).Add(x => x.To, Today));

        Assert.Contains("--c:#dc2626", cut.Find(".hm-cell[data-level]").GetAttribute("style"));
    }

    [Fact]
    public void Clicking_a_cell_opens_that_day_in_calendar()
    {
        var cut = Render<HeatmapPanel>(p => p.Add(x => x.From, Today.AddDays(-2)).Add(x => x.To, Today));
        cut.FindAll(".hm-cell[data-level]")[0].Click();
        Assert.Contains("calendar?view=day&date=2026-11-14", Services.GetRequiredService<NavigationManager>().Uri);
    }

    [Fact]
    public void Separate_mode_renders_one_heatmap_per_activity()
    {
        AddTemplate("music");
        AddTemplate("mood");
        AddTemplate("sport");

        Services.GetRequiredService<NavigationManager>().NavigateTo("heatmap?separate=1");
        var cut = Render<HeatmapPage>();

        Assert.Equal(3, cut.FindAll("[data-testid=heatmap-panel]").Count);
    }
}

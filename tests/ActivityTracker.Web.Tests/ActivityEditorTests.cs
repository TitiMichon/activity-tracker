using ActivityTracker.Core.Models;
using ActivityTracker.Web.Pages;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace ActivityTracker.Web.Tests;

public class ActivityEditorTests : WebTestBase
{
    private NavigationManager Nav => Services.GetRequiredService<NavigationManager>();

    [Fact]
    public void Creating_a_quantity_activity_with_goal_and_deadline()
    {
        var cut = Render<ActivityEditorPage>();

        cut.Find("[data-testid=activity-name]").Input("Guitar");
        cut.FindAll(".segmented button").First(b => b.TextContent.Contains("Quantity")).Click();
        cut.Find("[data-testid=activity-unit]").Change("min");
        cut.Find("[aria-label='Icon 🎸']").Click();
        cut.Find("[aria-label='Color #8b5cf6']").Click();
        cut.Find("[data-testid=activity-frequency]").Change(nameof(Frequency.Weekly));
        cut.FindAll(".day-toggle").First(b => b.TextContent == "Tu").Click();
        cut.Find("[data-testid=activity-deadline]").Change("2026-12-31");
        cut.Find("[data-testid=activity-has-goal]").Change(true);
        cut.Find("[data-testid=goal-target]").Change("120");
        cut.Find("[data-testid=activity-form]").Submit();

        var a = Assert.Single(Tracker.Activities);
        Assert.Equal("Guitar", a.Name);
        Assert.Equal("🎸", a.Icon);
        Assert.Equal("#8b5cf6", a.Color);
        Assert.Equal(ActivityKind.Quantity, a.Kind);
        Assert.Equal("min", a.Unit);
        Assert.Equal(Frequency.Weekly, a.Schedule.Frequency);
        Assert.Equal([DayOfWeek.Tuesday], a.Schedule.DaysOfWeek);
        Assert.Equal(new DateOnly(2026, 12, 31), a.Deadline);
        Assert.Equal(120, a.Goal!.Target);
        Assert.Equal(GoalMetric.Sum, a.Goal.Metric);
        Assert.Equal(Today, a.StartDate);
        Assert.EndsWith("/activities", Nav.Uri);
    }

    [Fact]
    public void Invalid_form_shows_errors_and_saves_nothing()
    {
        var cut = Render<ActivityEditorPage>();

        cut.Find("[data-testid=activity-form]").Submit();

        Assert.Contains("Name is required", cut.Find("[data-testid=activity-errors]").TextContent);
        Assert.Empty(Tracker.Activities);
    }

    [Fact]
    public void Scale_type_starts_with_mood_preset_options()
    {
        var cut = Render<ActivityEditorPage>();
        cut.FindAll(".segmented button").First(b => b.TextContent.Contains("Scale")).Click();
        Assert.Equal(5, cut.FindAll(".option-edit").Count);

        cut.Find("[aria-label='Remove option Awful']").Click();
        cut.Find("[data-testid=activity-name]").Input("Energy");
        cut.Find("[data-testid=activity-form]").Submit();

        Assert.Equal(4, Assert.Single(Tracker.Activities).Options.Count);
    }

    [Fact]
    public void Editing_works_on_a_copy_until_saved()
    {
        var a = Add(Check("Sport"));
        var cut = Render<ActivityEditorPage>(p => p.Add(x => x.Id, a.Id));

        cut.Find("[data-testid=activity-name]").Input("Changed");

        Assert.Equal("Sport", Tracker.GetActivity(a.Id)!.Name);
        cut.Find("[data-testid=activity-form]").Submit();
        Assert.Equal("Changed", Tracker.GetActivity(a.Id)!.Name);
    }

    [Fact]
    public void Delete_needs_confirmation()
    {
        var a = Add(Check("Sport"));
        Sync(Tracker.LogAsync(a.Id));
        var cut = Render<ActivityEditorPage>(p => p.Add(x => x.Id, a.Id));

        var delete = cut.FindAll("button").First(b => b.TextContent.Trim() == "Delete");
        delete.Click();
        Assert.Single(Tracker.Activities);

        cut.FindAll("button").First(b => b.TextContent.Contains("Delete it")).Click();
        Assert.Empty(Tracker.Activities);
        Assert.Empty(Tracker.Entries);
    }

    [Fact]
    public void Unknown_id_shows_not_found()
    {
        var cut = Render<ActivityEditorPage>(p => p.Add(x => x.Id, Guid.NewGuid()));
        Assert.Contains("Activity not found", cut.Markup);
    }

    [Fact]
    public void Template_button_creates_activity_and_opens_it()
    {
        var cut = Render<ActivitiesPage>();

        cut.Find("[data-testid=template-mood]").Click();

        var mood = Assert.Single(Tracker.Activities);
        Assert.Equal("Mood", mood.Name);
        Assert.EndsWith($"/activities/{mood.Id}", Nav.Uri);
    }

    [Fact]
    public void Activities_can_be_reordered()
    {
        Add(Check("A"));
        Add(Check("B"));
        var cut = Render<ActivitiesPage>();

        cut.Find("[aria-label='Move B up']").Click();

        Assert.Equal(["B", "A"], Tracker.Activities.Select(a => a.Name));
    }
}

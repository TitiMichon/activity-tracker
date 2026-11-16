using ActivityTracker.Core.Models;
using ActivityTracker.Web.Components;
using Bunit;

namespace ActivityTracker.Web.Tests;

public class QuickAddTests : WebTestBase
{
    [Fact]
    public void Empty_state_invites_to_create_an_activity()
    {
        var cut = Render<QuickAdd>();
        Assert.Contains("No activities yet", cut.Markup);
        Assert.NotNull(cut.Find("a[href='activities']"));
    }

    [Fact]
    public void Tapping_a_check_activity_logs_it_immediately_with_undo()
    {
        var sport = Add(Check("Sport"));
        var toast = Render<ToastHost>();
        var cut = Render<QuickAdd>();

        cut.Find("[aria-label='Log Sport']").Click();

        var entry = Assert.Single(Tracker.Entries);
        Assert.Equal(sport.Id, entry.ActivityId);
        Assert.Equal(new DateTime(2026, 11, 16, 9, 30, 0), entry.Timestamp);
        cut.WaitForAssertion(() => Assert.Contains("1× today", cut.Markup));

        toast.WaitForAssertion(() => Assert.Contains("Sport", toast.Markup));
        toast.Find(".toast .btn.link").Click();
        Assert.Empty(Tracker.Entries);
    }

    [Fact]
    public void Quantity_activity_opens_panel_prefilled_with_default_value()
    {
        var music = AddTemplate("music"); // default 30 min
        var cut = Render<QuickAdd>();

        cut.Find("[aria-label='Log Music practice']").Click();
        var input = cut.Find(".qa-value");
        Assert.Equal("30", input.GetAttribute("value"));

        input.Change("42");
        cut.Find("[data-testid=qa-add]").Click();

        var entry = Assert.Single(Tracker.Entries);
        Assert.Equal(music.Id, entry.ActivityId);
        Assert.Equal(42, entry.Value);
        Assert.Empty(cut.FindAll("[data-testid=qa-panel]"));
    }

    [Fact]
    public void Increase_button_steps_the_value()
    {
        AddTemplate("music");
        var cut = Render<QuickAdd>();
        cut.Find("[aria-label='Log Music practice']").Click();

        cut.Find("[aria-label=Increase]").Click();

        Assert.Equal("35", cut.Find(".qa-value").GetAttribute("value"));
    }

    [Fact]
    public void Scale_activity_logs_the_chosen_option_with_note()
    {
        var mood = AddTemplate("mood");
        var cut = Render<QuickAdd>();

        cut.Find("[aria-label='Log Mood']").Click();
        cut.Find("input[aria-label=Note]").Change("sunny day");
        cut.Find("[aria-label=Great]").Click();

        var entry = Assert.Single(Tracker.Entries);
        Assert.Equal(mood.Id, entry.ActivityId);
        Assert.Equal("great", entry.OptionId);
        Assert.Equal("sunny day", entry.Note);
    }

    [Fact]
    public void Logging_on_another_day_keeps_current_time()
    {
        Add(Check("Sport"));
        var cut = Render<QuickAdd>(p => p.Add(x => x.Date, Today.AddDays(-3)));

        cut.Find("[aria-label='Log Sport']").Click();

        Assert.Equal(new DateTime(2026, 11, 13, 9, 30, 0), Assert.Single(Tracker.Entries).Timestamp);
    }

    [Fact]
    public void More_options_opens_full_editor()
    {
        AddTemplate("reading");
        var cut = Render<QuickAdd>();

        cut.Find("[aria-label='Log Reading']").Click();
        cut.FindAll("button").First(b => b.TextContent.Contains("More options")).Click();

        Assert.NotNull(cut.Find("[data-testid=entry-editor]"));
    }

    [Fact]
    public void Archived_activities_are_hidden()
    {
        var a = Add(Check("Old"));
        Sync(Tracker.SetArchivedAsync(a.Id, true));
        Add(Check("New"));

        var cut = Render<QuickAdd>();

        Assert.Single(cut.FindAll("[data-testid=qa-tile]"));
        Assert.DoesNotContain("Old", cut.Markup);
    }
}

using ActivityTracker.Core.Models;
using ActivityTracker.UI.Components;
using ActivityTracker.UI.Pages;
using ActivityTracker.UI.Services;
using AngleSharp.Dom;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace ActivityTracker.Web.Tests;

public class TodayViewTests : WebTestBase
{
    private static IElement TodayCard(IRenderedComponent<Home> cut) => cut.Find("[data-testid=todo-today]");

    private static IElement LongTermCard(IRenderedComponent<Home> cut) => cut.Find("[data-testid=todo-long-term]");

    private static void AddFrom(IElement card, string title)
    {
        card.QuerySelector("[data-testid=todo-input]")!.Input(title);
        card.QuerySelector("form.todo-add")!.Submit();
    }

    [Fact]
    public void Sections_come_in_order_today_scheduled_long_term()
    {
        Add(Check("Stretch", Frequency.Daily));
        var cut = Render<Home>();

        var sections = cut.FindAll("section[aria-label]").Select(s => s.GetAttribute("aria-label")).Where(l => l != "Quick add");

        Assert.Equal(["To do today", "Scheduled today", "Long-term to-do"], sections);
    }

    [Fact]
    public void Adding_from_today_and_long_term_lists()
    {
        var cut = Render<Home>();

        AddFrom(TodayCard(cut), "Buy milk");
        AddFrom(LongTermCard(cut), "Paint the fence");

        Assert.Equal(TodoList.Today, Tracker.Todos.Single(t => t.Title == "Buy milk").List);
        Assert.Equal(TodoList.LongTerm, Tracker.Todos.Single(t => t.Title == "Paint the fence").List);
        cut.WaitForAssertion(() => Assert.Contains("Buy milk", TodayCard(cut).TextContent));
        Assert.Contains("Paint the fence", LongTermCard(cut).TextContent);
        Assert.Equal("", TodayCard(cut).QuerySelector("[data-testid=todo-input]")!.GetAttribute("value"));
        Assert.Equal("1", TodayCard(cut).QuerySelector("[data-testid=todo-count]")!.TextContent);
    }

    [Fact]
    public void Blank_input_adds_nothing()
    {
        var cut = Render<Home>();

        AddFrom(TodayCard(cut), "   ");

        Assert.Empty(Tracker.Todos);
        Assert.True(TodayCard(cut).QuerySelector("button[type=submit]")!.HasAttribute("disabled"));
    }

    [Fact]
    public void Ticking_strikes_the_item_and_updates_the_count()
    {
        var todo = Sync(Tracker.AddTodoAsync("Buy milk", TodoList.Today));
        var cut = Render<Home>();

        cut.Find("[aria-label='Done: Buy milk']").Change(true);

        Assert.True(Tracker.GetTodo(todo.Id)!.IsDone);
        cut.WaitForAssertion(() => Assert.Contains("done", cut.Find("[data-testid=todo-item]").ClassName));
        Assert.Empty(TodayCard(cut).QuerySelectorAll("[data-testid=todo-count]"));
    }

    [Fact]
    public void Unfinished_items_from_earlier_days_show_since_when()
    {
        Sync(Tracker.AddTodoAsync("Call the bank", TodoList.Today));
        Time.Advance(TimeSpan.FromDays(2));

        var cut = Render<Home>();

        Assert.Equal("since Mon 16 Nov", cut.Find("[data-testid=todo-carried]").TextContent);
    }

    [Fact]
    public void Tapping_an_item_lets_you_rename_it()
    {
        var todo = Sync(Tracker.AddTodoAsync("Bank", TodoList.Today));
        var cut = Render<Home>();

        cut.Find(".todo-title").Click();
        cut.Find("[data-testid=todo-edit] input").Input("Call the bank");
        cut.Find("[data-testid=todo-edit]").Submit();

        Assert.Equal("Call the bank", Tracker.GetTodo(todo.Id)!.Title);
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[data-testid=todo-edit]")));
    }

    [Fact]
    public void Items_move_between_today_and_long_term()
    {
        var todo = Sync(Tracker.AddTodoAsync("Paint the fence", TodoList.LongTerm));
        var cut = Render<Home>();

        cut.Find(".todo-title").Click();
        Assert.Equal("Do it today", cut.Find("[data-testid=todo-move]").TextContent.Trim());
        cut.Find("[data-testid=todo-move]").Click();

        Assert.Equal(TodoList.Today, Tracker.GetTodo(todo.Id)!.List);
        cut.WaitForAssertion(() => Assert.Contains("Paint the fence", TodayCard(cut).TextContent));
        Assert.DoesNotContain("Paint the fence", LongTermCard(cut).TextContent);
    }

    [Fact]
    public void Deleting_can_be_undone_from_the_toast()
    {
        Sync(Tracker.AddTodoAsync("Buy milk", TodoList.Today));
        var cut = Render<Home>();
        var toasts = Services.GetRequiredService<ToastService>();

        cut.Find(".todo-title").Click();
        cut.Find("[data-testid=todo-delete]").Click();
        Assert.Empty(Tracker.Todos);
        Assert.Equal("“Buy milk” deleted", toasts.Current!.Message);

        Sync(toasts.RunActionAsync());
        Assert.Equal("Buy milk", Assert.Single(Tracker.Todos).Title);
    }

    [Fact]
    public void Scheduled_today_lists_only_planned_activities_by_reminder_time()
    {
        var evening = Check("Guitar", Frequency.Daily);
        evening.ReminderTime = new TimeOnly(18, 30);
        var morning = Check("Stretch", Frequency.Daily);
        morning.ReminderTime = new TimeOnly(7, 0);
        Add(evening);
        Add(morning);
        Add(Check("Anytime"));

        var cut = Render<Home>();
        var tiles = cut.Find("[data-testid=scheduled-today]").QuerySelectorAll("[data-testid=qa-tile]");

        Assert.Equal(["Log Stretch", "Log Guitar"], tiles.Select(t => t.GetAttribute("aria-label")));
        Assert.Contains("⏰ 18:30", tiles[1].TextContent);
    }

    [Fact]
    public void Logging_a_scheduled_activity_marks_it_done()
    {
        Add(Check("Stretch", Frequency.Daily));
        var cut = Render<Home>();

        cut.Find("[data-testid=scheduled-today] [aria-label='Log Stretch']").Click();

        Assert.Single(Tracker.Entries);
        cut.WaitForAssertion(() => Assert.Contains("1 / 1 done", cut.Find("[data-testid=scheduled-summary]").TextContent));
    }

    [Fact]
    public void Other_activities_log_and_objectives_are_folded_below()
    {
        Add(Check("Stretch", Frequency.Daily));
        var anytime = Add(Check("Anytime"));
        Sync(Tracker.LogAsync(anytime.Id));

        var cut = Render<Home>();
        var more = cut.Find("[data-testid=more-today]");

        Assert.False(more.HasAttribute("open"));
        Assert.Equal(["Log Anytime"], more.QuerySelectorAll("[data-testid=qa-tile]").Select(t => t.GetAttribute("aria-label")));
        Assert.Contains("Anytime", more.QuerySelector("[data-testid=entry-list]")!.TextContent);
    }

    [Fact]
    public void Without_activities_today_invites_to_create_one()
    {
        var cut = Render<Home>();

        Assert.Contains("No activities yet.", cut.Find("[data-testid=scheduled-today]").TextContent);
        Assert.Empty(cut.FindAll("[data-testid=more-today]"));
    }
}

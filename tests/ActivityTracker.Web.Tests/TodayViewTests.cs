using ActivityTracker.Core.Models;
using ActivityTracker.UI.Components;
using ActivityTracker.UI.Pages;
using Bunit;

namespace ActivityTracker.Web.Tests;

public class TodayViewTests : WebTestBase
{
    [Fact]
    public void Title_is_the_short_date()
    {
        var title = Render<Home>().Find("[data-testid=today-title]");

        Assert.Equal("16 Nov", title.TextContent);
        Assert.Equal("Monday 16 November 2026", title.GetAttribute("title"));
    }

    [Fact]
    public void Typing_a_task_and_pressing_enter_plans_it_today()
    {
        var cut = Render<DayList>();

        cut.Find("[data-testid=day-add-input]").Input("  Groceries ");
        cut.Find("form.todo-add").Submit();

        var task = Assert.Single(Tracker.Tasks);
        Assert.Equal("Groceries", task.Title);
        Assert.Equal(Today, task.PlannedDay);
        cut.WaitForAssertion(() => Assert.Contains("Groceries", cut.Find("[data-testid=day-task]").TextContent));
        Assert.Equal("", cut.Find("[data-testid=day-add-input]").GetAttribute("value"));
    }

    [Fact]
    public void Blank_tasks_cannot_be_added()
    {
        var cut = Render<DayList>();

        cut.Find("[data-testid=day-add-input]").Input("   ");

        Assert.True(cut.Find("form.todo-add button[type=submit]").HasAttribute("disabled"));
        cut.Find("form.todo-add").Submit();
        Assert.Empty(Tracker.Tasks);
    }

    [Fact]
    public void Shows_tasks_and_planned_activities_in_one_list_open_first()
    {
        var stretch = Add(Check("Stretch", Frequency.Daily));
        Add(Check("Unplanned"));
        var done = Sync(Tracker.AddTaskAsync("Call the bank", plannedDay: Today));
        Sync(Tracker.SetTaskDoneAsync(done.Id, true));
        Sync(Tracker.AddTaskAsync("Get a present", plannedDay: Today));
        Sync(Tracker.AddTaskAsync("Repair the car")); // backlog

        var cut = Render<DayList>();

        var rows = cut.FindAll(".todo-list > li").Select(li => li.TextContent).ToList();
        Assert.Equal(3, rows.Count);
        Assert.Contains("Get a present", rows[0]);
        Assert.Contains(stretch.Name, rows[1]);
        Assert.Contains("Call the bank", rows[2]);
        Assert.Equal("1 / 3 done", cut.Find("[data-testid=day-summary]").TextContent);
    }

    [Fact]
    public void Overdue_and_carried_over_tasks_are_tagged()
    {
        Sync(Tracker.AddTaskAsync("Pay the rent", dueDate: Today.AddDays(-2)));
        Sync(Tracker.AddTaskAsync("Window", plannedDay: Today.AddDays(-6)));

        var cut = Render<DayList>();

        var rows = cut.FindAll("[data-testid=day-task]");
        Assert.Contains("overdue", rows[0].ClassName);
        Assert.Equal("overdue, was due Sat 14 Nov", rows[0].QuerySelector("[data-testid=task-tag]")!.TextContent);
        Assert.Equal("since Tue 10 Nov", rows[1].QuerySelector("[data-testid=task-tag]")!.TextContent);
    }

    [Fact]
    public void Ticking_a_task_marks_it_done_and_moves_it_down()
    {
        var first = Sync(Tracker.AddTaskAsync("First", plannedDay: Today));
        Sync(Tracker.AddTaskAsync("Second", plannedDay: Today));
        var cut = Render<DayList>();

        cut.Find("[aria-label='Done: First']").Change(true);

        Assert.True(Tracker.GetTask(first.Id)!.IsDone);
        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll("[data-testid=day-task]");
            Assert.Contains("Second", rows[0].TextContent);
            Assert.Contains("done", rows[1].ClassName);
        });
    }

    [Fact]
    public void Tapping_a_task_renames_it()
    {
        var task = Sync(Tracker.AddTaskAsync("Grocries", plannedDay: Today));
        var cut = Render<DayList>();

        cut.Find("[data-testid=day-task] .todo-title").Click();
        Assert.Equal("true", cut.Find("[data-testid=day-task] .todo-title").GetAttribute("aria-expanded"));
        cut.Find("[data-testid=task-edit] input").Input("Groceries");
        cut.Find("[data-testid=task-edit]").Submit();

        Assert.Equal("Groceries", Tracker.GetTask(task.Id)!.Title);
        Assert.Empty(cut.FindAll("[data-testid=task-edit]"));
    }

    [Fact]
    public void Deleting_a_task_can_be_undone()
    {
        var task = Sync(Tracker.AddTaskAsync("Get a present", plannedDay: Today));
        var toast = Render<ToastHost>();
        var cut = Render<DayList>();

        cut.Find("[data-testid=day-task] .todo-title").Click();
        cut.Find("[data-testid=task-delete]").Click();
        Assert.Empty(Tracker.Tasks);

        toast.WaitForAssertion(() => Assert.Contains("“Get a present” deleted", toast.Markup));
        toast.Find(".toast .btn.link").Click();
        Assert.Equal(task.Id, Assert.Single(Tracker.Tasks).Id);
    }

    [Fact]
    public void Ticking_a_check_activity_logs_it_and_unticking_removes_the_entry_with_undo()
    {
        var stretch = Add(Check("Stretch", Frequency.Daily));
        var toast = Render<ToastHost>();
        var cut = Render<DayList>();

        cut.Find("[aria-label='Log Stretch']").Click();
        var entry = Assert.Single(Tracker.Entries);
        Assert.Equal(stretch.Id, entry.ActivityId);
        cut.WaitForAssertion(() => Assert.Contains("done", cut.Find("[data-testid=day-activity]").ClassName));

        cut.Find("[aria-label='Untick Stretch']").Click();
        Assert.Empty(Tracker.Entries);

        toast.WaitForAssertion(() => Assert.Contains("Stretch unticked", toast.Markup));
        toast.Find(".toast .btn.link").Click();
        Assert.Equal(entry.Id, Assert.Single(Tracker.Entries).Id);
    }

    [Fact]
    public void Several_times_a_day_counts_each_tap()
    {
        var water = Check("Water", Frequency.Daily);
        water.Schedule.TimesPerDay = 3;
        Add(water);
        var cut = Render<DayList>();

        cut.Find("[aria-label='Log Water']").Click();
        cut.Find("[aria-label='Log Water']").Click();

        Assert.Equal(2, Tracker.Entries.Count);
        cut.WaitForAssertion(() => Assert.Contains("2/3", cut.Find("[data-testid=day-activity]").TextContent));
    }

    [Fact]
    public void Quantity_and_scale_activities_open_the_log_panel_in_the_row()
    {
        var music = AddTemplate("music"); // daily, 30 min by default
        var cut = Render<DayList>();

        cut.Find("[aria-label='Log Music practice']").Click();
        cut.Find("[data-testid=day-activity] [data-testid=qa-add]").Click();

        Assert.Equal(30, Assert.Single(Tracker.Entries).Value);
        Assert.Empty(cut.FindAll("[data-testid=qa-panel]"));
        cut.WaitForAssertion(() => Assert.Contains("30 min", cut.Find("[data-testid=activity-detail]").TextContent));
        Assert.Equal(music.Id, Tracker.Entries[0].ActivityId);
    }

    [Fact]
    public void Reminder_time_shows_until_done()
    {
        var stretch = Check("Stretch", Frequency.Daily);
        stretch.ReminderTime = new TimeOnly(18, 30);
        Add(stretch);
        var cut = Render<DayList>();

        Assert.Contains("⏰ 18:30", cut.Find("[data-testid=day-activity]").TextContent);
        cut.Find("[aria-label='Log Stretch']").Click();
        cut.WaitForAssertion(() => Assert.DoesNotContain("⏰", cut.Find("[data-testid=day-activity]").TextContent));
    }

    [Fact]
    public void Empty_day_invites_to_add_a_task()
    {
        var cut = Render<DayList>();

        Assert.Contains("Nothing planned today", cut.Markup);
        Assert.Empty(cut.FindAll("[data-testid=day-summary]"));
    }

    [Fact]
    public void Other_activities_and_the_log_are_folded_below_the_list()
    {
        Add(Check("Stretch", Frequency.Daily));
        Add(Check("Unplanned"));

        var cut = Render<Home>();

        var more = cut.Find("[data-testid=more-today]");
        Assert.False(more.HasAttribute("open"));
        var tiles = more.QuerySelectorAll("[data-testid=qa-tile]").Select(t => t.GetAttribute("aria-label"));
        Assert.Equal(["Log Unplanned"], tiles);
        Assert.NotNull(more.QuerySelector("[data-testid=entry-list]") ?? more.QuerySelector(".muted"));
    }

    [Fact]
    public void Without_activities_today_invites_to_create_one()
    {
        var cut = Render<Home>();

        Assert.Contains("No activities yet", cut.Markup);
        Assert.Empty(cut.FindAll("[data-testid=more-today]"));
    }
}

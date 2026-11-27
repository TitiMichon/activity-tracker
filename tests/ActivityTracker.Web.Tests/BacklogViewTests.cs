using ActivityTracker.UI.Components;
using ActivityTracker.UI.Pages;
using Bunit;

namespace ActivityTracker.Web.Tests;

public class BacklogViewTests : WebTestBase
{
    [Fact]
    public void Adding_to_the_backlog_keeps_the_task_off_today()
    {
        var cut = Render<BacklogList>();

        cut.Find("[data-testid=backlog-add-input]").Input("Repair the car");
        cut.Find("form.todo-add").Submit();

        var task = Assert.Single(Tracker.Tasks);
        Assert.Null(task.PlannedDay);
        cut.WaitForAssertion(() => Assert.Contains("Repair the car", cut.Find("[data-testid=backlog-task]").TextContent));
        Assert.Equal("1 task", cut.Find("[data-testid=backlog-count]").TextContent);
        Assert.Empty(Tracker.TasksToday());
    }

    [Fact]
    public void Sorted_by_due_date_with_badges_and_soon_highlighted()
    {
        Sync(Tracker.AddTaskAsync("Window"));
        Sync(Tracker.AddTaskAsync("Taxes", dueDate: Today.AddDays(10)));
        Sync(Tracker.AddTaskAsync("Gift", dueDate: Today.AddDays(2)));
        Sync(Tracker.AddTaskAsync("Dentist", plannedDay: Today.AddDays(1)));
        Sync(Tracker.AddTaskAsync("Today's", plannedDay: Today));

        var rows = Render<BacklogList>().FindAll("[data-testid=backlog-task]");

        Assert.Equal(["Gift", "Taxes", "Window", "Dentist"], rows.Select(r => r.QuerySelector(".todo-title > span")!.TextContent));
        Assert.Contains("soon", rows[0].ClassName);
        Assert.Equal("due in 2 days", rows[0].QuerySelector("[data-testid=task-tag]")!.TextContent);
        Assert.DoesNotContain("soon", rows[1].ClassName);
        Assert.Null(rows[2].QuerySelector("[data-testid=task-tag]"));
        Assert.Equal("planned tomorrow", rows[3].QuerySelector("[data-testid=task-tag]")!.TextContent);
    }

    [Fact]
    public void Plan_for_today_moves_the_task_to_today_with_undo()
    {
        var task = Sync(Tracker.AddTaskAsync("Repair the car"));
        var toast = Render<ToastHost>();
        var cut = Render<BacklogList>();

        cut.Find("[data-testid=backlog-task] .todo-title").Click();
        cut.Find("[data-testid=task-plan-today]").Click();

        Assert.Equal(Today, Tracker.GetTask(task.Id)!.PlannedDay);
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[data-testid=backlog-task]")));
        toast.WaitForAssertion(() => Assert.Contains("“Repair the car” planned for today", toast.Markup));
        toast.Find(".toast .btn.link").Click();
        Assert.Null(Tracker.GetTask(task.Id)!.PlannedDay);
    }

    [Fact]
    public void Plan_for_a_day_needs_a_date()
    {
        var task = Sync(Tracker.AddTaskAsync("Change the window"));
        var cut = Render<BacklogList>();
        cut.Find("[data-testid=backlog-task] .todo-title").Click();

        Assert.True(cut.Find("[data-testid=task-plan-day-button]").HasAttribute("disabled"));
        cut.Find("[data-testid=task-plan-day]").Change(Today.AddDays(5).ToString("yyyy-MM-dd"));
        cut.Find("[data-testid=task-plan-day-button]").Click();

        Assert.Equal(Today.AddDays(5), Tracker.GetTask(task.Id)!.PlannedDay);
        cut.WaitForAssertion(() => Assert.Contains("planned Sat 21 Nov", cut.Find("[data-testid=backlog-task]").TextContent));
    }

    [Fact]
    public void Ticking_a_backlog_task_can_be_undone()
    {
        var task = Sync(Tracker.AddTaskAsync("Repair the car"));
        var toast = Render<ToastHost>();
        var cut = Render<BacklogList>();

        cut.Find("[data-testid=backlog-task] .todo-check").Change(true);

        Assert.True(Tracker.GetTask(task.Id)!.IsDone);
        toast.WaitForAssertion(() => Assert.Contains("“Repair the car” done", toast.Markup));
        toast.Find(".toast .btn.link").Click();
        Assert.False(Tracker.GetTask(task.Id)!.IsDone);
    }

    [Fact]
    public void A_planned_task_of_today_goes_back_to_the_backlog_but_a_due_one_cannot()
    {
        var planned = Sync(Tracker.AddTaskAsync("Groceries", plannedDay: Today));
        Sync(Tracker.AddTaskAsync("Pay the rent", plannedDay: Today, dueDate: Today));
        var cut = Render<DayList>();

        var rows = cut.FindAll("[data-testid=day-task]");
        var rent = rows.Single(r => r.TextContent.Contains("Pay the rent"));
        rent.QuerySelector(".todo-title")!.Click();
        Assert.Empty(cut.FindAll("[data-testid=task-to-backlog]"));

        cut.FindAll("[data-testid=day-task]").Single(r => r.TextContent.Contains("Groceries")).QuerySelector(".todo-title")!.Click();
        cut.Find("[data-testid=task-to-backlog]").Click();

        Assert.Null(Tracker.GetTask(planned.Id)!.PlannedDay);
        Assert.Equal(planned.Id, Assert.Single(Tracker.Backlog()).Id);
    }

    [Fact]
    public void Today_page_switches_between_today_and_the_backlog()
    {
        Sync(Tracker.AddTaskAsync("Repair the car"));
        var cut = Render<Home>();

        Assert.Equal("Backlog (1)", cut.Find("[data-testid=show-backlog]").TextContent);
        Assert.DoesNotContain("show-backlog", cut.Find(".today-layout").ClassName);
        Assert.NotNull(cut.Find("[data-testid=backlog]")); // in the page; CSS hides it on phones

        cut.Find("[data-testid=show-backlog]").Click();

        Assert.Contains("show-backlog", cut.Find(".today-layout").ClassName);
        Assert.Equal("true", cut.Find("[data-testid=show-backlog]").GetAttribute("aria-pressed"));
    }
}

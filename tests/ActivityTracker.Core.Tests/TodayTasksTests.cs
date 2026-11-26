using ActivityTracker.Core.Models;
using ActivityTracker.Core.Services;
using static ActivityTracker.Core.Tests.TestData;

namespace ActivityTracker.Core.Tests;

public class TodayTasksTests
{
    private static readonly DateTime Created = Monday.AddDays(-10).ToDateTime(new TimeOnly(8, 0));

    private static TaskItem Task(string title, DateOnly? planned = null, DateOnly? due = null, DateTime? doneAt = null, int createdMinutes = 0) => new()
    {
        Title = title,
        PlannedDay = planned,
        DueDate = due,
        DoneAt = doneAt,
        CreatedAt = Created.AddMinutes(createdMinutes),
    };

    private static DateTime At(DateOnly day, int hour = 10) => day.ToDateTime(new TimeOnly(hour, 0));

    [Fact]
    public void Shows_planned_due_overdue_and_carried_over_tasks_but_not_the_backlog_or_later_days()
    {
        var tasks = new[]
        {
            Task("Planned today", planned: Monday),
            Task("Due today", due: Monday),
            Task("Overdue", due: Monday.AddDays(-2)),
            Task("Carried over", planned: Monday.AddDays(-3)),
            Task("Backlog"),
            Task("Due later", due: Monday.AddDays(4)),
            Task("Planned tomorrow", planned: Monday.AddDays(1)),
        };

        var titles = TodayTasks.For(tasks, Monday).Select(t => t.Title);

        Assert.Equal(["Overdue", "Due today", "Carried over", "Planned today"], titles);
    }

    [Fact]
    public void A_task_planned_later_but_due_today_is_shown()
    {
        var task = Task("Tax return", planned: Monday.AddDays(2), due: Monday);

        Assert.Single(TodayTasks.For([task], Monday));
        Assert.Equal(TaskTiming.DueToday, TodayTasks.TimingOf(task, Monday));
    }

    [Fact]
    public void Done_tasks_stay_until_the_end_of_the_day_after_the_open_ones()
    {
        var tasks = new[]
        {
            Task("Ticked second", planned: Monday, doneAt: At(Monday, 11)),
            Task("Ticked first", planned: Monday.AddDays(-1), doneAt: At(Monday, 9)),
            Task("Ticked yesterday", planned: Monday.AddDays(-1), doneAt: At(Monday.AddDays(-1))),
            Task("Open", planned: Monday),
            Task("Backlog ticked today", doneAt: At(Monday)),
        };

        Assert.Equal(["Open", "Ticked first", "Ticked second"], TodayTasks.For(tasks, Monday).Select(t => t.Title));
    }

    [Fact]
    public void Same_urgency_keeps_the_oldest_deadline_then_creation_order_first()
    {
        var tasks = new[]
        {
            Task("Newer", planned: Monday, createdMinutes: 5),
            Task("Older", planned: Monday, createdMinutes: 1),
            Task("Overdue recent", due: Monday.AddDays(-1)),
            Task("Overdue old", due: Monday.AddDays(-5)),
        };

        Assert.Equal(["Overdue old", "Overdue recent", "Older", "Newer"], TodayTasks.For(tasks, Monday).Select(t => t.Title));
    }

    [Theory]
    [InlineData(null, -2, "overdue, was due Sat 14 Nov")]
    [InlineData(null, -1, "overdue, was due yesterday")]
    [InlineData(0, 0, "due today")]
    [InlineData(-6, null, "since Tue 10 Nov")]
    [InlineData(-1, null, "since yesterday")]
    [InlineData(0, 1, "due tomorrow")]
    [InlineData(0, 5, "due Sat 21 Nov")]
    [InlineData(0, null, null)]
    public void Tag_says_why_the_task_is_there(int? plannedOffset, int? dueOffset, string? expected)
    {
        var task = Task("T",
            planned: plannedOffset is { } p ? Monday.AddDays(p) : null,
            due: dueOffset is { } d ? Monday.AddDays(d) : null);

        Assert.Equal(expected, TodayTasks.Tag(task, Monday));
    }

    [Fact]
    public void Dates_from_another_year_show_the_year_and_done_tasks_have_no_tag()
    {
        Assert.Equal("since Mon 15 Dec 2025", TodayTasks.Tag(Task("T", planned: new DateOnly(2025, 12, 15)), Monday));
        Assert.Null(TodayTasks.Tag(Task("T", due: Monday.AddDays(-3), doneAt: At(Monday)), Monday));
    }
}

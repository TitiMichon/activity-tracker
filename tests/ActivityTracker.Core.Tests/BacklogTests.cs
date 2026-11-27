using ActivityTracker.Core.Models;
using ActivityTracker.Core.Services;
using static ActivityTracker.Core.Tests.TestData;

namespace ActivityTracker.Core.Tests;

public class BacklogTests
{
    private static readonly DateTime Created = Monday.AddDays(-10).ToDateTime(new TimeOnly(8, 0));

    private static TaskItem Task(string title, DateOnly? planned = null, DateOnly? due = null, bool done = false, int createdMinutes = 0) => new()
    {
        Title = title,
        PlannedDay = planned,
        DueDate = due,
        DoneAt = done ? Monday.ToDateTime(new TimeOnly(9, 0)) : null,
        CreatedAt = Created.AddMinutes(createdMinutes),
    };

    [Fact]
    public void Holds_open_tasks_not_on_todays_list_by_due_date_then_age()
    {
        var tasks = new[]
        {
            Task("Window", createdMinutes: 2),
            Task("Car", createdMinutes: 1),
            Task("Taxes", due: Monday.AddDays(20)),
            Task("Gift", due: Monday.AddDays(2)),
            Task("Dentist", planned: Monday.AddDays(3)),
            Task("Today", planned: Monday),
            Task("Overdue", due: Monday.AddDays(-1)),
            Task("Done", done: true),
        };

        var titles = Backlog.For(tasks, Monday).Select(t => t.Title);

        Assert.Equal(["Gift", "Taxes", "Dentist", "Car", "Window"], titles);
    }

    [Theory]
    [InlineData(null, null, false)]
    [InlineData(1, "due tomorrow", true)]
    [InlineData(3, "due in 3 days", true)]
    [InlineData(4, "due Fri 20 Nov", false)]
    public void Due_tag_and_soon_badge(int? dueOffset, string? expected, bool soon)
    {
        var task = Task("T", due: dueOffset is { } d ? Monday.AddDays(d) : null);

        Assert.Equal(expected, Backlog.DueTag(task, Monday));
        Assert.Equal(soon, Backlog.IsDueSoon(task, Monday));
    }

    [Fact]
    public void Planned_tag_only_for_a_later_day()
    {
        Assert.Equal("planned tomorrow", Backlog.PlannedTag(Task("T", planned: Monday.AddDays(1)), Monday));
        Assert.Equal("planned Thu 19 Nov", Backlog.PlannedTag(Task("T", planned: Monday.AddDays(3)), Monday));
        Assert.Null(Backlog.PlannedTag(Task("T", planned: Monday), Monday));
        Assert.Null(Backlog.PlannedTag(Task("T"), Monday));
    }
}

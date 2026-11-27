using ActivityTracker.Core.Models;

namespace ActivityTracker.Core.Services;

/// <summary>Why a task is on a given day's list.</summary>
public enum TaskTiming
{
    /// <summary>The due date has passed.</summary>
    Overdue,

    /// <summary>Due on that day.</summary>
    DueToday,

    /// <summary>Planned on an earlier day and not done yet.</summary>
    CarriedOver,

    /// <summary>Planned for that day.</summary>
    Planned,

    /// <summary>Neither planned nor due by that day (a backlog task).</summary>
    NotScheduled,
}

/// <summary>Which tasks a day's list shows, and in which order.</summary>
public static class TodayTasks
{
    /// <summary>
    /// Tasks on <paramref name="today"/>'s list: the ones still to do that are planned or due on or before today,
    /// plus the ones of that list ticked today. Open tasks first, most urgent first; done ones last, in the order they were ticked.
    /// </summary>
    public static IReadOnlyList<TaskItem> For(IEnumerable<TaskItem> tasks, DateOnly today) =>
        tasks.Where(t => TimingOf(t, today) != TaskTiming.NotScheduled && (!t.IsDone || DoneOn(t, today)))
            .OrderBy(t => t.IsDone)
            .ThenBy(t => t.DoneAt)
            .ThenBy(t => TimingOf(t, today))
            .ThenBy(t => t.DueDate ?? DateOnly.MaxValue)
            .ThenBy(t => t.PlannedDay ?? DateOnly.MaxValue)
            .ThenBy(t => t.CreatedAt)
            .ToList();

    /// <summary>The most urgent reason for the task to be on <paramref name="today"/>'s list.</summary>
    public static TaskTiming TimingOf(TaskItem task, DateOnly today) =>
        !(task.PlannedDay <= today || task.DueDate <= today) ? TaskTiming.NotScheduled
        : task.DueDate < today ? TaskTiming.Overdue
        : task.DueDate == today ? TaskTiming.DueToday
        : task.PlannedDay < today ? TaskTiming.CarriedOver
        : TaskTiming.Planned;

    /// <summary>
    /// Short label shown next to an open task's title: "overdue, was due Fri 13", "due today", "since Tue 10", "due tomorrow";
    /// null when simply planned today, and for done tasks.
    /// </summary>
    public static string? Tag(TaskItem task, DateOnly today) => task.IsDone ? null : TimingOf(task, today) switch
    {
        TaskTiming.Overdue => $"overdue, was due {Day(task.DueDate!.Value, today)}",
        TaskTiming.DueToday => "due today",
        TaskTiming.CarriedOver => $"since {Day(task.PlannedDay!.Value, today)}",
        _ when task.DueDate is { } due && due > today => $"due {Day(due, today)}",
        _ => null,
    };

    internal static string Day(DateOnly day, DateOnly today) =>
        day == today.AddDays(-1) ? "yesterday"
        : day == today.AddDays(1) ? "tomorrow"
        : day.ToString(day.Year == today.Year ? "ddd d MMM" : "ddd d MMM yyyy");

    private static bool DoneOn(TaskItem task, DateOnly day) => DateOnly.FromDateTime(task.DoneAt!.Value) == day;
}

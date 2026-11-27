using ActivityTracker.Core.Models;

namespace ActivityTracker.Core.Services;

/// <summary>The tasks waiting for a day: open, neither planned nor due by today (see <see cref="TodayTasks"/>).</summary>
public static class Backlog
{
    /// <summary>A task due within this many days gets the "soon" badge.</summary>
    public const int SoonDays = 3;

    /// <summary>Open tasks that are not on <paramref name="today"/>'s list: by due date (without one last), then oldest first.</summary>
    public static IReadOnlyList<TaskItem> For(IEnumerable<TaskItem> tasks, DateOnly today) =>
        tasks.Where(t => !t.IsDone && TodayTasks.TimingOf(t, today) == TaskTiming.NotScheduled)
            .OrderBy(t => t.DueDate ?? DateOnly.MaxValue)
            .ThenBy(t => t.CreatedAt)
            .ToList();

    /// <summary>"due tomorrow", "due in 3 days", "due Fri 4 Dec"; null without a due date.</summary>
    public static string? DueTag(TaskItem task, DateOnly today) => task.DueDate is not { } due ? null
        : due == today.AddDays(1) ? "due tomorrow"
        : IsDueSoon(task, today) ? $"due in {due.DayNumber - today.DayNumber} days"
        : $"due {TodayTasks.Day(due, today)}";

    /// <summary>Due in the next <see cref="SoonDays"/> days.</summary>
    public static bool IsDueSoon(TaskItem task, DateOnly today) =>
        task.DueDate is { } due && due > today && due.DayNumber - today.DayNumber <= SoonDays;

    /// <summary>"planned tomorrow", "planned Mon 30 Nov" for a task planned on a later day; null otherwise.</summary>
    public static string? PlannedTag(TaskItem task, DateOnly today) =>
        task.PlannedDay is { } day && day > today ? $"planned {TodayTasks.Day(day, today)}" : null;
}

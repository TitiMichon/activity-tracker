using ActivityTracker.Core.Models;

namespace ActivityTracker.Core.Services;

/// <summary>Which to-do items the Today view shows, and in which order.</summary>
public static class TodoLists
{
    /// <summary>
    /// "To do today": items planned for today or unfinished from an earlier day, plus the ones ticked today.
    /// Items still to do come first (oldest first), then the done ones in the order they were ticked.
    /// </summary>
    public static IReadOnlyList<TodoItem> ForToday(IEnumerable<TodoItem> todos, DateOnly today) =>
        Ordered(todos.Where(t => t.List == TodoList.Today && (t.IsDone ? DoneOn(t, today) : (t.Day ?? today) <= today)));

    /// <summary>"Long-term to-do": items still to do, plus the ones ticked today.</summary>
    public static IReadOnlyList<TodoItem> LongTerm(IEnumerable<TodoItem> todos, DateOnly today) =>
        Ordered(todos.Where(t => t.List == TodoList.LongTerm && (!t.IsDone || DoneOn(t, today))));

    /// <summary>True for an unfinished item planned on an earlier day.</summary>
    public static bool IsCarriedOver(TodoItem todo, DateOnly today) =>
        todo.List == TodoList.Today && !todo.IsDone && todo.Day < today;

    /// <summary>Trims a to-do title (or checklist item text, with <paramref name="what"/> = "An item").</summary>
    /// <exception cref="ArgumentException">The text is empty or too long.</exception>
    public static string CleanTitle(string? title, string what = "A to-do")
    {
        var clean = title?.Trim() ?? "";
        if (clean.Length == 0)
        {
            throw new ArgumentException($"{what} cannot be empty.", nameof(title));
        }

        if (clean.Length > TodoItem.MaxTitleLength)
        {
            throw new ArgumentException($"{what} is at most {TodoItem.MaxTitleLength} characters.", nameof(title));
        }

        return clean;
    }

    private static bool DoneOn(TodoItem todo, DateOnly day) => DateOnly.FromDateTime(todo.DoneAt!.Value) == day;

    private static List<TodoItem> Ordered(IEnumerable<TodoItem> items) =>
        items.OrderBy(t => t.IsDone)
            .ThenBy(t => t.DoneAt)
            .ThenBy(t => t.Day)
            .ThenBy(t => t.CreatedAt)
            .ToList();
}

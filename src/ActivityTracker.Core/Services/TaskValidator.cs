using ActivityTracker.Core.Models;

namespace ActivityTracker.Core.Services;

public static class TaskValidator
{
    /// <summary>Trims a task or subtask title.</summary>
    /// <exception cref="ArgumentException">The title is empty or too long.</exception>
    public static string CleanTitle(string? title, string what = "A task")
    {
        var clean = title?.Trim() ?? "";
        if (clean.Length == 0)
        {
            throw new ArgumentException($"{what} needs a title.", nameof(title));
        }

        if (clean.Length > TaskItem.MaxTitleLength)
        {
            throw new ArgumentException($"{what} title is at most {TaskItem.MaxTitleLength} characters.", nameof(title));
        }

        return clean;
    }

    /// <summary>Trims the notes; blank notes become null.</summary>
    /// <exception cref="ArgumentException">The notes are too long.</exception>
    public static string? CleanNotes(string? notes)
    {
        var clean = notes?.Trim();
        if (string.IsNullOrEmpty(clean))
        {
            return null;
        }

        if (clean.Length > TaskItem.MaxNotesLength)
        {
            throw new ArgumentException($"Notes are at most {TaskItem.MaxNotesLength} characters.", nameof(notes));
        }

        return clean;
    }

    /// <summary>Cleans the titles and notes of <paramref name="task"/> in place.</summary>
    /// <exception cref="ArgumentException">A title is empty or too long, or there are too many subtasks.</exception>
    public static void Clean(TaskItem task)
    {
        task.Title = CleanTitle(task.Title);
        task.Notes = CleanNotes(task.Notes);
        task.Subtasks ??= [];
        if (task.Subtasks.Count > TaskItem.MaxSubtasks)
        {
            throw new ArgumentException($"A task has at most {TaskItem.MaxSubtasks} subtasks.", nameof(task));
        }

        foreach (var subtask in task.Subtasks)
        {
            subtask.Title = CleanTitle(subtask.Title, "A subtask");
        }
    }
}

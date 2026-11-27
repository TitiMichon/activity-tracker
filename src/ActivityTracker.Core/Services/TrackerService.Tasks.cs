using ActivityTracker.Core.Models;

namespace ActivityTracker.Core.Services;

public sealed partial class TrackerService
{
    public IReadOnlyList<TaskItem> Tasks => _data.Tasks;

    public TaskItem? GetTask(Guid id) => _data.Tasks.FirstOrDefault(t => t.Id == id);

    /// <summary>Tasks on today's list (see <see cref="TodayTasks.For"/>).</summary>
    public IReadOnlyList<TaskItem> TasksToday() => TodayTasks.For(_data.Tasks, Today);

    /// <summary>Open tasks not on today's list (see <see cref="Backlog.For"/>).</summary>
    public IReadOnlyList<TaskItem> Backlog() => Services.Backlog.For(_data.Tasks, Today);

    /// <summary>Adds a task. Without a planned day it goes to the backlog.</summary>
    /// <exception cref="ArgumentException">The title is empty or too long.</exception>
    /// <exception cref="KeyNotFoundException">The activity does not exist.</exception>
    public async Task<TaskItem> AddTaskAsync(string title, DateOnly? plannedDay = null, DateOnly? dueDate = null, Guid? activityId = null)
    {
        var task = new TaskItem
        {
            Title = TaskValidator.CleanTitle(title),
            PlannedDay = plannedDay,
            DueDate = dueDate,
            ActivityId = activityId,
            CreatedAt = Now,
        };
        RequireActivityOrNone(task.ActivityId);
        _data.Tasks.Add(task);
        await PersistAsync();
        return task;
    }

    /// <summary>Adds or replaces a task (task editor, undo of a deletion).</summary>
    /// <exception cref="ArgumentException">A title is empty or too long, or there are too many subtasks.</exception>
    /// <exception cref="KeyNotFoundException">The activity does not exist.</exception>
    public async Task SaveTaskAsync(TaskItem task)
    {
        TaskValidator.Clean(task);
        RequireActivityOrNone(task.ActivityId);
        if (task.CreatedAt == default)
        {
            task.CreatedAt = Now;
        }

        var index = _data.Tasks.FindIndex(t => t.Id == task.Id);
        if (index >= 0)
        {
            _data.Tasks[index] = task;
        }
        else
        {
            _data.Tasks.Add(task);
        }

        await PersistAsync();
    }

    public async Task SetTaskDoneAsync(Guid id, bool done)
    {
        var task = RequireTask(id);
        if (task.IsDone == done)
        {
            return;
        }

        task.DoneAt = done ? Now : null;
        await PersistAsync();
    }

    /// <exception cref="ArgumentException">The title is empty or too long.</exception>
    public async Task RenameTaskAsync(Guid id, string title)
    {
        RequireTask(id).Title = TaskValidator.CleanTitle(title);
        await PersistAsync();
    }

    /// <summary>Plans the task for a day (it shows in Today then), or sends it back to the backlog with null.</summary>
    public async Task PlanTaskAsync(Guid id, DateOnly? day)
    {
        RequireTask(id).PlannedDay = day;
        await PersistAsync();
    }

    /// <summary>Sets or clears (null) the deadline.</summary>
    public async Task SetTaskDueDateAsync(Guid id, DateOnly? dueDate)
    {
        RequireTask(id).DueDate = dueDate;
        await PersistAsync();
    }

    public async Task DeleteTaskAsync(Guid id)
    {
        if (_data.Tasks.RemoveAll(t => t.Id == id) > 0)
        {
            await PersistAsync();
        }
    }

    /// <summary>Tasks of a deleted activity are kept, without the link.</summary>
    private void UnlinkTasks(Guid activityId)
    {
        foreach (var task in _data.Tasks.Where(t => t.ActivityId == activityId))
        {
            task.ActivityId = null;
        }
    }

    private void RequireActivityOrNone(Guid? activityId)
    {
        if (activityId is { } id && GetActivity(id) is null)
        {
            throw new KeyNotFoundException($"Activity {id} not found.");
        }
    }

    private TaskItem RequireTask(Guid id) => GetTask(id) ?? throw new KeyNotFoundException($"Task {id} not found.");
}

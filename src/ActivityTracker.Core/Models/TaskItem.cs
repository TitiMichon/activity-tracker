using System.Text.Json.Serialization;

namespace ActivityTracker.Core.Models;

/// <summary>
/// Something to do once (repair the car, buy a present...). Without a planned day it waits in the backlog.
/// Named TaskItem so it never clashes with <see cref="System.Threading.Tasks.Task"/>.
/// </summary>
public sealed class TaskItem
{
    public const int MaxTitleLength = 200;
    public const int MaxNotesLength = 4000;
    public const int MaxSubtasks = 100;

    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = "";

    public string? Notes { get; set; }

    /// <summary>The day I intend to do it: puts the task in Today. Null keeps it in the backlog.</summary>
    public DateOnly? PlannedDay { get; set; }

    /// <summary>Hard deadline: the task shows in Today on that day even if not planned, and is overdue after.</summary>
    public DateOnly? DueDate { get; set; }

    /// <summary>Steps of the task (one level), in display order.</summary>
    public List<Subtask> Subtasks { get; set; } = [];

    /// <summary>Optional activity the task belongs to (e.g. "Learn a partition" for Music).</summary>
    public Guid? ActivityId { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>When the task was ticked; null while it is still to do.</summary>
    public DateTime? DoneAt { get; set; }

    [JsonIgnore]
    public bool IsDone => DoneAt is not null;
}

/// <summary>One step of a <see cref="TaskItem"/> (e.g. "Clean the windows" in "Cleanup").</summary>
public sealed class Subtask
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = "";

    public bool Done { get; set; }
}

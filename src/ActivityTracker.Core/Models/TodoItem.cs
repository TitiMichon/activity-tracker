using System.Text.Json.Serialization;

namespace ActivityTracker.Core.Models;

/// <summary>Which to-do list an item belongs to.</summary>
public enum TodoList
{
    /// <summary>Planned for a given day; unfinished items carry over to the next days.</summary>
    Today,

    /// <summary>No date: things to do some day.</summary>
    LongTerm,
}

/// <summary>A to-do item shown on the Today view.</summary>
public sealed class TodoItem
{
    public const int MaxTitleLength = 200;

    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = "";

    public TodoList List { get; set; } = TodoList.Today;

    /// <summary>Day the item is planned for (only for <see cref="TodoList.Today"/>).</summary>
    public DateOnly? Day { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>When the item was ticked; null while it is still to do.</summary>
    public DateTime? DoneAt { get; set; }

    /// <summary>Optional widget linked to the item, e.g. a checklist.</summary>
    public TodoWidget? Widget { get; set; }

    [JsonIgnore]
    public bool IsDone => DoneAt is not null;
}

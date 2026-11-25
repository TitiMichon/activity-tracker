namespace ActivityTracker.Core.Models;

/// <summary>The kinds of widget a to-do can carry. New kinds can be added without changing the to-do itself.</summary>
public enum WidgetKind
{
    /// <summary>A list of items to tick, e.g. the shopping list of "Do the groceries".</summary>
    Checklist,
}

/// <summary>A configurable widget attached to a to-do item.</summary>
public sealed class TodoWidget
{
    public WidgetKind Kind { get; set; } = WidgetKind.Checklist;

    /// <summary>Items of a <see cref="WidgetKind.Checklist"/>.</summary>
    public List<ChecklistItem> Items { get; set; } = [];

    /// <summary>When every item is ticked the to-do is done; unticking or adding an item reopens it.</summary>
    public bool CompletesTodo { get; set; } = true;
}

public sealed class ChecklistItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Text { get; set; } = "";

    public bool Done { get; set; }
}

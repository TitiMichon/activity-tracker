using ActivityTracker.Core.Models;

namespace ActivityTracker.Core.Services;

public sealed partial class TrackerService
{
    /// <summary>Attaches a widget to a to-do (keeps the existing one if it has one).</summary>
    public async Task<TodoWidget> AddWidgetAsync(Guid todoId, WidgetKind kind)
    {
        var todo = RequireTodo(todoId);
        todo.Widget ??= new TodoWidget { Kind = kind };
        await PersistAsync();
        return todo.Widget;
    }

    public async Task RemoveWidgetAsync(Guid todoId)
    {
        RequireTodo(todoId).Widget = null;
        await PersistAsync();
    }

    public async Task SetWidgetCompletesTodoAsync(Guid todoId, bool completes)
    {
        var todo = RequireTodo(todoId);
        RequireChecklist(todo).CompletesTodo = completes;
        SyncWithChecklist(todo);
        await PersistAsync();
    }

    /// <exception cref="ArgumentException">The text is empty or too long.</exception>
    public async Task<ChecklistItem> AddChecklistItemAsync(Guid todoId, string text)
    {
        var todo = RequireTodo(todoId);
        var item = new ChecklistItem { Text = TodoLists.CleanTitle(text, "An item") };
        RequireChecklist(todo).Items.Add(item);
        SyncWithChecklist(todo);
        await PersistAsync();
        return item;
    }

    /// <summary>Ticks or unticks an item. Returns true when this ticked the last item and so completed the to-do.</summary>
    public async Task<bool> SetChecklistItemDoneAsync(Guid todoId, Guid itemId, bool done)
    {
        var todo = RequireTodo(todoId);
        var item = RequireChecklist(todo).Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new KeyNotFoundException($"Checklist item {itemId} not found.");
        var wasDone = todo.IsDone;
        item.Done = done;
        SyncWithChecklist(todo);
        await PersistAsync();
        return !wasDone && todo.IsDone;
    }

    public async Task DeleteChecklistItemAsync(Guid todoId, Guid itemId)
    {
        var todo = RequireTodo(todoId);
        RequireChecklist(todo).Items.RemoveAll(i => i.Id == itemId);
        SyncWithChecklist(todo);
        await PersistAsync();
    }

    /// <summary>Unticks every item, e.g. to reuse a shopping list.</summary>
    public async Task UntickChecklistAsync(Guid todoId)
    {
        var todo = RequireTodo(todoId);
        RequireChecklist(todo).Items.ForEach(i => i.Done = false);
        SyncWithChecklist(todo);
        await PersistAsync();
    }

    public async Task RemoveTickedChecklistItemsAsync(Guid todoId)
    {
        var todo = RequireTodo(todoId);
        RequireChecklist(todo).Items.RemoveAll(i => i.Done);
        SyncWithChecklist(todo);
        await PersistAsync();
    }

    private static TodoWidget RequireChecklist(TodoItem todo) =>
        todo.Widget is { Kind: WidgetKind.Checklist } widget
            ? widget
            : throw new InvalidOperationException($"“{todo.Title}” has no checklist.");

    /// <summary>With <see cref="TodoWidget.CompletesTodo"/>, the to-do is done exactly when every item is ticked.</summary>
    private void SyncWithChecklist(TodoItem todo)
    {
        if (todo.Widget is not { CompletesTodo: true, Items.Count: > 0 } widget)
        {
            return;
        }

        var allTicked = widget.Items.TrueForAll(i => i.Done);
        if (allTicked && !todo.IsDone)
        {
            todo.DoneAt = Now;
        }
        else if (!allTicked && todo.IsDone)
        {
            todo.DoneAt = null;
            if (todo.List == TodoList.Today)
            {
                todo.Day = Today;
            }
        }
    }
}

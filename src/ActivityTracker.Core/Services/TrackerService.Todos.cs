using ActivityTracker.Core.Models;

namespace ActivityTracker.Core.Services;

public sealed partial class TrackerService
{
    public IReadOnlyList<TodoItem> Todos => _data.Todos;

    public IReadOnlyList<TodoItem> TodayTodos() => TodoLists.ForToday(_data.Todos, Today);

    public IReadOnlyList<TodoItem> LongTermTodos() => TodoLists.LongTerm(_data.Todos, Today);

    public TodoItem? GetTodo(Guid id) => _data.Todos.FirstOrDefault(t => t.Id == id);

    /// <exception cref="ArgumentException">The title is empty or too long.</exception>
    public async Task<TodoItem> AddTodoAsync(string title, TodoList list)
    {
        var todo = new TodoItem
        {
            Title = TodoLists.CleanTitle(title),
            List = list,
            Day = list == TodoList.Today ? Today : null,
            CreatedAt = Now,
        };
        _data.Todos.Add(todo);
        await PersistAsync();
        return todo;
    }

    /// <summary>Adds back or replaces a to-do (used to undo a deletion).</summary>
    public async Task SaveTodoAsync(TodoItem todo)
    {
        todo.Title = TodoLists.CleanTitle(todo.Title);
        var index = _data.Todos.FindIndex(t => t.Id == todo.Id);
        if (index >= 0)
        {
            _data.Todos[index] = todo;
        }
        else
        {
            _data.Todos.Add(todo);
        }

        await PersistAsync();
    }

    public async Task SetTodoDoneAsync(Guid id, bool done)
    {
        var todo = RequireTodo(id);
        if (todo.IsDone == done)
        {
            return;
        }

        todo.DoneAt = done ? Now : null;
        if (!done && todo.List == TodoList.Today)
        {
            // Ticked on an earlier day and reopened: it is to do today again.
            todo.Day = Today;
        }

        await PersistAsync();
    }

    /// <exception cref="ArgumentException">The title is empty or too long.</exception>
    public async Task RenameTodoAsync(Guid id, string title)
    {
        RequireTodo(id).Title = TodoLists.CleanTitle(title);
        await PersistAsync();
    }

    /// <summary>Moves a to-do to the other list; moving to "today" plans it for today.</summary>
    public async Task MoveTodoAsync(Guid id, TodoList list)
    {
        var todo = RequireTodo(id);
        todo.List = list;
        todo.Day = list == TodoList.Today ? Today : null;
        await PersistAsync();
    }

    public async Task DeleteTodoAsync(Guid id)
    {
        if (_data.Todos.RemoveAll(t => t.Id == id) > 0)
        {
            await PersistAsync();
        }
    }

    private TodoItem RequireTodo(Guid id) => GetTodo(id) ?? throw new KeyNotFoundException($"To-do {id} not found.");
}

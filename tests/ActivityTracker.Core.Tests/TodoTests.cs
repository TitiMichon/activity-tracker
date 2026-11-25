using ActivityTracker.Core.Models;
using ActivityTracker.Core.Persistence;
using ActivityTracker.Core.Services;
using Microsoft.Extensions.Time.Testing;
using static ActivityTracker.Core.Tests.TestData;

namespace ActivityTracker.Core.Tests;

public class TodoListsTests
{
    private static TodoItem Todo(string title, TodoList list = TodoList.Today, DateOnly? day = null, DateTime? doneAt = null, int createdHour = 8) => new()
    {
        Title = title,
        List = list,
        Day = list == TodoList.Today ? day ?? Monday : null,
        CreatedAt = (day ?? Monday).ToDateTime(new TimeOnly(createdHour, 0)),
        DoneAt = doneAt,
    };

    [Fact]
    public void Today_shows_items_of_the_day_and_unfinished_ones_from_earlier_days()
    {
        var todos = new[]
        {
            Todo("today"),
            Todo("left from friday", day: Monday.AddDays(-3)),
            Todo("done friday", day: Monday.AddDays(-3), doneAt: Monday.AddDays(-3).ToDateTime(new TimeOnly(18, 0))),
            Todo("tomorrow", day: Monday.AddDays(1)),
            Todo("some day", TodoList.LongTerm),
        };

        var titles = TodoLists.ForToday(todos, Monday).Select(t => t.Title);

        Assert.Equal(["left from friday", "today"], titles);
    }

    [Fact]
    public void Items_ticked_today_stay_visible_after_the_open_ones()
    {
        var todos = new[]
        {
            Todo("done at 9", doneAt: Monday.ToDateTime(new TimeOnly(9, 0))),
            Todo("open", createdHour: 10),
            Todo("old one done today", day: Monday.AddDays(-2), doneAt: Monday.ToDateTime(new TimeOnly(8, 0))),
        };

        var titles = TodoLists.ForToday(todos, Monday).Select(t => t.Title);

        Assert.Equal(["open", "old one done today", "done at 9"], titles);
    }

    [Fact]
    public void Long_term_hides_items_done_on_earlier_days()
    {
        var todos = new[]
        {
            Todo("paint the fence", TodoList.LongTerm),
            Todo("done today", TodoList.LongTerm, doneAt: Monday.ToDateTime(new TimeOnly(9, 0))),
            Todo("done last week", TodoList.LongTerm, doneAt: Monday.AddDays(-7).ToDateTime(new TimeOnly(9, 0))),
            Todo("for today"),
        };

        var titles = TodoLists.LongTerm(todos, Monday).Select(t => t.Title);

        Assert.Equal(["paint the fence", "done today"], titles);
    }

    [Fact]
    public void Only_unfinished_items_from_earlier_days_are_carried_over()
    {
        Assert.True(TodoLists.IsCarriedOver(Todo("old", day: Monday.AddDays(-1)), Monday));
        Assert.False(TodoLists.IsCarriedOver(Todo("new"), Monday));
        Assert.False(TodoLists.IsCarriedOver(Todo("old done", day: Monday.AddDays(-1), doneAt: Monday.ToDateTime(TimeOnly.MinValue)), Monday));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Title_is_required(string? title) =>
        Assert.Throws<ArgumentException>(() => TodoLists.CleanTitle(title));

    [Fact]
    public void Title_is_trimmed_and_limited()
    {
        Assert.Equal("Buy milk", TodoLists.CleanTitle("  Buy milk "));
        Assert.Throws<ArgumentException>(() => TodoLists.CleanTitle(new string('a', TodoItem.MaxTitleLength + 1)));
    }
}

public class TrackerTodoTests
{
    private readonly InMemoryTrackerStore _store = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 16, 9, 30, 0, TimeSpan.Zero));
    private readonly TrackerService _sut;

    public TrackerTodoTests()
    {
        _time.SetLocalTimeZone(TimeZoneInfo.Utc);
        _sut = new TrackerService(_store, _time);
        _sut.EnsureLoadedAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task Added_todos_are_planned_for_today_or_long_term_and_persisted()
    {
        var milk = await _sut.AddTodoAsync(" Buy milk ", TodoList.Today);
        var fence = await _sut.AddTodoAsync("Paint the fence", TodoList.LongTerm);

        Assert.Equal("Buy milk", milk.Title);
        Assert.Equal(Monday, milk.Day);
        Assert.Null(fence.Day);
        Assert.Equal([milk], _sut.TodayTodos());
        Assert.Equal([fence], _sut.LongTermTodos());

        var reloaded = new TrackerService(_store, _time);
        await reloaded.EnsureLoadedAsync();
        Assert.Equal(["Buy milk", "Paint the fence"], reloaded.Todos.Select(t => t.Title));
    }

    [Fact]
    public async Task Empty_title_is_rejected_without_saving()
    {
        var saves = _store.SaveCount;

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.AddTodoAsync(" ", TodoList.Today));
        Assert.Equal(saves, _store.SaveCount);
    }

    [Fact]
    public async Task Unfinished_todos_carry_over_to_the_next_day()
    {
        var call = await _sut.AddTodoAsync("Call the bank", TodoList.Today);
        var done = await _sut.AddTodoAsync("Water the plants", TodoList.Today);
        await _sut.SetTodoDoneAsync(done.Id, true);

        _time.Advance(TimeSpan.FromDays(1));

        Assert.Equal([call], _sut.TodayTodos());
        Assert.True(TodoLists.IsCarriedOver(call, _sut.Today));
    }

    [Fact]
    public async Task Ticking_and_unticking_records_when_it_was_done()
    {
        var todo = await _sut.AddTodoAsync("Call the bank", TodoList.Today);

        await _sut.SetTodoDoneAsync(todo.Id, true);
        Assert.Equal(_sut.Now, todo.DoneAt);

        _time.Advance(TimeSpan.FromDays(2));
        await _sut.SetTodoDoneAsync(todo.Id, false);
        Assert.False(todo.IsDone);
        Assert.Equal(_sut.Today, todo.Day);
    }

    [Fact]
    public async Task Moving_between_lists_sets_or_clears_the_day()
    {
        var todo = await _sut.AddTodoAsync("Paint the fence", TodoList.LongTerm);

        await _sut.MoveTodoAsync(todo.Id, TodoList.Today);
        Assert.Equal((TodoList.Today, (DateOnly?)Monday), (todo.List, todo.Day));

        await _sut.MoveTodoAsync(todo.Id, TodoList.LongTerm);
        Assert.Equal((TodoList.LongTerm, (DateOnly?)null), (todo.List, todo.Day));
    }

    [Fact]
    public async Task Rename_delete_and_restore()
    {
        var todo = await _sut.AddTodoAsync("Bank", TodoList.Today);

        await _sut.RenameTodoAsync(todo.Id, "Call the bank");
        Assert.Equal("Call the bank", _sut.GetTodo(todo.Id)!.Title);

        await _sut.DeleteTodoAsync(todo.Id);
        Assert.Empty(_sut.Todos);

        await _sut.SaveTodoAsync(todo);
        Assert.Equal([todo], _sut.Todos);
    }

    [Fact]
    public async Task Todos_are_exported_imported_and_cleared_with_the_rest()
    {
        await _sut.AddTodoAsync("Buy milk", TodoList.Today);
        var json = _sut.Export();

        await _sut.ClearAsync();
        Assert.Empty(_sut.Todos);

        await _sut.ImportAsync(json);
        Assert.Equal("Buy milk", Assert.Single(_sut.Todos).Title);
    }

    [Fact]
    public void Data_saved_before_todos_existed_loads_with_an_empty_list()
    {
        Assert.Empty(TrackerJson.Deserialize("""{"schemaVersion":1,"activities":[],"entries":[]}""").Todos);
        Assert.Empty(TrackerJson.Deserialize("""{"schemaVersion":1,"todos":null}""").Todos);
    }
}

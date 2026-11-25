using ActivityTracker.Core.Models;
using ActivityTracker.Core.Persistence;
using ActivityTracker.Core.Services;
using Microsoft.Extensions.Time.Testing;

namespace ActivityTracker.Core.Tests;

public class TodoWidgetTests
{
    private readonly InMemoryTrackerStore _store = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 16, 9, 30, 0, TimeSpan.Zero));
    private readonly TrackerService _sut;
    private readonly TodoItem _groceries;

    public TodoWidgetTests()
    {
        _time.SetLocalTimeZone(TimeZoneInfo.Utc);
        _sut = new TrackerService(_store, _time);
        _sut.EnsureLoadedAsync().GetAwaiter().GetResult();
        _groceries = _sut.AddTodoAsync("Do the groceries", TodoList.Today).GetAwaiter().GetResult();
    }

    private async Task<(ChecklistItem Milk, ChecklistItem Eggs)> ShoppingListAsync()
    {
        await _sut.AddWidgetAsync(_groceries.Id, WidgetKind.Checklist);
        return (await _sut.AddChecklistItemAsync(_groceries.Id, " Milk "), await _sut.AddChecklistItemAsync(_groceries.Id, "Eggs"));
    }

    [Fact]
    public async Task A_todo_gets_a_checklist_that_is_saved_with_it()
    {
        var (milk, _) = await ShoppingListAsync();

        Assert.Equal("Milk", milk.Text);
        var reloaded = new TrackerService(_store, _time);
        await reloaded.EnsureLoadedAsync();
        var widget = reloaded.GetTodo(_groceries.Id)!.Widget!;
        Assert.Equal(WidgetKind.Checklist, widget.Kind);
        Assert.Equal(["Milk", "Eggs"], widget.Items.Select(i => i.Text));
        Assert.True(widget.CompletesTodo);
    }

    [Fact]
    public async Task Adding_a_widget_twice_keeps_the_first_one()
    {
        var (milk, _) = await ShoppingListAsync();

        await _sut.AddWidgetAsync(_groceries.Id, WidgetKind.Checklist);

        Assert.Contains(milk, _groceries.Widget!.Items);
    }

    [Fact]
    public async Task Ticking_the_last_item_completes_the_todo()
    {
        var (milk, eggs) = await ShoppingListAsync();

        Assert.False(await _sut.SetChecklistItemDoneAsync(_groceries.Id, milk.Id, true));
        Assert.False(_groceries.IsDone);

        Assert.True(await _sut.SetChecklistItemDoneAsync(_groceries.Id, eggs.Id, true));
        Assert.True(_groceries.IsDone);
    }

    [Fact]
    public async Task Unticking_or_adding_an_item_reopens_the_todo()
    {
        var (milk, eggs) = await ShoppingListAsync();
        await _sut.SetChecklistItemDoneAsync(_groceries.Id, milk.Id, true);
        await _sut.SetChecklistItemDoneAsync(_groceries.Id, eggs.Id, true);

        await _sut.SetChecklistItemDoneAsync(_groceries.Id, eggs.Id, false);
        Assert.False(_groceries.IsDone);

        await _sut.SetChecklistItemDoneAsync(_groceries.Id, eggs.Id, true);
        await _sut.AddChecklistItemAsync(_groceries.Id, "Bread");
        Assert.False(_groceries.IsDone);
    }

    [Fact]
    public async Task Deleting_the_last_unticked_item_completes_the_todo()
    {
        var (milk, eggs) = await ShoppingListAsync();
        await _sut.SetChecklistItemDoneAsync(_groceries.Id, milk.Id, true);

        await _sut.DeleteChecklistItemAsync(_groceries.Id, eggs.Id);

        Assert.True(_groceries.IsDone);
    }

    [Fact]
    public async Task Without_auto_completion_the_todo_is_ticked_by_hand()
    {
        var (milk, eggs) = await ShoppingListAsync();
        await _sut.SetWidgetCompletesTodoAsync(_groceries.Id, false);

        Assert.False(await _sut.SetChecklistItemDoneAsync(_groceries.Id, milk.Id, true));
        Assert.False(await _sut.SetChecklistItemDoneAsync(_groceries.Id, eggs.Id, true));
        Assert.False(_groceries.IsDone);

        await _sut.SetWidgetCompletesTodoAsync(_groceries.Id, true);
        Assert.True(_groceries.IsDone);
    }

    [Fact]
    public async Task Untick_all_and_remove_ticked_items()
    {
        var (milk, eggs) = await ShoppingListAsync();
        await _sut.SetChecklistItemDoneAsync(_groceries.Id, milk.Id, true);

        await _sut.RemoveTickedChecklistItemsAsync(_groceries.Id);
        Assert.Equal([eggs], _groceries.Widget!.Items);

        await _sut.SetChecklistItemDoneAsync(_groceries.Id, eggs.Id, true);
        Assert.True(_groceries.IsDone);
        await _sut.UntickChecklistAsync(_groceries.Id);
        Assert.False(eggs.Done);
        Assert.False(_groceries.IsDone);
    }

    [Fact]
    public async Task Removing_the_widget_keeps_the_todo()
    {
        await ShoppingListAsync();

        await _sut.RemoveWidgetAsync(_groceries.Id);

        Assert.Null(_sut.GetTodo(_groceries.Id)!.Widget);
    }

    [Fact]
    public async Task Checklist_operations_need_a_checklist_and_valid_text()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.AddChecklistItemAsync(_groceries.Id, "Milk"));

        await _sut.AddWidgetAsync(_groceries.Id, WidgetKind.Checklist);
        var error = await Assert.ThrowsAsync<ArgumentException>(() => _sut.AddChecklistItemAsync(_groceries.Id, " "));
        Assert.StartsWith("An item cannot be empty.", error.Message);
    }

    [Fact]
    public void Todos_without_a_widget_are_not_written_with_one()
    {
        Assert.DoesNotContain("widget", _sut.Export());
    }
}

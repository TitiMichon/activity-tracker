using ActivityTracker.Core.Models;
using ActivityTracker.UI.Pages;
using ActivityTracker.UI.Services;
using AngleSharp.Dom;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace ActivityTracker.Web.Tests;

public class TodoWidgetComponentTests : WebTestBase
{
    private TodoItem Groceries() => Sync(Tracker.AddTodoAsync("Do the groceries", TodoList.Today));

    private TodoItem GroceriesWithList(params string[] items)
    {
        var todo = Groceries();
        Sync(Tracker.AddWidgetAsync(todo.Id, WidgetKind.Checklist));
        foreach (var item in items)
        {
            Sync(Tracker.AddChecklistItemAsync(todo.Id, item));
        }

        return todo;
    }

    private static IElement Checklist(IRenderedComponent<Home> cut) => cut.Find("[data-testid=checklist]");

    private static void AddItem(IRenderedComponent<Home> cut, string text)
    {
        cut.Find("[data-testid=checklist-input]").Input(text);
        cut.Find("[data-testid=checklist] form").Submit();
    }

    [Fact]
    public void A_todo_turns_into_a_checklist_from_its_menu()
    {
        var todo = Groceries();
        var cut = Render<Home>();

        cut.Find(".todo-title").Click();
        cut.Find("[data-testid=add-widget-checklist]").Click();

        Assert.Equal(WidgetKind.Checklist, Tracker.GetTodo(todo.Id)!.Widget!.Kind);
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[data-testid=todo-edit]")));
        Assert.Equal("true", cut.Find("[data-testid=widget-toggle]").GetAttribute("aria-expanded"));

        AddItem(cut, "Milk");
        AddItem(cut, "Eggs");

        Assert.Equal(["Milk", "Eggs"], Tracker.GetTodo(todo.Id)!.Widget!.Items.Select(i => i.Text));
        cut.WaitForAssertion(() => Assert.Equal("☑ 0/2", cut.Find("[data-testid=widget-toggle]").TextContent.Trim()));
        Assert.Equal("", cut.Find("[data-testid=checklist-input]").GetAttribute("value"));
    }

    [Fact]
    public void The_chip_shows_progress_and_folds_the_checklist()
    {
        var todo = GroceriesWithList("Milk", "Eggs");
        Sync(Tracker.SetChecklistItemDoneAsync(todo.Id, todo.Widget!.Items[0].Id, true));
        var cut = Render<Home>();

        var chip = cut.Find("[data-testid=widget-toggle]");
        Assert.Equal("☑ 1/2", chip.TextContent.Trim());
        Assert.Empty(cut.FindAll("[data-testid=checklist]"));

        chip.Click();
        Assert.Equal(2, cut.FindAll("[data-testid=checklist-item]").Count);
        Assert.Contains("done", cut.Find("[data-testid=checklist-item]").ClassName);

        cut.Find("[data-testid=widget-toggle]").Click();
        Assert.Empty(cut.FindAll("[data-testid=checklist]"));
    }

    [Fact]
    public void Ticking_every_item_completes_the_todo_with_a_message()
    {
        var todo = GroceriesWithList("Milk", "Eggs");
        var cut = Render<Home>();
        cut.Find("[data-testid=widget-toggle]").Click();

        cut.Find("[aria-label='Checklist of Do the groceries'] input[type=checkbox]").Change(true);
        Assert.False(Tracker.GetTodo(todo.Id)!.IsDone);
        cut.FindAll("[data-testid=checklist-item] input[type=checkbox]")[1].Change(true);

        Assert.True(Tracker.GetTodo(todo.Id)!.IsDone);
        Assert.Equal("All ticked: “Do the groceries” is done ✓", Services.GetRequiredService<ToastService>().Current!.Message);
        cut.WaitForAssertion(() => Assert.True(cut.Find("[aria-label='Done: Do the groceries']").HasAttribute("checked")));
    }

    [Fact]
    public void Items_can_be_removed()
    {
        var todo = GroceriesWithList("Milk", "Eggs");
        var cut = Render<Home>();
        cut.Find("[data-testid=widget-toggle]").Click();

        cut.Find("[aria-label='Remove Milk']").Click();

        Assert.Equal(["Eggs"], Tracker.GetTodo(todo.Id)!.Widget!.Items.Select(i => i.Text));
    }

    [Fact]
    public void Settings_turn_off_auto_completion_and_reset_the_list()
    {
        var todo = GroceriesWithList("Milk", "Eggs");
        Sync(Tracker.SetChecklistItemDoneAsync(todo.Id, todo.Widget!.Items[0].Id, true));
        var cut = Render<Home>();
        cut.Find("[data-testid=widget-toggle]").Click();
        cut.Find("[data-testid=widget-settings-toggle]").Click();

        Assert.Contains("Tick “Do the groceries” when every item is ticked", cut.Find("[data-testid=widget-settings]").TextContent);
        cut.Find("[data-testid=widget-completes]").Change(false);
        Assert.False(todo.Widget.CompletesTodo);

        cut.Find("[data-testid=untick-all]").Click();
        Assert.DoesNotContain(todo.Widget.Items, i => i.Done);
        cut.WaitForAssertion(() => Assert.True(cut.Find("[data-testid=remove-ticked]").HasAttribute("disabled")));
    }

    [Fact]
    public void Remove_ticked_items_keeps_the_rest()
    {
        var todo = GroceriesWithList("Milk", "Eggs");
        Sync(Tracker.SetChecklistItemDoneAsync(todo.Id, todo.Widget!.Items[0].Id, true));
        var cut = Render<Home>();
        cut.Find("[data-testid=widget-toggle]").Click();
        cut.Find("[data-testid=widget-settings-toggle]").Click();

        cut.Find("[data-testid=remove-ticked]").Click();

        Assert.Equal(["Eggs"], todo.Widget.Items.Select(i => i.Text));
    }

    [Fact]
    public void Removing_the_checklist_needs_a_confirmation()
    {
        var todo = GroceriesWithList("Milk");
        var cut = Render<Home>();
        cut.Find("[data-testid=widget-toggle]").Click();
        cut.Find("[data-testid=widget-settings-toggle]").Click();

        var remove = cut.Find("[data-testid=widget-settings] button.ghost");
        remove.Click();
        Assert.NotNull(Tracker.GetTodo(todo.Id)!.Widget);
        cut.Find("[data-testid=widget-settings] button.danger").Click();

        Assert.Null(Tracker.GetTodo(todo.Id)!.Widget);
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[data-testid=widget-toggle]")));
    }

    [Fact]
    public void A_todo_with_a_widget_offers_no_second_one()
    {
        GroceriesWithList();
        var cut = Render<Home>();

        cut.Find(".todo-title").Click();

        Assert.Empty(cut.FindAll("[data-testid=add-widget-checklist]"));
    }
}

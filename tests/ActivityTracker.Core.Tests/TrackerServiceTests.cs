using ActivityTracker.Core.Models;
using ActivityTracker.Core.Persistence;
using ActivityTracker.Core.Services;
using Microsoft.Extensions.Time.Testing;
using static ActivityTracker.Core.Tests.TestData;

namespace ActivityTracker.Core.Tests;

public class TrackerServiceTests
{
    private readonly InMemoryTrackerStore _store = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 16, 9, 30, 0, TimeSpan.Zero));
    private readonly TrackerService _sut;

    public TrackerServiceTests()
    {
        _time.SetLocalTimeZone(TimeZoneInfo.Utc);
        _sut = new TrackerService(_store, _time);
    }

    [Fact]
    public async Task Starts_empty_and_today_comes_from_time_provider()
    {
        await _sut.EnsureLoadedAsync();
        Assert.Empty(_sut.Activities);
        Assert.Equal(Monday, _sut.Today);
    }

    [Fact]
    public async Task Saving_invalid_activity_throws_and_persists_nothing()
    {
        await _sut.EnsureLoadedAsync();
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.SaveActivityAsync(new Activity { Name = " " }));
        Assert.Equal(0, _store.SaveCount);
    }

    [Fact]
    public async Task Saved_activity_is_persisted_and_reloaded()
    {
        await _sut.EnsureLoadedAsync();
        await _sut.SaveActivityAsync(ActivityTemplates.Create("mood", Monday));

        var reloaded = new TrackerService(_store, _time);
        await reloaded.EnsureLoadedAsync();

        var mood = Assert.Single(reloaded.Activities);
        Assert.Equal("Mood", mood.Name);
        Assert.Equal(ActivityKind.Scale, mood.Kind);
        Assert.Equal(5, mood.Options.Count);
    }

    [Fact]
    public async Task Changed_event_fires_on_mutation()
    {
        await _sut.EnsureLoadedAsync();
        var count = 0;
        _sut.Changed += () => count++;
        await _sut.SaveActivityAsync(Check());
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Quick_log_quantity_uses_default_then_last_value()
    {
        await _sut.EnsureLoadedAsync();
        var a = Quantity();
        await _sut.SaveActivityAsync(a);

        var first = await _sut.LogAsync(a.Id);
        Assert.Equal(1, first.Value); // no default, no history

        _time.Advance(TimeSpan.FromMinutes(1));
        await _sut.LogAsync(a.Id, 45);
        _time.Advance(TimeSpan.FromMinutes(1));
        var third = await _sut.LogAsync(a.Id);
        Assert.Equal(45, third.Value);

        a.DefaultValue = 20;
        var fourth = await _sut.LogAsync(a.Id);
        Assert.Equal(20, fourth.Value);
    }

    [Fact]
    public async Task Quick_log_uses_current_time_and_trims_note()
    {
        await _sut.EnsureLoadedAsync();
        var a = Check();
        await _sut.SaveActivityAsync(a);

        var e = await _sut.LogAsync(a.Id, note: "  felt good ");

        Assert.Equal(new DateTime(2026, 11, 16, 9, 30, 0), e.Timestamp);
        Assert.Equal("felt good", e.Note);
        Assert.Null(e.Value);
        Assert.Single(_sut.EntriesOn(Monday));
    }

    [Fact]
    public async Task Scale_log_requires_valid_option()
    {
        await _sut.EnsureLoadedAsync();
        var mood = Mood();
        await _sut.SaveActivityAsync(mood);

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.LogAsync(mood.Id, optionId: "nope"));
        var e = await _sut.LogAsync(mood.Id, optionId: "great");
        Assert.Equal("great", e.OptionId);
    }

    [Fact]
    public async Task Deleting_activity_removes_its_entries()
    {
        await _sut.EnsureLoadedAsync();
        var a = Check();
        var b = Check("Other");
        await _sut.SaveActivityAsync(a);
        await _sut.SaveActivityAsync(b);
        await _sut.LogAsync(a.Id);
        await _sut.LogAsync(b.Id);

        await _sut.DeleteActivityAsync(a.Id);

        Assert.Single(_sut.Activities);
        Assert.All(_sut.Entries, e => Assert.Equal(b.Id, e.ActivityId));
    }

    [Fact]
    public async Task Quick_add_orders_by_most_recent_use_and_hides_archived()
    {
        await _sut.EnsureLoadedAsync();
        var a = Check("A");
        var b = Check("B");
        var c = Check("C");
        await _sut.SaveActivityAsync(a);
        await _sut.SaveActivityAsync(b);
        await _sut.SaveActivityAsync(c);
        await _sut.LogAsync(a.Id);
        _time.Advance(TimeSpan.FromHours(1));
        await _sut.LogAsync(c.Id);
        await _sut.SetArchivedAsync(b.Id, true);

        Assert.Equal(["C", "A"], _sut.QuickAddActivities().Select(x => x.Name));
    }

    [Fact]
    public async Task Move_activity_changes_order()
    {
        await _sut.EnsureLoadedAsync();
        var a = Check("A");
        var b = Check("B");
        await _sut.SaveActivityAsync(a);
        await _sut.SaveActivityAsync(b);

        await _sut.MoveActivityAsync(b.Id, -1);

        Assert.Equal(["B", "A"], _sut.Activities.Select(x => x.Name));
    }

    [Fact]
    public async Task Edit_and_delete_entry()
    {
        await _sut.EnsureLoadedAsync();
        var a = Quantity();
        await _sut.SaveActivityAsync(a);
        var e = await _sut.LogAsync(a.Id, 10);

        var edited = TrackerJson.Clone(e);
        edited.Value = 99;
        edited.Color = "#000000";
        await _sut.SaveEntryAsync(edited);
        Assert.Equal(99, Assert.Single(_sut.Entries).Value);

        await _sut.DeleteEntryAsync(e.Id);
        Assert.Empty(_sut.Entries);
    }

    [Fact]
    public async Task Export_then_import_round_trips()
    {
        await _sut.EnsureLoadedAsync();
        var a = Quantity();
        await _sut.SaveActivityAsync(a);
        await _sut.LogAsync(a.Id, 12);
        await _sut.SetFirstDayOfWeekAsync(DayOfWeek.Sunday);
        var json = _sut.Export();

        var other = new TrackerService(new InMemoryTrackerStore(), _time);
        await other.ImportAsync(json);

        Assert.Equal(DayOfWeek.Sunday, other.FirstDayOfWeek);
        Assert.Equal(12, Assert.Single(other.Entries).Value);
        Assert.Equal(a.Id, Assert.Single(other.Activities).Id);
    }

    [Fact]
    public async Task Clear_keeps_settings()
    {
        await _sut.EnsureLoadedAsync();
        await _sut.SetFirstDayOfWeekAsync(DayOfWeek.Sunday);
        await _sut.SaveActivityAsync(Check());
        await _sut.ClearAsync();
        Assert.Empty(_sut.Activities);
        Assert.Equal(DayOfWeek.Sunday, _sut.FirstDayOfWeek);
    }

    [Fact]
    public async Task Due_on_lists_scheduled_activities()
    {
        await _sut.EnsureLoadedAsync();
        await _sut.SaveActivityAsync(Check("Daily", Frequency.Daily));
        await _sut.SaveActivityAsync(Check("Free"));
        Assert.Equal(["Daily"], _sut.DueOn(Monday).Select(a => a.Name));
    }
}

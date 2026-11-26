using ActivityTracker.Core.Models;
using ActivityTracker.Core.Persistence;
using ActivityTracker.Core.Services;
using Microsoft.Extensions.Time.Testing;
using static ActivityTracker.Core.Tests.TestData;

namespace ActivityTracker.Core.Tests;

public class TaskServiceTests
{
    private readonly InMemoryTrackerStore _store = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 16, 9, 30, 0, TimeSpan.Zero));
    private readonly TrackerService _sut;

    public TaskServiceTests()
    {
        _time.SetLocalTimeZone(TimeZoneInfo.Utc);
        _sut = new TrackerService(_store, _time);
    }

    private async Task<TrackerService> ReloadAsync()
    {
        var reloaded = new TrackerService(_store, _time);
        await reloaded.EnsureLoadedAsync();
        return reloaded;
    }

    [Fact]
    public async Task Added_task_is_trimmed_dated_and_persisted()
    {
        await _sut.EnsureLoadedAsync();
        var task = await _sut.AddTaskAsync("  Repair the car ", dueDate: Monday.AddDays(5));

        var saved = Assert.Single((await ReloadAsync()).Tasks);
        Assert.Equal(task.Id, saved.Id);
        Assert.Equal("Repair the car", saved.Title);
        Assert.Null(saved.PlannedDay);
        Assert.Equal(Monday.AddDays(5), saved.DueDate);
        Assert.Equal(_sut.Now, saved.CreatedAt);
        Assert.False(saved.IsDone);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task A_task_needs_a_title(string title)
    {
        await _sut.EnsureLoadedAsync();
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.AddTaskAsync(title));
        Assert.Equal(0, _store.SaveCount);
    }

    [Fact]
    public async Task Titles_have_a_maximum_length()
    {
        await _sut.EnsureLoadedAsync();
        await _sut.AddTaskAsync(new string('a', TaskItem.MaxTitleLength));
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.AddTaskAsync(new string('a', TaskItem.MaxTitleLength + 1)));
    }

    [Fact]
    public async Task A_task_can_only_link_an_existing_activity()
    {
        await _sut.EnsureLoadedAsync();
        var music = Check("Music");
        await _sut.SaveActivityAsync(music);

        var linked = await _sut.AddTaskAsync("Learn to read notes", plannedDay: Monday, activityId: music.Id);

        Assert.Equal(music.Id, linked.ActivityId);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.AddTaskAsync("Orphan", activityId: Guid.NewGuid()));
    }

    [Fact]
    public async Task Ticking_records_when_and_unticking_reopens()
    {
        await _sut.EnsureLoadedAsync();
        var task = await _sut.AddTaskAsync("Get a present");

        await _sut.SetTaskDoneAsync(task.Id, true);
        Assert.Equal(_sut.Now, _sut.GetTask(task.Id)!.DoneAt);

        await _sut.SetTaskDoneAsync(task.Id, false);
        Assert.False(_sut.GetTask(task.Id)!.IsDone);
    }

    [Fact]
    public async Task Ticking_twice_keeps_the_first_time_and_saves_once()
    {
        await _sut.EnsureLoadedAsync();
        var task = await _sut.AddTaskAsync("Get a present");
        await _sut.SetTaskDoneAsync(task.Id, true);
        var doneAt = task.DoneAt;
        var saves = _store.SaveCount;

        _time.Advance(TimeSpan.FromHours(1));
        await _sut.SetTaskDoneAsync(task.Id, true);

        Assert.Equal(doneAt, _sut.GetTask(task.Id)!.DoneAt);
        Assert.Equal(saves, _store.SaveCount);
    }

    [Fact]
    public async Task Plan_due_date_and_rename()
    {
        await _sut.EnsureLoadedAsync();
        var task = await _sut.AddTaskAsync("Groceries");

        await _sut.PlanTaskAsync(task.Id, Monday);
        await _sut.SetTaskDueDateAsync(task.Id, Monday.AddDays(1));
        await _sut.RenameTaskAsync(task.Id, " Big groceries ");

        var saved = Assert.Single((await ReloadAsync()).Tasks);
        Assert.Equal(Monday, saved.PlannedDay);
        Assert.Equal(Monday.AddDays(1), saved.DueDate);
        Assert.Equal("Big groceries", saved.Title);

        await _sut.PlanTaskAsync(task.Id, null);
        await _sut.SetTaskDueDateAsync(task.Id, null);
        Assert.Null(_sut.GetTask(task.Id)!.PlannedDay);
        Assert.Null(_sut.GetTask(task.Id)!.DueDate);
    }

    [Fact]
    public async Task Unknown_task_throws()
    {
        await _sut.EnsureLoadedAsync();
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.SetTaskDoneAsync(Guid.NewGuid(), true));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.PlanTaskAsync(Guid.NewGuid(), Monday));
    }

    [Fact]
    public async Task Saving_cleans_subtasks_and_notes()
    {
        await _sut.EnsureLoadedAsync();
        var task = new TaskItem
        {
            Title = " Cleanup ",
            Notes = "   ",
            Subtasks = [new Subtask { Title = " Windows " }, new Subtask { Title = "Floor", Done = true }],
        };

        await _sut.SaveTaskAsync(task);

        var saved = Assert.Single((await ReloadAsync()).Tasks);
        Assert.Equal("Cleanup", saved.Title);
        Assert.Null(saved.Notes);
        Assert.Equal(["Windows", "Floor"], saved.Subtasks.Select(s => s.Title));
        Assert.True(saved.Subtasks[1].Done);
        Assert.Equal(_sut.Now, saved.CreatedAt);
    }

    [Fact]
    public async Task Saving_rejects_an_empty_subtask_and_persists_nothing()
    {
        await _sut.EnsureLoadedAsync();
        var task = new TaskItem { Title = "Cleanup", Subtasks = [new Subtask { Title = " " }] };

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.SaveTaskAsync(task));
        Assert.Empty(_sut.Tasks);
        Assert.Equal(0, _store.SaveCount);
    }

    [Fact]
    public async Task Deleted_task_can_be_restored_by_saving_it_back()
    {
        await _sut.EnsureLoadedAsync();
        var task = await _sut.AddTaskAsync("Change the window");

        await _sut.DeleteTaskAsync(task.Id);
        Assert.Empty(_sut.Tasks);

        await _sut.SaveTaskAsync(task);
        Assert.Equal(task.Id, Assert.Single(_sut.Tasks).Id);
    }

    [Fact]
    public async Task Deleting_an_activity_keeps_its_tasks_unlinked()
    {
        await _sut.EnsureLoadedAsync();
        var music = Check("Music");
        await _sut.SaveActivityAsync(music);
        var task = await _sut.AddTaskAsync("Learn a partition", activityId: music.Id);

        await _sut.DeleteActivityAsync(music.Id);

        Assert.Null(_sut.GetTask(task.Id)!.ActivityId);
    }

    [Fact]
    public async Task Tasks_travel_in_backups_and_clear_removes_them()
    {
        await _sut.EnsureLoadedAsync();
        await _sut.AddTaskAsync("Repair the car");
        var backup = _sut.Export();

        await _sut.ClearAsync();
        Assert.Empty(_sut.Tasks);

        await _sut.ImportAsync(backup);
        Assert.Equal("Repair the car", Assert.Single(_sut.Tasks).Title);
    }
}

public class TrackerMigrationTests
{
    private const string V1Backup = """
        {"schemaVersion":1,"settings":{"firstDayOfWeek":"Sunday"},
         "activities":[{"id":"6f9d0f5e-8a5b-4a3b-9d55-1b9f2b7c1a10","name":"Sport","schedule":{"frequency":"Daily"}}],
         "entries":[{"id":"0b6f3e44-3f1e-4a7c-9b52-7d8e2c1f0a11","activityId":"6f9d0f5e-8a5b-4a3b-9d55-1b9f2b7c1a10","timestamp":"2026-11-15T08:00:00"}]}
        """;

    [Fact]
    public void Every_schema_version_has_a_migration_step() =>
        Assert.Equal(TrackerData.CurrentSchemaVersion - 1, TrackerMigrations.StepCount);

    [Fact]
    public void A_v1_backup_upgrades_to_the_current_schema_with_no_tasks()
    {
        var data = TrackerJson.Deserialize(V1Backup);

        Assert.Equal(TrackerData.CurrentSchemaVersion, data.SchemaVersion);
        Assert.Equal(DayOfWeek.Sunday, data.Settings.FirstDayOfWeek);
        Assert.Equal("Sport", Assert.Single(data.Activities).Name);
        Assert.Single(data.Entries);
        Assert.Empty(data.Tasks);
    }

    [Fact]
    public void Data_without_a_version_is_read_as_v1()
    {
        var data = TrackerJson.Deserialize("""{"activities":[{"name":"Sport"}]}""");

        Assert.Equal(TrackerData.CurrentSchemaVersion, data.SchemaVersion);
        Assert.Single(data.Activities);
        Assert.Empty(data.Tasks);
    }

    [Fact]
    public void Upgraded_data_is_saved_with_the_current_version()
    {
        var json = TrackerJson.Serialize(TrackerJson.Deserialize(V1Backup));

        Assert.Contains($"\"schemaVersion\":{TrackerData.CurrentSchemaVersion}", json);
        Assert.Contains("\"tasks\":[]", json);
    }

    [Fact]
    public void Tasks_round_trip_with_their_dates_and_subtasks()
    {
        var task = new TaskItem
        {
            Title = "Cleanup",
            PlannedDay = Monday,
            DueDate = Monday.AddDays(2),
            ActivityId = Guid.NewGuid(),
            CreatedAt = new DateTime(2026, 11, 15, 20, 0, 0),
            Subtasks = [new Subtask { Title = "Windows", Done = true }],
        };

        var json = TrackerJson.Serialize(new TrackerData { Tasks = [task] });
        var back = Assert.Single(TrackerJson.Deserialize(json).Tasks);

        Assert.Contains("\"plannedDay\":\"2026-11-16\"", json);
        Assert.Equal(task.PlannedDay, back.PlannedDay);
        Assert.Equal(task.DueDate, back.DueDate);
        Assert.Equal(task.ActivityId, back.ActivityId);
        Assert.Equal(task.CreatedAt, back.CreatedAt);
        Assert.True(Assert.Single(back.Subtasks).Done);
    }

    [Fact]
    public void Null_lists_in_a_hand_edited_file_become_empty()
    {
        var data = TrackerJson.Deserialize("""{"schemaVersion":2,"activities":null,"entries":null,"tasks":[{"title":"A","subtasks":null}]}""");

        Assert.Empty(data.Activities);
        Assert.Empty(data.Entries);
        Assert.Empty(Assert.Single(data.Tasks).Subtasks);
    }

    [Theory]
    [InlineData("[1,2]")]
    [InlineData("\"text\"")]
    [InlineData("""{"schemaVersion":2,"tasks":3}""")]
    public void Json_that_is_not_tracker_data_is_rejected(string json) =>
        Assert.Throws<FormatException>(() => TrackerJson.Deserialize(json));
}

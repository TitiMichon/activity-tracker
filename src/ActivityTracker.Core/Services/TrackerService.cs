using ActivityTracker.Core.Models;
using ActivityTracker.Core.Persistence;

namespace ActivityTracker.Core.Services;

/// <summary>
/// Application state: activities, entries, tasks and settings. Every mutation is persisted
/// immediately and raises <see cref="Changed"/> so the UI can refresh.
/// </summary>
public sealed partial class TrackerService(ITrackerStore store, TimeProvider time)
{
    private TrackerData _data = new();
    private bool _loaded;

    public event Action? Changed;

    public bool IsLoaded => _loaded;

    public TrackerSettings Settings => _data.Settings;

    public DayOfWeek FirstDayOfWeek => _data.Settings.FirstDayOfWeek;

    public DateTime Now => time.GetLocalNow().DateTime;

    public DateOnly Today => DateOnly.FromDateTime(Now);

    public IReadOnlyList<Activity> Activities =>
        _data.Activities.OrderBy(a => a.SortOrder).ThenBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase).ToList();

    public IReadOnlyList<Activity> ActiveActivities => Activities.Where(a => !a.Archived).ToList();

    public IReadOnlyList<Entry> Entries => _data.Entries;

    public async Task EnsureLoadedAsync()
    {
        if (_loaded)
        {
            return;
        }

        _data = TrackerJson.Deserialize(await store.LoadAsync());
        _loaded = true;
        Changed?.Invoke();
    }

    public Activity? GetActivity(Guid id) => _data.Activities.FirstOrDefault(a => a.Id == id);

    // ---------- Activities ----------

    /// <summary>Adds or replaces an activity.</summary>
    /// <exception cref="ArgumentException">The activity is invalid.</exception>
    public async Task SaveActivityAsync(Activity activity)
    {
        var errors = ActivityValidator.Validate(activity);
        if (errors.Count > 0)
        {
            throw new ArgumentException(string.Join(" ", errors), nameof(activity));
        }

        activity.Name = activity.Name.Trim();
        var index = _data.Activities.FindIndex(a => a.Id == activity.Id);
        if (index >= 0)
        {
            _data.Activities[index] = activity;
        }
        else
        {
            activity.SortOrder = _data.Activities.Count == 0 ? 0 : _data.Activities.Max(a => a.SortOrder) + 1;
            _data.Activities.Add(activity);
        }

        await PersistAsync();
    }

    public async Task SetArchivedAsync(Guid id, bool archived)
    {
        var activity = GetActivity(id) ?? throw new KeyNotFoundException($"Activity {id} not found.");
        activity.Archived = archived;
        await PersistAsync();
    }

    /// <summary>Deletes an activity and all its entries; its tasks are kept, unlinked.</summary>
    public async Task DeleteActivityAsync(Guid id)
    {
        _data.Activities.RemoveAll(a => a.Id == id);
        _data.Entries.RemoveAll(e => e.ActivityId == id);
        UnlinkTasks(id);
        await PersistAsync();
    }

    /// <summary>Moves an activity one position up (-1) or down (+1) in the list.</summary>
    public async Task MoveActivityAsync(Guid id, int direction)
    {
        var ordered = Activities.ToList();
        var index = ordered.FindIndex(a => a.Id == id);
        var target = index + Math.Sign(direction);
        if (index < 0 || target < 0 || target >= ordered.Count)
        {
            return;
        }

        (ordered[index], ordered[target]) = (ordered[target], ordered[index]);
        for (var i = 0; i < ordered.Count; i++)
        {
            ordered[i].SortOrder = i;
        }

        await PersistAsync();
    }

    // ---------- Entries ----------

    /// <summary>
    /// Quick-add an entry. For Quantity activities a missing value falls back to the
    /// activity default, then to the last logged value, then to 1.
    /// </summary>
    public async Task<Entry> LogAsync(Guid activityId, double? value = null, string? optionId = null, DateTime? at = null, string? note = null)
    {
        var activity = GetActivity(activityId) ?? throw new KeyNotFoundException($"Activity {activityId} not found.");
        var entry = new Entry
        {
            ActivityId = activityId,
            Timestamp = at ?? Now,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
        };

        switch (activity.Kind)
        {
            case ActivityKind.Quantity:
                entry.Value = value ?? activity.DefaultValue ?? LastValue(activityId) ?? 1;
                break;
            case ActivityKind.Scale:
                if (activity.FindOption(optionId) is null)
                {
                    throw new ArgumentException($"Option '{optionId}' does not exist for {activity.Name}.", nameof(optionId));
                }

                entry.OptionId = optionId;
                break;
        }

        _data.Entries.Add(entry);
        await PersistAsync();
        return entry;
    }

    /// <summary>Adds or replaces an entry (used by the edit dialog).</summary>
    public async Task SaveEntryAsync(Entry entry)
    {
        if (GetActivity(entry.ActivityId) is null)
        {
            throw new KeyNotFoundException($"Activity {entry.ActivityId} not found.");
        }

        var index = _data.Entries.FindIndex(e => e.Id == entry.Id);
        if (index >= 0)
        {
            _data.Entries[index] = entry;
        }
        else
        {
            _data.Entries.Add(entry);
        }

        await PersistAsync();
    }

    public async Task DeleteEntryAsync(Guid id)
    {
        if (_data.Entries.RemoveAll(e => e.Id == id) > 0)
        {
            await PersistAsync();
        }
    }

    public IEnumerable<Entry> EntriesBetween(DateOnly from, DateOnly to, Guid? activityId = null) =>
        _data.Entries
            .Where(e => e.Date >= from && e.Date <= to && (activityId is null || e.ActivityId == activityId))
            .OrderBy(e => e.Timestamp);

    public IEnumerable<Entry> EntriesOn(DateOnly date, Guid? activityId = null) => EntriesBetween(date, date, activityId);

    public IEnumerable<Entry> EntriesFor(Guid activityId) => _data.Entries.Where(e => e.ActivityId == activityId);

    public double? LastValue(Guid activityId) =>
        _data.Entries.Where(e => e.ActivityId == activityId && e.Value is not null)
            .OrderByDescending(e => e.Timestamp)
            .FirstOrDefault()?.Value;

    /// <summary>Active activities, most recently used first (for the quick-add bar).</summary>
    public IReadOnlyList<Activity> QuickAddActivities()
    {
        var lastUse = _data.Entries
            .GroupBy(e => e.ActivityId)
            .ToDictionary(g => g.Key, g => g.Max(e => e.Timestamp));
        return ActiveActivities
            .OrderByDescending(a => lastUse.GetValueOrDefault(a.Id, DateTime.MinValue))
            .ThenBy(a => a.SortOrder)
            .ToList();
    }

    public IReadOnlyList<Activity> DueOn(DateOnly date) =>
        ActiveActivities.Where(a => ScheduleCalculator.IsDue(a, date)).ToList();

    /// <summary>Activities shown on the calendar: all but the ones marked "hide from calendar".</summary>
    public IReadOnlyList<Activity> CalendarActivities => Activities.Where(a => !a.HideFromCalendar).ToList();

    public DayProgress ProgressOn(Activity activity, DateOnly date) => DayProgress.Of(activity, EntriesOn(date, activity.Id), date);

    /// <summary>Activities whose reminder time has passed today and that are still not done.</summary>
    public IReadOnlyList<Activity> PendingReminders() => ReminderCalculator.Pending(Activities, _data.Entries, Now);

    // ---------- Settings & data ----------

    public async Task SetFirstDayOfWeekAsync(DayOfWeek day)
    {
        _data.Settings.FirstDayOfWeek = day;
        await PersistAsync();
    }

    public string Export() => TrackerJson.Serialize(_data);

    /// <summary>Replaces all data with an export. Throws <see cref="FormatException"/> on invalid data.</summary>
    public async Task ImportAsync(string json)
    {
        _data = TrackerJson.Deserialize(json);
        _loaded = true;
        await PersistAsync();
    }

    public async Task ClearAsync()
    {
        _data = new TrackerData { Settings = _data.Settings };
        await PersistAsync();
    }

    private async Task PersistAsync()
    {
        await store.SaveAsync(TrackerJson.Serialize(_data));
        Changed?.Invoke();
    }
}

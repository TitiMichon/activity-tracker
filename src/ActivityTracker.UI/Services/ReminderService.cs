using ActivityTracker.Core.Services;
using ActivityTracker.UI.Platform;

namespace ActivityTracker.UI.Services;

/// <summary>
/// Checks every 30 seconds for planned activities whose reminder time has passed and that are not done yet,
/// and shows an in-app toast plus a system notification (when allowed), once per activity and day.
/// In the browser this only works while the app is open. Native apps also schedule an OS alarm
/// (<see cref="IReminderScheduler"/>) that posts the system notifications even when the app is closed.
/// </summary>
public sealed class ReminderService(
    TrackerService tracker,
    ToastService toasts,
    INotificationPlatform notifications,
    IKeyValueStorage storage,
    IReminderScheduler scheduler,
    TimeProvider time) : IDisposable
{
    public const string StorageKey = "activity-tracker:reminded";
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);

    private readonly HashSet<Guid> _sent = [];
    private DateOnly _sentDay;
    private CancellationTokenSource? _loop;

    /// <summary>"granted", "denied", "default" or "unsupported".</summary>
    public async Task<string> PermissionAsync() => await SafeAsync(notifications.PermissionAsync) ?? "unsupported";

    public async Task<string> RequestPermissionAsync() => await SafeAsync(notifications.RequestPermissionAsync) ?? "unsupported";

    public void Start()
    {
        if (_loop is not null)
        {
            return;
        }

        _loop = new CancellationTokenSource();
        tracker.Changed += OnTrackerChanged;
        _ = ScheduleSafelyAsync();
        _ = RunAsync(_loop.Token);
    }

    /// <summary>
    /// Plans the next OS reminder check: right away when a reminder is due now (the background check skips the ones
    /// already notified today), otherwise at the next reminder time.
    /// </summary>
    public Task ScheduleAsync() =>
        scheduler.ScheduleAsync(tracker.PendingReminders().Count > 0
            ? tracker.Now
            : ReminderCalculator.Next(tracker.Activities, tracker.Entries, tracker.Now));

    /// <summary>Sends the reminders that are due now. Returns the activities that were reminded.</summary>
    public async Task<IReadOnlyList<Guid>> CheckAsync()
    {
        await LoadSentAsync();
        var sent = new List<Guid>();
        foreach (var activity in tracker.PendingReminders())
        {
            if (!_sent.Add(activity.Id))
            {
                continue;
            }

            var message = ReminderCalculator.Message(activity, tracker.Entries, tracker.Today);
            toasts.Show($"⏰ {message}", duration: TimeSpan.FromSeconds(10));
            if (!scheduler.NotifiesInBackground)
            {
                await SafeAsync(() => notifications.ShowAsync("Reminder", message));
            }

            sent.Add(activity.Id);
        }

        if (sent.Count > 0)
        {
            await SafeAsync(() => storage.SetAsync(StorageKey, ReminderLog.Format(_sentDay, _sent)));
        }

        return sent;
    }

    public void Dispose()
    {
        tracker.Changed -= OnTrackerChanged;
        _loop?.Cancel();
        _loop?.Dispose();
    }

    private async Task RunAsync(CancellationToken token)
    {
        using var timer = new PeriodicTimer(CheckInterval, time);
        do
        {
            await CheckSafelyAsync();
        }
        while (await timer.WaitForNextTickAsync(token));
    }

    // A reminder set for a time already passed (and not done) fires right away rather than at the next tick.
    private void OnTrackerChanged()
    {
        _ = CheckSafelyAsync();
        _ = ScheduleSafelyAsync();
    }

    private async Task ScheduleSafelyAsync()
    {
        try
        {
            await ScheduleAsync();
        }
        catch (Exception ex)
        {
            // Background reminders are best effort; the in-app check keeps working.
            Console.Error.WriteLine($"Reminder scheduling failed: {ex.Message}");
        }
    }

    private async Task CheckSafelyAsync()
    {
        try
        {
            await CheckAsync();
        }
        catch (Exception ex)
        {
            // A failed check must not stop future reminders.
            Console.Error.WriteLine($"Reminder check failed: {ex.Message}");
        }
    }

    /// <summary>Restores what was already sent today, so reloading the app does not repeat reminders.</summary>
    private async Task LoadSentAsync()
    {
        var today = tracker.Today;
        if (_sentDay == today)
        {
            return;
        }

        _sent.Clear();
        _sentDay = today;
        _sent.UnionWith(ReminderLog.Parse(await SafeAsync(() => storage.GetAsync(StorageKey)), today));
    }

    private static async Task<T?> SafeAsync<T>(Func<Task<T>> call)
    {
        try
        {
            return await call();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Notifications and storage are best effort.
            return default;
        }
    }

    private static async Task SafeAsync(Func<Task> call)
    {
        try
        {
            await call();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Notifications and storage are best effort.
        }
    }
}

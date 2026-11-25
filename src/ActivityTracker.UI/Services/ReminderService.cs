using ActivityTracker.Core.Services;
using ActivityTracker.UI.Platform;

namespace ActivityTracker.UI.Services;

/// <summary>
/// Checks every 30 seconds for planned activities whose reminder time has passed and that are not done yet,
/// and shows a system notification (when allowed) plus an in-app toast, once per activity and day.
/// Runs while the app is open (browser tab or installed app): there is no server to push notifications.
/// </summary>
public sealed class ReminderService(
    TrackerService tracker, ToastService toasts, INotificationPlatform notifications, IKeyValueStorage storage, TimeProvider time) : IDisposable
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
        _ = RunAsync(_loop.Token);
    }

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
            await SafeAsync(() => notifications.ShowAsync("Reminder", message));
            sent.Add(activity.Id);
        }

        if (sent.Count > 0)
        {
            await SafeAsync(() => storage.SetAsync(StorageKey, $"{_sentDay:yyyy-MM-dd}|{string.Join(',', _sent)}"));
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
    private void OnTrackerChanged() => _ = CheckSafelyAsync();

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
        var stored = await SafeAsync(() => storage.GetAsync(StorageKey));
        if (stored?.Split('|') is [var day, var ids] && day == today.ToString("yyyy-MM-dd"))
        {
            foreach (var id in ids.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (Guid.TryParse(id, out var guid))
                {
                    _sent.Add(guid);
                }
            }
        }
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

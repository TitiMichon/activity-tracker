namespace ActivityTracker.UI.Services;

/// <summary>A single transient message, optionally with an action such as "Undo".</summary>
public sealed class ToastService
{
    private int _nextId;

    public sealed record Toast(int Id, string Message, string? ActionLabel, Func<Task>? Action);

    public Toast? Current { get; private set; }

    public event Action? Changed;

    public void Show(string message, string? actionLabel = null, Func<Task>? action = null, TimeSpan? duration = null)
    {
        var toast = new Toast(++_nextId, message, actionLabel, action);
        Current = toast;
        Changed?.Invoke();
        _ = DismissLaterAsync(toast.Id, duration ?? TimeSpan.FromSeconds(5));
    }

    public async Task RunActionAsync()
    {
        var toast = Current;
        Dismiss();
        if (toast?.Action is { } action)
        {
            await action();
        }
    }

    public void Dismiss()
    {
        Current = null;
        Changed?.Invoke();
    }

    private async Task DismissLaterAsync(int id, TimeSpan duration)
    {
        await Task.Delay(duration);
        if (Current?.Id == id)
        {
            Dismiss();
        }
    }
}

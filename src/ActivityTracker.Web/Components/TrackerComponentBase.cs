using ActivityTracker.Core.Services;
using Microsoft.AspNetCore.Components;

namespace ActivityTracker.Web.Components;

/// <summary>Base for components that read tracker data and must re-render when it changes.</summary>
public abstract class TrackerComponentBase : ComponentBase, IDisposable
{
    [Inject]
    protected TrackerService Tracker { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        Tracker.Changed += OnTrackerChanged;
        await Tracker.EnsureLoadedAsync();
    }

    private void OnTrackerChanged() => InvokeAsync(StateHasChanged);

    public virtual void Dispose()
    {
        Tracker.Changed -= OnTrackerChanged;
        GC.SuppressFinalize(this);
    }
}

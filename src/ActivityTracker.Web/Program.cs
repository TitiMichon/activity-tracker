using ActivityTracker.Core.Persistence;
using ActivityTracker.Core.Services;
using ActivityTracker.UI;
using ActivityTracker.UI.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ITrackerStore, LocalStorageTrackerStore>();
builder.Services.AddSingleton<TrackerService>();
builder.Services.AddSingleton<ToastService>();
builder.Services.AddSingleton<ReminderService>();

await builder.Build().RunAsync();

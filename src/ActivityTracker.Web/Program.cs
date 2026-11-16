using ActivityTracker.Core.Persistence;
using ActivityTracker.Core.Services;
using ActivityTracker.Web;
using ActivityTracker.Web.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ITrackerStore, LocalStorageTrackerStore>();
builder.Services.AddSingleton<TrackerService>();
builder.Services.AddSingleton<ToastService>();

await builder.Build().RunAsync();

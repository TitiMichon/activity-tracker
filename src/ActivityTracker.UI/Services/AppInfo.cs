using System.Reflection;

namespace ActivityTracker.UI.Services;

public static class AppInfo
{
    /// <summary>Semantic version of the build, e.g. "1.0.0" (the "+commit" suffix is removed).</summary>
    public static string Version { get; } =
        (typeof(AppInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0")
        .Split('+')[0];
}

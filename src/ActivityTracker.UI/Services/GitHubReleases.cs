using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using ActivityTracker.Core.Services;

namespace ActivityTracker.UI.Services;

/// <summary>Where the published versions of the app come from.</summary>
public interface IReleaseSource
{
    /// <summary>Published versions, newest first.</summary>
    /// <exception cref="HttpRequestException">Offline, or GitHub refused the request.</exception>
    Task<IReadOnlyList<AppRelease>> GetReleasesAsync(CancellationToken cancellationToken = default);
}

/// <summary>Reads the public GitHub releases of the repository (no token: 60 requests per hour and per network).</summary>
public sealed class GitHubReleases(HttpClient http) : IReleaseSource
{
    public const string Repository = "TitiMichon/activity-tracker";

    public static HttpClient CreateClient()
    {
        var http = new HttpClient { BaseAddress = new Uri("https://api.github.com/"), Timeout = TimeSpan.FromSeconds(20) };
        http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ActivityTracker", AppInfo.Version));
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return http;
    }

    public async Task<IReadOnlyList<AppRelease>> GetReleasesAsync(CancellationToken cancellationToken = default)
    {
        using var response = await http.GetAsync($"repos/{Repository}/releases?per_page=30", cancellationToken);
        if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests)
        {
            throw new HttpRequestException("GitHub limits how often updates can be checked. Try again in an hour.", null, response.StatusCode);
        }

        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return Parse(json.RootElement);
    }

    /// <summary>Turns the GitHub "list releases" JSON into releases; drafts and tags that are not versions are skipped.</summary>
    public static IReadOnlyList<AppRelease> Parse(JsonElement releases)
    {
        var result = new List<AppRelease>();
        if (releases.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var r in releases.EnumerateArray())
        {
            if (Bool(r, "draft") || !SemVersion.TryParse(String(r, "tag_name"), out var version))
            {
                continue;
            }

            string? apk = null;
            if (r.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
            {
                apk = assets.EnumerateArray()
                    .Where(a => String(a, "name")?.EndsWith(".apk", StringComparison.OrdinalIgnoreCase) == true)
                    .Select(a => String(a, "browser_download_url"))
                    .FirstOrDefault(url => url?.StartsWith("https://", StringComparison.Ordinal) == true);
            }

            DateTimeOffset? published = r.TryGetProperty("published_at", out var p) && p.TryGetDateTimeOffset(out var at) ? at : null;
            result.Add(new AppRelease(version, String(r, "tag_name")!, Bool(r, "prerelease"), apk, String(r, "body"), published));
        }

        return result;
    }

    private static string? String(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static bool Bool(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.True;
}

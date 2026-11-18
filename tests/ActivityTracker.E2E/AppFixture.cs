using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace ActivityTracker.E2E;

/// <summary>
/// Serves the published app (folder given by <c>E2E_APP_DIR</c>, i.e. the publish "wwwroot")
/// on a random local port and starts a headless Chromium.
/// </summary>
public sealed class AppFixture : IAsyncLifetime
{
    public const string AppDirVariable = "E2E_APP_DIR";

    private WebApplication? _host;
    private IPlaywright? _playwright;

    public static string? AppDir => Environment.GetEnvironmentVariable(AppDirVariable);

    public string BaseUrl { get; private set; } = "";

    public IBrowser Browser { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        if (string.IsNullOrEmpty(AppDir))
        {
            return; // Tests are skipped, see E2EFactAttribute.
        }

        var root = Path.GetFullPath(AppDir);
        if (!File.Exists(Path.Combine(root, "index.html")))
        {
            throw new InvalidOperationException($"{AppDirVariable}='{root}' does not contain index.html. Point it to the published wwwroot folder.");
        }

        var files = new PhysicalFileProvider(root);
        var contentTypes = new FileExtensionContentTypeProvider();
        contentTypes.Mappings[".dat"] = "application/octet-stream";
        contentTypes.Mappings[".webmanifest"] = "application/manifest+json";

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        _host = builder.Build();
        _host.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files });
        _host.UseStaticFiles(new StaticFileOptions { FileProvider = files, ContentTypeProvider = contentTypes });
        _host.MapFallbackToFile("index.html", new StaticFileOptions { FileProvider = files });
        await _host.StartAsync();

        BaseUrl = _host.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First().TrimEnd('/') + "/";

        // Downloads Chromium the first time (cached afterwards).
        var exitCode = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Playwright browser install failed with exit code {exitCode}.");
        }

        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    }

    /// <summary>A fresh page with empty storage, at the home page with the app loaded.</summary>
    public async Task<IPage> NewPageAsync(string path = "")
    {
        var context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = BaseUrl,
            ServiceWorkers = ServiceWorkerPolicy.Block,
            ViewportSize = new ViewportSize { Width = 1280, Height = 900 },
        });
        context.SetDefaultTimeout(20_000);
        var page = await context.NewPageAsync();
        await page.GotoAsync(BaseUrl + path);
        await page.Locator("main.content h1").First.WaitForAsync();
        return page;
    }

    public async Task DisposeAsync()
    {
        if (Browser is not null)
        {
            await Browser.CloseAsync();
        }

        _playwright?.Dispose();
        if (_host is not null)
        {
            await _host.DisposeAsync();
        }
    }
}

[CollectionDefinition(Name)]
public sealed class E2ECollection : ICollectionFixture<AppFixture>
{
    public const string Name = "E2E";
}

/// <summary>A fact that only runs when the published app location is configured.</summary>
public sealed class E2EFactAttribute : FactAttribute
{
    public E2EFactAttribute()
    {
        if (string.IsNullOrEmpty(AppFixture.AppDir))
        {
            Skip = $"Set {AppFixture.AppDirVariable} to the published wwwroot to run end-to-end tests (see scripts/validate.ps1).";
        }
    }
}

using System.Net;
using System.Text.Json;
using ActivityTracker.Core.Services;
using ActivityTracker.UI.Components;
using ActivityTracker.UI.Platform;
using ActivityTracker.UI.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace ActivityTracker.Web.Tests;

public class UpdateServiceTests : WebTestBase
{
    private readonly UpdateService _sut;

    public UpdateServiceTests()
    {
        Updater.IsSupported = true;
        Services.AddSingleton<IKeyValueStorage>(new MemoryKeyValueStorage()); // the browser storage needs real JS
        _sut = Services.GetRequiredService<UpdateService>();
        _sut.CurrentVersion = "1.1.0";
    }

    private IKeyValueStorage Storage => Services.GetRequiredService<IKeyValueStorage>();

    [Fact]
    public async Task Start_finds_a_newer_release_and_remembers_the_check()
    {
        Releases.Add("v1.2.0");

        await _sut.StartAsync();

        Assert.Equal("v1.2.0", _sut.Available!.Tag);
        Assert.Equal(Time.GetUtcNow(), _sut.LastCheck);
        Assert.NotNull(await Storage.GetAsync(UpdateService.LastCheckKey));
    }

    [Fact]
    public async Task Start_skips_the_check_when_the_last_one_is_recent()
    {
        await Storage.SetAsync(UpdateService.LastCheckKey, Time.GetUtcNow().AddHours(-1).ToString("O"));

        await _sut.StartAsync();
        Assert.Equal(0, Releases.Calls);

        await _sut.CheckAsync(); // "Check now" always asks
        Assert.Equal(1, Releases.Calls);
    }

    [Fact]
    public async Task Start_checks_again_after_six_hours()
    {
        await Storage.SetAsync(UpdateService.LastCheckKey, Time.GetUtcNow().AddHours(-6).ToString("O"));

        await _sut.StartAsync();

        Assert.Equal(1, Releases.Calls);
    }

    [Fact]
    public async Task Nothing_happens_where_updates_are_not_supported()
    {
        Updater.IsSupported = false;
        Releases.Add("v9.0.0");

        await _sut.StartAsync();
        await _sut.CheckAsync();

        Assert.Equal(0, Releases.Calls);
        Assert.Null(_sut.Available);
    }

    [Fact]
    public async Task Dev_builds_default_to_every_change_and_the_choice_is_saved()
    {
        _sut.CurrentVersion = "1.1.1-dev.3";
        Releases.Add("v1.1.1-dev.4", prerelease: true);

        await _sut.StartAsync();
        Assert.Equal(UpdateChannel.EveryChange, _sut.Channel);
        Assert.Equal("v1.1.1-dev.4", _sut.Available!.Tag);

        await _sut.SetChannelAsync(UpdateChannel.Stable);
        Assert.Null(_sut.Available);
        Assert.Equal("Stable", await Storage.GetAsync(UpdateService.ChannelKey));
    }

    [Fact]
    public async Task Offline_is_reported_without_throwing()
    {
        Releases.Failure = new HttpRequestException("No such host");

        await _sut.CheckAsync();

        Assert.Equal("Could not reach GitHub. Check your connection.", _sut.Error);
        Assert.Null(_sut.LastCheck);
        Assert.False(_sut.IsChecking);
    }

    [Fact]
    public async Task Rate_limit_message_is_shown()
    {
        Releases.Failure = new HttpRequestException("GitHub limits how often updates can be checked. Try again in an hour.", null, HttpStatusCode.Forbidden);

        await _sut.CheckAsync();

        Assert.StartsWith("GitHub limits", _sut.Error);
    }

    [Fact]
    public async Task Later_hides_this_version_until_a_newer_one()
    {
        Releases.Add("v1.2.0");
        await _sut.CheckAsync();

        await _sut.DismissAsync();
        Assert.True(_sut.IsDismissed);

        Releases.Add("v1.2.1");
        await _sut.CheckAsync();
        Assert.False(_sut.IsDismissed);
    }

    [Fact]
    public async Task Install_hands_the_release_to_the_platform_and_reports_failures()
    {
        var release = Releases.Add("v1.2.0");
        await _sut.CheckAsync();

        await _sut.InstallAsync();
        Assert.Equal([release], Updater.Installed);

        Updater.Failure = new InvalidOperationException("disk full");
        await _sut.InstallAsync();
        Assert.Equal("The update could not be installed: disk full", _sut.Error);
        Assert.False(_sut.IsInstalling);
    }

    [Fact]
    public async Task Missing_install_permission_is_a_hint_not_an_error()
    {
        Releases.Add("v1.2.0");
        await _sut.CheckAsync();
        Updater.Failure = new InstallPermissionNeededException();

        await _sut.InstallAsync();

        Assert.Null(_sut.Error);
        Assert.StartsWith("Allow Activity Tracker to install apps", _sut.Hint);

        Updater.Failure = null;
        await _sut.InstallAsync();
        Assert.Null(_sut.Hint);
        Assert.Single(Updater.Installed);
    }
}

public class UpdateUiTests : WebTestBase
{
    private UpdateService Updates => Services.GetRequiredService<UpdateService>();

    private void Supported(string current = "1.1.0")
    {
        Updater.IsSupported = true;
        Updates.CurrentVersion = current;
    }

    [Fact]
    public void Banner_offers_the_update_and_installs_it()
    {
        Supported();
        Releases.Add("v1.2.0");
        Sync(Updates.CheckAsync());

        var cut = Render<UpdateBanner>();

        Assert.Contains("Update available: v1.2.0", cut.Find("[data-testid=update-banner]").TextContent);
        cut.Find("[data-testid=update-install]").Click();
        Assert.Equal("v1.2.0", Assert.Single(Updater.Installed).Tag);
    }

    [Fact]
    public void Later_hides_the_banner()
    {
        Supported();
        Releases.Add("v1.2.0");
        Sync(Updates.CheckAsync());
        var cut = Render<UpdateBanner>();

        cut.Find("[data-testid=update-later]").Click();

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[data-testid=update-banner]")));
    }

    [Fact]
    public void No_banner_when_up_to_date_or_in_the_web_app()
    {
        Releases.Add("v1.2.0");
        Sync(Updates.CheckAsync()); // web app: not supported, never checks

        Assert.Empty(Render<UpdateBanner>().FindAll("[data-testid=update-banner]"));
        Assert.Empty(Render<UpdateSettings>().FindAll("[data-testid=update-settings]"));
    }

    [Fact]
    public void Settings_show_the_version_change_the_channel_and_check()
    {
        Supported();
        Releases.Add("v1.1.1-dev.5", prerelease: true);
        var cut = Render<UpdateSettings>();

        Assert.Contains("Installed: v1.1.0", cut.Find("[data-testid=update-status]").TextContent);
        cut.Find("[data-testid=update-check]").Click();
        cut.WaitForAssertion(() => Assert.Contains("Up to date", cut.Find("[data-testid=update-status]").TextContent));

        cut.Find("[data-testid=update-channel]").Change(nameof(UpdateChannel.EveryChange));

        cut.WaitForAssertion(() => Assert.Contains("v1.1.1-dev.5 is available", cut.Find("[data-testid=update-status]").TextContent));
        Assert.NotNull(cut.Find("[data-testid=update-install-settings]"));
    }
}

public class GitHubReleasesTests
{
    [Fact]
    public void Reads_versions_apks_and_prerelease_flags_and_skips_drafts_and_odd_tags()
    {
        const string json = """
            [
              {"tag_name":"v1.1.1-dev.44","prerelease":true,"draft":false,"body":"Dev build","published_at":"2026-11-27T10:00:00Z",
               "assets":[{"name":"activity-tracker-v1.1.1-dev.44.apk","browser_download_url":"https://github.com/x/releases/download/v1.1.1-dev.44/a.apk"}]},
              {"tag_name":"v1.1.0","prerelease":false,"draft":false,
               "assets":[{"name":"activity-tracker-v1.1.0-web.zip","browser_download_url":"https://github.com/x/web.zip"},
                         {"name":"activity-tracker-v1.1.0.apk","browser_download_url":"https://github.com/x/releases/download/v1.1.0/b.apk"}]},
              {"tag_name":"v1.0.0","prerelease":false,"draft":false,"assets":[]},
              {"tag_name":"v2.0.0","prerelease":false,"draft":true,"assets":[]},
              {"tag_name":"nightly","prerelease":true,"draft":false,"assets":[]}
            ]
            """;

        var releases = GitHubReleases.Parse(JsonDocument.Parse(json).RootElement);

        Assert.Equal(["v1.1.1-dev.44", "v1.1.0", "v1.0.0"], releases.Select(r => r.Tag));
        Assert.True(releases[0].IsPrerelease);
        Assert.Equal(new DateTimeOffset(2026, 11, 27, 10, 0, 0, TimeSpan.Zero), releases[0].PublishedAt);
        Assert.EndsWith("b.apk", releases[1].ApkUrl);
        Assert.Null(releases[2].ApkUrl);
    }

    [Fact]
    public void Unexpected_json_gives_no_releases() =>
        Assert.Empty(GitHubReleases.Parse(JsonDocument.Parse("""{"message":"Not Found"}""").RootElement));
}

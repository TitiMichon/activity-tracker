using ActivityTracker.Core.Services;

namespace ActivityTracker.Core.Tests;

public class SemVersionTests
{
    [Theory]
    [InlineData("1.1.0", "1.1.1-dev.1")]       // a dev build of the next patch is newer than the release
    [InlineData("1.1.1-dev.9", "1.1.1-dev.10")] // numbers compare as numbers
    [InlineData("1.1.1-dev.42", "1.1.1")]       // the release is newer than its dev builds
    [InlineData("1.1.1-dev.42", "1.2.0")]
    [InlineData("1.9.0", "1.10.0")]
    [InlineData("1.2.0-ci.5", "1.2.0-dev.1")]   // words compare as text
    [InlineData("1.2.0-dev", "1.2.0-dev.1")]    // fewer parts is older
    [InlineData("1.2.0-1", "1.2.0-dev")]        // numbers before words
    [InlineData("1.99.99", "2.0.0-dev.1")]
    public void Orders_versions(string older, string newer)
    {
        Assert.True(SemVersion.Parse(older) < SemVersion.Parse(newer));
        Assert.True(SemVersion.Parse(newer) > SemVersion.Parse(older));
    }

    [Theory]
    [InlineData("v1.2.3", "1.2.3")]
    [InlineData("1.2.3-dev.4+abc123", "1.2.3-dev.4")]
    [InlineData(" V1.0.0 ", "1.0.0")]
    public void Parses_tags_and_ignores_build_metadata(string text, string expected) =>
        Assert.Equal(expected, SemVersion.Parse(text).ToString());

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1.2")]
    [InlineData("1.2.3.4")]
    [InlineData("1.x.3")]
    [InlineData("1.2.3-")]
    [InlineData("1.2.3-dev..1")]
    [InlineData("-1.2.3")]
    [InlineData("latest")]
    public void Rejects_what_is_not_a_version(string? text) =>
        Assert.False(SemVersion.TryParse(text, out _));

    [Fact]
    public void Equal_versions_are_equal() =>
        Assert.Equal(SemVersion.Parse("v1.2.3-dev.4"), SemVersion.Parse("1.2.3-dev.4+x"));
}

public class AppUpdatesTests
{
    private static AppRelease Release(string tag, bool prerelease = false, bool apk = true) =>
        new(SemVersion.Parse(tag), tag, prerelease, apk ? $"https://example.test/{tag}.apk" : null);

    private static readonly AppRelease[] Published =
    [
        Release("v1.1.1-dev.44", prerelease: true),
        Release("v1.1.1-dev.43", prerelease: true),
        Release("v1.1.0"),
        Release("v1.0.0"),
    ];

    [Fact]
    public void Stable_channel_only_offers_releases()
    {
        Assert.Null(AppUpdates.Newest(Published, SemVersion.Parse("1.1.0"), UpdateChannel.Stable));
        Assert.Equal("v1.1.0", AppUpdates.Newest(Published, SemVersion.Parse("1.0.0"), UpdateChannel.Stable)!.Tag);
    }

    [Fact]
    public void Every_change_offers_the_newest_dev_build()
    {
        Assert.Equal("v1.1.1-dev.44", AppUpdates.Newest(Published, SemVersion.Parse("1.1.0"), UpdateChannel.EveryChange)!.Tag);
        Assert.Null(AppUpdates.Newest(Published, SemVersion.Parse("1.1.1-dev.44"), UpdateChannel.EveryChange));
    }

    [Fact]
    public void A_release_replaces_its_dev_builds_on_both_channels()
    {
        var withRelease = Published.Append(Release("v1.1.1")).ToList();

        Assert.Equal("v1.1.1", AppUpdates.Newest(withRelease, SemVersion.Parse("1.1.1-dev.44"), UpdateChannel.EveryChange)!.Tag);
        Assert.Equal("v1.1.1", AppUpdates.Newest(withRelease, SemVersion.Parse("1.1.1-dev.44"), UpdateChannel.Stable)!.Tag);
    }

    [Fact]
    public void Releases_without_an_apk_are_skipped()
    {
        var releases = new[] { Release("v1.2.0", apk: false), Release("v1.1.1") };

        Assert.Equal("v1.1.1", AppUpdates.Newest(releases, SemVersion.Parse("1.1.0"), UpdateChannel.Stable)!.Tag);
    }

    [Fact]
    public void A_version_with_a_suffix_is_a_dev_build_even_if_not_marked_prerelease()
    {
        var releases = new[] { Release("v1.2.0-dev.1") };

        Assert.Null(AppUpdates.Newest(releases, SemVersion.Parse("1.1.0"), UpdateChannel.Stable));
    }

    [Theory]
    [InlineData("1.1.0", UpdateChannel.Stable)]
    [InlineData("1.1.1-dev.42", UpdateChannel.EveryChange)]
    public void Dev_builds_follow_every_change_by_default(string current, UpdateChannel expected) =>
        Assert.Equal(expected, AppUpdates.DefaultChannel(SemVersion.Parse(current)));
}

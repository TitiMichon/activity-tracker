using ActivityTracker.UI.Platform;

namespace ActivityTracker.Web.Tests;

public sealed class FileKeyValueStorageTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "at-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    [Fact]
    public async Task Missing_key_returns_null()
    {
        Assert.Null(await new FileKeyValueStorage(_dir).GetAsync("nothing"));
    }

    [Fact]
    public async Task Values_survive_a_new_instance_and_are_overwritten()
    {
        await new FileKeyValueStorage(_dir).SetAsync("activity-tracker:data", "{\"v\":1}");
        await new FileKeyValueStorage(_dir).SetAsync("activity-tracker:data", "{\"v\":2}");

        Assert.Equal("{\"v\":2}", await new FileKeyValueStorage(_dir).GetAsync("activity-tracker:data"));
        Assert.Single(Directory.GetFiles(_dir)); // no temp file left
    }

    [Fact]
    public void Keys_map_to_safe_file_names()
    {
        var path = new FileKeyValueStorage(_dir).PathFor("activity-tracker:data/../x");
        Assert.Equal(Path.Combine(_dir, "activity-tracker_data_.._x.json"), path);
    }
}

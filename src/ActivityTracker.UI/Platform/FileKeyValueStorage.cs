using System.Text;

namespace ActivityTracker.UI.Platform;

/// <summary>
/// One file per key in a directory (native apps: the app's private data folder).
/// Writes go to a temporary file first so a crash never leaves half-written data.
/// </summary>
public sealed class FileKeyValueStorage(string directory) : IKeyValueStorage
{
    public string PathFor(string key)
    {
        var name = new StringBuilder(key.Length);
        foreach (var c in key)
        {
            name.Append(char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' ? c : '_');
        }

        return Path.Combine(directory, name + ".json");
    }

    public async Task<string?> GetAsync(string key)
    {
        var path = PathFor(key);
        return File.Exists(path) ? await File.ReadAllTextAsync(path) : null;
    }

    public async Task SetAsync(string key, string value)
    {
        Directory.CreateDirectory(directory);
        var path = PathFor(key);
        var temp = path + ".tmp";
        await File.WriteAllTextAsync(temp, value);
        File.Move(temp, path, overwrite: true);
    }
}

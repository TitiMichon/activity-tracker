using ActivityTracker.UI.Platform;

namespace ActivityTracker.Mobile.Platform;

/// <summary>Writes the file to the cache folder and opens the share sheet (Drive, email, Files...).</summary>
public sealed class ShareFileExporter : IFileExporter
{
    public async Task ExportAsync(string fileName, string contentType, string content)
    {
        var path = Path.Combine(FileSystem.CacheDirectory, fileName);
        await File.WriteAllTextAsync(path, content);
        await MainThread.InvokeOnMainThreadAsync(() => Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Export backup",
            File = new ShareFile(path, contentType),
        }));
    }
}

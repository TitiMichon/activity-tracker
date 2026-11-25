namespace ActivityTracker.UI.Platform;

/// <summary>Hands a generated file to the user: a download in the browser, the share sheet on Android.</summary>
public interface IFileExporter
{
    Task ExportAsync(string fileName, string contentType, string content);
}

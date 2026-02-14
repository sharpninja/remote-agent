using Android.OS;
using Java.IO;

namespace RemoteAgent.App.Services;

public static partial class MediaSaveService
{
    private static string? SaveToDcimRemoteAgentAndroid(byte[] content, string contentType, string? suggestedFileName)
    {
        var dcim = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDcim);
        var folder = "Remote Agent";
        var dir = new Java.IO.File(dcim, folder);
        if (!dir.Exists())
            dir.Mkdirs();
        var ext = GetExtension(contentType, suggestedFileName);
        var displayName = string.IsNullOrWhiteSpace(suggestedFileName) ? $"remote_agent_{DateTime.UtcNow:yyyyMMdd_HHmmss}{ext}" : suggestedFileName;
        if (!displayName.Contains('.'))
            displayName += ext;
        var file = new Java.IO.File(dir, displayName);
        System.IO.File.WriteAllBytes(file.AbsolutePath, content);
        return folder + "/" + displayName;
    }

    private static string GetExtension(string contentType, string? fileName)
    {
        var ext = Path.GetExtension(fileName ?? "");
        if (!string.IsNullOrEmpty(ext)) return ext;
        return contentType?.ToLowerInvariant() switch
        {
            "image/jpeg" or "image/jpg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            _ => ".bin"
        };
    }
}

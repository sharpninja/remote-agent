namespace RemoteAgent.App.Services;

/// <summary>Saves received media (e.g. images from the agent) to DCIM/Remote Agent on the device so they appear in the gallery (TR-11.3).</summary>
/// <remarks>On Android uses the platform partial to save to the DCIM folder; on other platforms falls back to app data.</remarks>
/// <example><code>
/// var path = MediaSaveService.SaveToDcimRemoteAgent(media.Content.ToByteArray(), media.ContentType, media.FileName);
/// // Display "Saved: {path}" in chat
/// </code></example>
/// <see href="https://sharpninja.github.io/remote-agent/technical-requirements.html">Technical requirements (TR-11)</see>
public static partial class MediaSaveService
{
    /// <summary>Saves the content to DCIM/Remote Agent (Android) or app data (other platforms). Returns the saved path or filename for display.</summary>
    /// <param name="content">Raw file bytes.</param>
    /// <param name="contentType">MIME type (e.g. image/png).</param>
    /// <param name="suggestedFileName">Optional filename from the server.</param>
    /// <returns>Path or filename for display in the chat, or null on failure.</returns>
    public static string? SaveToDcimRemoteAgent(byte[] content, string contentType, string? suggestedFileName)
    {
#if ANDROID
        if (OperatingSystem.IsAndroid())
            return SaveToDcimRemoteAgentAndroid(content, contentType, suggestedFileName);
#endif
        return SaveToAppData(content, suggestedFileName);
    }

    private static string SaveToAppData(byte[] content, string? suggestedFileName)
    {
        var name = string.IsNullOrWhiteSpace(suggestedFileName) ? $"media_{DateTime.UtcNow:yyyyMMdd_HHmmss}.bin" : suggestedFileName;
        var path = Path.Combine(FileSystem.AppDataDirectory, "RemoteAgent", name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, content);
        return name;
    }
}

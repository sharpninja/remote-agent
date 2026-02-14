namespace RemoteAgent.App.Services;

/// <summary>Saves received media to DCIM/Remote Agent on device (TR-11.3).</summary>
public static partial class MediaSaveService
{
    public static string? SaveToDcimRemoteAgent(byte[] content, string contentType, string? suggestedFileName)
    {
        if (OperatingSystem.IsAndroid())
            return SaveToDcimRemoteAgentAndroid(content, contentType, suggestedFileName);
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

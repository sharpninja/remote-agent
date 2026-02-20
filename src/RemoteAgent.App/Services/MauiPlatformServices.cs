using RemoteAgent.App.Logic;
using RemoteAgent.Proto;

namespace RemoteAgent.App.Services;

public sealed class MauiAgentSelector(Func<Page?> pageFactory) : IAgentSelector
{
    public async Task<string?> SelectAsync(ServerInfoResponse serverInfo)
    {
        var agents = serverInfo.AvailableAgents.ToList();
        if (agents.Count == 0) return "";
        if (agents.Count == 1) return agents[0];

        var page = pageFactory();
        if (page == null) return null;

        var choice = await page.DisplayActionSheetAsync("Select agent", "Cancel", null, agents.ToArray());
        return string.IsNullOrEmpty(choice) ? null : choice;
    }
}

public sealed class MauiAttachmentPicker : IAttachmentPicker
{
    public async Task<PickedAttachment?> PickAsync()
    {
        try
        {
            var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                [DevicePlatform.Android] = new[] { "image/*", "video/*" },
                [DevicePlatform.WinUI] = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".mp4", ".webm" }
            });

            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Pick image or video",
                FileTypes = customFileType
            });
            if (result == null) return null;

            await using var stream = await result.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            return new PickedAttachment(ms.ToArray(), result.ContentType ?? "application/octet-stream", result.FileName ?? "attachment");
        }
        catch
        {
            return null;
        }
    }
}

public sealed class MauiPromptTemplateSelector(Func<Page?> pageFactory) : IPromptTemplateSelector
{
    public async Task<PromptTemplateDefinition?> SelectAsync(IReadOnlyList<PromptTemplateDefinition> templates)
    {
        if (templates.Count == 0) return null;
        if (templates.Count == 1) return templates[0];

        var page = pageFactory();
        if (page == null) return null;

        var labels = templates.Select(x => string.IsNullOrWhiteSpace(x.DisplayName) ? x.TemplateId : x.DisplayName).ToArray();
        var choice = await page.DisplayActionSheetAsync("Select prompt template", "Cancel", null, labels);
        if (string.IsNullOrWhiteSpace(choice) || string.Equals(choice, "Cancel", StringComparison.OrdinalIgnoreCase))
            return null;

        return templates.FirstOrDefault(t =>
            string.Equals(t.DisplayName, choice, StringComparison.Ordinal) ||
            string.Equals(t.TemplateId, choice, StringComparison.Ordinal));
    }
}

public sealed class MauiPromptVariableProvider(Func<Page?> pageFactory) : IPromptVariableProvider
{
    public async Task<string?> GetValueAsync(string variableName)
    {
        var page = pageFactory();
        if (page == null) return null;

        return await page.DisplayPromptAsync(
            title: "Template Input",
            message: $"Value for '{variableName}'",
            accept: "Apply",
            cancel: "Cancel",
            initialValue: "",
            keyboard: Keyboard.Text);
    }
}

public sealed class MauiSessionTerminationConfirmation(Func<Page?> pageFactory) : ISessionTerminationConfirmation
{
    public async Task<bool> ConfirmAsync(string sessionLabel)
    {
        var page = pageFactory();
        if (page == null) return false;

        return await page.DisplayAlertAsync(
            "Terminate Session",
            $"Terminate '{sessionLabel}'? This removes the session from local history.",
            "Terminate",
            "Cancel");
    }
}

public sealed class MauiDeleteMcpServerConfirmation(Func<Page?> pageFactory) : ISessionTerminationConfirmation
{
    public async Task<bool> ConfirmAsync(string serverId)
    {
        var page = pageFactory();
        if (page == null) return false;

        return await page.DisplayAlertAsync("Delete MCP Server", $"Delete '{serverId}'?", "Delete", "Cancel");
    }
}

public sealed class PlatformNotificationServiceAdapter : INotificationService
{
    public void Show(string title, string body)
    {
#if ANDROID
        PlatformNotificationService.ShowNotification(title, body);
#endif
    }
}

public sealed class MauiQrCodeScanner : IQrCodeScanner
{
    public async Task<string?> ScanAsync(string loginUrl)
    {
        var scanPage = new PairLoginPage(loginUrl);
        await MainThread.InvokeOnMainThreadAsync(
            () => Shell.Current.Navigation.PushModalAsync(scanPage, animated: true));
        return await scanPage.ResultTask;
    }
}
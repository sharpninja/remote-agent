using System.Text;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.Handlers;

/// <summary>Formats all status log entries as a Markdown list (oldest-first) and writes them to the clipboard.</summary>
public sealed class CopyStatusLogHandler(IClipboardService clipboard)
    : IRequestHandler<CopyStatusLogRequest, CommandResult>
{
    public async Task<CommandResult> HandleAsync(
        CopyStatusLogRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Entries.Count == 0)
            return CommandResult.Fail("Status log is empty.");

        var sb = new StringBuilder();
        sb.AppendLine("# Status Log");
        sb.AppendLine();
        foreach (var entry in request.Entries.Reverse())
            sb.AppendLine($"- `{entry.Timestamp:yyyy-MM-dd HH:mm:ss}` {entry.Message}");

        await clipboard.SetTextAsync(sb.ToString());
        return CommandResult.Ok();
    }
}

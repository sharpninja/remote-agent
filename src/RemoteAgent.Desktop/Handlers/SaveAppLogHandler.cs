using System.Globalization;
using System.Text;
using System.Text.Json;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.Handlers;

public sealed class SaveAppLogHandler : IRequestHandler<SaveAppLogRequest, CommandResult>
{
    public async Task<CommandResult> HandleAsync(SaveAppLogRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.FilePath))
            return CommandResult.Fail("File path is required.");

        try
        {
            var content = request.Format.ToLowerInvariant() switch
            {
                "json" => BuildJson(request.Entries),
                "csv"  => BuildCsv(request.Entries),
                _      => BuildText(request.Entries)
            };

            await File.WriteAllTextAsync(request.FilePath, content, Encoding.UTF8, cancellationToken);

            request.Workspace.StatusText = $"App log saved to {request.FilePath} ({request.Format.ToUpperInvariant()}).";
            return CommandResult.Ok();
        }
        catch (Exception ex)
        {
            return CommandResult.Fail($"Failed to save app log: {ex.Message}");
        }
    }

    private static string BuildText(IReadOnlyList<AppLogEntry> entries)
    {
        var sb = new StringBuilder();
        foreach (var e in entries)
        {
            sb.AppendLine($"[{e.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{e.Level,-11}] [{e.Category}] {e.Message}");
            if (e.ExceptionMessage != null)
                sb.AppendLine($"  Exception: {e.ExceptionMessage}");
        }
        return sb.ToString();
    }

    private static string BuildJson(IReadOnlyList<AppLogEntry> entries)
    {
        var rows = entries.Select(e => new
        {
            timestamp  = e.Timestamp.ToString("O", CultureInfo.InvariantCulture),
            level      = e.Level.ToString(),
            category   = e.Category,
            message    = e.Message,
            exception  = e.ExceptionMessage
        });
        return JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string BuildCsv(IReadOnlyList<AppLogEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Timestamp,Level,Category,Message,Exception");
        foreach (var e in entries)
        {
            sb.AppendLine(string.Join(',',
                CsvEscape(e.Timestamp.ToString("O", CultureInfo.InvariantCulture)),
                CsvEscape(e.Level.ToString()),
                CsvEscape(e.Category),
                CsvEscape(e.Message),
                CsvEscape(e.ExceptionMessage ?? "")));
        }
        return sb.ToString();
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}

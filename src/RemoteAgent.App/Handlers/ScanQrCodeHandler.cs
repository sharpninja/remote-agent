using RemoteAgent.App.Logic;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.App.Requests;
using RemoteAgent.App.ViewModels;

namespace RemoteAgent.App.Handlers;

/// <summary>
/// Handles QR-code / deep-link pairing.
/// <list type="bullet">
///   <item>If <see cref="ScanQrCodeRequest.RawUri"/> is already set (deep link), it is parsed directly.</item>
///   <item>Otherwise the camera scanner is invoked via <see cref="IQrCodeScanner"/>.</item>
/// </list>
/// On success the ViewModel's Host, Port, and ApiKey are populated and saved to preferences.
/// </summary>
public sealed class ScanQrCodeHandler(IQrCodeScanner scanner, IAppPreferences preferences)
    : IRequestHandler<ScanQrCodeRequest, CommandResult>
{
    private const string PrefServerHost = "ServerHost";
    private const string PrefServerPort = "ServerPort";
    private const string PrefApiKey     = "ApiKey";

    public async Task<CommandResult> HandleAsync(ScanQrCodeRequest request, CancellationToken ct = default)
    {
        string? raw;
        if (!string.IsNullOrWhiteSpace(request.RawUri))
        {
            raw = request.RawUri;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.Workspace.Host))
            {
                request.Workspace.Status = "Enter a host address before logging in.";
                return CommandResult.Fail("No host configured.");
            }

            var webPort = "1" + request.Workspace.Port; // e.g. 5244 → 15244
            var loginUrl = $"http://{request.Workspace.Host}:{webPort}/pair";

            raw = await scanner.ScanAsync(loginUrl);
            if (string.IsNullOrWhiteSpace(raw))
            {
                request.Workspace.Status = "Login cancelled.";
                return CommandResult.Fail("Login cancelled.");
            }
        }

        var result = ParseAndApply(raw.Trim(), request.Workspace);
        if (result.Success)
        {
            preferences.Set(PrefServerHost, request.Workspace.Host);
            preferences.Set(PrefServerPort, request.Workspace.Port);
            preferences.Set(PrefApiKey,     request.Workspace.ApiKey);
        }
        return result;
    }

    internal static CommandResult ParseAndApply(string raw, MainPageViewModel workspace)
    {
        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, "remoteagent", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(uri.Host, "pair", StringComparison.OrdinalIgnoreCase))
        {
            workspace.Status = "Invalid pairing URL.";
            return CommandResult.Fail("Invalid pairing URL.");
        }

        var query = ParseQuery(uri.Query);
        var key  = query.GetValueOrDefault("key",  "");
        var host = query.GetValueOrDefault("host", "");
        var port = query.GetValueOrDefault("port", "");

        if (string.IsNullOrWhiteSpace(host))
        {
            workspace.Status = "Pairing URL missing host.";
            return CommandResult.Fail("Pairing URL missing host.");
        }

        if (!int.TryParse(port, out var portNum) || portNum <= 0 || portNum > 65535)
        {
            workspace.Status = "Pairing URL missing or invalid port.";
            return CommandResult.Fail("Pairing URL missing or invalid port.");
        }

        workspace.Host   = host;
        workspace.Port   = portNum.ToString();
        workspace.ApiKey = key;

        workspace.Status = "Pairing details loaded. Tap Connect to continue.";
        return CommandResult.Ok();
    }

    internal static Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var trimmed = query.TrimStart('?');
        if (string.IsNullOrEmpty(trimmed)) return result;
        foreach (var part in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = part.IndexOf('=');
            if (idx < 0) continue;
            var k = Uri.UnescapeDataString(part[..idx]);
            var v = Uri.UnescapeDataString(part[(idx + 1)..]);
            result[k] = v;
        }
        return result;
    }
}

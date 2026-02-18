using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RemoteAgent.Service.Agents;

namespace RemoteAgent.Plugins.Ollama;

/// <summary>
/// Agent runner that connects to a locally running <see href="https://ollama.com">Ollama</see>
/// instance via its HTTP chat API (FR-8.1, TR-10.1).
/// </summary>
/// <remarks>
/// <para>Select this runner by setting <c>Agent:RunnerId</c> to the full type name
/// <c>RemoteAgent.Plugins.Ollama.OllamaAgentRunner</c> in appsettings, and by adding
/// the plugin assembly path to <c>Plugins:Assemblies</c>.</para>
/// <para>Configuration keys read from the <c>Ollama</c> section:</para>
/// <list type="bullet">
///   <item><c>Ollama:BaseUrl</c> — Ollama base URL (default: <c>http://localhost:11434</c>).</item>
///   <item><c>Ollama:DefaultModel</c> — Model used when the <c>command</c> parameter is blank (default: <c>llama3.2</c>).</item>
///   <item><c>Ollama:TimeoutSeconds</c> — Per-request HTTP timeout in seconds (default: 300).</item>
/// </list>
/// <para>The <c>command</c> parameter passed to <see cref="StartAsync"/> overrides the model
/// name for that individual session, allowing per-session model selection.</para>
/// </remarks>
/// <example><code>
/// // appsettings.json
/// "Agent": { "RunnerId": "RemoteAgent.Plugins.Ollama.OllamaAgentRunner", "Command": "llama3.2" },
/// "Plugins": { "Assemblies": [ "./plugins/RemoteAgent.Plugins.Ollama.dll" ] },
/// "Ollama": { "BaseUrl": "http://localhost:11434", "DefaultModel": "llama3.2", "TimeoutSeconds": 300 }
/// </code></example>
public sealed class OllamaAgentRunner : IAgentRunner, IDisposable
{
    /// <summary>Full type name used as the runner id in <c>Agent:RunnerId</c>.</summary>
    public const string RunnerId = "RemoteAgent.Plugins.Ollama.OllamaAgentRunner";

    private readonly ILogger<OllamaAgentRunner> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _defaultModel;
    private bool _disposed;

    /// <summary>
    /// Initialises the Ollama runner, configuring the shared <see cref="HttpClient"/> from
    /// the <c>Ollama</c> configuration section.
    /// </summary>
    /// <param name="logger">Logger injected by the host service DI container.</param>
    /// <param name="configuration">Application configuration; reads <c>Ollama:BaseUrl</c>,
    /// <c>Ollama:DefaultModel</c>, and <c>Ollama:TimeoutSeconds</c>.</param>
    public OllamaAgentRunner(ILogger<OllamaAgentRunner> logger, IConfiguration configuration)
    {
        _logger = logger;
        var baseUrl = (configuration["Ollama:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:11434") + "/";
        _defaultModel = configuration["Ollama:DefaultModel"] ?? "llama3.2";
        var timeout = int.TryParse(configuration["Ollama:TimeoutSeconds"], out var t) && t > 0 ? t : 300;

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(timeout)
        };
    }

    /// <inheritdoc/>
    /// <param name="command">Model name override (e.g. <c>"mistral"</c>). When blank, uses <c>Ollama:DefaultModel</c>.</param>
    /// <param name="arguments">Ignored; reserved for future use.</param>
    /// <param name="sessionId">Session identifier used in log messages.</param>
    /// <param name="logWriter">Optional per-session log file writer.</param>
    /// <param name="cancellationToken">Cancellation token (unused at start; propagated into the session).</param>
    public Task<IAgentSession?> StartAsync(
        string? command,
        string? arguments,
        string sessionId,
        StreamWriter? logWriter,
        CancellationToken cancellationToken = default)
    {
        var model = !string.IsNullOrWhiteSpace(command) ? command : _defaultModel;
        _logger.LogInformation("Starting Ollama session {SessionId} with model {Model}", sessionId, model);
        IAgentSession session = new OllamaAgentSession(model, _httpClient, logWriter, _logger);
        return Task.FromResult<IAgentSession?>(session);
    }

    /// <summary>Disposes the shared <see cref="HttpClient"/> owned by this runner.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _httpClient.Dispose();
    }
}

using System.IO.Pipelines;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using RemoteAgent.Service.Agents;

namespace RemoteAgent.Plugins.Ollama;

/// <summary>
/// Agent session backed by the Ollama HTTP streaming chat endpoint.
/// Maintains full conversation history across turns for multi-turn dialogue.
/// </summary>
internal sealed class OllamaAgentSession : IAgentSession
{
    private const string SystemPrompt = "You are a helpful assistant.";

    private readonly string _model;
    private readonly HttpClient _httpClient;
    private readonly StreamWriter? _logWriter;
    private readonly ILogger _logger;
    private readonly Channel<string> _inputChannel;
    private readonly Pipe _outputPipe;
    private readonly Pipe _errorPipe;
    private readonly CancellationTokenSource _cts;
    private readonly Task _processingTask;
    private readonly List<OllamaMessage> _history;
    private readonly StreamReader _standardOutput;
    private readonly StreamReader _standardError;
    private bool _disposed;

    internal OllamaAgentSession(string model, HttpClient httpClient, StreamWriter? logWriter, ILogger logger)
    {
        _model = model;
        _httpClient = httpClient;
        _logWriter = logWriter;
        _logger = logger;
        _inputChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true });
        _outputPipe = new Pipe();
        _errorPipe = new Pipe();
        _cts = new CancellationTokenSource();
        _history = [new OllamaMessage("system", SystemPrompt)];

        // leaveOpen: true — pipe lifetime is managed separately via Complete(); disposing the
        // reader must not close the underlying pipe stream prematurely.
        _standardOutput = new StreamReader(_outputPipe.Reader.AsStream(), Encoding.UTF8, false, 4096, leaveOpen: true);
        _standardError = new StreamReader(_errorPipe.Reader.AsStream(), Encoding.UTF8, false, 4096, leaveOpen: true);

        _processingTask = Task.Run(() => RunAsync(_cts.Token));
    }

    /// <inheritdoc/>
    public StreamReader StandardOutput => _standardOutput;

    /// <inheritdoc/>
    public StreamReader StandardError => _standardError;

    /// <inheritdoc/>
    public bool HasExited => _cts.IsCancellationRequested || _processingTask.IsCompleted;

    /// <inheritdoc/>
    public async Task SendInputAsync(string text, CancellationToken cancellationToken = default)
        => await _inputChannel.Writer.WriteAsync(text, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public void Stop()
    {
        _cts.Cancel();
        _inputChannel.Writer.TryComplete();
    }

    // ---------------------------------------------------------------------------
    // Background processing loop
    // ---------------------------------------------------------------------------

    private async Task RunAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var userInput in _inputChannel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
            {
                _history.Add(new OllamaMessage("user", userInput));
                _logWriter?.WriteLine($"[user] {userInput}");

                var reply = await CallOllamaAsync(ct).ConfigureAwait(false);
                if (reply is not null)
                    _history.Add(new OllamaMessage("assistant", reply));
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama session error");
            var errBytes = Encoding.UTF8.GetBytes($"Ollama session error: {ex.Message}\n");
            await _errorPipe.Writer.WriteAsync(errBytes, CancellationToken.None).ConfigureAwait(false);
            await _errorPipe.Writer.FlushAsync(CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            // Completing the writers signals EOF to the service's stdout/stderr readers.
            _outputPipe.Writer.Complete();
            _errorPipe.Writer.Complete();
        }
    }

    private async Task<string?> CallOllamaAsync(CancellationToken ct)
    {
        var body = JsonSerializer.Serialize(
            new OllamaChatRequest(_model, new List<OllamaMessage>(_history), Stream: true),
            OllamaJsonContext.Default.OllamaChatRequest);

        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync("api/chat", content, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Ollama API request failed for model {Model}", _model);
            var errBytes = Encoding.UTF8.GetBytes($"Ollama error: {ex.Message}\n");
            await _errorPipe.Writer.WriteAsync(errBytes, ct).ConfigureAwait(false);
            await _errorPipe.Writer.FlushAsync(ct).ConfigureAwait(false);
            return null;
        }

        using (response)
        {
            var fullReply = new StringBuilder();
            var lineBuffer = new StringBuilder();

            using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            string? jsonLine;
            while ((jsonLine = await reader.ReadLineAsync(ct).ConfigureAwait(false)) is not null)
            {
                if (string.IsNullOrWhiteSpace(jsonLine)) continue;

                OllamaChatResponse? chunk;
                try { chunk = JsonSerializer.Deserialize(jsonLine, OllamaJsonContext.Default.OllamaChatResponse); }
                catch { continue; }

                if (chunk?.Message?.Content is { Length: > 0 } token)
                {
                    fullReply.Append(token);
                    lineBuffer.Append(token);
                    _logWriter?.Write(token);

                    // Flush complete lines to the output pipe immediately for real-time streaming.
                    var buf = lineBuffer.ToString();
                    var nl = buf.LastIndexOf('\n');
                    if (nl >= 0)
                    {
                        var flush = Encoding.UTF8.GetBytes(buf[..(nl + 1)]);
                        await _outputPipe.Writer.WriteAsync(flush, ct).ConfigureAwait(false);
                        await _outputPipe.Writer.FlushAsync(ct).ConfigureAwait(false);
                        lineBuffer.Clear();
                        if (nl + 1 < buf.Length) lineBuffer.Append(buf[(nl + 1)..]);
                    }
                }

                if (chunk?.Done == true)
                {
                    // Flush any remaining partial line as a complete line once the response finishes.
                    if (lineBuffer.Length > 0)
                    {
                        lineBuffer.Append('\n');
                        var remaining = Encoding.UTF8.GetBytes(lineBuffer.ToString());
                        await _outputPipe.Writer.WriteAsync(remaining, ct).ConfigureAwait(false);
                        await _outputPipe.Writer.FlushAsync(ct).ConfigureAwait(false);
                        lineBuffer.Clear();
                    }
                    _logWriter?.WriteLine();
                    break;
                }
            }

            return fullReply.Length > 0 ? fullReply.ToString() : null;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        try { _processingTask.Wait(TimeSpan.FromSeconds(5)); } catch { }
        _standardOutput.Dispose();
        _standardError.Dispose();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}

// ---------------------------------------------------------------------------
// Ollama HTTP API DTOs
// ---------------------------------------------------------------------------

internal record OllamaMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content);

internal record OllamaChatRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("messages")] List<OllamaMessage> Messages,
    [property: JsonPropertyName("stream")] bool Stream);

internal record OllamaResponseMessage(
    [property: JsonPropertyName("role")] string? Role,
    [property: JsonPropertyName("content")] string? Content);

internal record OllamaChatResponse(
    [property: JsonPropertyName("message")] OllamaResponseMessage? Message,
    [property: JsonPropertyName("done")] bool Done);

[JsonSerializable(typeof(OllamaChatRequest))]
[JsonSerializable(typeof(OllamaChatResponse))]
[JsonSerializable(typeof(OllamaMessage))]
[JsonSerializable(typeof(OllamaResponseMessage))]
[JsonSerializable(typeof(List<OllamaMessage>))]
internal partial class OllamaJsonContext : JsonSerializerContext { }

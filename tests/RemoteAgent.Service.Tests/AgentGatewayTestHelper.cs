using System.Collections.Generic;
using Grpc.Core;
using RemoteAgent.Proto;
using Xunit.Abstractions;

namespace RemoteAgent.Service.Tests;

/// <summary>Helper for Connect tests: run agent invocation in a task, wait up to 60s, kill agent process on timeout; log agent stdout/stderr every second.</summary>
public static class AgentGatewayTestHelper
{
    public const int AgentTimeoutSeconds = 60;
    public const int LogIntervalSeconds = 1;
    public const int RequestWaitSecondsAfterReturn = 5;

    /// <summary>Runs the agent invocation asynchronously, waits up to 60 seconds for completion. If the wait expires, sends Stop and completes the request stream so the server kills the agent process, then returns (possibly partial) results.</summary>
    /// <param name="client">gRPC AgentGateway client.</param>
    /// <param name="writeRequestAsync">Delegate that writes to the call's request stream (e.g. Start, text, then Complete). Receives the call and cancellation token.</param>
    /// <param name="output">xUnit output for logging.</param>
    /// <returns>Collected outputs, errors, events, and event messages.</returns>
    public static async Task<(List<string> Outputs, List<string> Errors, List<SessionEvent.Types.Kind> Events, List<string> EventMessages)> RunAgentInvocationWithTimeoutAsync(
        AgentGateway.AgentGatewayClient client,
        Func<AsyncDuplexStreamingCall<ClientMessage, ServerMessage>, CancellationToken, Task> writeRequestAsync,
        ITestOutputHelper output)
    {
        // Run entire wait/timeout logic on thread pool so we never depend on test's sync context (avoids deadlock and ensures 60s timer runs).
        return await Task.Run(async () =>
        {
            var cts = new CancellationTokenSource();
            var call = client.Connect(cancellationToken: cts.Token);

            // Encapsulate full invocation: write requests (in background) + read response stream with logging. No internal timeout.
            var agentTask = RunAgentInvocationAsync(call, writeRequestAsync, output, cts);

            // Wait up to 60 seconds for completion (timer on thread pool, no dependency on caller token).
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(AgentTimeoutSeconds));
            var completed = await Task.WhenAny(agentTask, timeoutTask).ConfigureAwait(false);

            if (completed == timeoutTask)
            {
                output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Timeout after {AgentTimeoutSeconds}s — killing agent (sending Stop and completing request stream).");
                cts.Cancel();
                try
                {
                    await call.RequestStream.WriteAsync(new ClientMessage
                    {
                        Control = new SessionControl { Action = SessionControl.Types.Action.Stop }
                    }).ConfigureAwait(false);
                    await call.RequestStream.CompleteAsync().ConfigureAwait(false);
                }
                catch
                {
                    // ignore
                }
                // Wait for the invocation task to finish (read loop will end on disconnect/cancel).
                var waitTask = Task.WhenAny(agentTask, Task.Delay(TimeSpan.FromSeconds(RequestWaitSecondsAfterReturn)));
                await waitTask.ConfigureAwait(false);
                if (!agentTask.IsCompleted)
                    output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Agent task did not finish within {RequestWaitSecondsAfterReturn}s after kill.");
            }

            List<string> outputs;
            List<string> errors;
            List<SessionEvent.Types.Kind> events;
            List<string> eventMessages;
            if (agentTask.IsCompleted)
            {
                (outputs, errors, events, eventMessages) = await agentTask.ConfigureAwait(false);
            }
            else
            {
                outputs = new List<string>();
                errors = new List<string>();
                events = new List<SessionEvent.Types.Kind>();
                eventMessages = new List<string>();
            }

            // Flush accumulated stdout/stderr to test output so it's visible even if per-second logging wasn't (e.g. from background thread).
            if (outputs.Count > 0 || errors.Count > 0)
            {
                output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Final — stdout: {outputs.Count} line(s), stderr: {errors.Count} line(s)");
                if (outputs.Count > 0)
                    output.WriteLine(string.Join(Environment.NewLine, outputs));
                if (errors.Count > 0)
                    output.WriteLine(string.Join(Environment.NewLine, errors));
            }
            return (outputs, errors, events, eventMessages);
        }).ConfigureAwait(false);
    }

    /// <summary>Runs one agent invocation: starts request writer in background, reads response stream with 1s logging, returns collected data.</summary>
    private static async Task<(List<string> Outputs, List<string> Errors, List<SessionEvent.Types.Kind> Events, List<string> EventMessages)> RunAgentInvocationAsync(
        AsyncDuplexStreamingCall<ClientMessage, ServerMessage> call,
        Func<AsyncDuplexStreamingCall<ClientMessage, ServerMessage>, CancellationToken, Task> writeRequestAsync,
        ITestOutputHelper output,
        CancellationTokenSource cts)
    {
        output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Agent invocation started (timeout {AgentTimeoutSeconds}s, log every {LogIntervalSeconds}s).");

        var requestTask = Task.Run(async () =>
        {
            await writeRequestAsync(call, cts.Token).ConfigureAwait(false);
        }, cts.Token);

        var (outputs, errors, events, eventMessages) = await ReadResponseStreamWithLoggingAsync(
            call.ResponseStream, call.RequestStream, output, cts.Token).ConfigureAwait(false);

        await WaitRequestTaskOrTimeout(requestTask, output).ConfigureAwait(false);
        return (outputs, errors, events, eventMessages);
    }

    /// <summary>Reads the response stream until end or cancellation. Logs accumulated stdout/stderr to xUnit every second. Does not enforce a timeout.</summary>
    private static async Task<(List<string> Outputs, List<string> Errors, List<SessionEvent.Types.Kind> Events, List<string> EventMessages)> ReadResponseStreamWithLoggingAsync(
        IAsyncStreamReader<ServerMessage> responseStream,
        IClientStreamWriter<ClientMessage> requestStream,
        ITestOutputHelper output,
        CancellationToken cancellationToken)
    {
        var outputs = new List<string>();
        var errors = new List<string>();
        var events = new List<SessionEvent.Types.Kind>();
        var eventMessages = new List<string>();
        var sync = new object();

        var logCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var logTask = Task.Run(async () =>
        {
            while (!logCts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(LogIntervalSeconds), logCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                lock (sync)
                {
                    if (outputs.Count == 0 && errors.Count == 0)
                        continue;
                    var now = DateTime.UtcNow;
                    if (outputs.Count > 0)
                        output.WriteLine($"[{now:HH:mm:ss}] stdout: {string.Join(Environment.NewLine, outputs)}");
                    if (errors.Count > 0)
                        output.WriteLine($"[{now:HH:mm:ss}] stderr: {string.Join(Environment.NewLine, errors)}");
                }
            }
        }, logCts.Token);

        try
        {
            while (await responseStream.MoveNext(cancellationToken).ConfigureAwait(false))
            {
                var msg = responseStream.Current;
                lock (sync)
                {
                    if (msg.PayloadCase == ServerMessage.PayloadOneofCase.Output)
                        outputs.Add(msg.Output);
                    if (msg.PayloadCase == ServerMessage.PayloadOneofCase.Error)
                        errors.Add(msg.Error);
                    if (msg.PayloadCase == ServerMessage.PayloadOneofCase.Event && msg.Event != null)
                    {
                        events.Add(msg.Event.Kind);
                        eventMessages.Add(msg.Event.Message ?? "");
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when caller cancels (e.g. timeout).
        }

        logCts.Cancel();
        try
        {
            await logTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // expected
        }

        lock (sync)
        {
            return (new List<string>(outputs), new List<string>(errors), new List<SessionEvent.Types.Kind>(events), new List<string>(eventMessages));
        }
    }

    /// <summary>Waits for the request (write) task with a short timeout so the test does not hang if the stream is stuck.</summary>
    private static async Task WaitRequestTaskOrTimeout(Task requestTask, ITestOutputHelper? output = null)
    {
        var delay = Task.Delay(TimeSpan.FromSeconds(RequestWaitSecondsAfterReturn));
        var completed = await Task.WhenAny(requestTask, delay).ConfigureAwait(false);
        if (completed == delay)
            output?.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Request task did not complete within {RequestWaitSecondsAfterReturn}s (stream likely closed).");
        else
            await requestTask.ConfigureAwait(false);
    }
}

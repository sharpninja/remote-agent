using System.Collections.Generic;
using Grpc.Core;
using RemoteAgent.Proto;
using Xunit.Abstractions;

namespace RemoteAgent.Service.IntegrationTests;

/// <summary>Helper for Connect tests: run agent invocation in a task, wait with timeout, then stop agent/complete stream if needed; logs agent stdout/stderr every second.</summary>
public static class AgentGatewayTestHelper
{
    public const int AgentTimeoutSeconds = 20;
    public const int LogIntervalSeconds = 1;
    public const int RequestWaitSecondsAfterReturn = 3;

    /// <summary>Runs the agent invocation asynchronously, waits up to timeout for completion. If timeout expires, sends Stop and completes request stream, then cancels call.</summary>
    public static async Task<(List<string> Outputs, List<string> Errors, List<SessionEvent.Types.Kind> Events, List<string> EventMessages)> RunAgentInvocationWithTimeoutAsync(
        AgentGateway.AgentGatewayClient client,
        Func<AsyncDuplexStreamingCall<ClientMessage, ServerMessage>, CancellationToken, Task> writeRequestAsync,
        ITestOutputHelper output)
    {
        output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] RunAgentInvocationWithTimeoutAsync: starting.");
        return await Task.Run(async () =>
        {
            var cts = new CancellationTokenSource();
            output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] RunAgentInvocationWithTimeoutAsync: calling Connect().");
            var call = client.Connect(cancellationToken: cts.Token);
            output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] RunAgentInvocationWithTimeoutAsync: Connect() returned, starting agent task.");

            var agentTask = RunAgentInvocationAsync(call, writeRequestAsync, output, cts);

            output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] RunAgentInvocationWithTimeoutAsync: waiting up to {AgentTimeoutSeconds}s for agent task or timeout.");
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(AgentTimeoutSeconds));
            var completed = await Task.WhenAny(agentTask, timeoutTask).ConfigureAwait(false);
            output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] RunAgentInvocationWithTimeoutAsync: wait finished (completed={(completed == agentTask ? "agentTask" : "timeout")}).");

            if (completed == timeoutTask)
            {
                output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Timeout after {AgentTimeoutSeconds}s — sending Stop and completing request stream.");
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
                finally
                {
                    cts.Cancel();
                }

                var waitTask = Task.WhenAny(agentTask, Task.Delay(TimeSpan.FromSeconds(RequestWaitSecondsAfterReturn)));
                await waitTask.ConfigureAwait(false);
                if (!agentTask.IsCompleted)
                    output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Agent task did not finish within {RequestWaitSecondsAfterReturn}s after timeout cleanup.");
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

    private static async Task<(List<string> Outputs, List<string> Errors, List<SessionEvent.Types.Kind> Events, List<string> EventMessages)> RunAgentInvocationAsync(
        AsyncDuplexStreamingCall<ClientMessage, ServerMessage> call,
        Func<AsyncDuplexStreamingCall<ClientMessage, ServerMessage>, CancellationToken, Task> writeRequestAsync,
        ITestOutputHelper output,
        CancellationTokenSource cts)
    {
        output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] RunAgentInvocationAsync: started (timeout {AgentTimeoutSeconds}s, log every {LogIntervalSeconds}s).");

        var requestTask = Task.Run(async () =>
        {
            output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] RunAgentInvocationAsync: request (write) task started.");
            try
            {
                await writeRequestAsync(call, cts.Token).ConfigureAwait(false);
                output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] RunAgentInvocationAsync: request (write) task completed.");
            }
            catch (Exception ex)
            {
                output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] RunAgentInvocationAsync: request (write) task failed: {ex.Message}");
                throw;
            }
        }, cts.Token);

        output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] RunAgentInvocationAsync: starting ReadResponseStreamWithLoggingAsync.");
        var (outputs, errors, events, eventMessages) = await ReadResponseStreamWithLoggingAsync(
            call.ResponseStream, output, cts.Token).ConfigureAwait(false);
        output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] RunAgentInvocationAsync: ReadResponseStreamWithLoggingAsync returned (outputs={outputs.Count}, errors={errors.Count}, events={events.Count}).");

        output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] RunAgentInvocationAsync: waiting for request task.");
        await WaitRequestTaskOrTimeout(requestTask, output).ConfigureAwait(false);
        output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] RunAgentInvocationAsync: done.");
        return (outputs, errors, events, eventMessages);
    }

    private static async Task<(List<string> Outputs, List<string> Errors, List<SessionEvent.Types.Kind> Events, List<string> EventMessages)> ReadResponseStreamWithLoggingAsync(
        IAsyncStreamReader<ServerMessage> responseStream,
        ITestOutputHelper output,
        CancellationToken cancellationToken)
    {
        var outputs = new List<string>();
        var errors = new List<string>();
        var events = new List<SessionEvent.Types.Kind>();
        var eventMessages = new List<string>();
        var sync = new object();
        var messageCount = 0;

        output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] ReadResponseStreamWithLoggingAsync: starting read loop.");
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
            while (true)
            {
                output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] ReadResponseStreamWithLoggingAsync: calling MoveNext (message #{messageCount + 1}).");
                var hasNext = await responseStream.MoveNext(cancellationToken).ConfigureAwait(false);
                if (!hasNext)
                {
                    output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] ReadResponseStreamWithLoggingAsync: MoveNext returned false, stream ended.");
                    break;
                }
                messageCount++;
                var msg = responseStream.Current;
                lock (sync)
                {
                    if (msg.PayloadCase == ServerMessage.PayloadOneofCase.Output)
                    {
                        outputs.Add(msg.Output);
                        output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] ReadResponseStreamWithLoggingAsync: received Output (#{messageCount}).");
                    }
                    if (msg.PayloadCase == ServerMessage.PayloadOneofCase.Error)
                    {
                        errors.Add(msg.Error);
                        output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] ReadResponseStreamWithLoggingAsync: received Error (#{messageCount}).");
                    }
                    if (msg.PayloadCase == ServerMessage.PayloadOneofCase.Event && msg.Event != null)
                    {
                        events.Add(msg.Event.Kind);
                        eventMessages.Add(msg.Event.Message ?? "");
                        output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] ReadResponseStreamWithLoggingAsync: received Event {msg.Event.Kind} (#{messageCount}).");
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] ReadResponseStreamWithLoggingAsync: OperationCanceledException (expected on timeout/cancel).");
        }
        catch (Exception ex)
        {
            output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] ReadResponseStreamWithLoggingAsync: exception: {ex.GetType().Name} — {ex.Message}");
            throw;
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

    private static async Task WaitRequestTaskOrTimeout(Task requestTask, ITestOutputHelper? output = null)
    {
        output?.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] WaitRequestTaskOrTimeout: waiting up to {RequestWaitSecondsAfterReturn}s for request task.");
        var delay = Task.Delay(TimeSpan.FromSeconds(RequestWaitSecondsAfterReturn));
        var completed = await Task.WhenAny(requestTask, delay).ConfigureAwait(false);
        if (completed == delay)
            output?.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] WaitRequestTaskOrTimeout: request task did not complete within {RequestWaitSecondsAfterReturn}s (stream likely closed).");
        else
        {
            await requestTask.ConfigureAwait(false);
            output?.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] WaitRequestTaskOrTimeout: request task completed.");
        }
    }
}

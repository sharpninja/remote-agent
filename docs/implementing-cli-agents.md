# Implementing a New CLI Agent with the Server

This guide describes how to implement a new CLI agent that integrates with the Remote Agent server. The Remote Agent system supports pluggable CLI agents through an extensible architecture based on the strategy pattern (TR-10.1).

## Overview

The Remote Agent server can work with different CLI agents through two approaches:

1. **Process-based agents** – Execute an external command-line program (default implementation)
2. **Plugin-based agents** – Load custom .NET assemblies that implement agent behavior programmatically

Both approaches use the `IAgentRunner` interface to provide a uniform way to start and manage agent sessions.

## Architecture

### Core Interfaces

#### IAgentRunner

The `IAgentRunner` interface is the foundation for all agent implementations:

```csharp
namespace RemoteAgent.Service.Agents;

public interface IAgentRunner
{
    /// <summary>Starts an agent session.</summary>
    /// <param name="command">Agent command (e.g. executable path). May be ignored by plugin runners.</param>
    /// <param name="arguments">Optional arguments. May be ignored by plugin runners.</param>
    /// <param name="sessionId">Session identifier for logging.</param>
    /// <param name="logWriter">Optional session log writer.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    Task<IAgentSession?> StartAsync(
        string? command,
        string? arguments,
        string sessionId,
        StreamWriter? logWriter,
        CancellationToken cancellationToken = default);
}
```

#### IAgentSession

The `IAgentSession` interface represents an active agent session with communication channels:

```csharp
namespace RemoteAgent.Service.Agents;

public interface IAgentSession : IDisposable
{
    /// <summary>Standard input stream to send messages to the agent.</summary>
    StreamWriter StandardInput { get; }
    
    /// <summary>Standard output stream to receive agent output.</summary>
    StreamReader StandardOutput { get; }
    
    /// <summary>Standard error stream to receive agent errors.</summary>
    StreamReader StandardError { get; }
    
    /// <summary>Wait for the agent to exit.</summary>
    Task WaitForExitAsync(CancellationToken cancellationToken = default);
}
```

## Implementation Option 1: Process-Based Agent

Process-based agents are the simplest approach. The server spawns an external executable and communicates through standard input/output streams.

### Configuration

Configure the agent in `appsettings.json` or `appsettings.Development.json`:

```json
{
  "Agent": {
    "Command": "/path/to/your/agent",
    "Arguments": "--option value",
    "LogDirectory": "/var/log/remote-agent",
    "RunnerId": "process"
  }
}
```

### Configuration Options

- **Command**: Full path to the executable or script
- **Arguments**: Command-line arguments to pass to the agent
- **LogDirectory**: Directory for session log files (defaults to temp directory)
- **RunnerId**: Runner implementation to use. When unset or empty: Linux (and other non-Windows) defaults to `"process"` (Cursor/agent CLI); Windows defaults to `"copilot-windows"`. Use `"process"` or `"copilot-windows"` explicitly to override.
- **DataDirectory**: Directory for LiteDB storage and uploaded media (defaults to ./data)

### Example: GitHub Copilot CLI on Windows

Use the built-in **copilot-windows** strategy to run [GitHub Copilot CLI](https://docs.github.com/en/copilot/how-tos/copilot-cli) on Windows. Install Copilot CLI (e.g. `winget install GitHub.Copilot` or `npm install -g @github/copilot`), then set `RunnerId` to `"copilot-windows"`. If `Command` is not set, the service uses `copilot` (from PATH).

```json
{
  "Agent": {
    "Command": "",
    "Arguments": "",
    "LogDirectory": "",
    "RunnerId": "copilot-windows"
  }
}
```

Optionally set `Command` to the full path of `copilot.exe` if it is not on PATH.

### Example: Simple Echo Agent

The simplest test agent echoes input back as output:

```json
{
  "Agent": {
    "Command": "/bin/cat",
    "Arguments": "",
    "LogDirectory": "/tmp/agent-logs"
  }
}
```

### Example: Python Agent

```json
{
  "Agent": {
    "Command": "/usr/bin/python3",
    "Arguments": "/path/to/your/agent.py --mode interactive",
    "LogDirectory": "/var/log/remote-agent"
  }
}
```

### Example: Node.js Agent

```json
{
  "Agent": {
    "Command": "/usr/bin/node",
    "Arguments": "/path/to/your/agent.js",
    "LogDirectory": "/var/log/remote-agent"
  }
}
```

### Agent Requirements

Process-based agents must:

1. **Read from stdin** – Accept input messages line-by-line from standard input
2. **Write to stdout** – Send output messages to standard output
3. **Write to stderr** – Send error messages to standard error
4. **Support graceful termination** – Handle SIGTERM and exit cleanly
5. **Flush output** – Ensure output is flushed after each message for real-time streaming

### Example Agent Template (Python)

```python
#!/usr/bin/env python3
import sys
import signal

def handle_sigterm(signum, frame):
    """Handle termination signal gracefully."""
    sys.stdout.write("Agent shutting down...\n")
    sys.stdout.flush()
    sys.exit(0)

signal.signal(signal.SIGTERM, handle_sigterm)

def main():
    sys.stdout.write("Agent started and ready\n")
    sys.stdout.flush()
    
    # Read commands from stdin
    for line in sys.stdin:
        command = line.strip()
        
        if not command:
            continue
            
        # Process the command
        try:
            # Your agent logic here
            result = f"Processed: {command}"
            sys.stdout.write(f"{result}\n")
            sys.stdout.flush()
        except Exception as e:
            sys.stderr.write(f"Error: {str(e)}\n")
            sys.stderr.flush()

if __name__ == "__main__":
    main()
```

### Example Agent Template (Node.js)

```javascript
#!/usr/bin/env node
const readline = require('readline');

// Handle graceful termination
process.on('SIGTERM', () => {
    console.log('Agent shutting down...');
    process.exit(0);
});

// Create readline interface for stdin
const rl = readline.createInterface({
    input: process.stdin,
    output: process.stdout,
    terminal: false
});

console.log('Agent started and ready');

// Process each line from stdin
rl.on('line', (line) => {
    const command = line.trim();
    
    if (!command) return;
    
    try {
        // Your agent logic here
        const result = `Processed: ${command}`;
        console.log(result);
    } catch (error) {
        console.error(`Error: ${error.message}`);
    }
});

rl.on('close', () => {
    console.log('Agent session ended');
    process.exit(0);
});
```

## Implementation Option 2: Plugin-Based Agent

Plugin-based agents are .NET assemblies that implement `IAgentRunner` and optionally `IAgentSession`. This approach provides full programmatic control over agent behavior.

### Step 1: Create a Plugin Project

Create a new .NET class library project:

```bash
dotnet new classlib -n MyCustomAgent -f net10.0
cd MyCustomAgent
dotnet add reference /path/to/RemoteAgent.Service/RemoteAgent.Service.csproj
```

### Step 2: Implement IAgentRunner

```csharp
using RemoteAgent.Service.Agents;
using Microsoft.Extensions.Logging;

namespace MyCustomAgent;

public class CustomAgentRunner : IAgentRunner
{
    private readonly ILogger<CustomAgentRunner> _logger;
    
    public CustomAgentRunner(ILogger<CustomAgentRunner> logger)
    {
        _logger = logger;
    }
    
    public async Task<IAgentSession?> StartAsync(
        string? command,
        string? arguments,
        string sessionId,
        StreamWriter? logWriter,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting custom agent for session {SessionId}", sessionId);
        
        try
        {
            // Initialize your agent
            var session = new CustomAgentSession(sessionId, logWriter, _logger);
            await session.InitializeAsync(cancellationToken);
            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start custom agent");
            return null;
        }
    }
}
```

### Step 3: Implement IAgentSession

```csharp
using System.IO.Pipelines;
using RemoteAgent.Service.Agents;
using Microsoft.Extensions.Logging;

namespace MyCustomAgent;

public class CustomAgentSession : IAgentSession
{
    private readonly string _sessionId;
    private readonly StreamWriter? _logWriter;
    private readonly ILogger _logger;
    private readonly Pipe _inputPipe;
    private readonly Pipe _outputPipe;
    private readonly Pipe _errorPipe;
    private readonly CancellationTokenSource _cts;
    private Task? _processingTask;
    
    public CustomAgentSession(string sessionId, StreamWriter? logWriter, ILogger logger)
    {
        _sessionId = sessionId;
        _logWriter = logWriter;
        _logger = logger;
        _inputPipe = new Pipe();
        _outputPipe = new Pipe();
        _errorPipe = new Pipe();
        _cts = new CancellationTokenSource();
    }
    
    public StreamWriter StandardInput => new StreamWriter(_inputPipe.Writer.AsStream());
    public StreamReader StandardOutput => new StreamReader(_outputPipe.Reader.AsStream());
    public StreamReader StandardError => new StreamReader(_errorPipe.Reader.AsStream());
    
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        // Start background processing
        _processingTask = ProcessMessagesAsync(_cts.Token);
        await Task.CompletedTask;
    }
    
    private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
    {
        var reader = new StreamReader(_inputPipe.Reader.AsStream());
        var writer = new StreamWriter(_outputPipe.Writer.AsStream()) { AutoFlush = true };
        
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line == null) break;
                
                // Process the input
                var result = await ProcessCommandAsync(line, cancellationToken);
                await writer.WriteLineAsync(result);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing messages");
        }
    }
    
    private async Task<string> ProcessCommandAsync(string command, CancellationToken cancellationToken)
    {
        // Implement your agent logic here
        await Task.Delay(10, cancellationToken); // Simulate work
        return $"Processed: {command}";
    }
    
    public async Task WaitForExitAsync(CancellationToken cancellationToken = default)
    {
        if (_processingTask != null)
        {
            await _processingTask;
        }
    }
    
    public void Dispose()
    {
        _cts.Cancel();
        _processingTask?.Wait(TimeSpan.FromSeconds(5));
        _cts.Dispose();
    }
}
```

### Step 4: Build the Plugin

```bash
dotnet build -c Release
```

### Step 5: Configure the Server to Load the Plugin

Add the plugin to `appsettings.json`:

```json
{
  "Agent": {
    "RunnerId": "MyCustomAgent.CustomAgentRunner",
    "LogDirectory": "/var/log/remote-agent"
  },
  "Plugins": {
    "Assemblies": [
      "/path/to/MyCustomAgent.dll"
    ]
  }
}
```

The `RunnerId` should match the full type name of your runner implementation. The plugin loader will discover and register it by this name.

## Plugin Discovery and Registration

The server uses `PluginLoader` to discover and register agent runners:

1. **Built-in runner**: The "process" runner is always available
2. **Plugin assemblies**: Loaded from paths specified in `Plugins:Assemblies`
3. **Type discovery**: Each assembly is scanned for types implementing `IAgentRunner`
4. **Registration**: Runners are registered by their full type name (e.g., "MyCustomAgent.CustomAgentRunner")
5. **Dependency injection**: Plugins can use constructor injection to access services

### Plugin Dependencies

Plugin runners can inject services through their constructor:

```csharp
public class AdvancedAgentRunner : IAgentRunner
{
    private readonly ILogger<AdvancedAgentRunner> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    
    public AdvancedAgentRunner(
        ILogger<AdvancedAgentRunner> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }
    
    // Implementation...
}
```

## Agent Communication Protocol

### Input Messages

Agents receive messages from the client through standard input (or the session's input pipe). Each message is typically a line of text terminated by a newline character.

### Output Messages

Agents send responses through:
- **Standard output** – For normal responses (mapped to `ServerMessage.output`)
- **Standard error** – For errors and diagnostics (mapped to `ServerMessage.error`)

### Message Flow

1. Client sends `ClientMessage` with text or control commands
2. Server forwards text messages to the agent's stdin
3. Agent processes the message and writes to stdout/stderr
4. Server reads agent output and streams it to client as `ServerMessage`

### Session Lifecycle

1. **START**: Client sends SessionControl.START
   - Server calls `IAgentRunner.StartAsync()`
   - Session begins when `IAgentSession` is returned
   - Server sends `SessionEvent.SESSION_STARTED`

2. **ACTIVE**: Messages flow bidirectionally
   - Client text → Agent stdin
   - Agent stdout → Client output
   - Agent stderr → Client error

3. **STOP**: Client sends SessionControl.STOP or disconnects
   - Server disposes the `IAgentSession`
   - Agent receives termination signal
   - Server sends `SessionEvent.SESSION_STOPPED`

## Advanced Features

### Multiple Agent Support

The server can host multiple agent types simultaneously. Clients can select which agent to use:

```json
{
  "Agent": {
    "RunnerId": "process"
  },
  "Plugins": {
    "Assemblies": [
      "plugins/CursorAgent.dll",
      "plugins/CustomAI.dll",
      "plugins/CodeAnalyzer.dll"
    ]
  }
}
```

Clients call `GetServerInfo()` to retrieve the list of available agents and specify the desired agent when starting a session.

### Local Storage Integration

Agents can integrate with the server's LiteDB storage:

```csharp
public class StorageAwareRunner : IAgentRunner
{
    private readonly ILocalStorage _localStorage;
    
    public StorageAwareRunner(ILocalStorage localStorage)
    {
        _localStorage = localStorage;
    }
    
    public async Task<IAgentSession?> StartAsync(...)
    {
        // Access persisted data
        var history = _localStorage.GetRecentRequests(sessionId, 10);
        // Use history to provide context to the agent
        return new MySession(history);
    }
}
```

### Media Handling

Agents can access uploaded media through the `MediaStorageService`:

```csharp
public class MediaAwareRunner : IAgentRunner
{
    private readonly MediaStorageService _mediaStorage;
    
    public MediaAwareRunner(MediaStorageService mediaStorage)
    {
        _mediaStorage = mediaStorage;
    }
    
    public async Task<IAgentSession?> StartAsync(...)
    {
        // Plugin agents can access uploaded media
        // Media is stored in Agent:DataDirectory
        return new MediaSession(_mediaStorage);
    }
}
```

## Testing Your Agent

### Unit Testing Process-Based Agents

Test your external executable independently:

```bash
# Test input/output
echo "test command" | /path/to/your/agent

# Test with file input
/path/to/your/agent < test_input.txt

# Monitor stderr
/path/to/your/agent 2> errors.log
```

### Unit Testing Plugin Agents

Create xUnit tests for your plugin:

```csharp
public class CustomAgentRunnerTests
{
    [Fact]
    public async Task StartAsync_ShouldReturnSession()
    {
        var logger = new Mock<ILogger<CustomAgentRunner>>().Object;
        var runner = new CustomAgentRunner(logger);
        
        var session = await runner.StartAsync(
            null, null, "test-session", null, CancellationToken.None);
            
        Assert.NotNull(session);
    }
    
    [Fact]
    public async Task Session_ShouldProcessInput()
    {
        // Test agent session behavior
        var session = /* create session */;
        
        await session.StandardInput.WriteLineAsync("test input");
        var output = await session.StandardOutput.ReadLineAsync();
        
        Assert.Contains("test input", output);
    }
}
```

### Integration Testing

Run the server with your agent and test with a client:

```bash
# Start server with your agent
dotnet run --project src/RemoteAgent.Service

# In another terminal, test with grpcurl
grpcurl -plaintext -d @ localhost:5243 proto.AgentGateway/Connect
```

## Deployment

### Docker Deployment with Custom Agent

#### Process-Based Agent

Mount your agent executable into the container:

```bash
docker run -p 5243:5243 \
  -e Agent__Command=/app/custom-agent \
  -e Agent__LogDirectory=/app/logs \
  -v /host/path/to/agent:/app/custom-agent:ro \
  -v /host/logs:/app/logs \
  ghcr.io/sharpninja/remote-agent/service:latest
```

#### Plugin-Based Agent

Build a custom Docker image with your plugin:

```dockerfile
FROM ghcr.io/sharpninja/remote-agent/service:latest

# Copy plugin assembly
COPY MyCustomAgent/bin/Release/net10.0/MyCustomAgent.dll /app/plugins/

# Override appsettings
COPY custom-appsettings.json /app/appsettings.Production.json
```

Build and run:

```bash
docker build -t custom-remote-agent .
docker run -p 5243:5243 \
  -e Agent__RunnerId=MyCustomAgent.CustomAgentRunner \
  custom-remote-agent
```

### Environment Variables

Override configuration via environment variables:

- `Agent__Command` – Agent executable path
- `Agent__Arguments` – Agent arguments
- `Agent__LogDirectory` – Log directory
- `Agent__RunnerId` – Runner implementation
- `Agent__DataDirectory` – Data directory
- `Plugins__Assemblies__0` – First plugin path
- `Plugins__Assemblies__1` – Second plugin path

Example:

```bash
export Agent__Command="/usr/local/bin/my-agent"
export Agent__LogDirectory="/var/log/remote-agent"
export Plugins__Assemblies__0="/opt/plugins/CustomAgent.dll"
dotnet run --project src/RemoteAgent.Service
```

## Troubleshooting

### Agent Not Starting

**Problem**: Server logs "Agent:Command not configured"

**Solution**: 
- Verify `Agent:Command` is set in appsettings.json
- Ensure the path is absolute and the file exists
- Check file permissions (must be executable)

### No Output from Agent

**Problem**: Agent runs but no output appears in client

**Solution**:
- Ensure agent flushes stdout after each write
- Check agent is writing to stdout, not stderr
- Verify agent is line-buffered, not block-buffered
- Test agent independently: `echo "test" | /path/to/agent`

### Plugin Not Loading

**Problem**: Plugin runner not found in registry

**Solution**:
- Verify assembly path in `Plugins:Assemblies` is correct
- Check assembly exists and is readable
- Ensure assembly targets net10.0
- Verify type implements `IAgentRunner`
- Check for missing dependencies in plugin assembly
- Review server logs for plugin loading errors

### Agent Crashes or Hangs

**Problem**: Agent process exits unexpectedly or stops responding

**Solution**:
- Check agent logs in `Agent:LogDirectory`
- Test agent with manual input
- Add error handling in agent code
- Verify agent handles SIGTERM gracefully
- Check for deadlocks in async code
- Monitor resource usage (memory, CPU)

## Best Practices

### Security

1. **Validate input** – Always sanitize and validate messages before processing
2. **Limit permissions** – Run agents with minimal required permissions
3. **Sandbox execution** – Consider containerizing agents for isolation
4. **Avoid shell injection** – Don't pass user input directly to shell commands
5. **Encrypt sensitive data** – Use secure storage for credentials and secrets

### Performance

1. **Use async I/O** – Prefer async operations for all I/O
2. **Buffer efficiently** – Use appropriate buffer sizes for streaming
3. **Flush promptly** – Flush output after each message for low latency
4. **Manage resources** – Dispose streams and processes properly
5. **Handle backpressure** – Implement flow control for high-volume scenarios

### Reliability

1. **Handle errors gracefully** – Catch exceptions and report them via stderr
2. **Log comprehensively** – Use the provided log writer for debugging
3. **Support cancellation** – Respect cancellation tokens
4. **Implement health checks** – Respond to ping/health messages
5. **Test edge cases** – Test with empty input, large messages, special characters

### Maintainability

1. **Follow conventions** – Match the coding style of the project
2. **Document behavior** – Add XML comments to public APIs
3. **Write tests** – Create unit and integration tests
4. **Version APIs** – Use semantic versioning for plugin APIs
5. **Provide examples** – Include sample configurations and usage

## Examples and Templates

The repository includes example agents:

- **Echo agent** (`/bin/cat`) – Simple test agent that echoes input
- **ProcessAgentRunner** – Reference implementation for process-based agents
- See `tests/RemoteAgent.Service.Tests` for integration test examples

## Summary

To implement a new CLI agent:

1. **Choose approach**: Process-based (simplest) or plugin-based (most flexible)
2. **Implement interface**: Follow the `IAgentRunner` contract
3. **Handle I/O**: Read from stdin, write to stdout/stderr
4. **Configure**: Update appsettings.json with agent settings
5. **Test**: Verify agent behavior independently and with server
6. **Deploy**: Use Docker or direct deployment as appropriate

For questions or support, see the [main README](https://github.com/sharpninja/remote-agent#readme) or open an issue on GitHub.

## References

- **Functional Requirements**: [functional-requirements.md](functional-requirements.md) – See FR-8.1 for plugin extensibility
- **Technical Requirements**: [technical-requirements.md](technical-requirements.md) – See TR-10.1 and TR-10.2 for architecture
- **Source Code**: 
  - `src/RemoteAgent.Service/Agents/IAgentRunner.cs` – Core interface
  - `src/RemoteAgent.Service/Agents/ProcessAgentRunner.cs` – Default implementation
  - `src/RemoteAgent.Service/PluginLoader.cs` – Plugin discovery
- **Tests**: `tests/RemoteAgent.Service.Tests` – Integration test examples

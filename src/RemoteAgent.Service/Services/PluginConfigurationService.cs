using System.Text.Json;
using Microsoft.Extensions.Options;

namespace RemoteAgent.Service.Services;

/// <summary>Stores plugin assembly configuration for management APIs.</summary>
public sealed class PluginConfigurationService
{
    private readonly object _sync = new();
    private readonly string _filePath;
    private readonly ILogger<PluginConfigurationService> _logger;
    private List<string> _assemblies;

    public PluginConfigurationService(IOptions<AgentOptions> agentOptions, IOptions<PluginsOptions> pluginsOptions, ILogger<PluginConfigurationService> logger)
    {
        _logger = logger;
        var dataDir = agentOptions.Value.DataDirectory?.Trim();
        if (string.IsNullOrWhiteSpace(dataDir))
            dataDir = Path.Combine(AppContext.BaseDirectory, "data");
        Directory.CreateDirectory(dataDir);
        _filePath = Path.Combine(dataDir, "plugins.json");
        _assemblies = new List<string>(pluginsOptions.Value.Assemblies ?? []);

        var persisted = TryReadPersisted();
        if (persisted.Count > 0)
            _assemblies = persisted;
    }

    public IReadOnlyList<string> GetAssemblies()
    {
        lock (_sync)
        {
            return _assemblies.ToArray();
        }
    }

    public IReadOnlyList<string> UpdateAssemblies(IEnumerable<string> assemblies)
    {
        var sanitized = assemblies
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        lock (_sync)
        {
            _assemblies = sanitized;
            TryPersist(_assemblies);
            return _assemblies.ToArray();
        }
    }

    private List<string> TryReadPersisted()
    {
        try
        {
            if (!File.Exists(_filePath))
                return [];
            var json = File.ReadAllText(_filePath);
            var doc = JsonSerializer.Deserialize<PluginConfigDocument>(json);
            return doc?.Assemblies?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read persisted plugin configuration");
            return [];
        }
    }

    private void TryPersist(IReadOnlyList<string> assemblies)
    {
        try
        {
            var doc = new PluginConfigDocument { Assemblies = assemblies.ToList() };
            var json = JsonSerializer.Serialize(doc);
            File.WriteAllText(_filePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist plugin configuration");
        }
    }

    private sealed class PluginConfigDocument
    {
        public List<string> Assemblies { get; set; } = [];
    }
}

using LiteDB;
using Microsoft.Extensions.Options;

namespace RemoteAgent.Service.Services;

/// <summary>Stores reusable prompt templates for chat clients. Templates use Handlebars-compatible placeholder syntax.</summary>
public sealed class PromptTemplateService
{
    private readonly string _dbPath;
    private const string CollectionName = "prompt_templates";

    public PromptTemplateService(IOptions<AgentOptions> options)
    {
        var dataDir = options.Value.DataDirectory?.Trim();
        if (string.IsNullOrWhiteSpace(dataDir))
            dataDir = Path.Combine(AppContext.BaseDirectory, "data");
        Directory.CreateDirectory(dataDir);
        _dbPath = Path.Combine(dataDir, "remote-agent.db");
    }

    public IReadOnlyList<PromptTemplateRecord> List()
    {
        using var db = new LiteDatabase(_dbPath);
        var col = db.GetCollection<PromptTemplateRecord>(CollectionName);
        col.EnsureIndex(x => x.TemplateId, unique: true);
        SeedDefaults(col);
        return col.FindAll().OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public PromptTemplateRecord Upsert(PromptTemplateRecord record)
    {
        using var db = new LiteDatabase(_dbPath);
        var col = db.GetCollection<PromptTemplateRecord>(CollectionName);
        col.EnsureIndex(x => x.TemplateId, unique: true);

        var now = DateTimeOffset.UtcNow;
        var id = string.IsNullOrWhiteSpace(record.TemplateId)
            ? GenerateId(record.DisplayName)
            : record.TemplateId.Trim();

        var existing = col.FindOne(x => x.TemplateId == id);
        var current = new PromptTemplateRecord
        {
            TemplateId = id,
            DisplayName = string.IsNullOrWhiteSpace(record.DisplayName) ? id : record.DisplayName.Trim(),
            Description = record.Description?.Trim() ?? "",
            TemplateContent = record.TemplateContent?.Trim() ?? "",
            CreatedUtc = existing?.CreatedUtc ?? now,
            UpdatedUtc = now
        };

        col.Upsert(current);
        return current;
    }

    public bool Delete(string templateId)
    {
        if (string.IsNullOrWhiteSpace(templateId)) return false;
        using var db = new LiteDatabase(_dbPath);
        var col = db.GetCollection<PromptTemplateRecord>(CollectionName);
        return col.DeleteMany(x => x.TemplateId == templateId.Trim()) > 0;
    }

    private static string GenerateId(string? name)
    {
        var baseValue = string.IsNullOrWhiteSpace(name) ? "prompt-template" : name.Trim().ToLowerInvariant();
        var chars = baseValue.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray();
        var cleaned = new string(chars);
        return string.IsNullOrWhiteSpace(cleaned) ? "prompt-template" : cleaned;
    }

    private static void SeedDefaults(ILiteCollection<PromptTemplateRecord> col)
    {
        if (col.Count() > 0) return;

        var now = DateTimeOffset.UtcNow;
        col.Insert(new PromptTemplateRecord
        {
            TemplateId = "bug-triage",
            DisplayName = "Bug Triage",
            Description = "Summarize a bug report and propose root-cause hypotheses.",
            TemplateContent = "You are triaging a defect. Title: {{title}}\nEnvironment: {{environment}}\nObserved behavior: {{observed_behavior}}\nExpected behavior: {{expected_behavior}}\nProvide likely causes and next debugging steps.",
            CreatedUtc = now,
            UpdatedUtc = now
        });

        col.Insert(new PromptTemplateRecord
        {
            TemplateId = "change-plan",
            DisplayName = "Change Plan",
            Description = "Create an implementation plan with risk controls.",
            TemplateContent = "Create an implementation plan for {{feature_name}} in {{component}}. Constraints: {{constraints}}. Include phased rollout, validation, and rollback.",
            CreatedUtc = now,
            UpdatedUtc = now
        });
    }
}

public sealed class PromptTemplateRecord
{
    [BsonId]
    public string TemplateId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public string TemplateContent { get; set; } = "";
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
}

using Deepr.Domain.Enums;

namespace Deepr.Domain.ValueObjects;

public class CouncilMember
{
    public Guid AgentId { get; private set; }
    public string Name { get; private set; }
    public Role Role { get; private set; }
    public bool IsAi { get; private set; }
    public string? SystemPromptOverride { get; private set; }
    /// <summary>References a configured AI provider name (e.g. "openai", "ollama")</summary>
    public string? ModelProvider { get; private set; }
    /// <summary>Overrides the provider's default model (e.g. "gpt-4o-mini", "llama3.1")</summary>
    public string? ModelId { get; private set; }

    private CouncilMember()
    {
        Name = string.Empty;
    }

    public CouncilMember(Guid agentId, string name, Role role, bool isAi,
        string? systemPromptOverride = null, string? modelProvider = null, string? modelId = null)
    {
        AgentId = agentId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Role = role;
        IsAi = isAi;
        SystemPromptOverride = systemPromptOverride;
        ModelProvider = modelProvider;
        ModelId = modelId;
    }

    public void UpdateSystemPrompt(string? systemPrompt)
    {
        SystemPromptOverride = systemPrompt;
    }

    public void AssignRole(Role newRole)
    {
        Role = newRole;
    }

    public void UpdateModelConfig(string? modelProvider, string? modelId)
    {
        ModelProvider = modelProvider;
        ModelId = modelId;
    }
}

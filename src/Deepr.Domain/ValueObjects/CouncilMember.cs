using Deepr.Domain.Enums;

namespace Deepr.Domain.ValueObjects;

public class CouncilMember
{
    public Guid AgentId { get; private set; }
    public string Name { get; private set; }
    public Role Role { get; private set; }
    public bool IsAi { get; private set; }
    public string? SystemPromptOverride { get; private set; }

    private CouncilMember()
    {
        Name = string.Empty;
    }

    public CouncilMember(Guid agentId, string name, Role role, bool isAi, string? systemPromptOverride = null)
    {
        AgentId = agentId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Role = role;
        IsAi = isAi;
        SystemPromptOverride = systemPromptOverride;
    }

    public void UpdateSystemPrompt(string? systemPrompt)
    {
        SystemPromptOverride = systemPrompt;
    }

    public void AssignRole(Role newRole)
    {
        Role = newRole;
    }
}

using Deepr.Domain.Enums;

namespace Deepr.Application.DTOs;

public class CouncilDto
{
    public Guid Id { get; set; }
    public Guid IssueId { get; set; }
    public MethodType SelectedMethod { get; set; }
    public ToolType SelectedTool { get; set; }
    public List<CouncilMemberDto> Agents { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class CouncilMemberDto
{
    public Guid AgentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Role Role { get; set; }
    public bool IsAi { get; set; }
    public string? SystemPromptOverride { get; set; }
}

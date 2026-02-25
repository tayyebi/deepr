namespace Deepr.Web.Models;

public class IssueDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ContextVector { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CouncilDto
{
    public Guid Id { get; set; }
    public Guid IssueId { get; set; }
    public int SelectedMethod { get; set; }
    public int SelectedTool { get; set; }
    public List<CouncilMemberDto> Agents { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class CouncilMemberDto
{
    public Guid AgentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Role { get; set; }
    public bool IsAi { get; set; }
    public string? SystemPromptOverride { get; set; }
}

public class SessionDto
{
    public Guid Id { get; set; }
    public Guid CouncilId { get; set; }
    public int Status { get; set; }
    public int CurrentRoundNumber { get; set; }
    public string StatePayload { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class SessionRoundDto
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public int RoundNumber { get; set; }
    public string Instructions { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public List<ContributionDto> Contributions { get; set; } = new();
}

public class ContributionDto
{
    public Guid Id { get; set; }
    public Guid AgentId { get; set; }
    public string RawContent { get; set; } = string.Empty;
    public string StructuredData { get; set; } = string.Empty;
}

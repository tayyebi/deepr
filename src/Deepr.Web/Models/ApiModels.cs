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

// ─── MCDA standalone module ────────────────────────────────────────────────

/// <summary>A single criterion for a standalone MCDA computation.</summary>
public class McdaCriterionModel
{
    public string Name { get; set; } = string.Empty;
    public double Weight { get; set; } = 1.0;
    public bool IsBenefit { get; set; } = true;
}

/// <summary>Input payload for a standalone MCDA computation.</summary>
public class McdaInputModel
{
    public List<string> Options { get; set; } = new();
    public List<McdaCriterionModel> Criteria { get; set; } = new();
    public Dictionary<string, Dictionary<string, double>> Scores { get; set; } = new();
}

/// <summary>Result returned by the standalone MCDA endpoints.</summary>
public class McdaResultModel
{
    public string Method { get; set; } = string.Empty;
    public List<string> Ranking { get; set; } = new();
    public Dictionary<string, double> Scores { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
}

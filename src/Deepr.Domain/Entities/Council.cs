using Deepr.Domain.Common;
using Deepr.Domain.Enums;
using Deepr.Domain.ValueObjects;

namespace Deepr.Domain.Entities;

public class Council : BaseEntity
{
    public Guid IssueId { get; private set; }
    public MethodType SelectedMethod { get; private set; }
    public ToolType SelectedTool { get; private set; }

    private readonly List<CouncilMember> _agents = new();
    public IReadOnlyCollection<CouncilMember> Agents => _agents.AsReadOnly();

    private Council()
    {
    }

    public Council(Guid issueId, MethodType selectedMethod, ToolType selectedTool)
    {
        IssueId = issueId;
        SelectedMethod = selectedMethod;
        SelectedTool = selectedTool;
    }

    public void AddMember(CouncilMember member)
    {
        if (member == null)
            throw new ArgumentNullException(nameof(member));

        if (_agents.Any(a => a.AgentId == member.AgentId))
            throw new InvalidOperationException($"Agent {member.AgentId} is already a member of this council");

        _agents.Add(member);
    }

    public void RemoveMember(Guid agentId)
    {
        var member = _agents.FirstOrDefault(a => a.AgentId == agentId);
        if (member != null)
        {
            _agents.Remove(member);
        }
    }

    public void ChangeMethod(MethodType newMethod)
    {
        SelectedMethod = newMethod;
    }

    public void ChangeTool(ToolType newTool)
    {
        SelectedTool = newTool;
    }

    public CouncilMember? GetMember(Guid agentId)
    {
        return _agents.FirstOrDefault(a => a.AgentId == agentId);
    }
}

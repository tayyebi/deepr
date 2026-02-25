using Deepr.Application.DTOs;
using Deepr.Application.Interfaces;
using Deepr.Domain.Entities;
using Deepr.Domain.Enums;
using Deepr.Domain.ValueObjects;
using MediatR;

namespace Deepr.Application.Councils.Commands;

public record AddMemberCommand(Guid CouncilId, Guid AgentId, string Name, Role Role, bool IsAi, string? SystemPromptOverride = null) : IRequest<CouncilDto>;

public class AddMemberCommandHandler : IRequestHandler<AddMemberCommand, CouncilDto>
{
    private readonly IRepository<Council> _repository;

    public AddMemberCommandHandler(IRepository<Council> repository)
    {
        _repository = repository;
    }

    public async Task<CouncilDto> Handle(AddMemberCommand request, CancellationToken cancellationToken)
    {
        var council = await _repository.GetByIdAsync(request.CouncilId, cancellationToken)
            ?? throw new InvalidOperationException($"Council {request.CouncilId} not found");

        var member = new CouncilMember(request.AgentId, request.Name, request.Role, request.IsAi, request.SystemPromptOverride);
        council.AddMember(member);
        await _repository.UpdateAsync(council, cancellationToken);

        return new CouncilDto
        {
            Id = council.Id,
            IssueId = council.IssueId,
            SelectedMethod = council.SelectedMethod,
            SelectedTool = council.SelectedTool,
            Agents = council.Agents.Select(a => new CouncilMemberDto
            {
                AgentId = a.AgentId,
                Name = a.Name,
                Role = a.Role,
                IsAi = a.IsAi,
                SystemPromptOverride = a.SystemPromptOverride
            }).ToList(),
            CreatedAt = council.CreatedAt
        };
    }
}

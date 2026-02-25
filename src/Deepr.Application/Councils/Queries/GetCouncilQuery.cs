using Deepr.Application.DTOs;
using Deepr.Application.Interfaces;
using Deepr.Domain.Entities;
using MediatR;

namespace Deepr.Application.Councils.Queries;

public record GetCouncilQuery(Guid Id) : IRequest<CouncilDto?>;

public class GetCouncilQueryHandler : IRequestHandler<GetCouncilQuery, CouncilDto?>
{
    private readonly IRepository<Council> _repository;

    public GetCouncilQueryHandler(IRepository<Council> repository)
    {
        _repository = repository;
    }

    public async Task<CouncilDto?> Handle(GetCouncilQuery request, CancellationToken cancellationToken)
    {
        var council = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (council == null) return null;
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

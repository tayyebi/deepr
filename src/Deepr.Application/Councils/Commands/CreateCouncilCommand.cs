using Deepr.Application.DTOs;
using Deepr.Application.Interfaces;
using Deepr.Domain.Entities;
using Deepr.Domain.Enums;
using MediatR;

namespace Deepr.Application.Councils.Commands;

public record CreateCouncilCommand(Guid IssueId, MethodType SelectedMethod, ToolType SelectedTool) : IRequest<CouncilDto>;

public class CreateCouncilCommandHandler : IRequestHandler<CreateCouncilCommand, CouncilDto>
{
    private readonly IRepository<Council> _repository;

    public CreateCouncilCommandHandler(IRepository<Council> repository)
    {
        _repository = repository;
    }

    public async Task<CouncilDto> Handle(CreateCouncilCommand request, CancellationToken cancellationToken)
    {
        var council = new Council(request.IssueId, request.SelectedMethod, request.SelectedTool);
        await _repository.AddAsync(council, cancellationToken);
        return new CouncilDto
        {
            Id = council.Id,
            IssueId = council.IssueId,
            SelectedMethod = council.SelectedMethod,
            SelectedTool = council.SelectedTool,
            Agents = new List<CouncilMemberDto>(),
            CreatedAt = council.CreatedAt
        };
    }
}

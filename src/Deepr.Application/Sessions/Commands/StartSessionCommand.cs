using Deepr.Application.DTOs;
using Deepr.Application.Interfaces;
using Deepr.Domain.Entities;
using MediatR;

namespace Deepr.Application.Sessions.Commands;

public record StartSessionCommand(Guid CouncilId) : IRequest<SessionDto>;

public class StartSessionCommandHandler : IRequestHandler<StartSessionCommand, SessionDto>
{
    private readonly IRepository<Session> _sessionRepository;
    private readonly IRepository<Council> _councilRepository;
    private readonly IRepository<Issue> _issueRepository;
    private readonly ISessionOrchestrator _orchestrator;

    public StartSessionCommandHandler(
        IRepository<Session> sessionRepository,
        IRepository<Council> councilRepository,
        IRepository<Issue> issueRepository,
        ISessionOrchestrator orchestrator)
    {
        _sessionRepository = sessionRepository;
        _councilRepository = councilRepository;
        _issueRepository = issueRepository;
        _orchestrator = orchestrator;
    }

    public async Task<SessionDto> Handle(StartSessionCommand request, CancellationToken cancellationToken)
    {
        var council = await _councilRepository.GetByIdAsync(request.CouncilId, cancellationToken)
            ?? throw new InvalidOperationException($"Council {request.CouncilId} not found");

        var issue = await _issueRepository.GetByIdAsync(council.IssueId, cancellationToken)
            ?? throw new InvalidOperationException($"Issue {council.IssueId} not found");

        var session = await _orchestrator.StartSessionAsync(council, issue, cancellationToken);
        await _sessionRepository.AddAsync(session, cancellationToken);

        return new SessionDto
        {
            Id = session.Id,
            CouncilId = session.CouncilId,
            Status = session.Status,
            CurrentRoundNumber = session.CurrentRoundNumber,
            StatePayload = session.StatePayload,
            CreatedAt = session.CreatedAt
        };
    }
}

using Deepr.Application.Interfaces;
using Deepr.Domain.Entities;
using MediatR;

namespace Deepr.Application.Sessions.Commands;

public record FinalizeSessionCommand(Guid SessionId) : IRequest<string>;

public class FinalizeSessionCommandHandler : IRequestHandler<FinalizeSessionCommand, string>
{
    private readonly IRepository<Session> _sessionRepository;
    private readonly ISessionOrchestrator _orchestrator;

    public FinalizeSessionCommandHandler(IRepository<Session> sessionRepository, ISessionOrchestrator orchestrator)
    {
        _sessionRepository = sessionRepository;
        _orchestrator = orchestrator;
    }

    public async Task<string> Handle(FinalizeSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(request.SessionId, cancellationToken)
            ?? throw new InvalidOperationException($"Session {request.SessionId} not found");

        var result = await _orchestrator.FinalizeSessionAsync(session, cancellationToken);
        await _sessionRepository.UpdateAsync(session, cancellationToken);
        return result;
    }
}

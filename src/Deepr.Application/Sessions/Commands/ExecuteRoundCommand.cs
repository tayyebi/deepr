using Deepr.Application.Interfaces;
using Deepr.Domain.Entities;
using MediatR;

namespace Deepr.Application.Sessions.Commands;

public record SessionRoundDto
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public int RoundNumber { get; set; }
    public string Instructions { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public List<ContributionDto> Contributions { get; set; } = new();
}

public record ContributionDto
{
    public Guid Id { get; set; }
    public Guid AgentId { get; set; }
    public string RawContent { get; set; } = string.Empty;
    public string StructuredData { get; set; } = string.Empty;
}

public record ExecuteRoundCommand(Guid SessionId) : IRequest<SessionRoundDto>;

public class ExecuteRoundCommandHandler : IRequestHandler<ExecuteRoundCommand, SessionRoundDto>
{
    private readonly IRepository<Session> _sessionRepository;
    private readonly IRepository<Council> _councilRepository;
    private readonly IRepository<SessionRound> _roundRepository;
    private readonly ISessionOrchestrator _orchestrator;

    public ExecuteRoundCommandHandler(
        IRepository<Session> sessionRepository,
        IRepository<Council> councilRepository,
        IRepository<SessionRound> roundRepository,
        ISessionOrchestrator orchestrator)
    {
        _sessionRepository = sessionRepository;
        _councilRepository = councilRepository;
        _roundRepository = roundRepository;
        _orchestrator = orchestrator;
    }

    public async Task<SessionRoundDto> Handle(ExecuteRoundCommand request, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(request.SessionId, cancellationToken)
            ?? throw new InvalidOperationException($"Session {request.SessionId} not found");

        var council = await _councilRepository.GetByIdAsync(session.CouncilId, cancellationToken)
            ?? throw new InvalidOperationException($"Council {session.CouncilId} not found");

        var round = await _orchestrator.ExecuteNextRoundAsync(session, council, cancellationToken);
        await _roundRepository.AddAsync(round, cancellationToken);
        await _sessionRepository.UpdateAsync(session, cancellationToken);

        return new SessionRoundDto
        {
            Id = round.Id,
            SessionId = round.SessionId,
            RoundNumber = round.RoundNumber,
            Instructions = round.Instructions,
            Summary = round.Summary,
            Contributions = round.Contributions.Select(c => new ContributionDto
            {
                Id = c.Id,
                AgentId = c.AgentId,
                RawContent = c.RawContent,
                StructuredData = c.StructuredData
            }).ToList()
        };
    }
}

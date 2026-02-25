using Deepr.Application.DTOs;
using Deepr.Application.Interfaces;
using Deepr.Domain.Entities;
using MediatR;

namespace Deepr.Application.Sessions.Queries;

public record GetSessionQuery(Guid Id) : IRequest<SessionDto?>;

public class GetSessionQueryHandler : IRequestHandler<GetSessionQuery, SessionDto?>
{
    private readonly IRepository<Session> _repository;

    public GetSessionQueryHandler(IRepository<Session> repository)
    {
        _repository = repository;
    }

    public async Task<SessionDto?> Handle(GetSessionQuery request, CancellationToken cancellationToken)
    {
        var session = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (session == null) return null;
        return new SessionDto
        {
            Id = session.Id,
            CouncilId = session.CouncilId,
            Status = session.Status,
            CurrentRoundNumber = session.CurrentRoundNumber,
            StatePayload = session.StatePayload,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt
        };
    }
}

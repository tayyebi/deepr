using Deepr.Application.DTOs;
using Deepr.Application.Interfaces;
using Deepr.Domain.Entities;
using MediatR;

namespace Deepr.Application.Issues.Queries;

public record GetIssueQuery(Guid Id) : IRequest<IssueDto?>;

public class GetIssueQueryHandler : IRequestHandler<GetIssueQuery, IssueDto?>
{
    private readonly IRepository<Issue> _repository;

    public GetIssueQueryHandler(IRepository<Issue> repository)
    {
        _repository = repository;
    }

    public async Task<IssueDto?> Handle(GetIssueQuery request, CancellationToken cancellationToken)
    {
        var issue = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (issue == null) return null;
        return new IssueDto
        {
            Id = issue.Id,
            Title = issue.Title,
            ContextVector = issue.ContextVector,
            OwnerId = issue.OwnerId,
            IsArchived = issue.IsArchived,
            CreatedAt = issue.CreatedAt,
            UpdatedAt = issue.UpdatedAt
        };
    }
}

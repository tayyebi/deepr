using Deepr.Application.DTOs;
using Deepr.Application.Interfaces;
using Deepr.Domain.Entities;
using MediatR;

namespace Deepr.Application.Issues.Queries;

public record GetAllIssuesQuery : IRequest<List<IssueDto>>;

public class GetAllIssuesQueryHandler : IRequestHandler<GetAllIssuesQuery, List<IssueDto>>
{
    private readonly IRepository<Issue> _repository;

    public GetAllIssuesQueryHandler(IRepository<Issue> repository)
    {
        _repository = repository;
    }

    public async Task<List<IssueDto>> Handle(GetAllIssuesQuery request, CancellationToken cancellationToken)
    {
        var issues = await _repository.GetAllAsync(cancellationToken);
        return issues.Select(issue => new IssueDto
        {
            Id = issue.Id,
            Title = issue.Title,
            ContextVector = issue.ContextVector,
            OwnerId = issue.OwnerId,
            IsArchived = issue.IsArchived,
            CreatedAt = issue.CreatedAt,
            UpdatedAt = issue.UpdatedAt
        }).ToList();
    }
}

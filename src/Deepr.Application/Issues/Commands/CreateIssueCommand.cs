using Deepr.Application.DTOs;
using Deepr.Application.Interfaces;
using Deepr.Domain.Entities;
using MediatR;

namespace Deepr.Application.Issues.Commands;

public record CreateIssueCommand(string Title, string ContextVector, Guid OwnerId) : IRequest<IssueDto>;

public class CreateIssueCommandHandler : IRequestHandler<CreateIssueCommand, IssueDto>
{
    private readonly IRepository<Issue> _repository;

    public CreateIssueCommandHandler(IRepository<Issue> repository)
    {
        _repository = repository;
    }

    public async Task<IssueDto> Handle(CreateIssueCommand request, CancellationToken cancellationToken)
    {
        var issue = new Issue(request.Title, request.ContextVector, request.OwnerId);
        await _repository.AddAsync(issue, cancellationToken);
        return new IssueDto
        {
            Id = issue.Id,
            Title = issue.Title,
            ContextVector = issue.ContextVector,
            OwnerId = issue.OwnerId,
            IsArchived = issue.IsArchived,
            CreatedAt = issue.CreatedAt
        };
    }
}

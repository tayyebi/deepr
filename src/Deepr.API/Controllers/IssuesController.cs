using Deepr.Application.DTOs;
using Deepr.Application.Issues.Commands;
using Deepr.Application.Issues.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Deepr.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IssuesController : ControllerBase
{
    private readonly IMediator _mediator;

    public IssuesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Gets all issues</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<IssueDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<IssueDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAllIssuesQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>Gets an issue by ID</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(IssueDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IssueDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetIssueQuery(id), cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>Creates a new issue</summary>
    [HttpPost]
    [ProducesResponseType(typeof(IssueDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<IssueDto>> Create([FromBody] CreateIssueRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateIssueCommand(request.Title, request.ContextVector, request.OwnerId), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}

public record CreateIssueRequest(string Title, string ContextVector, Guid OwnerId);

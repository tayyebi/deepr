using Deepr.Application.Councils.Commands;
using Deepr.Application.Councils.Queries;
using Deepr.Application.DTOs;
using Deepr.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Deepr.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CouncilsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CouncilsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Gets a council by ID</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CouncilDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CouncilDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCouncilQuery(id), cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>Creates a new council for an issue</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CouncilDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CouncilDto>> Create([FromBody] CreateCouncilRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateCouncilCommand(request.IssueId, request.SelectedMethod, request.SelectedTool), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Adds a member to a council</summary>
    [HttpPost("{id:guid}/members")]
    [ProducesResponseType(typeof(CouncilDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CouncilDto>> AddMember(Guid id, [FromBody] AddMemberRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new AddMemberCommand(id, request.AgentId, request.Name, request.Role, request.IsAi, request.SystemPromptOverride), cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }
}

public record CreateCouncilRequest(Guid IssueId, MethodType SelectedMethod, ToolType SelectedTool);
public record AddMemberRequest(Guid AgentId, string Name, Role Role, bool IsAi, string? SystemPromptOverride = null);

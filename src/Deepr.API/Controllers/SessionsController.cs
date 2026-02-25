using Deepr.Application.DTOs;
using Deepr.Application.Sessions.Commands;
using Deepr.Application.Sessions.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Deepr.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SessionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Gets a session by ID</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SessionDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSessionQuery(id), cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>Starts a new session for a council</summary>
    [HttpPost("start")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<SessionDto>> Start([FromBody] StartSessionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new StartSessionCommand(request.CouncilId), cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Executes the next round in a session</summary>
    [HttpPost("{id:guid}/execute-round")]
    [ProducesResponseType(typeof(SessionRoundDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SessionRoundDto>> ExecuteRound(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new ExecuteRoundCommand(id), cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Finalizes a session and returns the final result</summary>
    [HttpPost("{id:guid}/finalize")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<string>> Finalize(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new FinalizeSessionCommand(id), cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }
}

public record StartSessionRequest(Guid CouncilId);

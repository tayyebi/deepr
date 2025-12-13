using Deepr.Application.DTOs;
using Deepr.Domain.Entities;
using Deepr.Domain.Enums;

namespace Deepr.Application.Interfaces;

public interface IDecisionMethod
{
    MethodType Type { get; }

    /// <summary>
    /// Determines what should be asked to the agents next based on the current session history.
    /// Returns the prompt text and whether the session is complete.
    /// </summary>
    Task<NextPromptResult> GetNextPromptAsync(Session session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aggregates all contributions from a round into a summary and updates the session state.
    /// Returns the summary text, updated state payload, and whether to continue.
    /// </summary>
    Task<AggregationResult> AggregateRoundAsync(SessionRound round, string currentStatePayload, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a session can start with the given council configuration.
    /// </summary>
    Task<bool> ValidateCouncilAsync(Council council, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initializes the state payload for a new session.
    /// </summary>
    Task<string> InitializeStateAsync(Council council, Issue issue, CancellationToken cancellationToken = default);
}

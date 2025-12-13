using Deepr.Domain.Entities;

namespace Deepr.Application.Interfaces;

/// <summary>
/// Orchestrates the execution of a session by coordinating the decision method, tool adapter, and agent driver.
/// This is the main engine that manages the "Cartesian Product" of Methods x Tools x Agents.
/// </summary>
public interface ISessionOrchestrator
{
    /// <summary>
    /// Starts a new session for the given council and issue.
    /// </summary>
    Task<Session> StartSessionAsync(Council council, Issue issue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the next round of the session by:
    /// 1. Getting the next prompt from the decision method
    /// 2. Sending it to all agents via the agent driver
    /// 3. Parsing responses using the tool adapter
    /// 4. Aggregating results via the decision method
    /// </summary>
    Task<SessionRound> ExecuteNextRoundAsync(Session session, Council council, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a session should continue or is complete.
    /// </summary>
    Task<bool> ShouldContinueSessionAsync(Session session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finalizes a session and returns the final result.
    /// </summary>
    Task<string> FinalizeSessionAsync(Session session, CancellationToken cancellationToken = default);
}

using Deepr.Domain.ValueObjects;

namespace Deepr.Application.Interfaces;

public interface IAgentDriver
{
    /// <summary>
    /// Sends a prompt to an agent (human or AI) and retrieves their response.
    /// </summary>
    Task<string> GetResponseAsync(CouncilMember agent, string prompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an agent is available to respond (e.g., online, not busy).
    /// </summary>
    Task<bool> IsAgentAvailableAsync(Guid agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification to an agent without expecting a response.
    /// </summary>
    Task NotifyAgentAsync(Guid agentId, string message, CancellationToken cancellationToken = default);
}

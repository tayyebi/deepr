using Deepr.Application.Interfaces;
using Deepr.Domain.ValueObjects;

namespace Deepr.Infrastructure.AgentDrivers;

/// <summary>
/// A simple echo agent driver that generates placeholder responses.
/// Used when no AI configuration is available.
/// </summary>
public class EchoAgentDriver : IAgentDriver
{
    public Task<string> GetResponseAsync(CouncilMember agent, string prompt, CancellationToken cancellationToken = default)
    {
        var response = agent.Role switch
        {
            Domain.Enums.Role.Chairman => $"[{agent.Name} - Chairman] I acknowledge the prompt and suggest we proceed methodically: {prompt.Substring(0, Math.Min(50, prompt.Length))}...",
            Domain.Enums.Role.Expert => $"[{agent.Name} - Expert] From my expertise, I would analyze this as follows: This requires careful consideration of multiple factors.",
            Domain.Enums.Role.Critic => $"[{agent.Name} - Critic] I see potential issues with this approach. We should consider the risks and alternative perspectives.",
            Domain.Enums.Role.Observer => $"[{agent.Name} - Observer] I've noted the discussion. The group seems to be converging on key points.",
            _ => $"[{agent.Name}] Acknowledged: {prompt.Substring(0, Math.Min(30, prompt.Length))}..."
        };

        return Task.FromResult(response);
    }

    public Task<bool> IsAgentAvailableAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task NotifyAgentAsync(Guid agentId, string message, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

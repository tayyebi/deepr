using Deepr.Application.Interfaces;
using Deepr.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Deepr.Infrastructure.AgentDrivers;

public class AgentDriverFactory : IAgentDriverFactory
{
    private readonly AiProviderRegistry _registry;
    private readonly ILogger<AgentDriverFactory> _logger;

    public AgentDriverFactory(AiProviderRegistry registry, ILogger<AgentDriverFactory> logger)
    {
        _registry = registry;
        _logger = logger;
    }

    public IAgentDriver GetDriver(CouncilMember member)
    {
        if (!member.IsAi)
        {
            _logger.LogDebug("Member '{Name}' is not AI, using EchoAgentDriver", member.Name);
            return new EchoAgentDriver();
        }

        var providerName = member.ModelProvider ?? _registry.DefaultProviderName;

        if (providerName is null)
        {
            _logger.LogWarning("No AI providers configured. Using EchoAgentDriver for member '{Name}'", member.Name);
            return new EchoAgentDriver();
        }

        var config = _registry.GetProviderConfig(providerName);
        if (config is null)
        {
            _logger.LogWarning("AI provider '{Provider}' not found for member '{Name}'. Using EchoAgentDriver", providerName, member.Name);
            return new EchoAgentDriver();
        }

        var chatService = _registry.GetService(providerName, member.ModelId);
        if (chatService is null)
        {
            _logger.LogWarning("Failed to create chat service for provider '{Provider}'. Using EchoAgentDriver", providerName);
            return new EchoAgentDriver();
        }

        var timeoutSeconds = config.TimeoutSeconds;
        _logger.LogDebug("Using {Provider}/{Model} for member '{Name}' (timeout={Timeout}s)",
            providerName, member.ModelId ?? config.DefaultModel, member.Name, timeoutSeconds);

        return new SemanticKernelAgentDriver(chatService, timeoutSeconds);
    }
}

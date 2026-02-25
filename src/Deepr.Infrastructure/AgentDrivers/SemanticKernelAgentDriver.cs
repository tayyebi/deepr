using Deepr.Application.Interfaces;
using Deepr.Domain.ValueObjects;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Deepr.Infrastructure.AgentDrivers;

/// <summary>
/// AI agent driver using Microsoft Semantic Kernel with OpenAI.
/// Requires OpenAI:ApiKey to be configured.
/// </summary>
public class SemanticKernelAgentDriver : IAgentDriver
{
    private readonly IChatCompletionService _chatService;

    public SemanticKernelAgentDriver(IChatCompletionService chatService)
    {
        _chatService = chatService;
    }

    public async Task<string> GetResponseAsync(CouncilMember agent, string prompt, CancellationToken cancellationToken = default)
    {
        var history = new ChatHistory();

        var systemPrompt = agent.SystemPromptOverride
            ?? $"You are {agent.Name}, a {agent.Role} in a decision-making council. " +
               "Provide thoughtful, concise responses based on your role.";

        history.AddSystemMessage(systemPrompt);
        history.AddUserMessage(prompt);

        var response = await _chatService.GetChatMessageContentAsync(history, cancellationToken: cancellationToken);
        return response.Content ?? "[No response]";
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

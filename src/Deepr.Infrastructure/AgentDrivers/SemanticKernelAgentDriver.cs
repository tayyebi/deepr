using Deepr.Application.Interfaces;
using Deepr.Domain.ValueObjects;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Deepr.Infrastructure.AgentDrivers;

/// <summary>
/// AI agent driver using Microsoft Semantic Kernel.
/// Supports any provider that implements IChatCompletionService (OpenAI, Azure OpenAI, Ollama, etc.).
/// </summary>
public class SemanticKernelAgentDriver : IAgentDriver
{
    private readonly IChatCompletionService _chatService;
    private readonly int _timeoutSeconds;

    public SemanticKernelAgentDriver(IChatCompletionService chatService, int timeoutSeconds = 120)
    {
        _chatService = chatService;
        _timeoutSeconds = timeoutSeconds;
    }

    public async Task<string> GetResponseAsync(CouncilMember agent, string prompt, CancellationToken cancellationToken = default)
    {
        var history = new ChatHistory();

        var systemPrompt = agent.SystemPromptOverride
            ?? $"You are {agent.Name}, a {agent.Role} in a decision-making council. " +
               "Provide thoughtful, concise responses based on your role.";

        history.AddSystemMessage(systemPrompt);
        history.AddUserMessage(prompt);

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(_timeoutSeconds));

            var response = await _chatService.GetChatMessageContentAsync(history, cancellationToken: cts.Token);
            return response.Content ?? "[No response]";
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return $"[AI Error: Request timed out after {_timeoutSeconds}s for agent {agent.Name}]";
        }
        catch (HttpRequestException ex)
        {
            return $"[AI Error: {ex.Message}]";
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return $"[AI Error: {ex.GetType().Name} - {ex.Message}]";
        }
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

using System.Text;
using System.Text.RegularExpressions;
using Deepr.Application.Interfaces;
using Deepr.Domain.ValueObjects;

namespace Deepr.Infrastructure.AgentDrivers;

/// <summary>
/// A simple echo agent driver that generates placeholder responses.
/// Used when no AI configuration is available.
/// Returns structured scoring output when a VOTING ROUND prompt is detected.
/// </summary>
public class EchoAgentDriver : IAgentDriver
{
    public Task<string> GetResponseAsync(CouncilMember agent, string prompt, CancellationToken cancellationToken = default)
    {
        if (prompt.Contains("VOTING ROUND:"))
            return Task.FromResult(GenerateVotingResponse(agent, prompt));

        var response = agent.Role switch
        {
            Domain.Enums.Role.Chairman => $"[{agent.Name} - Moderator] I acknowledge the prompt and suggest we proceed methodically: {prompt.Substring(0, Math.Min(50, prompt.Length))}...",
            Domain.Enums.Role.Expert => $"[{agent.Name} - Expert] From my expertise, I would analyze this as follows: This requires careful consideration of multiple factors.",
            Domain.Enums.Role.Critic => $"[{agent.Name} - Critic] I see potential issues with this approach. We should consider the risks and alternative perspectives.",
            Domain.Enums.Role.Observer => $"[{agent.Name} - Observer] I've noted the discussion. The group seems to be converging on key points.",
            _ => $"[{agent.Name}] Acknowledged: {prompt.Substring(0, Math.Min(30, prompt.Length))}..."
        };

        return Task.FromResult(response);
    }

    public Task<bool> IsAgentAvailableAsync(Guid agentId, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task NotifyAgentAsync(Guid agentId, string message, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <summary>
    /// Generates structured voting scores for a VOTING ROUND prompt.
    /// Options and criteria are extracted from the prompt text.
    /// Scores are deterministic based on the agent's name hash.
    /// </summary>
    private static string GenerateVotingResponse(CouncilMember agent, string prompt)
    {
        var optionsMatch = Regex.Match(prompt, @"Options:\s*(.+)");
        var criteriaMatch = Regex.Match(prompt, @"Criteria & weights:\s*(.+)");

        if (!optionsMatch.Success || !criteriaMatch.Success)
            return $"[{agent.Name} - {agent.Role}] Unable to parse voting prompt.";

        var options = optionsMatch.Groups[1].Value.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
        var criteriaNames = Regex.Matches(criteriaMatch.Groups[1].Value, @"(\w+)\(")
            .Select(m => m.Groups[1].Value.Trim())
            .Where(s => s.Length > 0)
            .ToArray();

        // Deterministic per-agent scores based on name hash.
        // Using a stable seed means the same agent always produces the same demo scores,
        // which makes results reproducible for testing without a live AI model.
        var seed = Math.Abs(agent.Name.GetHashCode()) % 997 + 1;
        var rng = new Random(seed);

        var sb = new StringBuilder();
        sb.AppendLine($"[{agent.Name} - {agent.Role}] My scoring assessment for each option:");
        sb.AppendLine("SCORES:");
        foreach (var option in options)
        {
            var scores = criteriaNames.Select(c => $"{c}={rng.Next(6, 10)}");
            sb.AppendLine($"{option}: {string.Join(" | ", scores)}");
        }

        return sb.ToString().TrimEnd();
    }
}

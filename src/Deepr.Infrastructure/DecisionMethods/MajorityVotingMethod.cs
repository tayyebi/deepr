using System.Text.Json;
using Deepr.Application.DTOs;
using Deepr.Application.Interfaces;
using Deepr.Domain.Entities;
using Deepr.Domain.Enums;

namespace Deepr.Infrastructure.DecisionMethods;

/// <summary>
/// Majority Voting — 2 rounds:
/// Round 1: Open discussion where each agent proposes and argues for options.
/// Round 2: Each agent casts a single vote for the option they favour.
/// The option with the most votes wins (plurality if no majority reached).
/// </summary>
public class MajorityVotingMethod : IDecisionMethod
{
    private const int MaxRounds = 2;

    public MethodType Type => MethodType.MajorityVoting;

    public Task<NextPromptResult> GetNextPromptAsync(Session session, CancellationToken cancellationToken = default)
    {
        if (session.CurrentRoundNumber >= MaxRounds)
        {
            return Task.FromResult(new NextPromptResult
            {
                IsSessionComplete = true,
                CompletionReason = "Majority voting complete after discussion and vote rounds.",
                PromptText = string.Empty
            });
        }

        string topic = "the topic";
        string options = string.Empty;
        try
        {
            var state = JsonSerializer.Deserialize<JsonElement>(session.StatePayload);
            if (state.TryGetProperty("topic", out var t)) topic = t.GetString() ?? topic;
            if (state.TryGetProperty("options", out var o)) options = o.GetString() ?? string.Empty;
        }
        catch { }

        var prompt = session.CurrentRoundNumber == 0
            ? $"Round 1 — Open Discussion: Share your perspective on \"{topic}\". " +
              "Propose the option you favour, provide your rationale, and address any concerns. " +
              (string.IsNullOrEmpty(options) ? "" : $"The options under consideration are: {options}.")
            : $"Round 2 — Vote: Based on the preceding discussion about \"{topic}\", " +
              "cast your vote by clearly stating: VOTE: [your chosen option]. " +
              "Provide one sentence of justification for your choice.";

        return Task.FromResult(new NextPromptResult { PromptText = prompt, IsSessionComplete = false });
    }

    public Task<AggregationResult> AggregateRoundAsync(SessionRound round, string currentStatePayload, CancellationToken cancellationToken = default)
    {
        var contributions = round.Contributions.Select(c => c.RawContent).ToList();

        string summary;
        string updatedState;

        if (round.RoundNumber == 1)
        {
            summary = $"Discussion Round:\n" + string.Join("\n---\n", contributions);
            var state = new { topic = GetTopic(currentStatePayload), options = GetOptions(currentStatePayload), discussion = contributions };
            updatedState = JsonSerializer.Serialize(state);
        }
        else
        {
            var votes = contributions
                .Select(c =>
                {
                    var idx = c.IndexOf("VOTE:", StringComparison.OrdinalIgnoreCase);
                    return idx >= 0 ? c[(idx + 5)..].Split('\n')[0].Trim() : c.Split('\n')[0].Trim();
                })
                .GroupBy(v => v, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .ToList();

            var winner = votes.FirstOrDefault()?.Key ?? "No clear winner";
            var tally = string.Join(", ", votes.Select(g => $"{g.Key}: {g.Count()} vote(s)"));
            summary = $"Vote Tally: {tally}\n🏆 Winner: {winner}";
            var state = new { topic = GetTopic(currentStatePayload), winner, tally, votes = contributions };
            updatedState = JsonSerializer.Serialize(state);
        }

        return Task.FromResult(new AggregationResult
        {
            SummaryText = summary,
            UpdatedStatePayload = updatedState,
            ShouldContinue = round.RoundNumber < MaxRounds
        });
    }

    public Task<bool> ValidateCouncilAsync(Council council, CancellationToken cancellationToken = default) =>
        Task.FromResult(council.Agents.Count >= 2);

    public Task<string> InitializeStateAsync(Council council, Issue issue, CancellationToken cancellationToken = default)
    {
        var state = new { topic = issue.Title, context = issue.ContextVector, options = string.Empty };
        return Task.FromResult(JsonSerializer.Serialize(state));
    }

    private static string GetTopic(string payload)
    {
        try { var s = JsonSerializer.Deserialize<JsonElement>(payload); if (s.TryGetProperty("topic", out var t)) return t.GetString() ?? ""; } catch { }
        return "";
    }

    private static string GetOptions(string payload)
    {
        try { var s = JsonSerializer.Deserialize<JsonElement>(payload); if (s.TryGetProperty("options", out var o)) return o.GetString() ?? ""; } catch { }
        return "";
    }
}

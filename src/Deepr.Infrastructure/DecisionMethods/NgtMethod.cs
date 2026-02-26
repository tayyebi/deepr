using System.Text.Json;
using Deepr.Application.DTOs;
using Deepr.Application.Interfaces;
using Deepr.Domain.Entities;
using Deepr.Domain.Enums;

namespace Deepr.Infrastructure.DecisionMethods;

public class NgtMethod : IDecisionMethod
{
    private const int MaxRounds = 4;

    public MethodType Type => MethodType.NGT;

    public Task<NextPromptResult> GetNextPromptAsync(Session session, CancellationToken cancellationToken = default)
    {
        if (session.CurrentRoundNumber >= MaxRounds)
        {
            return Task.FromResult(new NextPromptResult
            {
                PromptText = string.Empty,
                IsSessionComplete = true,
                CompletionReason = "NGT session complete after idea generation, sharing, clarification, and voting phases."
            });
        }

        string topic = "the topic";
        string ideasSummary = string.Empty;
        try
        {
            var state = JsonSerializer.Deserialize<JsonElement>(session.StatePayload);
            if (state.TryGetProperty("topic", out var topicProp))
                topic = topicProp.GetString() ?? topic;
            if (state.TryGetProperty("ideasSummary", out var summaryProp))
                ideasSummary = summaryProp.GetString() ?? string.Empty;
        }
        catch { }

        var promptText = session.CurrentRoundNumber switch
        {
            0 => $"NGT Phase 1 — Silent Generation: Without discussion, individually write down all ideas related to: {topic}. " +
                 "List as many ideas as possible.",
            1 => $"NGT Phase 2 — Round-Robin Sharing: Share your ideas one at a time in turn. " +
                 $"Previous ideas collected:\n{ideasSummary}\nAdd any new ideas not yet listed for: {topic}.",
            2 => $"NGT Phase 3 — Clarification: Review and clarify the collected ideas. " +
                 $"Discuss the meaning of each idea:\n{ideasSummary}\nAsk questions or provide explanations where needed for: {topic}.",
            3 => $"NGT Phase 4 — Voting & Ranking: Rank the top ideas by importance for: {topic}. " +
                 $"Ideas to consider:\n{ideasSummary}\nAssign a priority score (1=low, 5=high) and brief rationale for each.",
            _ => string.Empty
        };

        return Task.FromResult(new NextPromptResult
        {
            PromptText = promptText,
            IsSessionComplete = false
        });
    }

    public Task<AggregationResult> AggregateRoundAsync(SessionRound round, string currentStatePayload, CancellationToken cancellationToken = default)
    {
        var contributions = round.Contributions.Select(c => c.RawContent).ToList();

        var phaseLabel = round.RoundNumber switch
        {
            1 => "Silent Generation",
            2 => "Round-Robin Sharing",
            3 => "Clarification",
            4 => "Voting & Ranking",
            _ => $"Phase {round.RoundNumber}"
        };

        var summary = $"NGT Phase {round.RoundNumber} — {phaseLabel}:\n" +
                      string.Join("\n", contributions.Select(c => $"- {c}"));

        var shouldContinue = round.RoundNumber < MaxRounds;

        var stateObj = new
        {
            roundsCompleted = round.RoundNumber,
            ideasSummary = summary,
            lastPhase = phaseLabel
        };

        return Task.FromResult(new AggregationResult
        {
            SummaryText = summary,
            UpdatedStatePayload = JsonSerializer.Serialize(stateObj),
            ShouldContinue = shouldContinue
        });
    }

    public Task<bool> ValidateCouncilAsync(Council council, CancellationToken cancellationToken = default)
        => Task.FromResult(council.Agents.Count >= 2);

    public Task<string> InitializeStateAsync(Council council, Issue issue, CancellationToken cancellationToken = default)
    {
        var state = new { topic = issue.Title, context = issue.ContextVector, roundsCompleted = 0, ideasSummary = "" };
        return Task.FromResult(JsonSerializer.Serialize(state));
    }
}

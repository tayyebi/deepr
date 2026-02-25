using System.Text.Json;
using Deepr.Application.DTOs;
using Deepr.Application.Interfaces;
using Deepr.Domain.Entities;
using Deepr.Domain.Enums;

namespace Deepr.Infrastructure.DecisionMethods;

public class DelphiMethod : IDecisionMethod
{
    private const int MaxRounds = 3;

    public MethodType Type => MethodType.Delphi;

    public Task<NextPromptResult> GetNextPromptAsync(Session session, CancellationToken cancellationToken = default)
    {
        if (session.CurrentRoundNumber >= MaxRounds)
        {
            return Task.FromResult(new NextPromptResult
            {
                PromptText = string.Empty,
                IsSessionComplete = true,
                CompletionReason = $"Delphi consensus reached after {MaxRounds} rounds."
            });
        }

        string topic = "the topic";
        string previousSummary = string.Empty;
        try
        {
            var state = JsonSerializer.Deserialize<JsonElement>(session.StatePayload);
            if (state.TryGetProperty("topic", out var topicProp))
                topic = topicProp.GetString() ?? topic;
            if (state.TryGetProperty("lastSummary", out var summaryProp))
                previousSummary = summaryProp.GetString() ?? string.Empty;
        }
        catch { }

        var promptText = session.CurrentRoundNumber == 0
            ? $"Round 1 - Initial Assessment: Please provide your expert opinion on: {topic}. " +
              "Focus on key factors, risks, and recommendations."
            : $"Round {session.CurrentRoundNumber + 1} - Refinement: Based on the group's previous responses:\n\n" +
              $"{previousSummary}\n\nPlease refine your position. Do you agree or disagree? Why?";

        return Task.FromResult(new NextPromptResult
        {
            PromptText = promptText,
            IsSessionComplete = false
        });
    }

    public Task<AggregationResult> AggregateRoundAsync(SessionRound round, string currentStatePayload, CancellationToken cancellationToken = default)
    {
        var contributions = round.Contributions
            .Select(c => $"Expert: {c.RawContent}")
            .ToList();

        var summary = $"Round {round.RoundNumber} Summary:\n" + string.Join("\n\n", contributions);
        var shouldContinue = round.RoundNumber < MaxRounds;

        var stateObj = new
        {
            roundsCompleted = round.RoundNumber,
            lastSummary = summary,
            contributions = contributions
        };

        return Task.FromResult(new AggregationResult
        {
            SummaryText = summary,
            UpdatedStatePayload = JsonSerializer.Serialize(stateObj),
            ShouldContinue = shouldContinue
        });
    }

    public Task<bool> ValidateCouncilAsync(Council council, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(council.Agents.Count >= 2);
    }

    public Task<string> InitializeStateAsync(Council council, Issue issue, CancellationToken cancellationToken = default)
    {
        var state = new { topic = issue.Title, context = issue.ContextVector, roundsCompleted = 0, lastSummary = "" };
        return Task.FromResult(JsonSerializer.Serialize(state));
    }
}

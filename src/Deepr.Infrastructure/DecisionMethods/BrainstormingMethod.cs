using System.Text.Json;
using Deepr.Application.DTOs;
using Deepr.Application.Interfaces;
using Deepr.Domain.Entities;
using Deepr.Domain.Enums;

namespace Deepr.Infrastructure.DecisionMethods;

public class BrainstormingMethod : IDecisionMethod
{
    private const int MaxRounds = 1;

    public MethodType Type => MethodType.Brainstorming;

    public Task<NextPromptResult> GetNextPromptAsync(Session session, CancellationToken cancellationToken = default)
    {
        if (session.CurrentRoundNumber >= MaxRounds)
        {
            return Task.FromResult(new NextPromptResult
            {
                PromptText = string.Empty,
                IsSessionComplete = true,
                CompletionReason = "Brainstorming session complete after idea collection round."
            });
        }

        string topic = "the topic";
        try
        {
            var state = JsonSerializer.Deserialize<JsonElement>(session.StatePayload);
            if (state.TryGetProperty("topic", out var topicProp))
                topic = topicProp.GetString() ?? topic;
        }
        catch { }

        return Task.FromResult(new NextPromptResult
        {
            PromptText = $"Please brainstorm as many ideas as possible about: {topic}. " +
                         "There are no wrong answers. Share all your ideas freely.",
            IsSessionComplete = false
        });
    }

    public Task<AggregationResult> AggregateRoundAsync(SessionRound round, string currentStatePayload, CancellationToken cancellationToken = default)
    {
        var ideas = round.Contributions
            .Select(c => $"- {c.RawContent}")
            .ToList();

        var summary = $"Round {round.RoundNumber} Ideas:\n" + string.Join("\n", ideas);

        var stateObj = new { roundsCompleted = round.RoundNumber, allIdeas = ideas };
        var updatedState = JsonSerializer.Serialize(stateObj);

        return Task.FromResult(new AggregationResult
        {
            SummaryText = summary,
            UpdatedStatePayload = updatedState,
            ShouldContinue = round.RoundNumber < MaxRounds
        });
    }

    public Task<bool> ValidateCouncilAsync(Council council, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(council.Agents.Any());
    }

    public Task<string> InitializeStateAsync(Council council, Issue issue, CancellationToken cancellationToken = default)
    {
        var state = new { topic = issue.Title, context = issue.ContextVector, roundsCompleted = 0 };
        return Task.FromResult(JsonSerializer.Serialize(state));
    }
}

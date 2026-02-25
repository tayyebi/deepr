using System.Text.Json;
using Deepr.Application.DTOs;
using Deepr.Application.Interfaces;
using Deepr.Domain.Entities;
using Deepr.Domain.Enums;

namespace Deepr.Infrastructure.DecisionMethods;

public class ConsensusMethod : IDecisionMethod
{
    private const int MaxRounds = 2;

    public MethodType Type => MethodType.ConsensusBuilding;

    public Task<NextPromptResult> GetNextPromptAsync(Session session, CancellationToken cancellationToken = default)
    {
        if (session.CurrentRoundNumber >= MaxRounds)
        {
            return Task.FromResult(new NextPromptResult
            {
                IsSessionComplete = true,
                CompletionReason = "Consensus building complete.",
                PromptText = string.Empty
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

        var prompt = session.CurrentRoundNumber == 0
            ? $"Please share your perspective on reaching consensus about: {topic}"
            : $"Based on group discussion, please indicate your level of agreement and any remaining concerns about: {topic}";

        return Task.FromResult(new NextPromptResult { PromptText = prompt, IsSessionComplete = false });
    }

    public Task<AggregationResult> AggregateRoundAsync(SessionRound round, string currentStatePayload, CancellationToken cancellationToken = default)
    {
        var responses = round.Contributions.Select(c => c.RawContent).ToList();
        var summary = $"Consensus Round {round.RoundNumber}: " + string.Join(" | ", responses);
        var state = new { roundsCompleted = round.RoundNumber, summary };
        return Task.FromResult(new AggregationResult
        {
            SummaryText = summary,
            UpdatedStatePayload = JsonSerializer.Serialize(state),
            ShouldContinue = round.RoundNumber < MaxRounds
        });
    }

    public Task<bool> ValidateCouncilAsync(Council council, CancellationToken cancellationToken = default) =>
        Task.FromResult(council.Agents.Any());

    public Task<string> InitializeStateAsync(Council council, Issue issue, CancellationToken cancellationToken = default)
    {
        var state = new { topic = issue.Title, context = issue.ContextVector };
        return Task.FromResult(JsonSerializer.Serialize(state));
    }
}

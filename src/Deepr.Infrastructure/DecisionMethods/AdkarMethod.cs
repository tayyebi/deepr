using System.Text.Json;
using Deepr.Application.DTOs;
using Deepr.Application.Interfaces;
using Deepr.Domain.Entities;
using Deepr.Domain.Enums;

namespace Deepr.Infrastructure.DecisionMethods;

public class AdkarMethod : IDecisionMethod
{
    private const int MaxRounds = 5;

    private static readonly string[] PhaseNames =
    [
        "Awareness",
        "Desire",
        "Knowledge",
        "Ability",
        "Reinforcement"
    ];

    private static readonly string[] PhasePrompts =
    [
        "Awareness — Assess the awareness of the need for change regarding: {topic}. " +
            "What do stakeholders currently know? What communication gaps exist?",
        "Desire — Evaluate the desire and willingness to support this change: {topic}. " +
            "What motivates or inhibits stakeholder buy-in?",
        "Knowledge — Identify knowledge and training gaps for implementing the change: {topic}. " +
            "What skills, processes, or information are needed?",
        "Ability — Evaluate the ability to implement required behaviours and skills for: {topic}. " +
            "What barriers exist? What support mechanisms are in place?",
        "Reinforcement — Plan how to sustain and reinforce the change: {topic}. " +
            "What mechanisms will prevent regression and celebrate progress?"
    ];

    public MethodType Type => MethodType.ADKAR;

    public Task<NextPromptResult> GetNextPromptAsync(Session session, CancellationToken cancellationToken = default)
    {
        if (session.CurrentRoundNumber >= MaxRounds)
        {
            return Task.FromResult(new NextPromptResult
            {
                PromptText = string.Empty,
                IsSessionComplete = true,
                CompletionReason = "ADKAR assessment complete across all five change phases."
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

        var phase = session.CurrentRoundNumber;
        var prompt = PhasePrompts[phase].Replace("{topic}", topic);

        return Task.FromResult(new NextPromptResult
        {
            PromptText = $"ADKAR Phase {phase + 1}/5 — {PhaseNames[phase]}: {prompt}",
            IsSessionComplete = false
        });
    }

    public Task<AggregationResult> AggregateRoundAsync(SessionRound round, string currentStatePayload, CancellationToken cancellationToken = default)
    {
        var phase = round.RoundNumber - 1;
        var phaseName = phase >= 0 && phase < PhaseNames.Length ? PhaseNames[phase] : $"Phase {round.RoundNumber}";

        var contributions = round.Contributions
            .Select(c => $"- {c.RawContent}")
            .ToList();

        var summary = $"ADKAR Phase {round.RoundNumber} — {phaseName}:\n" + string.Join("\n", contributions);
        var shouldContinue = round.RoundNumber < MaxRounds;

        JsonElement currentState = default;
        try { currentState = JsonSerializer.Deserialize<JsonElement>(currentStatePayload); } catch { }

        var phases = new Dictionary<string, List<string>>();
        if (currentState.ValueKind == JsonValueKind.Object &&
            currentState.TryGetProperty("phases", out var existingPhases))
        {
            foreach (var p in existingPhases.EnumerateObject())
                phases[p.Name] = p.Value.EnumerateArray().Select(e => e.GetString() ?? "").ToList();
        }

        phases[phaseName] = contributions;

        var stateObj = new { roundsCompleted = round.RoundNumber, phases };
        return Task.FromResult(new AggregationResult
        {
            SummaryText = summary,
            UpdatedStatePayload = JsonSerializer.Serialize(stateObj),
            ShouldContinue = shouldContinue
        });
    }

    public Task<bool> ValidateCouncilAsync(Council council, CancellationToken cancellationToken = default)
        => Task.FromResult(council.Agents.Any());

    public Task<string> InitializeStateAsync(Council council, Issue issue, CancellationToken cancellationToken = default)
    {
        var state = new
        {
            topic = issue.Title,
            context = issue.ContextVector,
            roundsCompleted = 0,
            phases = new Dictionary<string, List<string>>()
        };
        return Task.FromResult(JsonSerializer.Serialize(state));
    }
}

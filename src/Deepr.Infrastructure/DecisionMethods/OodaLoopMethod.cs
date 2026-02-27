using System.Text.Json;
using Deepr.Application.DTOs;
using Deepr.Application.Interfaces;
using Deepr.Domain.Entities;
using Deepr.Domain.Enums;

namespace Deepr.Infrastructure.DecisionMethods;

/// <summary>
/// OODA Loop (Boyd) — 4 rounds for rapid, iterative decision-making:
/// Round 1: Observe    — Gather raw facts, data points, and signals from the environment.
/// Round 2: Orient     — Analyse and make sense of what was observed; identify patterns, biases, mental models.
/// Round 3: Decide     — Select a course of action from the available options.
/// Round 4: Act        — Define the implementation plan, success metrics, and feedback loop for the next OODA cycle.
/// Best suited for fast-moving situations, crisis decisions, and competitive strategy.
/// </summary>
public class OodaLoopMethod : IDecisionMethod
{
    private const int MaxRounds = 4;

    public MethodType Type => MethodType.OODALoop;

    public Task<NextPromptResult> GetNextPromptAsync(Session session, CancellationToken cancellationToken = default)
    {
        if (session.CurrentRoundNumber >= MaxRounds)
        {
            return Task.FromResult(new NextPromptResult
            {
                IsSessionComplete = true,
                CompletionReason = "OODA Loop complete — observe, orient, decide, and act phases finished.",
                PromptText = string.Empty
            });
        }

        string topic = "the situation";
        string context = string.Empty;
        try
        {
            var state = JsonSerializer.Deserialize<JsonElement>(session.StatePayload);
            if (state.TryGetProperty("topic", out var t)) topic = t.GetString() ?? topic;
            if (state.TryGetProperty("context", out var c)) context = c.GetString() ?? context;
        }
        catch { }

        var prompt = session.CurrentRoundNumber switch
        {
            0 => $"OODA — O (Observe): Gather and report raw observations about \"{topic}\".\n\n" +
                 $"Context: {context}\n\n" +
                 "List only FACTS and SIGNALS — no interpretation yet. What data is available? " +
                 "What signals from the environment, competitors, customers, or technology are relevant? " +
                 "What is notably absent or ambiguous? Be specific and factual.",

            1 => $"OODA — Orient: Make sense of the observations about \"{topic}\".\n\n" +
                 "Orient by: (1) identifying patterns in the observations, " +
                 "(2) surfacing mental models or biases that might distort our view, " +
                 "(3) analysing cultural, organisational, and historical context, " +
                 "(4) identifying what the data means for the decision at hand. " +
                 "Challenge your own assumptions aggressively.",

            2 => $"OODA — D (Decide): Select a course of action for \"{topic}\".\n\n" +
                 "Based on the observations and orientation, " +
                 "(1) list the feasible options available, " +
                 "(2) evaluate each against speed, effectiveness, and reversibility, " +
                 "(3) select the option you recommend and state why it outperforms the alternatives. " +
                 "State your DECISION clearly.",

            _ => $"OODA — A (Act): Define the implementation and feedback loop for \"{topic}\".\n\n" +
                 "Specify: (1) the immediate actions to be taken and by whom, " +
                 "(2) the timeline and key milestones, " +
                 "(3) the success metrics that will tell us if the decision is working, " +
                 "(4) the early warning signals that should trigger a new OODA cycle, " +
                 "(5) the feedback mechanism to feed observations back into the next loop."
        };

        return Task.FromResult(new NextPromptResult { PromptText = prompt, IsSessionComplete = false });
    }

    public Task<AggregationResult> AggregateRoundAsync(SessionRound round, string currentStatePayload, CancellationToken cancellationToken = default)
    {
        var contributions = round.Contributions.Select(c => c.RawContent).ToList();
        var phase = round.RoundNumber switch { 1 => "Observe", 2 => "Orient", 3 => "Decide", _ => "Act" };
        var summary = $"OODA — {phase}:\n" + string.Join("\n---\n", contributions);

        var stateObj = new { roundsCompleted = round.RoundNumber, phase, contributions };
        return Task.FromResult(new AggregationResult
        {
            SummaryText = summary,
            UpdatedStatePayload = JsonSerializer.Serialize(stateObj),
            ShouldContinue = round.RoundNumber < MaxRounds
        });
    }

    public Task<bool> ValidateCouncilAsync(Council council, CancellationToken cancellationToken = default) =>
        Task.FromResult(council.Agents.Any());

    public Task<string> InitializeStateAsync(Council council, Issue issue, CancellationToken cancellationToken = default)
    {
        var state = new { topic = issue.Title, context = issue.ContextVector, roundsCompleted = 0 };
        return Task.FromResult(JsonSerializer.Serialize(state));
    }
}

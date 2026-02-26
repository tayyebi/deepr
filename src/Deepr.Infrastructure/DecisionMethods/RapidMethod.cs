using System.Text.Json;
using Deepr.Application.DTOs;
using Deepr.Application.Interfaces;
using Deepr.Domain.Entities;
using Deepr.Domain.Enums;

namespace Deepr.Infrastructure.DecisionMethods;

/// <summary>
/// RAPID Framework (Bain &amp; Company) — 4 rounds with clear role assignment:
/// Round 1: Recommend  — Agent(s) with Moderator role present the recommendation with full rationale.
/// Round 2: Input       — Expert agents provide domain-specific input and data.
/// Round 3: Agree       — Expert/Critic agents flag issues requiring resolution before deciding.
/// Round 4: Decide      — Moderator delivers the final decision incorporating all inputs and objections.
/// (Perform is an execution step handled outside the session.)
/// </summary>
public class RapidMethod : IDecisionMethod
{
    private const int MaxRounds = 4;

    public MethodType Type => MethodType.RAPID;

    public Task<NextPromptResult> GetNextPromptAsync(Session session, CancellationToken cancellationToken = default)
    {
        if (session.CurrentRoundNumber >= MaxRounds)
        {
            return Task.FromResult(new NextPromptResult
            {
                IsSessionComplete = true,
                CompletionReason = "RAPID framework complete — recommendation, input, agreement, and decision phases done.",
                PromptText = string.Empty
            });
        }

        string topic = "the decision";
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
            0 => $"RAPID — R (Recommend): You are tasked with recommending a decision about \"{topic}\".\n\n" +
                 $"Context: {context}\n\n" +
                 "If you hold the Recommend role: Present your specific recommendation, the key data points supporting it, " +
                 "the options you evaluated, and why you are recommending this option over alternatives.\n" +
                 "If you hold an Expert or Critic role: Listen carefully and note any questions you will raise in the Input round.",

            1 => $"RAPID — I (Input): Provide domain-specific expert input on the recommendation for \"{topic}\".\n\n" +
                 "Share data, facts, constraints, or considerations that should influence the decision. " +
                 "Be specific and evidence-based. This is not your vote — it is information the decision-maker needs.",

            2 => $"RAPID — A (Agree): Identify any issues, objections, or conditions required before you can agree to proceed with the recommendation on \"{topic}\".\n\n" +
                 "If you agree: State clearly that you agree and why.\n" +
                 "If you have conditions: State them precisely — what must change or be guaranteed for you to agree?\n" +
                 "If you disagree: State why and what alternative you propose.",

            _ => $"RAPID — D (Decide): You hold the Decide authority for \"{topic}\".\n\n" +
                 "Review the recommendation, expert inputs, and agreement/objection round. " +
                 "Deliver the final decision. State: (1) DECISION: [what is decided], " +
                 "(2) KEY CONDITIONS from the Agree round that will be honoured, " +
                 "(3) any expert inputs that changed the recommendation, " +
                 "(4) the rationale for overriding or accepting the recommendation."
        };

        return Task.FromResult(new NextPromptResult { PromptText = prompt, IsSessionComplete = false });
    }

    public Task<AggregationResult> AggregateRoundAsync(SessionRound round, string currentStatePayload, CancellationToken cancellationToken = default)
    {
        var contributions = round.Contributions.Select(c => c.RawContent).ToList();
        var phase = round.RoundNumber switch { 1 => "R — Recommend", 2 => "I — Input", 3 => "A — Agree", _ => "D — Decide" };
        var summary = $"RAPID {phase}:\n" + string.Join("\n---\n", contributions);

        var stateObj = new { roundsCompleted = round.RoundNumber, phase, contributions };
        return Task.FromResult(new AggregationResult
        {
            SummaryText = summary,
            UpdatedStatePayload = JsonSerializer.Serialize(stateObj),
            ShouldContinue = round.RoundNumber < MaxRounds
        });
    }

    public Task<bool> ValidateCouncilAsync(Council council, CancellationToken cancellationToken = default) =>
        Task.FromResult(council.Agents.Count >= 2);

    public Task<string> InitializeStateAsync(Council council, Issue issue, CancellationToken cancellationToken = default)
    {
        var state = new { topic = issue.Title, context = issue.ContextVector, roundsCompleted = 0 };
        return Task.FromResult(JsonSerializer.Serialize(state));
    }
}

using System.Text.Json;
using Deepr.Application.DTOs;
using Deepr.Application.Interfaces;
using Deepr.Domain.Entities;
using Deepr.Domain.Enums;

namespace Deepr.Infrastructure.DecisionMethods;

/// <summary>
/// Cost–Benefit Analysis (CBA) — 3 rounds:
/// Round 1: Identify and estimate all COSTS (financial, time, risk, opportunity cost).
/// Round 2: Identify and estimate all BENEFITS (financial, strategic, operational, reputational).
/// Round 3: Synthesise — weigh costs vs benefits, compute net value, and deliver a recommendation.
/// </summary>
public class CostBenefitAnalysisMethod : IDecisionMethod
{
    private const int MaxRounds = 3;

    public MethodType Type => MethodType.CostBenefitAnalysis;

    public Task<NextPromptResult> GetNextPromptAsync(Session session, CancellationToken cancellationToken = default)
    {
        if (session.CurrentRoundNumber >= MaxRounds)
        {
            return Task.FromResult(new NextPromptResult
            {
                IsSessionComplete = true,
                CompletionReason = "Cost–Benefit Analysis complete after cost, benefit, and synthesis rounds.",
                PromptText = string.Empty
            });
        }

        string topic = "the proposal";
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
            0 => $"Round 1 — Cost Identification: Analyse the COSTS of \"{topic}\".\n\n" +
                 $"Context: {context}\n\n" +
                 "For each cost, specify: (1) cost category (financial/time/risk/opportunity), " +
                 "(2) estimated magnitude, (3) likelihood of occurrence, (4) time horizon. " +
                 "Be thorough — include direct costs, indirect costs, hidden costs, and opportunity costs.",

            1 => $"Round 2 — Benefit Identification: Analyse the BENEFITS of \"{topic}\".\n\n" +
                 $"Context: {context}\n\n" +
                 "For each benefit, specify: (1) benefit category (financial/strategic/operational/reputational), " +
                 "(2) estimated magnitude, (3) confidence level, (4) time horizon. " +
                 "Be thorough — include direct benefits, indirect benefits, and option value.",

            _ => $"Round 3 — Synthesis & Recommendation: Weigh ALL costs and benefits for \"{topic}\".\n\n" +
                 "Provide: (1) a net value assessment (do benefits outweigh costs?), " +
                 "(2) the key risk factors that could change the CBA outcome, " +
                 "(3) any non-quantifiable factors that should influence the decision, " +
                 "(4) a clear recommendation: PROCEED / DO NOT PROCEED / PROCEED WITH CONDITIONS, " +
                 "and (5) if conditional, specify the conditions."
        };

        return Task.FromResult(new NextPromptResult { PromptText = prompt, IsSessionComplete = false });
    }

    public Task<AggregationResult> AggregateRoundAsync(SessionRound round, string currentStatePayload, CancellationToken cancellationToken = default)
    {
        var contributions = round.Contributions.Select(c => c.RawContent).ToList();
        var label = round.RoundNumber switch { 1 => "Cost Analysis", 2 => "Benefit Analysis", _ => "Synthesis & Recommendation" };
        var summary = $"{label}:\n" + string.Join("\n---\n", contributions);

        var stateObj = new { roundsCompleted = round.RoundNumber, phase = label, contributions };
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

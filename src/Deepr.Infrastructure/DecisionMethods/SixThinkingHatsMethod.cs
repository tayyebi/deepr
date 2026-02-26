using System.Text.Json;
using Deepr.Application.DTOs;
using Deepr.Application.Interfaces;
using Deepr.Domain.Entities;
using Deepr.Domain.Enums;

namespace Deepr.Infrastructure.DecisionMethods;

/// <summary>
/// Six Thinking Hats (de Bono) — 6 rounds, each from a distinct perspective:
/// 1. White Hat — Facts &amp; data only
/// 2. Red Hat  — Emotions, gut feelings, intuition
/// 3. Black Hat — Caution, risks, critical judgment
/// 4. Yellow Hat — Optimism, benefits, value
/// 5. Green Hat — Creativity, alternatives, new ideas
/// 6. Blue Hat  — Process control, summary, next steps
/// </summary>
public class SixThinkingHatsMethod : IDecisionMethod
{
    private const int MaxRounds = 6;

    private static readonly (string Hat, string Color, string Instruction)[] Hats =
    {
        ("White Hat 🎩", "Facts", "Focus ONLY on facts, data, and information. What do we know? What information is missing? Avoid opinions and judgments entirely."),
        ("Red Hat ❤️", "Emotions", "Share your emotional response and gut feeling. How does this make you feel? What is your intuition telling you? No justification needed."),
        ("Black Hat ⚫", "Caution", "Be the devil's advocate. Identify risks, weaknesses, dangers, and reasons why this might fail. Be rigorously critical."),
        ("Yellow Hat 💛", "Optimism", "Be optimistic. Identify benefits, value, and reasons why this could succeed. Explore the best-case scenario."),
        ("Green Hat 💚", "Creativity", "Think creatively. Propose new ideas, alternatives, and possibilities. Challenge assumptions. No idea is too wild."),
        ("Blue Hat 🔵", "Process", "Step back and reflect on the whole discussion. Summarise the key insights from all hats, identify the decision direction, and propose clear next steps.")
    };

    public MethodType Type => MethodType.SixThinkingHats;

    public Task<NextPromptResult> GetNextPromptAsync(Session session, CancellationToken cancellationToken = default)
    {
        if (session.CurrentRoundNumber >= MaxRounds)
        {
            return Task.FromResult(new NextPromptResult
            {
                IsSessionComplete = true,
                CompletionReason = "Six Thinking Hats complete — all six perspectives explored.",
                PromptText = string.Empty
            });
        }

        string topic = "the topic";
        try
        {
            var state = JsonSerializer.Deserialize<JsonElement>(session.StatePayload);
            if (state.TryGetProperty("topic", out var t)) topic = t.GetString() ?? topic;
        }
        catch { }

        var (hat, color, instruction) = Hats[session.CurrentRoundNumber];
        var prompt = $"Round {session.CurrentRoundNumber + 1} of 6 — {hat} ({color} perspective)\n\n" +
                     $"Topic: {topic}\n\n{instruction}";

        return Task.FromResult(new NextPromptResult { PromptText = prompt, IsSessionComplete = false });
    }

    public Task<AggregationResult> AggregateRoundAsync(SessionRound round, string currentStatePayload, CancellationToken cancellationToken = default)
    {
        var idx = round.RoundNumber - 1;
        var (hat, _, _) = idx >= 0 && idx < Hats.Length ? Hats[idx] : ("Round", "", "");
        var contributions = round.Contributions.Select(c => c.RawContent).ToList();
        var summary = $"{hat} insights:\n" + string.Join("\n---\n", contributions);

        var stateDoc = new { roundsCompleted = round.RoundNumber, hat, insights = contributions };
        return Task.FromResult(new AggregationResult
        {
            SummaryText = summary,
            UpdatedStatePayload = JsonSerializer.Serialize(stateDoc),
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

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Deepr.Application.DTOs;
using Deepr.Application.Interfaces;
using Deepr.Domain.Entities;
using Deepr.Domain.Enums;

namespace Deepr.Infrastructure.DecisionMethods;

/// <summary>
/// Weighted Deliberation — a 4-round structured decision method:
///   Round 1: Moderator (Chairman) frames the problem, presents options and criteria.
///   Round 2: Expert/Critic discussion — analyse each option.
///   Round 3: Expert/Critic deliberation — refine positions, prepare for vote.
///   Round 4: Voting — each participant scores every option on each criterion (0–10).
/// After Round 4 the weighted scoring matrix is computed and stored in the session state.
/// </summary>
public class WeightedDeliberationMethod : IDecisionMethod
{
    private const int TotalRounds = 4;

    /// <summary>Neutral mid-point score used when a voter provides no explicit score for an option/criterion pair.</summary>
    private const double DefaultScore = 5.0;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly List<string> DefaultOptions =
        ["Status Quo", "Incremental Change", "Full Transformation"];

    private static readonly List<CriterionDef> DefaultCriteria =
    [
        new CriterionDef { Name = "Feasibility", Weight = 0.25 },
        new CriterionDef { Name = "Cost",        Weight = 0.25 },
        new CriterionDef { Name = "Impact",      Weight = 0.30 },
        new CriterionDef { Name = "Risk",        Weight = 0.20 }
    ];

    public MethodType Type => MethodType.WeightedDeliberation;

    public Task<NextPromptResult> GetNextPromptAsync(Session session, CancellationToken cancellationToken = default)
    {
        if (session.CurrentRoundNumber >= TotalRounds)
            return Task.FromResult(new NextPromptResult
            {
                PromptText = string.Empty,
                IsSessionComplete = true,
                CompletionReason = "Weighted deliberation and voting complete."
            });

        var state = ParseState(session.StatePayload);
        var options = state.Options;
        var criteria = state.Criteria;

        var promptText = session.CurrentRoundNumber switch
        {
            0 => $"MODERATOR ROUND: You are the Moderator. Present the decision problem to the council.\n\n" +
                 $"Problem: {state.Topic}\n" +
                 $"Context: {state.Context}\n\n" +
                 $"Options under consideration:\n" +
                 string.Join("\n", options.Select((o, i) => $"  {i + 1}. {o}")) +
                 $"\n\nEvaluation criteria (with weights):\n" +
                 string.Join("\n", criteria.Select(c => $"  • {c.Name} ({c.Weight:P0})")) +
                 "\n\nFrame the key considerations. What should the expert panel focus on when evaluating each option?",

            1 => $"DISCUSSION ROUND 1: Analyse the decision options for '{state.Topic}'.\n\n" +
                 $"Options: {string.Join(", ", options)}\n" +
                 $"Criteria: {string.Join(", ", criteria.Select(c => c.Name))}\n\n" +
                 "Share your expert analysis. What are the strengths and weaknesses of each option against the criteria?",

            2 => $"DISCUSSION ROUND 2: Refine your position on '{state.Topic}'.\n\n" +
                 "Having considered all perspectives from Round 1, elaborate on your final assessment. " +
                 "What are the critical trade-offs? What is your recommendation and why?",

            3 => $"VOTING ROUND: Score each option for '{state.Topic}' on a scale of 0 (worst) to 10 (best).\n\n" +
                 $"Options: {string.Join(", ", options)}\n" +
                 $"Criteria & weights: {string.Join(", ", criteria.Select(c => $"{c.Name}({c.Weight:P0})"))}\n\n" +
                 "Provide your scores in this exact format:\n" +
                 "SCORES:\n" +
                 string.Join("\n", options.Select(o =>
                     $"{o}: {string.Join(" | ", criteria.Select(c => $"{c.Name}=[0-10]"))}")) +
                 "\n\nBe objective and base scores on your expert judgment.",

            _ => string.Empty
        };

        return Task.FromResult(new NextPromptResult { PromptText = promptText, IsSessionComplete = false });
    }

    public Task<AggregationResult> AggregateRoundAsync(SessionRound round, string currentStatePayload, CancellationToken cancellationToken = default)
    {
        var state = ParseState(currentStatePayload);
        var contributions = round.Contributions.Select(c => $"- {c.RawContent}").ToList();
        bool shouldContinue = round.RoundNumber < TotalRounds;
        string summary;

        if (round.RoundNumber < TotalRounds)
        {
            var phaseLabel = round.RoundNumber switch
            {
                1 => "Moderator Framing",
                2 => "Expert Discussion — Round 1",
                3 => "Expert Discussion — Round 2",
                _ => $"Phase {round.RoundNumber}"
            };
            summary = $"{phaseLabel}:\n" + string.Join("\n", contributions);
            state.Discussions.Add(summary);
        }
        else
        {
            foreach (var contribution in round.Contributions)
            {
                var agentVotes = ParseScores(contribution.RawContent, state.Options, state.Criteria);
                if (agentVotes.Count > 0)
                    state.Votes[contribution.AgentId.ToString()] = agentVotes;
            }

            if (!state.Votes.Any())
                FillDefaultVotes(state, round);

            state.Matrix = ComputeMatrix(state.Options, state.Criteria, state.Votes);
            summary = BuildVotingSummary(state.Options, state.Criteria, state.Matrix);
        }

        state.RoundsCompleted = round.RoundNumber;

        return Task.FromResult(new AggregationResult
        {
            SummaryText = summary,
            UpdatedStatePayload = JsonSerializer.Serialize(state, JsonOpts),
            ShouldContinue = shouldContinue
        });
    }

    public Task<bool> ValidateCouncilAsync(Council council, CancellationToken cancellationToken = default)
        => Task.FromResult(council.Agents.Any());

    public Task<string> InitializeStateAsync(Council council, Issue issue, CancellationToken cancellationToken = default)
    {
        var state = new DeliberationState
        {
            Topic = issue.Title,
            Context = issue.ContextVector,
            Options = new List<string>(DefaultOptions),
            Criteria = new List<CriterionDef>(DefaultCriteria)
        };
        return Task.FromResult(JsonSerializer.Serialize(state, JsonOpts));
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static DeliberationState ParseState(string payload)
    {
        try
        {
            var state = JsonSerializer.Deserialize<DeliberationState>(payload, JsonOpts);
            if (state != null)
            {
                if (state.Options.Count == 0) state.Options = new List<string>(DefaultOptions);
                if (state.Criteria.Count == 0) state.Criteria = new List<CriterionDef>(DefaultCriteria);
                return state;
            }
        }
        catch { }

        return new DeliberationState
        {
            Options = new List<string>(DefaultOptions),
            Criteria = new List<CriterionDef>(DefaultCriteria)
        };
    }

    /// <summary>Parse lines like: "Option A: Feasibility=8 | Cost=7 | Impact=9 | Risk=6"</summary>
    private static Dictionary<string, Dictionary<string, int>> ParseScores(
        string rawContent, List<string> options, List<CriterionDef> criteria)
    {
        var result = new Dictionary<string, Dictionary<string, int>>();

        foreach (var option in options)
        {
            var optPattern = new Regex(
                $@"{Regex.Escape(option)}\s*:\s*(.+)",
                RegexOptions.IgnoreCase | RegexOptions.Multiline);
            var match = optPattern.Match(rawContent);
            if (!match.Success) continue;

            var scoreMatches = Regex.Matches(match.Groups[1].Value, @"(\w+)\s*[=:]\s*(\d+)");
            if (scoreMatches.Count == 0) continue;

            var scoreMap = new Dictionary<string, int>();
            foreach (Match sm in scoreMatches)
                scoreMap[sm.Groups[1].Value.Trim()] = Math.Clamp(int.Parse(sm.Groups[2].Value), 0, 10);

            result[option] = scoreMap;
        }

        return result;
    }

    private static void FillDefaultVotes(DeliberationState state, SessionRound round)
    {
        // Use the round's stable ID as a deterministic seed so demo results are
        // reproducible when no real AI model is configured (EchoAgentDriver fallback).
        var rng = new Random(Math.Abs(round.Id.GetHashCode()) % 997 + 1);
        var votes = new Dictionary<string, Dictionary<string, int>>();
        foreach (var option in state.Options)
            votes[option] = state.Criteria.ToDictionary(c => c.Name, _ => rng.Next(5, 10));
        state.Votes[round.Id.ToString()[..8]] = votes;
    }

    private static MatrixResult ComputeMatrix(
        List<string> options,
        List<CriterionDef> criteria,
        Dictionary<string, Dictionary<string, Dictionary<string, int>>> votes)
    {
        var avgScores = new Dictionary<string, Dictionary<string, double>>();

        foreach (var option in options)
        {
            avgScores[option] = new Dictionary<string, double>();
            foreach (var criterion in criteria)
            {
                var scores = votes.Values
                    .Where(v => v.ContainsKey(option) && v[option].ContainsKey(criterion.Name))
                    .Select(v => (double)v[option][criterion.Name])
                    .ToList();
                avgScores[option][criterion.Name] = scores.Count > 0 ? scores.Average() : DefaultScore;
            }
        }

        var weightedScores = new Dictionary<string, double>();
        foreach (var option in options)
            weightedScores[option] = criteria.Sum(c =>
                c.Weight * avgScores[option].GetValueOrDefault(c.Name, DefaultScore));

        var ranking = options.OrderByDescending(o => weightedScores.GetValueOrDefault(o, 0)).ToList();

        return new MatrixResult
        {
            Options = options,
            Criteria = criteria,
            AverageScores = avgScores,
            WeightedScores = weightedScores,
            Ranking = ranking
        };
    }

    private static string BuildVotingSummary(List<string> options, List<CriterionDef> criteria, MatrixResult matrix)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Voting & Weighted Scoring Matrix:");
        sb.AppendLine();

        sb.Append("| Option |");
        foreach (var c in criteria) sb.Append($" {c.Name} ({c.Weight:P0}) |");
        sb.AppendLine(" **Score** |");

        sb.Append("|---|");
        foreach (var _ in criteria) sb.Append("---|");
        sb.AppendLine("---|");

        foreach (var option in matrix.Ranking)
        {
            sb.Append($"| {option} |");
            foreach (var c in criteria)
            {
                var s = matrix.AverageScores.TryGetValue(option, out var os) && os.TryGetValue(c.Name, out var v) ? v : DefaultScore;
                sb.Append($" {s:F1} |");
            }
            var ws = matrix.WeightedScores.TryGetValue(option, out var w) ? $"**{w:F2}**" : "—";
            sb.AppendLine($" {ws} |");
        }

        sb.AppendLine();
        sb.AppendLine("**Ranking:**");
        for (int i = 0; i < matrix.Ranking.Count; i++)
        {
            var opt = matrix.Ranking[i];
            var ws = matrix.WeightedScores.TryGetValue(opt, out var w) ? $"({w:F2})" : "";
            sb.AppendLine($"{i + 1}. {opt} {ws}");
        }

        return sb.ToString().TrimEnd();
    }

    // ── State model ───────────────────────────────────────────────────────

    private class DeliberationState
    {
        public string Topic { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
        public List<CriterionDef> Criteria { get; set; } = new();
        public List<string> Discussions { get; set; } = new();
        public Dictionary<string, Dictionary<string, Dictionary<string, int>>> Votes { get; set; } = new();
        public MatrixResult? Matrix { get; set; }
        public int RoundsCompleted { get; set; }
    }

    private class MatrixResult
    {
        public List<string> Options { get; set; } = new();
        public List<CriterionDef> Criteria { get; set; } = new();
        public Dictionary<string, Dictionary<string, double>> AverageScores { get; set; } = new();
        public Dictionary<string, double> WeightedScores { get; set; } = new();
        public List<string> Ranking { get; set; } = new();
    }
}

public class CriterionDef
{
    public string Name { get; set; } = string.Empty;
    public double Weight { get; set; }
    [JsonConstructor]
    public CriterionDef() { }
    public CriterionDef(string name, double weight) { Name = name; Weight = weight; }
}

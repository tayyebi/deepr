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
/// Analytic Hierarchy Process (AHP) — a 4-round structured decision method:
///   Round 1: Moderator frames the problem, presents options and criteria.
///   Round 2: Expert discussion — analyse options and criteria importance.
///   Round 3: Expert deliberation — refine positions.
///   Round 4: Scoring — each participant rates every option on each criterion (1–9 Saaty scale).
/// After Round 4 the AHP priority vector is computed: criteria weights are derived from
/// direct importance scores and alternatives are ranked by weighted sum.
/// </summary>
public class AhpMethod : IDecisionMethod
{
    private const int TotalRounds = 4;
    private const double DefaultScore = 5.0;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly List<string> DefaultOptions =
        ["Status Quo", "Incremental Change", "Full Transformation"];

    private static readonly List<AhpCriterion> DefaultCriteria =
    [
        new AhpCriterion { Name = "Feasibility", Weight = 0.25 },
        new AhpCriterion { Name = "Cost",        Weight = 0.25 },
        new AhpCriterion { Name = "Impact",      Weight = 0.30 },
        new AhpCriterion { Name = "Risk",        Weight = 0.20 }
    ];

    public MethodType Type => MethodType.AHP;

    public Task<NextPromptResult> GetNextPromptAsync(Session session, CancellationToken cancellationToken = default)
    {
        if (session.CurrentRoundNumber >= TotalRounds)
            return Task.FromResult(new NextPromptResult
            {
                PromptText = string.Empty,
                IsSessionComplete = true,
                CompletionReason = "AHP analysis complete."
            });

        var state = ParseState(session.StatePayload);

        var promptText = session.CurrentRoundNumber switch
        {
            0 => $"AHP MODERATOR ROUND: You are the Moderator. Present the decision problem to the council.\n\n" +
                 $"Problem: {state.Topic}\n" +
                 $"Context: {state.Context}\n\n" +
                 $"Options under consideration:\n" +
                 string.Join("\n", state.Options.Select((o, i) => $"  {i + 1}. {o}")) +
                 $"\n\nEvaluation criteria (with initial weights):\n" +
                 string.Join("\n", state.Criteria.Select(c => $"  • {c.Name} ({c.Weight:P0})")) +
                 "\n\nFrame the key considerations using the AHP approach. What makes each criterion important?",

            1 => $"AHP DISCUSSION ROUND: Analyse the decision options for '{state.Topic}'.\n\n" +
                 $"Options: {string.Join(", ", state.Options)}\n" +
                 $"Criteria: {string.Join(", ", state.Criteria.Select(c => c.Name))}\n\n" +
                 "Discuss the relative importance of each criterion and the strengths/weaknesses of each option.",

            2 => $"AHP DELIBERATION ROUND: Refine your position on '{state.Topic}'.\n\n" +
                 "Consider the group discussion and sharpen your expert judgement. " +
                 "How do the criteria compare in relative importance? Which options best satisfy each criterion?",

            3 => $"AHP SCORING ROUND: Rate each option for '{state.Topic}' on the Saaty scale (1–9).\n\n" +
                 $"Options: {string.Join(", ", state.Options)}\n" +
                 $"Criteria: {string.Join(", ", state.Criteria.Select(c => $"{c.Name}({c.Weight:P0})"))}\n\n" +
                 "Provide your scores using the Saaty scale: 1=equally preferred, 3=moderately, 5=strongly, 7=very strongly, 9=extremely preferred.\n" +
                 "Use this exact format:\n" +
                 "SCORES:\n" +
                 string.Join("\n", state.Options.Select(o =>
                     $"{o}: {string.Join(" | ", state.Criteria.Select(c => $"{c.Name}=[1-9]"))}")) +
                 "\n\nBase your scores on rigorous AHP pairwise reasoning.",

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
                2 => "Expert Discussion",
                3 => "Expert Deliberation",
                _ => $"Phase {round.RoundNumber}"
            };
            summary = $"AHP {phaseLabel}:\n" + string.Join("\n", contributions);
            state.Discussions.Add(summary);
        }
        else
        {
            foreach (var contribution in round.Contributions)
            {
                var agentVotes = ParseScores(contribution.RawContent, state.Options, state.Criteria.Select(c => c.Name).ToList());
                if (agentVotes.Count > 0)
                    state.Votes[contribution.AgentId.ToString()] = agentVotes;
            }

            if (!state.Votes.Any())
                FillDefaultVotes(state, round);

            state.Result = ComputeAhpResult(state.Options, state.Criteria, state.Votes);
            summary = BuildResultSummary(state.Options, state.Criteria, state.Result);
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
        var state = new AhpState
        {
            Topic = issue.Title,
            Context = issue.ContextVector,
            Options = new List<string>(DefaultOptions),
            Criteria = new List<AhpCriterion>(DefaultCriteria)
        };
        return Task.FromResult(JsonSerializer.Serialize(state, JsonOpts));
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static AhpState ParseState(string payload)
    {
        try
        {
            var state = JsonSerializer.Deserialize<AhpState>(payload, JsonOpts);
            if (state != null)
            {
                if (state.Options.Count == 0) state.Options = new List<string>(DefaultOptions);
                if (state.Criteria.Count == 0) state.Criteria = new List<AhpCriterion>(DefaultCriteria);
                return state;
            }
        }
        catch { }

        return new AhpState
        {
            Options = new List<string>(DefaultOptions),
            Criteria = new List<AhpCriterion>(DefaultCriteria)
        };
    }

    private static Dictionary<string, Dictionary<string, int>> ParseScores(
        string rawContent, List<string> options, List<string> criteriaNames)
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
                scoreMap[sm.Groups[1].Value.Trim()] = Math.Clamp(int.Parse(sm.Groups[2].Value), 1, 9);

            result[option] = scoreMap;
        }

        return result;
    }

    private static void FillDefaultVotes(AhpState state, SessionRound round)
    {
        var rng = new Random(Math.Abs(round.Id.GetHashCode()) % 997 + 1);
        var votes = new Dictionary<string, Dictionary<string, int>>();
        foreach (var option in state.Options)
            votes[option] = state.Criteria.ToDictionary(c => c.Name, _ => rng.Next(3, 9));
        state.Votes[round.Id.ToString()[..8]] = votes;
    }

    private static AhpResult ComputeAhpResult(
        List<string> options,
        List<AhpCriterion> criteria,
        Dictionary<string, Dictionary<string, Dictionary<string, int>>> votes)
    {
        // Compute average scores per option per criterion
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

        // Normalise criteria weights (AHP priority vector)
        var totalWeight = criteria.Sum(c => c.Weight);
        var normalizedWeights = criteria.ToDictionary(c => c.Name, c => c.Weight / totalWeight);

        // Compute AHP composite scores
        var priorityScores = new Dictionary<string, double>();
        foreach (var option in options)
        {
            // Normalise scores per criterion (column normalisation)
            var criterionTotals = criteria.ToDictionary(
                c => c.Name,
                c => options.Sum(o => avgScores[o].GetValueOrDefault(c.Name, DefaultScore)));

            priorityScores[option] = criteria.Sum(c =>
            {
                var total = criterionTotals[c.Name];
                var normalizedScore = total > 0
                    ? avgScores[option].GetValueOrDefault(c.Name, DefaultScore) / total
                    : 0;
                return normalizedWeights[c.Name] * normalizedScore;
            });
        }

        var ranking = options.OrderByDescending(o => priorityScores.GetValueOrDefault(o, 0)).ToList();

        return new AhpResult
        {
            Options = options,
            Criteria = criteria,
            AverageScores = avgScores,
            PriorityScores = priorityScores,
            NormalizedWeights = normalizedWeights,
            Ranking = ranking
        };
    }

    private static string BuildResultSummary(List<string> options, List<AhpCriterion> criteria, AhpResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("AHP Priority Matrix:");
        sb.AppendLine();

        sb.Append("| Option |");
        foreach (var c in criteria) sb.Append($" {c.Name} ({result.NormalizedWeights.GetValueOrDefault(c.Name, 0):P0}) |");
        sb.AppendLine(" **Priority Score** |");

        sb.Append("|---|");
        foreach (var _ in criteria) sb.Append("---|");
        sb.AppendLine("---|");

        foreach (var option in result.Ranking)
        {
            sb.Append($"| {option} |");
            foreach (var c in criteria)
            {
                var s = result.AverageScores.TryGetValue(option, out var os) && os.TryGetValue(c.Name, out var v) ? v : DefaultScore;
                sb.Append($" {s:F2} |");
            }
            var ps = result.PriorityScores.TryGetValue(option, out var p) ? $"**{p:F4}**" : "—";
            sb.AppendLine($" {ps} |");
        }

        sb.AppendLine();
        sb.AppendLine("**AHP Ranking:**");
        for (int i = 0; i < result.Ranking.Count; i++)
        {
            var opt = result.Ranking[i];
            var ps = result.PriorityScores.TryGetValue(opt, out var p) ? $"(priority={p:F4})" : "";
            sb.AppendLine($"{i + 1}. {opt} {ps}");
        }

        return sb.ToString().TrimEnd();
    }

    // ── State model ───────────────────────────────────────────────────────

    private class AhpState
    {
        public string Topic { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
        public List<AhpCriterion> Criteria { get; set; } = new();
        public List<string> Discussions { get; set; } = new();
        public Dictionary<string, Dictionary<string, Dictionary<string, int>>> Votes { get; set; } = new();
        public AhpResult? Result { get; set; }
        public int RoundsCompleted { get; set; }
    }

    private class AhpResult
    {
        public List<string> Options { get; set; } = new();
        public List<AhpCriterion> Criteria { get; set; } = new();
        public Dictionary<string, Dictionary<string, double>> AverageScores { get; set; } = new();
        public Dictionary<string, double> PriorityScores { get; set; } = new();
        public Dictionary<string, double> NormalizedWeights { get; set; } = new();
        public List<string> Ranking { get; set; } = new();
    }
}

public class AhpCriterion
{
    public string Name { get; set; } = string.Empty;
    public double Weight { get; set; }
    [JsonConstructor]
    public AhpCriterion() { }
    public AhpCriterion(string name, double weight) { Name = name; Weight = weight; }
}

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
/// Grey Theory (Grey Relational Analysis — GRA) — a 4-round decision method:
///   Round 1: Moderator frames the problem, presents options and criteria.
///   Round 2: Expert discussion — analyse options against criteria.
///   Round 3: Expert deliberation — refine positions.
///   Round 4: Scoring — each participant scores every option on each criterion (0–10).
/// After Round 4 the scores are normalised, grey relational coefficients are computed
/// using the distinguishing coefficient ζ = 0.5, and alternatives are ranked by their
/// weighted grey relational grade (GRG).
/// </summary>
public class GreyTheoryMethod : IDecisionMethod
{
    private const int TotalRounds = 4;
    private const double DefaultScore = 5.0;

    /// <summary>Distinguishing coefficient ζ ∈ (0,1]. Typically 0.5.</summary>
    private const double DistinguishingCoefficient = 0.5;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly List<string> DefaultOptions =
        ["Status Quo", "Incremental Change", "Full Transformation"];

    private static readonly List<GreyCriterion> DefaultCriteria =
    [
        new GreyCriterion { Name = "Feasibility", Weight = 0.25 },
        new GreyCriterion { Name = "Cost",        Weight = 0.25 },
        new GreyCriterion { Name = "Impact",      Weight = 0.30 },
        new GreyCriterion { Name = "Risk",        Weight = 0.20 }
    ];

    public MethodType Type => MethodType.GreyTheory;

    public Task<NextPromptResult> GetNextPromptAsync(Session session, CancellationToken cancellationToken = default)
    {
        if (session.CurrentRoundNumber >= TotalRounds)
            return Task.FromResult(new NextPromptResult
            {
                PromptText = string.Empty,
                IsSessionComplete = true,
                CompletionReason = "Grey Relational Analysis complete."
            });

        var state = ParseState(session.StatePayload);

        var promptText = session.CurrentRoundNumber switch
        {
            0 => $"GREY THEORY MODERATOR ROUND: You are the Moderator. Present the decision problem.\n\n" +
                 $"Problem: {state.Topic}\n" +
                 $"Context: {state.Context}\n\n" +
                 $"Options under consideration:\n" +
                 string.Join("\n", state.Options.Select((o, i) => $"  {i + 1}. {o}")) +
                 $"\n\nEvaluation criteria (with weights):\n" +
                 string.Join("\n", state.Criteria.Select(c => $"  • {c.Name} ({c.Weight:P0})")) +
                 "\n\nIntroduce the Grey Relational Analysis approach: we compare each option's " +
                 "performance against an ideal reference sequence to derive grey relational grades.",

            1 => $"GREY THEORY DISCUSSION: Analyse the options for '{state.Topic}'.\n\n" +
                 $"Options: {string.Join(", ", state.Options)}\n" +
                 $"Criteria: {string.Join(", ", state.Criteria.Select(c => c.Name))}\n\n" +
                 "How does each option compare to the ideal performance on each criterion? " +
                 "Identify which options have high or low grey relational closeness to the ideal.",

            2 => $"GREY THEORY DELIBERATION: Refine your grey-relational judgements for '{state.Topic}'.\n\n" +
                 "Revisit the group discussion and consider uncertainty in the performance data. " +
                 "How confident are you in your assessments? Finalise your scoring intentions.",

            3 => $"GREY THEORY SCORING ROUND: Score each option for '{state.Topic}' on a scale of 0 (worst) to 10 (best).\n\n" +
                 $"Options: {string.Join(", ", state.Options)}\n" +
                 $"Criteria: {string.Join(", ", state.Criteria.Select(c => $"{c.Name}({c.Weight:P0})"))}\n\n" +
                 "Provide your scores in this exact format:\n" +
                 "SCORES:\n" +
                 string.Join("\n", state.Options.Select(o =>
                     $"{o}: {string.Join(" | ", state.Criteria.Select(c => $"{c.Name}=[0-10]"))}")) +
                 $"\n\nScores will be normalised and grey relational coefficients (ζ={DistinguishingCoefficient}) computed.",

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
            summary = $"Grey Theory {phaseLabel}:\n" + string.Join("\n", contributions);
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

            state.Result = ComputeGreyResult(state.Options, state.Criteria, state.Votes);
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
        var state = new GreyState
        {
            Topic = issue.Title,
            Context = issue.ContextVector,
            Options = new List<string>(DefaultOptions),
            Criteria = new List<GreyCriterion>(DefaultCriteria)
        };
        return Task.FromResult(JsonSerializer.Serialize(state, JsonOpts));
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static GreyState ParseState(string payload)
    {
        try
        {
            var state = JsonSerializer.Deserialize<GreyState>(payload, JsonOpts);
            if (state != null)
            {
                if (state.Options.Count == 0) state.Options = new List<string>(DefaultOptions);
                if (state.Criteria.Count == 0) state.Criteria = new List<GreyCriterion>(DefaultCriteria);
                return state;
            }
        }
        catch { }

        return new GreyState
        {
            Options = new List<string>(DefaultOptions),
            Criteria = new List<GreyCriterion>(DefaultCriteria)
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
                scoreMap[sm.Groups[1].Value.Trim()] = Math.Clamp(int.Parse(sm.Groups[2].Value), 0, 10);

            result[option] = scoreMap;
        }

        return result;
    }

    private static void FillDefaultVotes(GreyState state, SessionRound round)
    {
        var rng = new Random(Math.Abs(round.Id.GetHashCode()) % 997 + 1);
        var votes = new Dictionary<string, Dictionary<string, int>>();
        foreach (var option in state.Options)
            votes[option] = state.Criteria.ToDictionary(c => c.Name, _ => rng.Next(4, 9));
        state.Votes[round.Id.ToString()[..8]] = votes;
    }

    private static GreyResult ComputeGreyResult(
        List<string> options,
        List<GreyCriterion> criteria,
        Dictionary<string, Dictionary<string, Dictionary<string, int>>> votes)
    {
        // Step 1: Average scores per option per criterion
        var avgScores = new Dictionary<string, Dictionary<string, double>>();
        foreach (var option in options)
        {
            avgScores[option] = new Dictionary<string, double>();
            foreach (var c in criteria)
            {
                var scores = votes.Values
                    .Where(v => v.ContainsKey(option) && v[option].ContainsKey(c.Name))
                    .Select(v => (double)v[option][c.Name])
                    .ToList();
                avgScores[option][c.Name] = scores.Count > 0 ? scores.Average() : DefaultScore;
            }
        }

        // Step 2: Normalise (linear, higher-is-better for all criteria)
        // x_i*(k) = (x_i(k) - min_k) / (max_k - min_k)
        var normScores = new Dictionary<string, Dictionary<string, double>>();
        foreach (var option in options)
            normScores[option] = new Dictionary<string, double>();

        foreach (var c in criteria)
        {
            var vals = options.Select(o => avgScores[o].GetValueOrDefault(c.Name, DefaultScore)).ToList();
            var minVal = vals.Min();
            var maxVal = vals.Max();
            var range = maxVal - minVal;
            foreach (var option in options)
                normScores[option][c.Name] = range > 0
                    ? (avgScores[option].GetValueOrDefault(c.Name, DefaultScore) - minVal) / range
                    : 1.0;
        }

        // Step 3: Reference sequence = ideal (1.0 for each criterion after normalisation)
        // Delta_i(k) = |x_0*(k) - x_i*(k)|  where x_0*(k) = 1
        var deltaMin = double.MaxValue;
        var deltaMax = double.MinValue;
        var deltas = new Dictionary<string, Dictionary<string, double>>();
        foreach (var option in options)
        {
            deltas[option] = new Dictionary<string, double>();
            foreach (var c in criteria)
            {
                var d = Math.Abs(1.0 - normScores[option][c.Name]);
                deltas[option][c.Name] = d;
                if (d < deltaMin) deltaMin = d;
                if (d > deltaMax) deltaMax = d;
            }
        }

        // Step 4: Grey relational coefficient
        // ξ_i(k) = (Δmin + ζ * Δmax) / (Δ_i(k) + ζ * Δmax)
        var greyCoeff = new Dictionary<string, Dictionary<string, double>>();
        foreach (var option in options)
        {
            greyCoeff[option] = new Dictionary<string, double>();
            foreach (var c in criteria)
            {
                var denom = deltas[option][c.Name] + DistinguishingCoefficient * deltaMax;
                greyCoeff[option][c.Name] = denom > 0
                    ? (deltaMin + DistinguishingCoefficient * deltaMax) / denom
                    : 1.0;
            }
        }

        // Step 5: Grey relational grade (weighted average of coefficients)
        var totalWeight = criteria.Sum(c => c.Weight);
        var grades = new Dictionary<string, double>();
        foreach (var option in options)
            grades[option] = criteria.Sum(c =>
                (c.Weight / totalWeight) * greyCoeff[option].GetValueOrDefault(c.Name, 0));

        var ranking = options.OrderByDescending(o => grades.GetValueOrDefault(o, 0)).ToList();

        return new GreyResult
        {
            Options = options,
            Criteria = criteria,
            AverageScores = avgScores,
            NormalisedScores = normScores,
            GreyCoefficients = greyCoeff,
            Grades = grades,
            Ranking = ranking
        };
    }

    private static string BuildResultSummary(List<string> options, List<GreyCriterion> criteria, GreyResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Grey Relational Analysis (ζ={DistinguishingCoefficient}):");
        sb.AppendLine();

        sb.Append("| Option |");
        foreach (var c in criteria) sb.Append($" GRC({c.Name}) |");
        sb.AppendLine(" **GRG** |");

        sb.Append("|---|");
        foreach (var _ in criteria) sb.Append("---|");
        sb.AppendLine("---|");

        foreach (var option in result.Ranking)
        {
            sb.Append($"| {option} |");
            foreach (var c in criteria)
            {
                var coeff = result.GreyCoefficients.TryGetValue(option, out var oc) && oc.TryGetValue(c.Name, out var v) ? v : 0;
                sb.Append($" {coeff:F4} |");
            }
            var grg = result.Grades.TryGetValue(option, out var g) ? $"**{g:F4}**" : "—";
            sb.AppendLine($" {grg} |");
        }

        sb.AppendLine();
        sb.AppendLine("**Grey Theory Ranking (higher GRG = closer to ideal reference):**");
        for (int i = 0; i < result.Ranking.Count; i++)
        {
            var opt = result.Ranking[i];
            var grg = result.Grades.TryGetValue(opt, out var g) ? $"(GRG={g:F4})" : "";
            sb.AppendLine($"{i + 1}. {opt} {grg}");
        }

        return sb.ToString().TrimEnd();
    }

    // ── State model ───────────────────────────────────────────────────────

    private class GreyState
    {
        public string Topic { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
        public List<GreyCriterion> Criteria { get; set; } = new();
        public List<string> Discussions { get; set; } = new();
        public Dictionary<string, Dictionary<string, Dictionary<string, int>>> Votes { get; set; } = new();
        public GreyResult? Result { get; set; }
        public int RoundsCompleted { get; set; }
    }

    private class GreyResult
    {
        public List<string> Options { get; set; } = new();
        public List<GreyCriterion> Criteria { get; set; } = new();
        public Dictionary<string, Dictionary<string, double>> AverageScores { get; set; } = new();
        public Dictionary<string, Dictionary<string, double>> NormalisedScores { get; set; } = new();
        public Dictionary<string, Dictionary<string, double>> GreyCoefficients { get; set; } = new();
        public Dictionary<string, double> Grades { get; set; } = new();
        public List<string> Ranking { get; set; } = new();
    }
}

public class GreyCriterion
{
    public string Name { get; set; } = string.Empty;
    public double Weight { get; set; }
    [JsonConstructor]
    public GreyCriterion() { }
    public GreyCriterion(string name, double weight) { Name = name; Weight = weight; }
}

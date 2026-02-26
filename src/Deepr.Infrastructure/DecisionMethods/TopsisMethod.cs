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
/// TOPSIS (Technique for Order of Preference by Similarity to Ideal Solution) — a 4-round method:
///   Round 1: Moderator frames the problem, presents options and criteria.
///   Round 2: Expert discussion — analyse options against criteria.
///   Round 3: Expert deliberation — refine positions.
///   Round 4: Scoring — each participant scores every option on each criterion (0–10).
/// After Round 4 the decision matrix is normalised, weighted, and the distances to the
/// positive ideal solution (PIS) and negative ideal solution (NIS) are computed.
/// Alternatives are ranked by their closeness coefficient (higher is better).
/// </summary>
public class TopsisMethod : IDecisionMethod
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

    private static readonly List<TopsisCriterion> DefaultCriteria =
    [
        new TopsisCriterion { Name = "Feasibility", Weight = 0.25, IsBenefit = true },
        new TopsisCriterion { Name = "Cost",        Weight = 0.25, IsBenefit = false },
        new TopsisCriterion { Name = "Impact",      Weight = 0.30, IsBenefit = true },
        new TopsisCriterion { Name = "Risk",        Weight = 0.20, IsBenefit = false }
    ];

    public MethodType Type => MethodType.TOPSIS;

    public Task<NextPromptResult> GetNextPromptAsync(Session session, CancellationToken cancellationToken = default)
    {
        if (session.CurrentRoundNumber >= TotalRounds)
            return Task.FromResult(new NextPromptResult
            {
                PromptText = string.Empty,
                IsSessionComplete = true,
                CompletionReason = "TOPSIS ideal-solution analysis complete."
            });

        var state = ParseState(session.StatePayload);

        var promptText = session.CurrentRoundNumber switch
        {
            0 => $"TOPSIS MODERATOR ROUND: You are the Moderator. Present the decision problem.\n\n" +
                 $"Problem: {state.Topic}\n" +
                 $"Context: {state.Context}\n\n" +
                 $"Options under consideration:\n" +
                 string.Join("\n", state.Options.Select((o, i) => $"  {i + 1}. {o}")) +
                 $"\n\nEvaluation criteria (weight | type):\n" +
                 string.Join("\n", state.Criteria.Select(c =>
                     $"  • {c.Name} ({c.Weight:P0} | {(c.IsBenefit ? "benefit" : "cost")})")) +
                 "\n\nIntroduce the TOPSIS approach: we seek the option closest to the ideal and farthest from the anti-ideal solution.",

            1 => $"TOPSIS DISCUSSION: Analyse the options for '{state.Topic}'.\n\n" +
                 $"Options: {string.Join(", ", state.Options)}\n" +
                 $"Criteria: {string.Join(", ", state.Criteria.Select(c => $"{c.Name} ({(c.IsBenefit ? "↑ benefit" : "↓ cost")})"))} \n\n" +
                 "Assess how well each option performs on benefit and cost criteria. " +
                 "Which options resemble the ideal solution most closely?",

            2 => $"TOPSIS DELIBERATION: Refine your distance-based judgements for '{state.Topic}'.\n\n" +
                 "Revisit the group discussion. Consider the trade-offs between ideal proximity " +
                 "and anti-ideal distance. Confirm your scoring intentions.",

            3 => $"TOPSIS SCORING ROUND: Score each option for '{state.Topic}' on a scale of 0 (worst) to 10 (best).\n\n" +
                 $"Options: {string.Join(", ", state.Options)}\n" +
                 $"Criteria: {string.Join(", ", state.Criteria.Select(c => $"{c.Name}({c.Weight:P0},{(c.IsBenefit ? "B" : "C")})"))}\n\n" +
                 "Provide your scores in this exact format:\n" +
                 "SCORES:\n" +
                 string.Join("\n", state.Options.Select(o =>
                     $"{o}: {string.Join(" | ", state.Criteria.Select(c => $"{c.Name}=[0-10]"))}")) +
                 "\n\nScores will be used to compute closeness coefficients via TOPSIS.",

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
            summary = $"TOPSIS {phaseLabel}:\n" + string.Join("\n", contributions);
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

            state.Result = ComputeTopsisResult(state.Options, state.Criteria, state.Votes);
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
        var state = new TopsisState
        {
            Topic = issue.Title,
            Context = issue.ContextVector,
            Options = new List<string>(DefaultOptions),
            Criteria = new List<TopsisCriterion>(DefaultCriteria)
        };
        return Task.FromResult(JsonSerializer.Serialize(state, JsonOpts));
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static TopsisState ParseState(string payload)
    {
        try
        {
            var state = JsonSerializer.Deserialize<TopsisState>(payload, JsonOpts);
            if (state != null)
            {
                if (state.Options.Count == 0) state.Options = new List<string>(DefaultOptions);
                if (state.Criteria.Count == 0) state.Criteria = new List<TopsisCriterion>(DefaultCriteria);
                return state;
            }
        }
        catch { }

        return new TopsisState
        {
            Options = new List<string>(DefaultOptions),
            Criteria = new List<TopsisCriterion>(DefaultCriteria)
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

    private static void FillDefaultVotes(TopsisState state, SessionRound round)
    {
        var rng = new Random(Math.Abs(round.Id.GetHashCode()) % 997 + 1);
        var votes = new Dictionary<string, Dictionary<string, int>>();
        foreach (var option in state.Options)
            votes[option] = state.Criteria.ToDictionary(c => c.Name, _ => rng.Next(4, 9));
        state.Votes[round.Id.ToString()[..8]] = votes;
    }

    private static TopsisResult ComputeTopsisResult(
        List<string> options,
        List<TopsisCriterion> criteria,
        Dictionary<string, Dictionary<string, Dictionary<string, int>>> votes)
    {
        // Step 1: Compute average scores
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

        // Step 2: Normalise the decision matrix (vector normalisation)
        var normScores = new Dictionary<string, Dictionary<string, double>>();
        foreach (var option in options)
            normScores[option] = new Dictionary<string, double>();

        foreach (var c in criteria)
        {
            var euclideanNorm = Math.Sqrt(options.Sum(o =>
                Math.Pow(avgScores[o].GetValueOrDefault(c.Name, DefaultScore), 2)));
            if (euclideanNorm == 0) euclideanNorm = 1;
            foreach (var option in options)
                normScores[option][c.Name] =
                    avgScores[option].GetValueOrDefault(c.Name, DefaultScore) / euclideanNorm;
        }

        // Step 3: Weighted normalised matrix
        var totalWeight = criteria.Sum(c => c.Weight);
        var weightedNorm = new Dictionary<string, Dictionary<string, double>>();
        foreach (var option in options)
        {
            weightedNorm[option] = new Dictionary<string, double>();
            foreach (var c in criteria)
                weightedNorm[option][c.Name] = (c.Weight / totalWeight) * normScores[option][c.Name];
        }

        // Step 4: Positive ideal solution (PIS) and negative ideal solution (NIS)
        var pis = new Dictionary<string, double>();
        var nis = new Dictionary<string, double>();
        foreach (var c in criteria)
        {
            var vals = options.Select(o => weightedNorm[o][c.Name]).ToList();
            pis[c.Name] = c.IsBenefit ? vals.Max() : vals.Min();
            nis[c.Name] = c.IsBenefit ? vals.Min() : vals.Max();
        }

        // Step 5: Distances to PIS and NIS
        var distanceToPis = new Dictionary<string, double>();
        var distanceToNis = new Dictionary<string, double>();
        foreach (var option in options)
        {
            distanceToPis[option] = Math.Sqrt(criteria.Sum(c =>
                Math.Pow(weightedNorm[option][c.Name] - pis[c.Name], 2)));
            distanceToNis[option] = Math.Sqrt(criteria.Sum(c =>
                Math.Pow(weightedNorm[option][c.Name] - nis[c.Name], 2)));
        }

        // Step 6: Closeness coefficient
        var closeness = new Dictionary<string, double>();
        foreach (var option in options)
        {
            var dPlus = distanceToPis[option];
            var dMinus = distanceToNis[option];
            closeness[option] = dPlus + dMinus > 0 ? dMinus / (dPlus + dMinus) : 0;
        }

        var ranking = options.OrderByDescending(o => closeness.GetValueOrDefault(o, 0)).ToList();

        return new TopsisResult
        {
            Options = options,
            Criteria = criteria,
            AverageScores = avgScores,
            DistanceToPis = distanceToPis,
            DistanceToNis = distanceToNis,
            ClosenessCoefficients = closeness,
            Ranking = ranking
        };
    }

    private static string BuildResultSummary(List<string> options, List<TopsisCriterion> criteria, TopsisResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("TOPSIS Closeness Analysis:");
        sb.AppendLine();

        sb.Append("| Option |");
        foreach (var c in criteria) sb.Append($" {c.Name} ({(c.IsBenefit ? "B" : "C")}) |");
        sb.AppendLine(" D+ | D- | **C*** |");

        sb.Append("|---|");
        foreach (var _ in criteria) sb.Append("---|");
        sb.AppendLine("---|---|---|");

        foreach (var option in result.Ranking)
        {
            sb.Append($"| {option} |");
            foreach (var c in criteria)
            {
                var s = result.AverageScores.TryGetValue(option, out var os) && os.TryGetValue(c.Name, out var v) ? v : DefaultScore;
                sb.Append($" {s:F1} |");
            }
            var dPlus = result.DistanceToPis.TryGetValue(option, out var dp) ? dp : 0;
            var dMinus = result.DistanceToNis.TryGetValue(option, out var dm) ? dm : 0;
            var cc = result.ClosenessCoefficients.TryGetValue(option, out var ccValue) ? $"**{ccValue:F4}**" : "—";
            sb.AppendLine($" {dPlus:F4} | {dMinus:F4} | {cc} |");
        }

        sb.AppendLine();
        sb.AppendLine("**TOPSIS Ranking (higher C* = closer to ideal):**");
        for (int i = 0; i < result.Ranking.Count; i++)
        {
            var opt = result.Ranking[i];
            var cc = result.ClosenessCoefficients.TryGetValue(opt, out var c) ? $"(C*={c:F4})" : "";
            sb.AppendLine($"{i + 1}. {opt} {cc}");
        }

        return sb.ToString().TrimEnd();
    }

    // ── State model ───────────────────────────────────────────────────────

    private class TopsisState
    {
        public string Topic { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
        public List<TopsisCriterion> Criteria { get; set; } = new();
        public List<string> Discussions { get; set; } = new();
        public Dictionary<string, Dictionary<string, Dictionary<string, int>>> Votes { get; set; } = new();
        public TopsisResult? Result { get; set; }
        public int RoundsCompleted { get; set; }
    }

    private class TopsisResult
    {
        public List<string> Options { get; set; } = new();
        public List<TopsisCriterion> Criteria { get; set; } = new();
        public Dictionary<string, Dictionary<string, double>> AverageScores { get; set; } = new();
        public Dictionary<string, double> DistanceToPis { get; set; } = new();
        public Dictionary<string, double> DistanceToNis { get; set; } = new();
        public Dictionary<string, double> ClosenessCoefficients { get; set; } = new();
        public List<string> Ranking { get; set; } = new();
    }
}

public class TopsisCriterion
{
    public string Name { get; set; } = string.Empty;
    public double Weight { get; set; }

    /// <summary>True if higher score is better (benefit criterion); false if lower is better (cost criterion).</summary>
    public bool IsBenefit { get; set; } = true;

    [JsonConstructor]
    public TopsisCriterion() { }
    public TopsisCriterion(string name, double weight, bool isBenefit = true)
    {
        Name = name;
        Weight = weight;
        IsBenefit = isBenefit;
    }
}

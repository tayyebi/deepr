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
/// PROMETHEE II (Preference Ranking Organisation Method for Enrichment Evaluation) — a 4-round method:
///   Round 1: Moderator frames the problem, presents options and criteria.
///   Round 2: Expert discussion — analyse options against criteria.
///   Round 3: Expert deliberation — refine positions.
///   Round 4: Scoring — each participant scores every option on each criterion (0–10).
/// After Round 4 pairwise preference indices are computed using the linear preference function,
/// and alternatives are ranked by their net outranking flow (φ = φ+ − φ−).
/// </summary>
public class PrometheeMethod : IDecisionMethod
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

    private static readonly List<PrometheeCriterion> DefaultCriteria =
    [
        new PrometheeCriterion { Name = "Feasibility", Weight = 0.25 },
        new PrometheeCriterion { Name = "Cost",        Weight = 0.25 },
        new PrometheeCriterion { Name = "Impact",      Weight = 0.30 },
        new PrometheeCriterion { Name = "Risk",        Weight = 0.20 }
    ];

    public MethodType Type => MethodType.PROMETHEE;

    public Task<NextPromptResult> GetNextPromptAsync(Session session, CancellationToken cancellationToken = default)
    {
        if (session.CurrentRoundNumber >= TotalRounds)
            return Task.FromResult(new NextPromptResult
            {
                PromptText = string.Empty,
                IsSessionComplete = true,
                CompletionReason = "PROMETHEE II net-flow ranking complete."
            });

        var state = ParseState(session.StatePayload);

        var promptText = session.CurrentRoundNumber switch
        {
            0 => $"PROMETHEE MODERATOR ROUND: You are the Moderator. Present the decision problem.\n\n" +
                 $"Problem: {state.Topic}\n" +
                 $"Context: {state.Context}\n\n" +
                 $"Options under consideration:\n" +
                 string.Join("\n", state.Options.Select((o, i) => $"  {i + 1}. {o}")) +
                 $"\n\nEvaluation criteria (with weights):\n" +
                 string.Join("\n", state.Criteria.Select(c => $"  • {c.Name} ({c.Weight:P0})")) +
                 "\n\nIntroduce the PROMETHEE II approach: pairwise preference flows determine the final ranking.",

            1 => $"PROMETHEE DISCUSSION: Analyse the options for '{state.Topic}'.\n\n" +
                 $"Options: {string.Join(", ", state.Options)}\n" +
                 $"Criteria: {string.Join(", ", state.Criteria.Select(c => c.Name))}\n\n" +
                 "Compare options pairwise. Where does one option clearly outperform another, and by how much? " +
                 "Identify strong and weak preferences.",

            2 => $"PROMETHEE DELIBERATION: Finalise your preference judgements for '{state.Topic}'.\n\n" +
                 "Consider the group's pairwise comparisons. Are your preferences consistent? " +
                 "What is your overall view on the net flow ranking?",

            3 => $"PROMETHEE SCORING ROUND: Score each option for '{state.Topic}' on a scale of 0 (worst) to 10 (best).\n\n" +
                 $"Options: {string.Join(", ", state.Options)}\n" +
                 $"Criteria: {string.Join(", ", state.Criteria.Select(c => $"{c.Name}({c.Weight:P0})"))}\n\n" +
                 "Provide your scores in this exact format:\n" +
                 "SCORES:\n" +
                 string.Join("\n", state.Options.Select(o =>
                     $"{o}: {string.Join(" | ", state.Criteria.Select(c => $"{c.Name}=[0-10]"))}")) +
                 "\n\nScores will be used to compute pairwise preference indices and net outranking flows.",

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
            summary = $"PROMETHEE {phaseLabel}:\n" + string.Join("\n", contributions);
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

            state.Result = ComputePrometheeResult(state.Options, state.Criteria, state.Votes);
            summary = BuildResultSummary(state.Options, state.Result);
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
        var state = new PrometheeState
        {
            Topic = issue.Title,
            Context = issue.ContextVector,
            Options = new List<string>(DefaultOptions),
            Criteria = new List<PrometheeCriterion>(DefaultCriteria)
        };
        return Task.FromResult(JsonSerializer.Serialize(state, JsonOpts));
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static PrometheeState ParseState(string payload)
    {
        try
        {
            var state = JsonSerializer.Deserialize<PrometheeState>(payload, JsonOpts);
            if (state != null)
            {
                if (state.Options.Count == 0) state.Options = new List<string>(DefaultOptions);
                if (state.Criteria.Count == 0) state.Criteria = new List<PrometheeCriterion>(DefaultCriteria);
                return state;
            }
        }
        catch { }

        return new PrometheeState
        {
            Options = new List<string>(DefaultOptions),
            Criteria = new List<PrometheeCriterion>(DefaultCriteria)
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

    private static void FillDefaultVotes(PrometheeState state, SessionRound round)
    {
        var rng = new Random(Math.Abs(round.Id.GetHashCode()) % 997 + 1);
        var votes = new Dictionary<string, Dictionary<string, int>>();
        foreach (var option in state.Options)
            votes[option] = state.Criteria.ToDictionary(c => c.Name, _ => rng.Next(4, 9));
        state.Votes[round.Id.ToString()[..8]] = votes;
    }

    private static PrometheeResult ComputePrometheeResult(
        List<string> options,
        List<PrometheeCriterion> criteria,
        Dictionary<string, Dictionary<string, Dictionary<string, int>>> votes)
    {
        // Average scores per option per criterion
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

        var totalWeight = criteria.Sum(c => c.Weight);

        // Compute pairwise preference index π(a,b) using linear preference function
        // P_j(d) = max(d,0)/10 where d = f_j(a)-f_j(b) (linear type over [0,10])
        var positiveFlow = new Dictionary<string, double>();
        var negativeFlow = new Dictionary<string, double>();
        int n = options.Count;

        foreach (var a in options)
        {
            double phiPlus = 0;
            double phiMinus = 0;
            foreach (var b in options)
            {
                if (a == b) continue;

                // π(a,b)
                double piAB = criteria.Sum(c =>
                {
                    var diff = avgScores[a].GetValueOrDefault(c.Name, DefaultScore) -
                               avgScores[b].GetValueOrDefault(c.Name, DefaultScore);
                    var pref = Math.Max(diff, 0) / 10.0;
                    return (c.Weight / totalWeight) * pref;
                });

                // π(b,a)
                double piBA = criteria.Sum(c =>
                {
                    var diff = avgScores[b].GetValueOrDefault(c.Name, DefaultScore) -
                               avgScores[a].GetValueOrDefault(c.Name, DefaultScore);
                    var pref = Math.Max(diff, 0) / 10.0;
                    return (c.Weight / totalWeight) * pref;
                });

                phiPlus += piAB;
                phiMinus += piBA;
            }

            int divisor = n > 1 ? n - 1 : 1;
            positiveFlow[a] = phiPlus / divisor;
            negativeFlow[a] = phiMinus / divisor;
        }

        var netFlow = new Dictionary<string, double>();
        foreach (var option in options)
            netFlow[option] = positiveFlow.GetValueOrDefault(option, 0) - negativeFlow.GetValueOrDefault(option, 0);

        var ranking = options.OrderByDescending(o => netFlow.GetValueOrDefault(o, 0)).ToList();

        return new PrometheeResult
        {
            Options = options,
            Criteria = criteria,
            AverageScores = avgScores,
            PositiveFlow = positiveFlow,
            NegativeFlow = negativeFlow,
            NetFlow = netFlow,
            Ranking = ranking
        };
    }

    private static string BuildResultSummary(List<string> options, PrometheeResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("PROMETHEE II Net Flow Analysis:");
        sb.AppendLine();

        sb.AppendLine("| Option | φ+ (Leaving) | φ− (Entering) | **φ (Net)** |");
        sb.AppendLine("|---|---|---|---|");

        foreach (var option in result.Ranking)
        {
            var phiPlus = result.PositiveFlow.TryGetValue(option, out var pp) ? pp : 0;
            var phiMinus = result.NegativeFlow.TryGetValue(option, out var pm) ? pm : 0;
            var phiNet = result.NetFlow.TryGetValue(option, out var pn) ? $"**{pn:+0.0000;-0.0000;0.0000}**" : "—";
            sb.AppendLine($"| {option} | {phiPlus:F4} | {phiMinus:F4} | {phiNet} |");
        }

        sb.AppendLine();
        sb.AppendLine("**PROMETHEE II Ranking (higher net flow φ = more preferred):**");
        for (int i = 0; i < result.Ranking.Count; i++)
        {
            var opt = result.Ranking[i];
            var phi = result.NetFlow.TryGetValue(opt, out var n) ? $"(φ={n:+0.0000;-0.0000;0.0000})" : "";
            sb.AppendLine($"{i + 1}. {opt} {phi}");
        }

        return sb.ToString().TrimEnd();
    }

    // ── State model ───────────────────────────────────────────────────────

    private class PrometheeState
    {
        public string Topic { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
        public List<PrometheeCriterion> Criteria { get; set; } = new();
        public List<string> Discussions { get; set; } = new();
        public Dictionary<string, Dictionary<string, Dictionary<string, int>>> Votes { get; set; } = new();
        public PrometheeResult? Result { get; set; }
        public int RoundsCompleted { get; set; }
    }

    private class PrometheeResult
    {
        public List<string> Options { get; set; } = new();
        public List<PrometheeCriterion> Criteria { get; set; } = new();
        public Dictionary<string, Dictionary<string, double>> AverageScores { get; set; } = new();
        public Dictionary<string, double> PositiveFlow { get; set; } = new();
        public Dictionary<string, double> NegativeFlow { get; set; } = new();
        public Dictionary<string, double> NetFlow { get; set; } = new();
        public List<string> Ranking { get; set; } = new();
    }
}

public class PrometheeCriterion
{
    public string Name { get; set; } = string.Empty;
    public double Weight { get; set; }
    [JsonConstructor]
    public PrometheeCriterion() { }
    public PrometheeCriterion(string name, double weight) { Name = name; Weight = weight; }
}

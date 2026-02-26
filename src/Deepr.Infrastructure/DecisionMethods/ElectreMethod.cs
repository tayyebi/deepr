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
/// ELECTRE (Elimination Et Choix Traduisant la Réalité) — a 4-round outranking decision method:
///   Round 1: Moderator frames the problem, presents options and criteria.
///   Round 2: Expert discussion — analyse options against criteria.
///   Round 3: Expert deliberation — refine positions.
///   Round 4: Scoring — each participant scores every option on each criterion (0–10).
/// After Round 4 the concordance and discordance matrices are computed, outranking
/// relations are established, and alternatives are ranked by net outranking score.
/// </summary>
public class ElectreMethod : IDecisionMethod
{
    private const int TotalRounds = 4;
    private const double DefaultScore = 5.0;

    /// <summary>Concordance threshold: an alternative outranks another only when C ≥ ConcordanceThreshold.</summary>
    private const double ConcordanceThreshold = 0.6;

    /// <summary>Discordance threshold: outranking is vetoed when D > DiscordanceThreshold.</summary>
    private const double DiscordanceThreshold = 0.4;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly List<string> DefaultOptions =
        ["Status Quo", "Incremental Change", "Full Transformation"];

    private static readonly List<ElectreCriterion> DefaultCriteria =
    [
        new ElectreCriterion { Name = "Feasibility", Weight = 0.25 },
        new ElectreCriterion { Name = "Cost",        Weight = 0.25 },
        new ElectreCriterion { Name = "Impact",      Weight = 0.30 },
        new ElectreCriterion { Name = "Risk",        Weight = 0.20 }
    ];

    public MethodType Type => MethodType.ELECTRE;

    public Task<NextPromptResult> GetNextPromptAsync(Session session, CancellationToken cancellationToken = default)
    {
        if (session.CurrentRoundNumber >= TotalRounds)
            return Task.FromResult(new NextPromptResult
            {
                PromptText = string.Empty,
                IsSessionComplete = true,
                CompletionReason = "ELECTRE outranking analysis complete."
            });

        var state = ParseState(session.StatePayload);

        var promptText = session.CurrentRoundNumber switch
        {
            0 => $"ELECTRE MODERATOR ROUND: You are the Moderator. Present the decision problem.\n\n" +
                 $"Problem: {state.Topic}\n" +
                 $"Context: {state.Context}\n\n" +
                 $"Options under consideration:\n" +
                 string.Join("\n", state.Options.Select((o, i) => $"  {i + 1}. {o}")) +
                 $"\n\nEvaluation criteria (with weights):\n" +
                 string.Join("\n", state.Criteria.Select(c => $"  • {c.Name} ({c.Weight:P0})")) +
                 "\n\nIntroduce the ELECTRE outranking approach. What thresholds and trade-offs matter?",

            1 => $"ELECTRE DISCUSSION: Analyse the options for '{state.Topic}'.\n\n" +
                 $"Options: {string.Join(", ", state.Options)}\n" +
                 $"Criteria: {string.Join(", ", state.Criteria.Select(c => c.Name))}\n\n" +
                 "For each option, assess its performance on each criterion. " +
                 "Identify pairwise preferences: which options outperform others and on which criteria?",

            2 => $"ELECTRE DELIBERATION: Refine your outranking judgements for '{state.Topic}'.\n\n" +
                 "Review the group discussion. Finalise your views on which options should be eliminated " +
                 "and which should be preferred based on concordance (strength of preference) " +
                 "and discordance (intensity of disadvantage).",

            3 => $"ELECTRE SCORING ROUND: Score each option for '{state.Topic}' on a scale of 0 (worst) to 10 (best).\n\n" +
                 $"Options: {string.Join(", ", state.Options)}\n" +
                 $"Criteria: {string.Join(", ", state.Criteria.Select(c => $"{c.Name}({c.Weight:P0})"))}\n\n" +
                 "Provide your scores in this exact format:\n" +
                 "SCORES:\n" +
                 string.Join("\n", state.Options.Select(o =>
                     $"{o}: {string.Join(" | ", state.Criteria.Select(c => $"{c.Name}=[0-10]"))}")) +
                 "\n\nScores will be used to build concordance and discordance matrices.",

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
            summary = $"ELECTRE {phaseLabel}:\n" + string.Join("\n", contributions);
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

            state.Result = ComputeElectreResult(state.Options, state.Criteria, state.Votes);
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
        var state = new ElectreState
        {
            Topic = issue.Title,
            Context = issue.ContextVector,
            Options = new List<string>(DefaultOptions),
            Criteria = new List<ElectreCriterion>(DefaultCriteria)
        };
        return Task.FromResult(JsonSerializer.Serialize(state, JsonOpts));
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static ElectreState ParseState(string payload)
    {
        try
        {
            var state = JsonSerializer.Deserialize<ElectreState>(payload, JsonOpts);
            if (state != null)
            {
                if (state.Options.Count == 0) state.Options = new List<string>(DefaultOptions);
                if (state.Criteria.Count == 0) state.Criteria = new List<ElectreCriterion>(DefaultCriteria);
                return state;
            }
        }
        catch { }

        return new ElectreState
        {
            Options = new List<string>(DefaultOptions),
            Criteria = new List<ElectreCriterion>(DefaultCriteria)
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

    private static void FillDefaultVotes(ElectreState state, SessionRound round)
    {
        var rng = new Random(Math.Abs(round.Id.GetHashCode()) % 997 + 1);
        var votes = new Dictionary<string, Dictionary<string, int>>();
        foreach (var option in state.Options)
            votes[option] = state.Criteria.ToDictionary(c => c.Name, _ => rng.Next(4, 9));
        state.Votes[round.Id.ToString()[..8]] = votes;
    }

    private static ElectreResult ComputeElectreResult(
        List<string> options,
        List<ElectreCriterion> criteria,
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
        var normWeights = criteria.ToDictionary(c => c.Name, c => c.Weight / totalWeight);

        // Build concordance and discordance matrices
        var concordance = new Dictionary<string, Dictionary<string, double>>();
        var discordance = new Dictionary<string, Dictionary<string, double>>();

        foreach (var a in options)
        {
            concordance[a] = new Dictionary<string, double>();
            discordance[a] = new Dictionary<string, double>();
            foreach (var b in options)
            {
                if (a == b) { concordance[a][b] = 1.0; discordance[a][b] = 0.0; continue; }

                // Concordance: sum of weights where a is at least as good as b
                double cScore = criteria
                    .Where(c => avgScores[a].GetValueOrDefault(c.Name, DefaultScore) >=
                                avgScores[b].GetValueOrDefault(c.Name, DefaultScore))
                    .Sum(c => normWeights[c.Name]);

                // Discordance: max normalised disadvantage of a over b
                double dScore = criteria
                    .Select(c =>
                    {
                        var diff = avgScores[b].GetValueOrDefault(c.Name, DefaultScore) -
                                   avgScores[a].GetValueOrDefault(c.Name, DefaultScore);
                        return diff > 0 ? diff / 10.0 : 0.0;
                    })
                    .DefaultIfEmpty(0.0)
                    .Max();

                concordance[a][b] = cScore;
                discordance[a][b] = dScore;
            }
        }

        // Outranking matrix: a outranks b when C(a,b) >= threshold AND D(a,b) <= threshold
        var outranking = new Dictionary<string, Dictionary<string, bool>>();
        var netScore = new Dictionary<string, int>();
        foreach (var a in options)
        {
            outranking[a] = new Dictionary<string, bool>();
            netScore[a] = 0;
            foreach (var b in options)
            {
                if (a == b) { outranking[a][b] = false; continue; }
                bool aOutranksB = concordance[a][b] >= ConcordanceThreshold &&
                                  discordance[a][b] <= DiscordanceThreshold;
                outranking[a][b] = aOutranksB;
                if (aOutranksB) netScore[a]++;
            }
        }

        // Penalise alternatives that are outranked
        foreach (var a in options)
            foreach (var b in options)
                if (a != b && outranking[b].TryGetValue(a, out var bOutranksA) && bOutranksA)
                    netScore[a]--;

        var ranking = options.OrderByDescending(o => netScore.GetValueOrDefault(o, 0)).ToList();

        return new ElectreResult
        {
            Options = options,
            Criteria = criteria,
            AverageScores = avgScores,
            ConcordanceMatrix = concordance,
            DiscordanceMatrix = discordance,
            NetOutrankingScores = netScore,
            Ranking = ranking
        };
    }

    private static string BuildResultSummary(List<string> options, ElectreResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"ELECTRE Outranking Analysis (c*={ConcordanceThreshold:F1}, d*={DiscordanceThreshold:F1}):");
        sb.AppendLine();

        sb.AppendLine("**Concordance Matrix (C ≥ 0.6 → favours outranking):**");
        sb.Append("| |");
        foreach (var b in result.Options) sb.Append($" {b} |");
        sb.AppendLine();
        sb.Append("|---|");
        foreach (var _ in result.Options) sb.Append("---|");
        sb.AppendLine();
        foreach (var a in result.Options)
        {
            sb.Append($"| {a} |");
            foreach (var b in result.Options)
                sb.Append($" {result.ConcordanceMatrix[a][b]:F2} |");
            sb.AppendLine();
        }

        sb.AppendLine();
        sb.AppendLine("**Net Outranking Scores & Ranking:**");
        for (int i = 0; i < result.Ranking.Count; i++)
        {
            var opt = result.Ranking[i];
            var ns = result.NetOutrankingScores.TryGetValue(opt, out var n) ? n : 0;
            sb.AppendLine($"{i + 1}. {opt} (net score={ns:+#;-#;0})");
        }

        return sb.ToString().TrimEnd();
    }

    // ── State model ───────────────────────────────────────────────────────

    private class ElectreState
    {
        public string Topic { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
        public List<ElectreCriterion> Criteria { get; set; } = new();
        public List<string> Discussions { get; set; } = new();
        public Dictionary<string, Dictionary<string, Dictionary<string, int>>> Votes { get; set; } = new();
        public ElectreResult? Result { get; set; }
        public int RoundsCompleted { get; set; }
    }

    private class ElectreResult
    {
        public List<string> Options { get; set; } = new();
        public List<ElectreCriterion> Criteria { get; set; } = new();
        public Dictionary<string, Dictionary<string, double>> AverageScores { get; set; } = new();
        public Dictionary<string, Dictionary<string, double>> ConcordanceMatrix { get; set; } = new();
        public Dictionary<string, Dictionary<string, double>> DiscordanceMatrix { get; set; } = new();
        public Dictionary<string, int> NetOutrankingScores { get; set; } = new();
        public List<string> Ranking { get; set; } = new();
    }
}

public class ElectreCriterion
{
    public string Name { get; set; } = string.Empty;
    public double Weight { get; set; }
    [JsonConstructor]
    public ElectreCriterion() { }
    public ElectreCriterion(string name, double weight) { Name = name; Weight = weight; }
}

using System.Text;
using Deepr.Application.DTOs;
using Deepr.Application.Interfaces;

namespace Deepr.Infrastructure.Services;

/// <summary>
/// Standalone MCDA service — all five algorithms implemented as pure functions.
/// No AI, no sessions, no database required.
/// </summary>
public class McdaService : IMcdaService
{
    private const double DistinguishingCoefficient = 0.5; // ζ for Grey Theory
    private const double ConcordanceThreshold = 0.6;      // c* for ELECTRE
    private const double DiscordanceThreshold = 0.4;      // d* for ELECTRE

    /// <summary>
    /// Score range used to normalise pairwise differences in ELECTRE and PROMETHEE
    /// (assumes inputs on a 0–10 scale by default).
    /// </summary>
    private const double ScoreRange = 10.0;

    // ─── Public entry points ────────────────────────────────────────────────

    public McdaResult RunAhp(McdaInput input)
    {
        Validate(input);
        var (options, criteria, scores) = Normalise(input);

        // Column-normalise the decision matrix
        var colTotals = criteria.ToDictionary(c => c.Name,
            c => options.Sum(o => scores[o].GetValueOrDefault(c.Name, 0)));

        var normMatrix = options.ToDictionary(
            o => o,
            o => criteria.ToDictionary(
                c => c.Name,
                c => colTotals[c.Name] > 0 ? scores[o].GetValueOrDefault(c.Name, 0) / colTotals[c.Name] : 0));

        // Priority score = weighted average of normalised columns
        var priorityScores = options.ToDictionary(
            o => o,
            o => criteria.Sum(c => c.Weight * normMatrix[o].GetValueOrDefault(c.Name, 0)));

        var ranking = options.OrderByDescending(o => priorityScores[o]).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("**AHP Priority Matrix:**");
        sb.AppendLine();
        sb.Append("| Option |");
        foreach (var c in criteria) sb.Append($" {c.Name} (w={c.Weight:F3}) |");
        sb.AppendLine(" **Priority** |");
        sb.Append("|---|");
        foreach (var _ in criteria) sb.Append("---|");
        sb.AppendLine("---|");
        foreach (var o in ranking)
        {
            sb.Append($"| {o} |");
            foreach (var c in criteria) sb.Append($" {normMatrix[o][c.Name]:F4} |");
            sb.AppendLine($" **{priorityScores[o]:F4}** |");
        }
        AppendRanking(sb, ranking, priorityScores, "AHP Ranking");

        return new McdaResult
        {
            Method = "AHP",
            Ranking = ranking,
            Scores = priorityScores,
            Summary = sb.ToString().TrimEnd(),
            Details = new Dictionary<string, object>
            {
                ["normalisedMatrix"] = normMatrix
            }
        };
    }

    public McdaResult RunElectre(McdaInput input)
    {
        Validate(input);
        var (options, criteria, scores) = Normalise(input);

        var concordance = new Dictionary<string, Dictionary<string, double>>();
        var discordance = new Dictionary<string, Dictionary<string, double>>();

        foreach (var a in options)
        {
            concordance[a] = new Dictionary<string, double>();
            discordance[a] = new Dictionary<string, double>();
            foreach (var b in options)
            {
                if (a == b) { concordance[a][b] = 1.0; discordance[a][b] = 0.0; continue; }

                concordance[a][b] = criteria
                    .Where(c => scores[a].GetValueOrDefault(c.Name, 0) >= scores[b].GetValueOrDefault(c.Name, 0))
                    .Sum(c => c.Weight);

                discordance[a][b] = criteria
                    .Select(c =>
                    {
                        var diff = scores[b].GetValueOrDefault(c.Name, 0) - scores[a].GetValueOrDefault(c.Name, 0);
                        return diff > 0 ? diff / ScoreRange : 0.0;
                    })
                    .DefaultIfEmpty(0.0)
                    .Max();
            }
        }

        var netScores = new Dictionary<string, double>();
        foreach (var a in options)
        {
            double net = 0;
            foreach (var b in options)
            {
                if (a == b) continue;
                bool aOutB = concordance[a][b] >= ConcordanceThreshold && discordance[a][b] <= DiscordanceThreshold;
                bool bOutA = concordance[b][a] >= ConcordanceThreshold && discordance[b][a] <= DiscordanceThreshold;
                if (aOutB) net++;
                if (bOutA) net--;
            }
            netScores[a] = net;
        }

        var ranking = options.OrderByDescending(o => netScores[o]).ToList();

        var sb = new StringBuilder();
        sb.AppendLine($"**ELECTRE Outranking Analysis (c*={ConcordanceThreshold}, d*={DiscordanceThreshold}):**");
        sb.AppendLine();
        sb.AppendLine("Concordance Matrix:");
        sb.Append("| |");
        foreach (var b in options) sb.Append($" {b} |");
        sb.AppendLine();
        sb.Append("|---|");
        foreach (var _ in options) sb.Append("---|");
        sb.AppendLine();
        foreach (var a in options)
        {
            sb.Append($"| {a} |");
            foreach (var b in options) sb.Append($" {concordance[a][b]:F2} |");
            sb.AppendLine();
        }
        AppendRanking(sb, ranking, netScores, "ELECTRE Ranking (by net outranking score)");

        return new McdaResult
        {
            Method = "ELECTRE",
            Ranking = ranking,
            Scores = netScores,
            Summary = sb.ToString().TrimEnd(),
            Details = new Dictionary<string, object>
            {
                ["concordanceMatrix"] = concordance,
                ["discordanceMatrix"] = discordance
            }
        };
    }

    public McdaResult RunTopsis(McdaInput input)
    {
        Validate(input);
        var (options, criteria, scores) = Normalise(input);

        // Vector normalisation
        var euclidNorm = criteria.ToDictionary(
            c => c.Name,
            c => Math.Sqrt(options.Sum(o => Math.Pow(scores[o].GetValueOrDefault(c.Name, 0), 2))));

        var normMatrix = options.ToDictionary(
            o => o,
            o => criteria.ToDictionary(
                c => c.Name,
                c => euclidNorm[c.Name] > 0 ? scores[o].GetValueOrDefault(c.Name, 0) / euclidNorm[c.Name] : 0));

        // Weighted normalised matrix
        var wNorm = options.ToDictionary(
            o => o,
            o => criteria.ToDictionary(
                c => c.Name,
                c => c.Weight * normMatrix[o][c.Name]));

        // PIS and NIS
        var pis = criteria.ToDictionary(
            c => c.Name,
            c => c.IsBenefit
                ? options.Max(o => wNorm[o][c.Name])
                : options.Min(o => wNorm[o][c.Name]));
        var nis = criteria.ToDictionary(
            c => c.Name,
            c => c.IsBenefit
                ? options.Min(o => wNorm[o][c.Name])
                : options.Max(o => wNorm[o][c.Name]));

        var dPlus = options.ToDictionary(
            o => o,
            o => Math.Sqrt(criteria.Sum(c => Math.Pow(wNorm[o][c.Name] - pis[c.Name], 2))));
        var dMinus = options.ToDictionary(
            o => o,
            o => Math.Sqrt(criteria.Sum(c => Math.Pow(wNorm[o][c.Name] - nis[c.Name], 2))));

        var closeness = options.ToDictionary(
            o => o,
            o => dPlus[o] + dMinus[o] > 0 ? dMinus[o] / (dPlus[o] + dMinus[o]) : 0.0);

        var ranking = options.OrderByDescending(o => closeness[o]).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("**TOPSIS Closeness Analysis:**");
        sb.AppendLine();
        sb.Append("| Option |");
        foreach (var c in criteria) sb.Append($" {c.Name} ({(c.IsBenefit ? "B" : "C")}) |");
        sb.AppendLine(" D+ | D- | **C*** |");
        sb.Append("|---|");
        foreach (var _ in criteria) sb.Append("---|");
        sb.AppendLine("---|---|---|");
        foreach (var o in ranking)
        {
            sb.Append($"| {o} |");
            foreach (var c in criteria) sb.Append($" {scores[o].GetValueOrDefault(c.Name, 0):F2} |");
            sb.AppendLine($" {dPlus[o]:F4} | {dMinus[o]:F4} | **{closeness[o]:F4}** |");
        }
        AppendRanking(sb, ranking, closeness, "TOPSIS Ranking (higher C* = closer to ideal)");

        return new McdaResult
        {
            Method = "TOPSIS",
            Ranking = ranking,
            Scores = closeness,
            Summary = sb.ToString().TrimEnd(),
            Details = new Dictionary<string, object>
            {
                ["distanceToPis"] = dPlus,
                ["distanceToNis"] = dMinus,
                ["positiveIdealSolution"] = pis,
                ["negativeIdealSolution"] = nis
            }
        };
    }

    public McdaResult RunPromethee(McdaInput input)
    {
        Validate(input);
        var (options, criteria, scores) = Normalise(input);

        var phiPlus = new Dictionary<string, double>();
        var phiMinus = new Dictionary<string, double>();
        int n = options.Count;

        foreach (var a in options)
        {
            double plus = 0, minus = 0;
            foreach (var b in options)
            {
                if (a == b) continue;

                // Linear preference function: P(d) = max(d,0)/ScoreRange
                double piAB = criteria.Sum(c =>
                {
                    var diff = scores[a].GetValueOrDefault(c.Name, 0) - scores[b].GetValueOrDefault(c.Name, 0);
                    return c.Weight * Math.Max(diff, 0) / ScoreRange;
                });
                double piBA = criteria.Sum(c =>
                {
                    var diff = scores[b].GetValueOrDefault(c.Name, 0) - scores[a].GetValueOrDefault(c.Name, 0);
                    return c.Weight * Math.Max(diff, 0) / ScoreRange;
                });

                plus += piAB;
                minus += piBA;
            }
            int pairwiseComparisons = n > 1 ? n - 1 : 1;
            phiPlus[a] = plus / pairwiseComparisons;
            phiMinus[a] = minus / pairwiseComparisons;
        }

        var netFlow = options.ToDictionary(o => o, o => phiPlus[o] - phiMinus[o]);
        var ranking = options.OrderByDescending(o => netFlow[o]).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("**PROMETHEE II Net Flow Analysis:**");
        sb.AppendLine();
        sb.AppendLine("| Option | φ+ (Leaving) | φ− (Entering) | **φ (Net)** |");
        sb.AppendLine("|---|---|---|---|");
        foreach (var o in ranking)
            sb.AppendLine($"| {o} | {phiPlus[o]:F4} | {phiMinus[o]:F4} | **{netFlow[o]:+0.0000;-0.0000;0.0000}** |");
        AppendRanking(sb, ranking, netFlow, "PROMETHEE II Ranking (higher φ = more preferred)");

        return new McdaResult
        {
            Method = "PROMETHEE",
            Ranking = ranking,
            Scores = netFlow,
            Summary = sb.ToString().TrimEnd(),
            Details = new Dictionary<string, object>
            {
                ["positiveFlow"] = phiPlus,
                ["negativeFlow"] = phiMinus
            }
        };
    }

    public McdaResult RunGreyTheory(McdaInput input)
    {
        Validate(input);
        var (options, criteria, scores) = Normalise(input);

        // Normalise: linear higher-is-better
        var normMatrix = options.ToDictionary(o => o, _ => new Dictionary<string, double>());
        foreach (var c in criteria)
        {
            var vals = options.Select(o => scores[o].GetValueOrDefault(c.Name, 0)).ToList();
            var min = vals.Min();
            var range = vals.Max() - min;
            foreach (var o in options)
                normMatrix[o][c.Name] = range > 0
                    ? (scores[o].GetValueOrDefault(c.Name, 0) - min) / range
                    : 1.0;
        }

        // Deltas vs ideal reference (1.0)
        double deltaMin = double.MaxValue, deltaMax = double.MinValue;
        var deltas = options.ToDictionary(o => o, _ => new Dictionary<string, double>());
        foreach (var o in options)
            foreach (var c in criteria)
            {
                var d = Math.Abs(1.0 - normMatrix[o][c.Name]);
                deltas[o][c.Name] = d;
                if (d < deltaMin) deltaMin = d;
                if (d > deltaMax) deltaMax = d;
            }

        // Grey relational coefficients
        var grc = options.ToDictionary(o => o, _ => new Dictionary<string, double>());
        foreach (var o in options)
            foreach (var c in criteria)
            {
                var denom = deltas[o][c.Name] + DistinguishingCoefficient * deltaMax;
                grc[o][c.Name] = denom > 0
                    ? (deltaMin + DistinguishingCoefficient * deltaMax) / denom
                    : 1.0;
            }

        // Grey relational grades
        var grades = options.ToDictionary(
            o => o,
            o => criteria.Sum(c => c.Weight * grc[o].GetValueOrDefault(c.Name, 0)));

        var ranking = options.OrderByDescending(o => grades[o]).ToList();

        var sb = new StringBuilder();
        sb.AppendLine($"**Grey Relational Analysis (ζ={DistinguishingCoefficient}):**");
        sb.AppendLine();
        sb.Append("| Option |");
        foreach (var c in criteria) sb.Append($" GRC({c.Name}) |");
        sb.AppendLine(" **GRG** |");
        sb.Append("|---|");
        foreach (var _ in criteria) sb.Append("---|");
        sb.AppendLine("---|");
        foreach (var o in ranking)
        {
            sb.Append($"| {o} |");
            foreach (var c in criteria) sb.Append($" {grc[o][c.Name]:F4} |");
            sb.AppendLine($" **{grades[o]:F4}** |");
        }
        AppendRanking(sb, ranking, grades, "Grey Theory Ranking (higher GRG = closer to ideal)");

        return new McdaResult
        {
            Method = "GreyTheory",
            Ranking = ranking,
            Scores = grades,
            Summary = sb.ToString().TrimEnd(),
            Details = new Dictionary<string, object>
            {
                ["greyRelationalCoefficients"] = grc,
                ["normalisedMatrix"] = normMatrix
            }
        };
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static void Validate(McdaInput input)
    {
        if (input.Options == null || input.Options.Count < 2)
            throw new ArgumentException("At least 2 options are required.", nameof(input));
        if (input.Criteria == null || input.Criteria.Count == 0)
            throw new ArgumentException("At least 1 criterion is required.", nameof(input));
        if (input.Scores == null || input.Scores.Count == 0)
            throw new ArgumentException("Scores must be provided.", nameof(input));
    }

    /// <summary>
    /// Returns options, weight-normalised criteria (sum-to-1), and filled score matrix
    /// (missing entries default to 5.0).
    /// </summary>
    private static (List<string> options, List<McdaCriterion> criteria, Dictionary<string, Dictionary<string, double>> scores) Normalise(McdaInput input)
    {
        var options = input.Options.Distinct().ToList();

        var totalWeight = input.Criteria.Sum(c => c.Weight);
        if (totalWeight <= 0) totalWeight = 1;

        var criteria = input.Criteria
            .Select(c => new McdaCriterion(c.Name, c.Weight / totalWeight, c.IsBenefit))
            .ToList();

        const double defaultScore = 5.0;
        var scores = options.ToDictionary(
            o => o,
            o =>
            {
                var row = input.Scores.TryGetValue(o, out var r) ? r : new Dictionary<string, double>();
                return criteria.ToDictionary(c => c.Name, c => row.TryGetValue(c.Name, out var v) ? v : defaultScore);
            });

        return (options, criteria, scores);
    }

    private static void AppendRanking(StringBuilder sb, List<string> ranking, Dictionary<string, double> scoreMap, string label)
    {
        sb.AppendLine();
        sb.AppendLine($"**{label}:**");
        for (int i = 0; i < ranking.Count; i++)
        {
            var opt = ranking[i];
            var s = scoreMap.TryGetValue(opt, out var v) ? v : 0;
            sb.AppendLine($"{i + 1}. {opt} ({s:+0.0000;-0.0000;0.0000})");
        }
    }
}

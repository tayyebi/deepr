using System.Text;
using System.Text.Json;
using Deepr.Application.DTOs;
using Deepr.Application.Interfaces;
using Deepr.Domain.Enums;
using Deepr.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Deepr.Infrastructure.Services;

public class SessionExportService : ISessionExportService
{
    private readonly ApplicationDbContext _db;

    /// <summary>Neutral mid-point score used when no explicit score is available for an option/criterion pair.</summary>
    private const double DefaultScore = 5.0;

    public SessionExportService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<DecisionSheetExport?> ExportDecisionSheetAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _db.Sessions
            .Include(s => s.Rounds)
            .ThenInclude(r => r.Contributions)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session == null) return null;

        var council = await _db.Councils
            .FirstOrDefaultAsync(c => c.Id == session.CouncilId, cancellationToken);
        if (council == null) return null;

        var issue = await _db.Issues
            .FirstOrDefaultAsync(i => i.Id == council.IssueId, cancellationToken);

        var agentNames = council.Agents.ToDictionary(
            a => a.AgentId,
            a => $"{a.Name} ({a.Role}{(a.IsAi ? " · AI" : " · Human")})");

        var sb = new StringBuilder();

        // Header
        sb.AppendLine($"# Decision Sheet: {issue?.Title ?? "Unknown Issue"}");
        sb.AppendLine();
        sb.AppendLine($"| Field | Value |");
        sb.AppendLine($"|---|---|");
        sb.AppendLine($"| **Issue** | {issue?.Title ?? "—"} |");
        sb.AppendLine($"| **Context** | {issue?.ContextVector ?? "—"} |");
        sb.AppendLine($"| **Decision Method** | {MethodLabel(council.SelectedMethod)} |");
        sb.AppendLine($"| **Analytical Tool** | {ToolLabel(council.SelectedTool)} |");
        sb.AppendLine($"| **Status** | {session.Status} |");
        sb.AppendLine($"| **Rounds Completed** | {session.CurrentRoundNumber} |");
        sb.AppendLine($"| **Session ID** | {session.Id} |");
        sb.AppendLine($"| **Exported** | {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC |");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        // Rounds
        foreach (var round in session.Rounds.OrderBy(r => r.RoundNumber))
        {
            var phaseLabel = GetPhaseLabel(council.SelectedMethod, round.RoundNumber);
            sb.AppendLine($"## Round {round.RoundNumber} — {phaseLabel}");
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(round.Instructions))
            {
                sb.AppendLine($"**Prompt:** {TruncateLine(round.Instructions, 200)}");
                sb.AppendLine();
            }

            foreach (var contribution in round.Contributions)
            {
                var agentLabel = agentNames.TryGetValue(contribution.AgentId, out var name) ? name : contribution.AgentId.ToString()[..8] + "…";
                sb.AppendLine($"### {agentLabel}");
                sb.AppendLine();
                AppendClassifiedData(sb, contribution.StructuredData, council.SelectedTool);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(round.Summary))
            {
                sb.AppendLine("**Round Summary:**");
                sb.AppendLine();
                sb.AppendLine(round.Summary);
                sb.AppendLine();
            }

            sb.AppendLine("---");
            sb.AppendLine();
        }

        // Matrix results table (WeightedDeliberation)
        if (council.SelectedMethod == MethodType.WeightedDeliberation)
            AppendMatrixTable(sb, session.StatePayload);

        // Final state summary if available
        if (!string.IsNullOrWhiteSpace(session.StatePayload) && session.StatePayload != "{}")
        {
            sb.AppendLine("## Final State");
            sb.AppendLine();
            sb.AppendLine("```json");
            try
            {
                var pretty = JsonSerializer.Serialize(
                    JsonSerializer.Deserialize<JsonElement>(session.StatePayload),
                    new JsonSerializerOptions { WriteIndented = true });
                sb.AppendLine(pretty);
            }
            catch
            {
                sb.AppendLine(session.StatePayload);
            }
            sb.AppendLine("```");
        }

        var content = Encoding.UTF8.GetBytes(sb.ToString());
        var safeTitle = new string((issue?.Title ?? "session").Select(c => char.IsLetterOrDigit(c) || c == '-' ? c : '-').ToArray());
        if (safeTitle.Length == 0) safeTitle = "session";
        var fileName = $"decision-sheet-{safeTitle[..Math.Min(safeTitle.Length, 40)]}-{session.Id.ToString()[..8]}.md";

        return new DecisionSheetExport
        {
            FileName = fileName,
            ContentType = "text/markdown",
            Content = content
        };
    }

    private static void AppendClassifiedData(StringBuilder sb, string structuredDataJson, ToolType tool)
    {
        try
        {
            var data = JsonSerializer.Deserialize<JsonElement>(structuredDataJson);

            var keys = tool switch
            {
                ToolType.SWOT => new[] { "strengths", "weaknesses", "opportunities", "threats" },
                ToolType.PESTLE => new[] { "political", "economic", "social", "technological", "legal", "environmental" },
                _ => Array.Empty<string>()
            };

            if (keys.Length > 0)
            {
                foreach (var key in keys)
                {
                    if (data.TryGetProperty(key, out var section))
                    {
                        var items = section.ValueKind == JsonValueKind.Array
                            ? section.EnumerateArray().Select(e => e.GetString()).Where(s => !string.IsNullOrWhiteSpace(s)).ToList()
                            : new List<string?>();

                        if (items.Any())
                        {
                            sb.AppendLine($"**{Capitalize(key)}:**");
                            foreach (var item in items)
                                sb.AppendLine($"- {item}");
                            sb.AppendLine();
                        }
                    }
                }
                return;
            }

            // WeightedScoring or other: show raw field
            if (data.TryGetProperty("raw", out var raw))
            {
                sb.AppendLine(raw.GetString());
                sb.AppendLine();
                return;
            }
        }
        catch { }

        // Fallback
        sb.AppendLine("*(no structured data)*");
        sb.AppendLine();
    }

    private static string GetPhaseLabel(MethodType method, int roundNumber) => method switch
    {
        MethodType.ADKAR => roundNumber switch
        {
            1 => "Awareness",
            2 => "Desire",
            3 => "Knowledge",
            4 => "Ability",
            5 => "Reinforcement",
            _ => $"Phase {roundNumber}"
        },
        MethodType.NGT or MethodType.NominalGroupTechnique => roundNumber switch
        {
            1 => "Silent Generation",
            2 => "Round-Robin Sharing",
            3 => "Clarification",
            4 => "Voting & Ranking",
            _ => $"Phase {roundNumber}"
        },
        MethodType.WeightedDeliberation => roundNumber switch
        {
            1 => "Moderator Framing",
            2 => "Expert Discussion",
            3 => "Expert Deliberation",
            4 => "Voting & Scoring",
            _ => $"Phase {roundNumber}"
        },
        MethodType.Delphi => roundNumber == 1 ? "Initial Assessment" : $"Refinement {roundNumber}",
        MethodType.ConsensusBuilding => roundNumber == 1 ? "Perspective Sharing" : "Agreement Tracking",
        MethodType.Brainstorming => "Idea Generation",
        _ => $"Round {roundNumber}"
    };

    private static string MethodLabel(MethodType m) => m switch
    {
        MethodType.Delphi => "Delphi (Anonymous Expert Consensus)",
        MethodType.NGT or MethodType.NominalGroupTechnique => "NGT (Nominal Group Technique)",
        MethodType.Brainstorming => "Brainstorming",
        MethodType.ConsensusBuilding => "Consensus Building",
        MethodType.ADKAR => "ADKAR (Change Management)",
        MethodType.WeightedDeliberation => "Weighted Deliberation (Discussion + Voting + Matrix)",
        _ => m.ToString()
    };

    private static string ToolLabel(ToolType t) => t switch
    {
        ToolType.SWOT => "SWOT Analysis",
        ToolType.WeightedScoring => "Weighted Scoring",
        ToolType.PESTLE => "PESTLE Analysis",
        ToolType.AHP => "AHP (Analytic Hierarchy Process)",
        ToolType.DecisionMatrix => "Decision Matrix",
        ToolType.CostBenefitAnalysis => "Cost-Benefit Analysis",
        _ => t.ToString()
    };

    private static string Capitalize(string s) =>
        s.Length < 2 ? s.ToUpperInvariant() : char.ToUpperInvariant(s[0]) + s[1..];

    private static string TruncateLine(string s, int maxLen) =>
        s.Length <= maxLen ? s : s[..maxLen] + "…";

    /// <summary>Renders the weighted scoring matrix from state payload as Markdown tables.</summary>
    private static void AppendMatrixTable(StringBuilder sb, string statePayload)
    {
        try
        {
            using var doc = JsonDocument.Parse(statePayload);
            var root = doc.RootElement;

            if (!root.TryGetProperty("matrix", out var matrix) || matrix.ValueKind != JsonValueKind.Object) return;
            if (!matrix.TryGetProperty("ranking", out var rankElem)) return;
            if (!matrix.TryGetProperty("criteria", out var criteriaElem)) return;
            if (!matrix.TryGetProperty("averageScores", out var avgElem)) return;
            if (!matrix.TryGetProperty("weightedScores", out var wsElem)) return;

            var ranking = rankElem.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => s.Length > 0).ToList();
            var criteria = criteriaElem.EnumerateArray()
                .Select(c => (Name: c.GetProperty("name").GetString() ?? "", Weight: c.GetProperty("weight").GetDouble()))
                .Where(c => c.Name.Length > 0)
                .ToList();

            if (ranking.Count == 0 || criteria.Count == 0) return;

            sb.AppendLine("## Decision Matrix Results");
            sb.AppendLine();
            sb.AppendLine("### Weighted Scoring Matrix");
            sb.AppendLine();

            // Header
            sb.Append("| Option |");
            foreach (var (name, weight) in criteria) sb.Append($" {name} ({weight:P0}) |");
            sb.AppendLine(" **Weighted Score** |");

            sb.Append("|---|");
            foreach (var _ in criteria) sb.Append("---|");
            sb.AppendLine("---|");

            // Rows
            foreach (var option in ranking)
            {
                sb.Append($"| {option} |");
                foreach (var (name, _) in criteria)
                {
                    double score = DefaultScore;
                    if (avgElem.TryGetProperty(option, out var optScores) &&
                        optScores.TryGetProperty(name, out var scoreElem))
                        score = scoreElem.GetDouble();
                    sb.Append($" {score:F1} |");
                }
                double ws = wsElem.TryGetProperty(option, out var wsVal) ? wsVal.GetDouble() : 0;
                sb.AppendLine($" **{ws:F2}** |");
            }

            sb.AppendLine();
            sb.AppendLine("### Final Ranking");
            sb.AppendLine();
            for (int i = 0; i < ranking.Count; i++)
            {
                var opt = ranking[i];
                double ws = wsElem.TryGetProperty(opt, out var wsVal) ? wsVal.GetDouble() : 0;
                var recommended = i == 0 ? " ✅ **Recommended**" : "";
                sb.AppendLine($"{i + 1}. **{opt}** — {ws:F2}{recommended}");
            }
            sb.AppendLine();
        }
        catch { }
    }
}

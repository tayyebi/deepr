using System.Text.Json;
using System.Text.RegularExpressions;
using Deepr.Application.DTOs;
using Deepr.Application.Interfaces;
using Deepr.Domain.Enums;

namespace Deepr.Infrastructure.ToolAdapters;

public class SwotToolAdapter : IToolAdapter
{
    public ToolType Type => ToolType.SWOT;

    public Task<ToolSchema> GetSchemaAsync(CancellationToken cancellationToken = default)
    {
        var schema = new
        {
            type = "object",
            properties = new
            {
                strengths = new { type = "array", items = new { type = "string" } },
                weaknesses = new { type = "array", items = new { type = "string" } },
                opportunities = new { type = "array", items = new { type = "string" } },
                threats = new { type = "array", items = new { type = "string" } }
            },
            required = new[] { "strengths", "weaknesses", "opportunities", "threats" }
        };

        return Task.FromResult(new ToolSchema
        {
            SchemaJson = JsonSerializer.Serialize(schema),
            Description = "SWOT Analysis: Strengths, Weaknesses, Opportunities, Threats"
        });
    }

    public Task<ParsedToolData> ParseResponseAsync(string rawContent, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, List<string>>
        {
            ["strengths"] = ExtractSection(rawContent, "strength"),
            ["weaknesses"] = ExtractSection(rawContent, "weakness|weaknesses"),
            ["opportunities"] = ExtractSection(rawContent, "opportunit"),
            ["threats"] = ExtractSection(rawContent, "threat")
        };

        return Task.FromResult(new ParsedToolData
        {
            StructuredDataJson = JsonSerializer.Serialize(result),
            IsValid = true
        });
    }

    public Task<bool> ValidateDataAsync(string structuredDataJson, CancellationToken cancellationToken = default)
    {
        try
        {
            var data = JsonSerializer.Deserialize<JsonElement>(structuredDataJson);
            return Task.FromResult(
                data.TryGetProperty("strengths", out _) &&
                data.TryGetProperty("weaknesses", out _) &&
                data.TryGetProperty("opportunities", out _) &&
                data.TryGetProperty("threats", out _));
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task<string> GeneratePromptTemplateAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            "Please structure your response as a SWOT analysis:\n\n" +
            "**Strengths:**\n- [list strengths]\n\n" +
            "**Weaknesses:**\n- [list weaknesses]\n\n" +
            "**Opportunities:**\n- [list opportunities]\n\n" +
            "**Threats:**\n- [list threats]");
    }

    private static List<string> ExtractSection(string content, string sectionPattern)
    {
        var items = new List<string>();
        var lines = content.Split('\n');
        var inSection = false;

        foreach (var line in lines)
        {
            if (Regex.IsMatch(line, sectionPattern, RegexOptions.IgnoreCase))
            {
                inSection = true;
                continue;
            }

            if (inSection)
            {
                var trimmed = line.TrimStart('-', '*', ' ', '\t');
                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    if (Regex.IsMatch(line, @"^\s*[A-Z][a-z]+:", RegexOptions.None))
                    {
                        inSection = false;
                        break;
                    }
                    items.Add(trimmed);
                }
            }
        }

        return items;
    }
}

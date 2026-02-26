using System.Text.Json;
using System.Text.RegularExpressions;
using Deepr.Application.DTOs;
using Deepr.Application.Interfaces;
using Deepr.Domain.Enums;

namespace Deepr.Infrastructure.ToolAdapters;

public class PestleToolAdapter : IToolAdapter
{
    public ToolType Type => ToolType.PESTLE;

    public Task<ToolSchema> GetSchemaAsync(CancellationToken cancellationToken = default)
    {
        var schema = new
        {
            type = "object",
            properties = new
            {
                political = new { type = "array", items = new { type = "string" } },
                economic = new { type = "array", items = new { type = "string" } },
                social = new { type = "array", items = new { type = "string" } },
                technological = new { type = "array", items = new { type = "string" } },
                legal = new { type = "array", items = new { type = "string" } },
                environmental = new { type = "array", items = new { type = "string" } }
            },
            required = new[] { "political", "economic", "social", "technological", "legal", "environmental" }
        };

        return Task.FromResult(new ToolSchema
        {
            SchemaJson = JsonSerializer.Serialize(schema),
            Description = "PESTLE Analysis: Political, Economic, Social, Technological, Legal, Environmental"
        });
    }

    public Task<ParsedToolData> ParseResponseAsync(string rawContent, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, List<string>>
        {
            ["political"] = ExtractSection(rawContent, "political"),
            ["economic"] = ExtractSection(rawContent, "economic"),
            ["social"] = ExtractSection(rawContent, "social"),
            ["technological"] = ExtractSection(rawContent, "technolog"),
            ["legal"] = ExtractSection(rawContent, "legal"),
            ["environmental"] = ExtractSection(rawContent, "environment")
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
                data.TryGetProperty("political", out _) &&
                data.TryGetProperty("economic", out _) &&
                data.TryGetProperty("social", out _) &&
                data.TryGetProperty("technological", out _) &&
                data.TryGetProperty("legal", out _) &&
                data.TryGetProperty("environmental", out _));
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task<string> GeneratePromptTemplateAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            "Please structure your response as a PESTLE analysis:\n\n" +
            "**Political:**\n- [list political factors]\n\n" +
            "**Economic:**\n- [list economic factors]\n\n" +
            "**Social:**\n- [list social factors]\n\n" +
            "**Technological:**\n- [list technological factors]\n\n" +
            "**Legal:**\n- [list legal factors]\n\n" +
            "**Environmental:**\n- [list environmental factors]");
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

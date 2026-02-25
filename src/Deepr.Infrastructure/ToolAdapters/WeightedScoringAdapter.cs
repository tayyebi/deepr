using System.Text.Json;
using Deepr.Application.DTOs;
using Deepr.Application.Interfaces;
using Deepr.Domain.Enums;

namespace Deepr.Infrastructure.ToolAdapters;

public class WeightedScoringAdapter : IToolAdapter
{
    public ToolType Type => ToolType.WeightedScoring;

    public Task<ToolSchema> GetSchemaAsync(CancellationToken cancellationToken = default)
    {
        var schema = new
        {
            type = "object",
            properties = new
            {
                options = new { type = "array", items = new { type = "string" } },
                criteria = new { type = "array", items = new { type = "string" } },
                scores = new { type = "object", description = "option -> criteria -> score (0-10)" }
            }
        };
        return Task.FromResult(new ToolSchema { SchemaJson = JsonSerializer.Serialize(schema), Description = "Weighted scoring matrix" });
    }

    public Task<ParsedToolData> ParseResponseAsync(string rawContent, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ParsedToolData { StructuredDataJson = JsonSerializer.Serialize(new { raw = rawContent }), IsValid = true });
    }

    public Task<bool> ValidateDataAsync(string structuredDataJson, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);

    public Task<string> GeneratePromptTemplateAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            "Please score each option against the criteria on a scale of 0-10:\n\n" +
            "Option: [name]\n" +
            "- Criterion 1: [score] - [reason]\n" +
            "- Criterion 2: [score] - [reason]");
    }
}
